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

        private int currentHealth;
        private float lastDamageTime;
        private bool isDead;
        private bool isInvulnerable;

        private SpriteRenderer spriteRenderer;
        private Color defaultColor = Color.white;
        private PlayerMovement playerMovement;
        private Coroutine blinkCoroutine;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        private void Awake()
        {
            currentHealth = maxHealth;
            playerMovement = GetComponent<PlayerMovement>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }
        }

        private void Start()
        {
            onHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        public void TakeDamage(int damage)
        {
            TakeDamage(damage, Vector2.zero, defaultKnockbackForce);
        }

        public void TakeDamage(int damage, Vector2 knockbackDirection, float knockbackForce = -1f)
        {
            if (isDead) return;
            if (Time.time < lastDamageTime + invulnerabilityDuration) return;

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
                    // สลับระหว่างสีแดงโปร่งแสง และสีปกติ
                    spriteRenderer.color = isFlash ? defaultColor : hurtFlashColor;
                }

                isFlash = !isFlash;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            // คืนค่าสีเดิมกลับมา
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
            isDead = true;
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }

            Debug.Log("[PlayerHealth] Player Died!");
            onDeath?.Invoke();
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
    }
}
