using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("HP เลือด (ตั้งที่ Prefab ของสัตว์แต่ละตัว: Chicken / Pig)")]
        [Tooltip("เลือดสูงสุดของสัตว์ตัวนี้ อ่านตอนเกิดเท่านั้น ไม่มีสคริปต์อื่น (EnemySpawner / HuntArea) แก้ทับ")]
        [Min(1)]
        [SerializeField] private int maxHealth = 30;

        [Header("Loot Drop (ไอเทมที่จะดรอปเมื่อตาย)")]
        [Tooltip("Prefab ของเนื้อที่จะดรอป (เช่น Chicken Meat หรือ Pig Meat)")]
        [SerializeField] private GameObject meatDropPrefab;

        [Tooltip("จำนวนชิ้นที่จะดรอป")]
        [SerializeField] private int dropCount = 1;

        [Header("Visual Feedback")]
        [Tooltip("กระพริบสีแดงเมื่อโดนฟัน")]
        [SerializeField] private Color hurtColor = Color.red;

        [Header("Knockback (แรงกระเด็นเมื่อโดนโจมตี)")]
        [Tooltip("ระยะเวลาที่กระเด็น (วินาที) ระหว่างนี้ระบบเดิน/ไล่ล่าจะหยุดควบคุมความเร็ว")]
        [SerializeField] private float knockbackDuration = 0.2f;

        [Header("Juice (เอฟเฟกต์ตอนตี/ตาย: hit stop, squash, death pop, เนื้อบินโค้ง, กล้องสั่น)")]
        [Tooltip("ตัวเลขทั้งหมดของเอฟเฟกต์อยู่ใน JuiceConfig (Assets/Settings/JuiceConfig.asset) ว่าง = ใช้ค่าเริ่มต้นในโค้ด")]
        [SerializeField] private JuiceConfig juice;

        [Header("Events")]
        public UnityEvent<int, int> onHealthChanged;
        public UnityEvent onDamaged;
        public UnityEvent onDied;

        private int currentHealth;
        private bool isDead;
        private SpriteRenderer spriteRenderer;
        private Color defaultColor = Color.white;
        private Rigidbody2D rb;
        private float knockbackTimer;
        private EnemyJuice enemyJuice;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsKnockedBack => knockbackTimer > 0f;

        private void Awake()
        {
            currentHealth = maxHealth;
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }

            // squash / death pop อยู่ที่ EnemyJuice (ถ้า prefab ยังไม่มีให้เพิ่มให้อัตโนมัติ)
            if (!TryGetComponent(out enemyJuice))
            {
                enemyJuice = gameObject.AddComponent<EnemyJuice>();
            }
            enemyJuice.Setup(juice);
        }

        private void FixedUpdate()
        {
            if (knockbackTimer <= 0f) return;

            knockbackTimer -= Time.fixedDeltaTime;
            if (rb != null)
            {
                // ชะลอความเร็วกระเด็นลงอย่างนุ่มนวล
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 12f);
            }
        }

        /// <summary>
        /// ผลักศัตรูให้กระเด็น (EnemyWander / PigEnemy จะไม่เขียนทับความเร็วระหว่างกระเด็น)
        /// </summary>
        public void ApplyKnockback(Vector2 velocity)
        {
            if (rb == null || isDead) return;
            rb.linearVelocity = velocity;
            knockbackTimer = knockbackDuration;
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return;

            currentHealth -= damage;
            onHealthChanged?.Invoke(currentHealth, maxHealth);
            onDamaged?.Invoke();

            // กระพริบสีเจ็บ
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor;
                CancelInvoke(nameof(ResetColor));
                Invoke(nameof(ResetColor), 0.12f);
            }

            bool lethal = currentHealth <= 0;
            PlayHitJuice(lethal);

            if (lethal)
            {
                Die();
            }
        }

        /// <summary>
        /// hit stop + กล้องสั่น + squash ตอนโดนตี (ตอนตายใช้ค่าที่แรง/นานกว่า และข้าม squash เพราะ death pop มาแทน)
        /// </summary>
        private void PlayHitJuice(bool lethal)
        {
            JuiceConfig cfg = JuiceDirector.Resolve(juice);

            if (cfg.hitStopEnabled)
            {
                JuiceDirector.HitStop(lethal ? cfg.hitStopOnKill : cfg.hitStopOnHit);
            }

            if (cfg.shakeEnabled)
            {
                if (lethal)
                {
                    float px = Random.Range(Mathf.Min(cfg.shakeOnKillPixels.x, cfg.shakeOnKillPixels.y),
                        Mathf.Max(cfg.shakeOnKillPixels.x, cfg.shakeOnKillPixels.y));
                    JuiceDirector.Shake(px, cfg.shakeOnKillTime, cfg.pixelsPerUnit);
                }
                else
                {
                    JuiceDirector.Shake(cfg.shakeOnHitPixels, cfg.shakeOnHitTime, cfg.pixelsPerUnit);
                }
            }

            if (!lethal && enemyJuice != null)
            {
                enemyJuice.PlayHit();
            }
        }

        public void SetDefaultColor(Color color)
        {
            defaultColor = color;
            if (spriteRenderer != null && !IsInvoking(nameof(ResetColor)))
            {
                spriteRenderer.color = color;
            }
        }

        private void ResetColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            // ปิด collider / AI ทันที ไม่ให้โดนตีซ้ำหรือวิ่งต่อระหว่างเล่น death pop
            DisableForDeath();

            DropMeat();

            // onDied ยิงตอนตายเหมือนเดิม (หลอด HP หายทันทีจาก EnemyHpBar)
            onDied?.Invoke();

            if (enemyJuice != null)
            {
                enemyJuice.PlayDeath(() => Destroy(gameObject));
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void DisableForDeath()
        {
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>())
            {
                c.enabled = false;
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
            }
            knockbackTimer = 0f;

            // หยุดสคริปต์เดินสุ่ม / หนี / พุ่งชน (ChickenEnemy, PigEnemy, EnemyWander) เหลือแค่ตัวที่ทำเอฟเฟกต์ตอนตาย
            foreach (MonoBehaviour mb in GetComponents<MonoBehaviour>())
            {
                if (mb == this || mb is EnemyJuice || mb is EnemyHpBar) continue;
                mb.enabled = false;
            }
        }

        /// <summary>
        /// ดรอปเนื้อตามจำนวนที่กำหนด แต่ละชิ้นบินไปคนละทิศ (ทิศแบ่งเท่าๆ กันรอบตัว เริ่มจากมุมสุ่ม) ตกในกรอบที่สัตว์ถูกขังไว้
        /// </summary>
        private void DropMeat()
        {
            if (meatDropPrefab == null || dropCount <= 0) return;

            JuiceConfig cfg = JuiceDirector.Resolve(juice);

            bool hasBounds = false;
            Vector2 boundsMin = Vector2.zero;
            Vector2 boundsMax = Vector2.zero;
            if (TryGetComponent(out EnemyWander wander))
            {
                hasBounds = wander.TryGetBoundary(out boundsMin, out boundsMax);
            }

            float step = 360f / dropCount;
            float baseAngle = Random.value * 360f;
            float jitter = Mathf.Min(cfg.meatDirectionJitter, step * 0.25f);

            for (int i = 0; i < dropCount; i++)
            {
                float angle = baseAngle + step * i + (dropCount > 1 ? Random.Range(-jitter, jitter) : 0f);
                MeatDropFx.Spawn(meatDropPrefab, transform.position, angle, cfg, hasBounds, boundsMin, boundsMax);
            }
        }
    }
}
