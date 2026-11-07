using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    /// <summary>
    /// โต๊ะจ่ายเงิน: ลูกค้าที่หยิบอาหารจากโต๊ะอาหาร (FoodSellCounter) แล้ว จะถืออาหารมาต่อแถวจ่ายเงินที่นี่
    /// จ่ายเสร็จเหรียญจะหล่นบนโต๊ะนี้ให้ผู้เล่นมาเก็บ
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CashierCounter : MonoBehaviour
    {
        [Header("Customer Queue (แถวรอจ่ายเงิน)")]
        [Tooltip("จุดที่ลูกค้าหัวแถวยืนจ่ายเงิน (ถ้าว่างจะสร้างไว้ใต้โต๊ะให้อัตโนมัติ)")]
        [SerializeField] private Transform customerPayPoint;

        [Tooltip("ทิศการต่อแถวจากจุดจ่ายเงิน (เช่น (0, -1) คือต่อแถวลงด้านล่าง)")]
        [SerializeField] private Vector2 queueDirection = new Vector2(0f, -1f);

        [Tooltip("ระยะห่างระหว่างลูกค้าในแถว (เมตร)")]
        [SerializeField] private float queueSpacing = 0.9f;

        [Tooltip("ลูกค้าหัวแถวต้องยืนรอกี่วินาทีก่อนจ่ายเสร็จ")]
        [SerializeField] private float payDuration = 0.6f;

        [Header("Money (เงินที่ได้)")]
        [Tooltip("Prefab เหรียญ (ปล่อยว่างจะเพิ่มเงินเข้า MoneyManager โดยตรง)")]
        [SerializeField] private GameObject coinPrefab;

        [Tooltip("ระยะเว้นจากขอบโต๊ะตอนสุ่มตำแหน่งเหรียญ (เมตร)")]
        [SerializeField] private float counterMoneyInset = 0.3f;

        [Tooltip("เสียงตอนลูกค้าจ่ายเงิน (ถ้ามี)")]
        [SerializeField] private AudioClip paySound;

        [Header("Player Ring (วงขาวจุดยืนผู้เล่น เหมือน Sell point)")]
        [Tooltip("วงขาวบนพื้นที่ให้ผู้เล่นยืน (child ของโต๊ะ) ผู้เล่นเข้าวงแล้วเหรียญบนโต๊ะจะลอยเข้าตัว")]
        [SerializeField] private Transform playerRing;

        [Tooltip("รัศมีวง (เมตร)")]
        [SerializeField] private float playerRingRadius = 0.5f;

        [Tooltip("สีวงตอนปกติ (สีขาว)")]
        [SerializeField] private Color playerRingColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("สีวงตอนผู้เล่นยืนอยู่ในวง")]
        [SerializeField] private Color playerRingActiveColor = new Color(0.3f, 1f, 0.6f, 0.95f);

        [Header("Events")]
        public UnityEvent<int> onPaid; // จำนวนเงินที่ได้

        private readonly List<CustomerAI> queue = new List<CustomerAI>();
        private readonly List<CoinPickup> coinsOnCounter = new List<CoinPickup>();
        private SpriteRenderer playerRingRenderer;
        private Transform playerTransform;

        public Transform CustomerPayPoint => customerPayPoint != null ? customerPayPoint : transform;
        public float PayDuration => payDuration;

        private void Awake()
        {
            // โต๊ะเป็นของแข็ง ลูกค้าจะเดินอ้อม
            Collider2D col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = false;

            if (customerPayPoint == null)
            {
                Transform existing = transform.Find("CustomerPayPoint");
                if (existing != null)
                {
                    customerPayPoint = existing;
                }
                else
                {
                    var point = new GameObject("CustomerPayPoint");
                    point.transform.SetParent(transform);
                    Bounds b = col != null ? col.bounds : new Bounds(transform.position, Vector3.one);
                    point.transform.position = new Vector3(b.center.x, b.min.y - 0.85f, transform.position.z);
                    customerPayPoint = point.transform;
                }
            }

            if (playerRing != null)
            {
                playerRingRenderer = playerRing.GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            UpdatePlayerRing();
        }

        /// <summary>
        /// วงขาวกระพริบเบา ๆ และเปลี่ยนเป็นสีเขียวเมื่อผู้เล่นเข้ามายืน (แบบเดียวกับ Sell point)
        /// </summary>
        private void UpdatePlayerRing()
        {
            if (playerRing == null || playerRingRenderer == null) return;

            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerCarry>();
                if (player != null) playerTransform = player.transform;
            }

            bool inRange = playerTransform != null &&
                           Vector2.Distance(playerRing.position, playerTransform.position) <= playerRingRadius;
            Color target = inRange ? playerRingActiveColor : playerRingColor;
            playerRingRenderer.color = Color.Lerp(playerRingRenderer.color, target, Time.deltaTime * 10f);

            // ผู้เล่นเข้าวง -> ดูดเหรียญทั้งหมดบนโต๊ะเข้าตัว
            if (inRange && coinsOnCounter.Count > 0)
            {
                foreach (var coin in coinsOnCounter)
                {
                    if (coin != null) coin.AttractTo(playerTransform);
                }
                coinsOnCounter.Clear();
            }

            // ชดเชยสเกลของโต๊ะ ให้วงมีขนาดในโลกตาม playerRingRadius
            Vector3 lossy = transform.lossyScale;
            float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.04f;
            float d = playerRingRadius * 2f * pulse;
            playerRing.localScale = new Vector3(
                d / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                d / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                1f);
        }

        // ========================== แถว ==========================

        public void JoinQueue(CustomerAI customer)
        {
            queue.RemoveAll(c => c == null);
            if (customer != null && !queue.Contains(customer)) queue.Add(customer);
        }

        public void LeaveQueue(CustomerAI customer)
        {
            queue.Remove(customer);
            queue.RemoveAll(c => c == null);
        }

        public bool IsHeadOfQueue(CustomerAI customer)
        {
            queue.RemoveAll(c => c == null);
            return queue.Count > 0 && queue[0] == customer;
        }

        public Vector3 GetQueuePosition(CustomerAI customer)
        {
            queue.RemoveAll(c => c == null);
            int index = queue.IndexOf(customer);
            if (index < 0) index = queue.Count;

            Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
            return CustomerPayPoint.position + (Vector3)(dir * (index * queueSpacing));
        }

        // ========================== จ่ายเงิน ==========================

        /// <summary>
        /// ลูกค้าจ่ายเงินค่าอาหารที่ถืออยู่: เหรียญหล่นบนโต๊ะ แล้วอาหารหายไป (ลูกค้าเอากลับบ้าน)
        /// </summary>
        public int ReceivePayment(CustomerAI customer, Item dish)
        {
            int price = dish != null && dish.SellPrice > 0 ? dish.SellPrice : 10;

            if (coinPrefab != null)
            {
                GameObject coin = Instantiate(coinPrefab, GetRandomPointOnCounter(), Quaternion.identity);
                if (coin.TryGetComponent<CoinPickup>(out var pickup))
                {
                    pickup.Value = price;

                    // เหรียญวางรอบนโต๊ะ ต้องเข้าวงขาวถึงจะดูดเข้าตัว (ไม่หายตามเวลา)
                    if (playerRing != null)
                    {
                        pickup.WaitForCollectZone();
                        coinsOnCounter.Add(pickup);
                    }
                }
            }
            else if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(price);
            }

            if (paySound != null)
            {
                AudioSource.PlayClipAtPoint(paySound, transform.position);
            }

            LeaveQueue(customer);
            onPaid?.Invoke(price);
            return price;
        }

        private Vector3 GetRandomPointOnCounter()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col == null) return transform.position;

            Bounds b = col.bounds;
            float halfW = Mathf.Max(0f, b.extents.x - counterMoneyInset);
            float halfH = Mathf.Max(0f, b.extents.y - counterMoneyInset);
            return new Vector3(
                b.center.x + Random.Range(-halfW, halfW),
                b.center.y + Random.Range(-halfH, halfH),
                transform.position.z);
        }

        private void OnDrawGizmos()
        {
            // วงจุดยืนผู้เล่น (สีฟ้า เหมือน Sell point)
            if (playerRing != null)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
                Gizmos.DrawWireSphere(playerRing.position, playerRingRadius);
                Gizmos.DrawLine(transform.position, playerRing.position);
            }

            // จุดจ่ายเงินและแถว (สีเขียว)
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.9f);
            Vector3 start = customerPayPoint != null ? customerPayPoint.position : transform.position + Vector3.down * 1.85f;
            Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
            Gizmos.DrawWireSphere(start, 0.25f);
            for (int i = 1; i < 4; i++)
            {
                Vector3 p = start + (Vector3)(dir * (i * queueSpacing));
                Gizmos.DrawWireSphere(p, 0.18f);
                Gizmos.DrawLine(start + (Vector3)(dir * ((i - 1) * queueSpacing)), p);
            }
        }
    }
}
