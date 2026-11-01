using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [Header("Enemy Health Stats (ปรับได้ใน Inspector)")]
        [Tooltip("เลือดสูงสุดของศัตรู")]
        [SerializeField] private int maxHealth = 30;

        [Header("Loot Drop (ไอเทมที่จะดรอปเมื่อตาย)")]
        [Tooltip("Prefab ของเนื้อที่จะดรอป (เช่น Chicken Meat หรือ Pig Meat)")]
        [SerializeField] private GameObject meatDropPrefab;

        [Tooltip("จำนวนชิ้นที่จะดรอป")]
        [SerializeField] private int dropCount = 1;

        [Header("Visual Feedback")]
        [Tooltip("กระพริบสีแดงเมื่อโดนฟัน")]
        [SerializeField] private Color hurtColor = Color.red;

        [Header("Events")]
        public UnityEvent<int, int> onHealthChanged;
        public UnityEvent onDamaged;
        public UnityEvent onDied;

        private int currentHealth;
        private bool isDead;
        private SpriteRenderer spriteRenderer;
        private Color defaultColor = Color.white;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }
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

            if (currentHealth <= 0)
            {
                Die();
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

            // ดรอปเนื้อตามจำนวนที่กำหนด
            if (meatDropPrefab != null)
            {
                for (int i = 0; i < dropCount; i++)
                {
                    Vector2 randomOffset = Random.insideUnitCircle * 0.4f;
                    Instantiate(meatDropPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
                }
            }

            onDied?.Invoke();
            Destroy(gameObject);
        }
    }
}
