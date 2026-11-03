using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health Settings (พลังชีวิต)")]
        [Tooltip("พลังชีวิตสูงสุดของ Player ปรับได้ใน Inspector")]
        [SerializeField] private int maxHealth = 100;

        [Tooltip("ระยะเวลาอมตะหลังโดนโจมตี (วินาที)")]
        [SerializeField] private float invulnerabilityDuration = 0.6f;

        [Header("Respawn Settings (การเกิดใหม่เมื่อตาย)")]
        [Tooltip("จุดเกิดใหม่ (Spawn Point) หากไม่ได้กำหนดจะใช้ตำแหน่งเริ่มต้นตอนเริ่มเกม หรือหาวัตถุชื่อ SpawnPoint")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("ตำแหน่งเกิดเริ่มต้น (บันทึกอัตโนมัติจากตำแหน่งเริ่มต้นของผู้เล่นในฉาก)")]
        [SerializeField] private Vector3 spawnPosition;

        [Tooltip("หน่วงเวลาก่อนเกิดใหม่หลังตาย (วินาที)")]
        [SerializeField] private float respawnDelay = 0.8f;

        [Tooltip("ระยะเวลาอมตะหลังเกิดใหม่ (วินาที) เพื่อความปลอดภัยไม่ให้โดนตีทันที")]
        [SerializeField] private float respawnInvulnerabilityDuration = 1.5f;

        [Header("Damage Visual Feedback (การกระพริบตอนโดนโจมตี)")]
        [Tooltip("เปิดให้ตัวละครกระพริบเมื่อโดนโจมตี")]
        [SerializeField] private bool enableBlinkEffect = true;

        [Tooltip("สีตอนกระพริบเมื่อโดนโจมตี (สีแดงโปร่งแสง)")]
        [SerializeField] private Color hurtFlashColor = new Color(1f, 0.35f, 0.35f, 0.35f);

        [Tooltip("ความถี่ในการกระพริบสลับ (วินาทีต่อครั้ง)")]
        [SerializeField] private float blinkInterval = 0.08f;

        [Header("Knockback Settings (แรงผลักเมื่อโดนโจมตี)")]
        [Tooltip("เปิดแรงกระเด็น (Knockback) เมื่อโดนดาเมจ")]
        [SerializeField] private bool enableKnockback = true;

        [Tooltip("แรงกระเด็นพื้นฐานเมื่อโดนดาเมจ")]
        [SerializeField] private float defaultKnockbackForce = 5f;

        [Tooltip("ระยะเวลาที่กระเด็น (วินาที)")]
        [SerializeField] private float knockbackDuration = 0.2f;

        [Header("Events")]
        public UnityEvent<int, int> onHealthChanged; // (currentHealth, maxHealth)
        public UnityEvent onDamaged;
        public UnityEvent onDeath;
        public UnityEvent onRespawn;

        private int currentHealth;
        private float lastDamageTime;
        private bool isDead;
        private bool isInvulnerable;

        private Vector3 initialSpawnPosition;
        private SpriteRenderer spriteRenderer;
        private Color defaultColor = Color.white;
        private PlayerMovement playerMovement;
        private PlayerCarry playerCarry;
        private Collider2D playerCollider;
        private Rigidbody2D rb;
        private Coroutine blinkCoroutine;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;
        public Transform SpawnPoint => spawnPoint;
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : spawnPosition;

        private void Awake()
        {
            currentHealth = maxHealth;
            playerMovement = GetComponent<PlayerMovement>();
            playerCarry = GetComponent<PlayerCarry>();
            playerCollider = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }

            initialSpawnPosition = transform.position;
            spawnPosition = transform.position;
        }

        private void Start()
        {
            InitSpawnPoint();
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void InitSpawnPoint()
        {
            if (spawnPoint == null)
            {
                GameObject foundSpawn = GameObject.FindWithTag("Respawn");
                if (foundSpawn == null) foundSpawn = GameObject.Find("PlayerSpawn");
                if (foundSpawn == null) foundSpawn = GameObject.Find("SpawnPoint");
                if (foundSpawn == null) foundSpawn = GameObject.Find("RespawnPoint");

                if (foundSpawn != null)
                {
                    spawnPoint = foundSpawn.transform;
                }
            }

            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
            }
        }

        public void SetSpawnPoint(Transform newSpawnPoint)
        {
            spawnPoint = newSpawnPoint;
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.position;
            }
        }

        public void SetSpawnPosition(Vector3 newPosition)
        {
            spawnPosition = newPosition;
            spawnPoint = null;
        }

        public Vector3 GetSpawnPosition()
        {
            if (spawnPoint != null) return spawnPoint.position;
            return spawnPosition;
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, Vector2.zero, defaultKnockbackForce);
        }

        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce = -1f)
        {
            if (isDead) return;
            if (isInvulnerable || Time.time < lastDamageTime + invulnerabilityDuration) return;

            lastDamageTime = Time.time;
            currentHealth -= damage;
            if (currentHealth < 0) currentHealth = 0;

            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onDamaged?.Invoke();

            // แรงผลักกระเด็น (Knockback)
            if (enableKnockback && playerMovement != null)
            {
                float force = knockbackForce > 0f ? knockbackForce : defaultKnockbackForce;
                playerMovement.ApplyKnockback(knockbackDirection, force, knockbackDuration);
            }

            // แอนิเมชันกระพริบตัวละคร
            if (enableBlinkEffect && gameObject.activeInHierarchy)
            {
                if (blinkCoroutine != null)
                {
                    StopCoroutine(blinkCoroutine);
                }
                blinkCoroutine = StartCoroutine(DamageBlinkRoutine());
            }

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private IEnumerator DamageBlinkRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            bool isFlash = false;

            while (elapsed < invulnerabilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = isFlash ? defaultColor : hurtFlashColor;
                }

                isFlash = !isFlash;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }

            isInvulnerable = false;
            blinkCoroutine = null;
        }

        public void Heal(int amount)
        {
            if (isDead) return;

            currentHealth += amount;
            if (currentHealth > maxHealth) currentHealth = maxHealth;

            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }

            // 1. ทำลายเฉพาะของที่แบกอยู่บนหลังผู้เล่นเท่านั้น (ไม่กระทบเงิน อาวุธ หรือของที่วางในฉาก)
            if (playerCarry != null)
            {
                playerCarry.ClearCarriedItems();
            }

            // 2. หยุดการเคลื่อนไหว
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            if (playerMovement != null)
            {
                playerMovement.enabled = false;
            }

            // 3. ปิด Collider ชั่วคราวเพื่อไม่ให้ศัตรูชนหรือโจมตีซ้ำ
            if (playerCollider != null)
            {
                playerCollider.enabled = false;
            }

            Debug.Log("[PlayerHealth] Player Died! Carried items cleared. Respawning at spawn point...");
            onDeath?.Invoke();

            // 4. เริ่มกระบวนการเกิดใหม่ (Respawn)
            StartCoroutine(RespawnRoutine());
        }

        private IEnumerator RespawnRoutine()
        {
            // ทำตัวละครกึ่งโปร่งแสงขณะรอเกิดใหม่
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.25f);
            }

            if (respawnDelay > 0f)
            {
                yield return new WaitForSeconds(respawnDelay);
            }

            // ย้ายไปยังตำแหน่งเกิด (Spawn Point)
            Vector3 targetSpawnPos = GetSpawnPosition();
            transform.position = targetSpawnPos;
            if (rb != null)
            {
                rb.position = targetSpawnPos;
                rb.linearVelocity = Vector2.zero;
            }

            // เปิดการชนและการเคลื่อนไหวกลับคืนมา
            if (playerCollider != null)
            {
                playerCollider.enabled = true;
            }
            if (playerMovement != null)
            {
                playerMovement.enabled = true;
            }

            // คืนค่าพลังชีวิตเต็ม
            ResetHealth();

            // ให้สถานะอมตะชั่วคราวหลังเกิดใหม่ พร้อมกระพริบเพื่อความปลอดภัย
            if (respawnInvulnerabilityDuration > 0f && gameObject.activeInHierarchy)
            {
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(RespawnInvulnerabilityRoutine(respawnInvulnerabilityDuration));
            }
            else if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }

            Debug.Log($"[PlayerHealth] Player Respawned successfully at {targetSpawnPos}!");
            onRespawn?.Invoke();
        }

        private IEnumerator RespawnInvulnerabilityRoutine(float duration)
        {
            isInvulnerable = true;
            lastDamageTime = Time.time + duration - invulnerabilityDuration;
            float elapsed = 0f;
            bool isFlash = false;

            while (elapsed < duration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = isFlash ? defaultColor : new Color(defaultColor.r, defaultColor.g, defaultColor.b, 0.4f);
                }

                isFlash = !isFlash;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }

            isInvulnerable = false;
            blinkCoroutine = null;
        }

        /// <summary>
        /// รีเซ็ตเลือดกลับมาเต็ม (เช่น เวลา Respawn)
        /// </summary>
        public void ResetHealth()
        {
            isDead = false;
            currentHealth = maxHealth;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 spawn = spawnPoint != null ? spawnPoint.position : (Application.isPlaying ? initialSpawnPosition : transform.position);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawn, 0.5f);
            Gizmos.DrawLine(transform.position, spawn);
        }
    }
}
