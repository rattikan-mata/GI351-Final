using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyWander))]
    [RequireComponent(typeof(Rigidbody2D))]
    public class PigEnemy : MonoBehaviour
    {
        [Header("Contact Damage (ดาเมจจากการชน Player)")]
        [Tooltip("ความเสียหายที่ทำใส่ Player เมื่อวิ่งชน")]
        [SerializeField] private int collisionDamage = 15;

        [Tooltip("แรงผลักกระเด็นเมื่อชนตัว Player")]
        [SerializeField] private float ramKnockbackForce = 6f;

        [Tooltip("คูลดาวน์การชนทำดาเมจซ้ำ (วินาที)")]
        [SerializeField] private float attackCooldown = 0.8f;

        [Header("Aggro Settings (การวิ่งไล่ล่าเมื่อโดนโจมตี)")]
        [Tooltip("ความเร็วในการวิ่งไล่ Player เมื่อโกรธ")]
        [SerializeField] private float chaseSpeed = 3.5f;

        [Tooltip("ระยะที่ Player หนีพ้นแล้วหมูจะเลิกตาม (ถ้าใส่ 0 จะตามไม่เลิกจนกว่าจะตาย)")]
        [SerializeField] private float deAggroDistance = 12f;

        [Tooltip("สีของหมูเวลาโกรธ")]
        [SerializeField] private Color angryColor = new Color(1f, 0.45f, 0.45f, 1f);

        [Header("Effects & Sounds")]
        [Tooltip("เสียงร้องเฉพาะของหมู (เช่น ตอนโกรธ หรือตอนชน)")]
        [SerializeField] private AudioClip oinkSound;

        [Tooltip("GameObject แสดงสถานะโกรธ (เช่น ไอคอนโกรธ ถ้ามี)")]
        [SerializeField] private GameObject angryIndicator;

        private EnemyHealth health;
        private EnemyWander wander;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;

        private Transform targetPlayer;
        private bool isEnraged;
        private float nextDamageTime;
        private Color initialColor = Color.white;

        public EnemyHealth Health => health;
        public EnemyWander Wander => wander;
        public bool IsEnraged => isEnraged;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            wander = GetComponent<EnemyWander>();
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            if (spriteRenderer != null)
            {
                initialColor = spriteRenderer.color;
            }

            if (angryIndicator != null)
            {
                angryIndicator.SetActive(false);
            }
        }

        private void Start()
        {
            // ฟัง Event เมื่อหมูโดนโจมตี จะทำให้หมูโกรธและเริ่มวิ่งไล่ล่า Player
            if (health != null)
            {
                health.onDamaged.AddListener(Enrage);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.onDamaged.RemoveListener(Enrage);
            }
        }

        private void FixedUpdate()
        {
            // ระหว่างกระเด็น ปล่อยให้ EnemyHealth ควบคุมความเร็ว
            if (isEnraged && !health.IsKnockedBack)
            {
                ChasePlayer();
            }
        }

        /// <summary>
        /// ทำให้หมูเข้าสู่สถานะโกรธและล็อกเป้าหมายไปที่ Player
        /// </summary>
        public void Enrage()
        {
            if (isEnraged) return;

            FindPlayer();
            if (targetPlayer == null) return;

            isEnraged = true;

            // ปิดระบบเดินสุ่ม เพื่อให้การควบคุมเป็นของระบบไล่ล่า
            if (wander != null)
            {
                wander.enabled = false;
            }

            // เปลี่ยนสีหมูให้แดงขึ้น
            if (health != null)
            {
                health.SetDefaultColor(angryColor);
            }

            if (angryIndicator != null)
            {
                angryIndicator.SetActive(true);
            }

            if (oinkSound != null)
            {
                AudioSource.PlayClipAtPoint(oinkSound, transform.position);
            }
        }

        /// <summary>
        /// หมูใจเย็นลงแล้วกลับไปเดินสุ่มตามปกติ
        /// </summary>
        public void CalmDown()
        {
            isEnraged = false;
            targetPlayer = null;
            rb.linearVelocity = Vector2.zero;

            // เปิดระบบเดินสุ่มตามเดิม
            if (wander != null)
            {
                wander.enabled = true;
                wander.PickNewTargetPoint();
            }

            if (health != null)
            {
                health.SetDefaultColor(initialColor);
            }

            if (angryIndicator != null)
            {
                angryIndicator.SetActive(false);
            }
        }

        private void ChasePlayer()
        {
            if (targetPlayer == null)
            {
                FindPlayer();
                if (targetPlayer == null)
                {
                    CalmDown();
                    return;
                }
            }

            // ถ้าผู้เล่นตายแล้ว ให้หยุดวิ่งไล่
            if (targetPlayer.TryGetComponent<PlayerHealth>(out var playerHealth) && playerHealth.IsDead)
            {
                CalmDown();
                return;
            }

            float distance = Vector2.Distance(transform.position, targetPlayer.position);

            // ถ้าหนีไกลเกินระยะที่กำหนด ให้หมูเลิกตาม
            if (deAggroDistance > 0f && distance > deAggroDistance)
            {
                CalmDown();
                return;
            }

            // วิ่งพุ่งตรงไปยัง Player
            Vector2 dir = ((Vector2)targetPlayer.position - rb.position).normalized;
            rb.linearVelocity = dir * chaseSpeed;

            // กลับหน้าซ้าย-ขวาตามทิศที่วิ่ง
            if (spriteRenderer != null)
            {
                if (dir.x > 0.05f) spriteRenderer.flipX = false;
                else if (dir.x < -0.05f) spriteRenderer.flipX = true;
            }
        }

        private void FindPlayer()
        {
            var player = FindFirstObjectByType<PlayerMovement>();
            if (player != null)
            {
                targetPlayer = player.transform;
            }
        }

        /// <summary>
        /// ชนทำดาเมจและแรงผลักใส่ Player
        /// </summary>
        private void TryDamagePlayer(GameObject target)
        {
            // ทำดาเมจชนเฉพาะเมื่อหมูอยู่ในสถานะโกรธ (หรือถ้าต้องการให้ชนได้ตลอดเวลาสามารถเอาเงื่อนไข isEnraged ออกได้)
            if (!isEnraged) return;
            if (Time.time < nextDamageTime) return;

            PlayerHealth playerHealth = target.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                nextDamageTime = Time.time + attackCooldown;
                Vector2 pushDir = ((Vector2)target.transform.position - rb.position).normalized;

                // ส่งทั้งดาเมจและแรงผลักให้ PlayerHealth จัดการกระเด็นและการกระพริบ
                playerHealth.TakeDamage(collisionDamage, pushDir, ramKnockbackForce);

                if (oinkSound != null)
                {
                    AudioSource.PlayClipAtPoint(oinkSound, transform.position);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDamagePlayer(collision.gameObject);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TryDamagePlayer(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDamagePlayer(other.gameObject);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryDamagePlayer(other.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            // วาดระยะเลิกตาม (De-Aggro Radius)
            if (deAggroDistance > 0f)
            {
                Gizmos.color = new Color(1f, 0.4f, 0f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, deAggroDistance);
            }
        }
    }
}
