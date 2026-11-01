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

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Collider2D col;
        private Rigidbody2D rb;

        public ItemType Type => itemType;
        public string ItemName => itemName;
        public int SellPrice => sellPrice;
        public SpriteRenderer SpriteRenderer => spriteRenderer;

        private void Awake()
        {
            col = GetComponent<Collider2D>();
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // ตั้งค่า Collider เป็น Trigger สำหรับให้ Player เดินชนแล้วเก็บได้
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        /// <summary>
        /// ปรับค่าราคาขายผ่าน Inspector หรือโค้ด
        /// </summary>
        public void SetSellPrice(int price)
        {
            sellPrice = price;
        }

        /// <summary>
        /// ซ่อนหรือปิดฟิสิกส์ตอนถูกเก็บเข้ากระเป๋า/ขึ้นหลัง Player
        /// </summary>
        public void SetCarriedState(bool isCarried)
        {
            if (col != null) col.enabled = !isCarried;
            if (rb != null)
            {
                rb.simulated = !isCarried;
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // ถ้า Player เดินมาชนแล้วกระเป๋ายังไม่เต็ม สามารถเก็บได้
            if (other.TryGetComponent<PlayerCarry>(out var playerCarry))
            {
                if (playerCarry.CanCarry())
                {
                    playerCarry.PickUpItem(this);
                }
            }
        }
    }
}
