using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Collider2D))]
    public class Item : MonoBehaviour
    {
        [Header("Item Configuration")]
        [Tooltip("ประเภทของไอเทม")]
        [SerializeField] private ItemType itemType;

        [Tooltip("ชื่อของไอเทม")]
        [SerializeField] private string itemName = "New Item";

        [Tooltip("ราคาขาย (ได้เงินเมื่อลูกค้าซื้อหรือขายที่จุดขาย)")]
        [SerializeField] private int sellPrice = 10;

        [Header("Despawn Settings (การสลายตัวของไอเทมเมื่อตกพื้น)")]
        [Tooltip("เปิดให้ไอเทมที่ตกอยู่บนพื้นหายไปตามเวลาหรือไม่ (ถ้าไม่ติ๊ก ไอเทมจะอยู่ตลอดไปไม่ despawn)")]
        [SerializeField] private bool canDespawn = false;

        [Tooltip("ระยะเวลาที่ไอเทมจะอยู่บนพื้นก่อนหายไป (วินาที)")]
        [SerializeField] private float despawnTime = 30f;

        [Tooltip("กระพริบเตือนก่อนไอเทมจะสลายตัว")]
        [SerializeField] private bool blinkBeforeDespawn = true;

        [Tooltip("เริ่มกระพริบเตือนก่อนหายไปกี่วินาทีก่อนหมดเวลา")]
        [SerializeField] private float blinkWarningTime = 5f;

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Collider2D col;
        private Rigidbody2D rb;
        private bool isCarried;
        private float currentDespawnTimer;
        private Color defaultColor = Color.white;

        public ItemType Type => itemType;
        public string ItemName => itemName;
        public int SellPrice => sellPrice;
        public SpriteRenderer SpriteRenderer => spriteRenderer;
        public bool IsCarried => isCarried;
        public bool CanDespawn => canDespawn;
        public float RemainingDespawnTime => currentDespawnTimer;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                defaultColor = spriteRenderer.color;
            }

            // ตั้งค่า Collider เป็น Trigger สำหรับให้ Player เดินชนแล้วเก็บได้
            if (col != null)
            {
                col.isTrigger = true;
            }

            currentDespawnTimer = despawnTime;
        }

        private void Update()
        {
            // ถ้ากำลังถูกแบกอยู่ หรือไม่ได้เปิดระบบ despawn ไม่ต้องนับเวลา
            if (isCarried || !canDespawn || despawnTime <= 0f) return;

            currentDespawnTimer -= Time.deltaTime;

            // กระพริบเตือนเมื่อใกล้หมดเวลาสลายตัว
            if (blinkBeforeDespawn && currentDespawnTimer <= blinkWarningTime)
            {
                HandleBlinkWarning();
            }

            // เมื่อหมดเวลา ให้ทำลายไอเทมทิ้ง
            if (currentDespawnTimer <= 0f)
            {
                Despawn();
            }
        }

        private void HandleBlinkWarning()
        {
            if (spriteRenderer == null) return;

            // ความถี่ในการกระพริบจะเร็วขึ้นเรื่อยๆ เมื่อใกล้หมดเวลา
            float progress = 1f - Mathf.Clamp01(currentDespawnTimer / Mathf.Max(0.1f, blinkWarningTime));
            float blinkFrequency = Mathf.Lerp(3f, 14f, progress);
            float alpha = Mathf.PingPong(Time.time * blinkFrequency, 1f) > 0.5f ? 1f : 0.2f;

            Color c = defaultColor;
            c.a = alpha;
            spriteRenderer.color = c;
        }

        private void ResetVisualColor()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = defaultColor;
            }
        }

        private void Despawn()
        {
            Destroy(gameObject);
        }

        /// <summary>
        /// ปรับค่าราคาขายผ่าน Inspector หรือโค้ด
        /// </summary>
        public void SetSellPrice(int price)
        {
            sellPrice = price;
        }

        /// <summary>
        /// ตั้งค่าระบบ Despawn ผ่านโค้ด
        /// </summary>
        public void SetDespawnSettings(bool enable, float timeInSeconds)
        {
            canDespawn = enable;
            despawnTime = timeInSeconds;
            currentDespawnTimer = timeInSeconds;
            ResetVisualColor();
        }

        /// <summary>
        /// ซ่อนหรือปิดฟิสิกส์ตอนถูกเก็บเข้ากระเป๋า/ขึ้นหลัง Player
        /// </summary>
        public void SetCarriedState(bool carried)
        {
            isCarried = carried;
            if (col != null) col.enabled = !carried;
            if (rb != null)
            {
                rb.simulated = !carried;
                rb.linearVelocity = Vector2.zero;
            }

            if (isCarried)
            {
                // ขณะถูกแบกอยู่ ให้คืนค่าสีเดิมและไม่นับเวลาสลายตัว
                ResetVisualColor();
            }
            else
            {
                // เมื่อถูกปล่อยลงพื้น ให้รีเซ็ตเวลานับถอยหลังใหม่
                currentDespawnTimer = despawnTime;
                ResetVisualColor();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryBePickedUpBy(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            TryBePickedUpBy(other);
        }

        private void TryBePickedUpBy(Collider2D other)
        {
            if (isCarried || other == null) return;

            // ตรวจหา PlayerCarry จากทั้ง collider ตรงๆ หรือจาก Parent
            var playerCarry = other.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null && playerCarry.CanCarry())
            {
                playerCarry.PickUpItem(this);
            }
        }
    }
}
