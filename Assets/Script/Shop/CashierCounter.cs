using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    /// <summary>
    /// โต๊ะจ่ายเงิน (แบบ Juice King): ลูกค้าที่หยิบอาหารจากแผง (FoodSellCounter) แล้ว ถืออาหารมาต่อแถวจ่ายเงินที่นี่
    /// ต้องมี "คนคิดเงิน" ลูกค้าถึงจะจ่ายได้: ผู้เล่นยืนในวงขาวหลังโต๊ะ หรือจ้างพนักงานแคชเชียร์ (CashierStaff) มาคิดแทน
    /// คิดทีละคน มีหลอดความคืบหน้าเหนือหัวลูกค้า จ่ายเสร็จเหรียญลอยไปกองบนโต๊ะ (1 เหรียญต่อ 1 จาน)
    /// ผู้เล่นเข้าวงแล้วเหรียญในกองจะลอยเข้าตัว
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

        [Header("Crowd (ยืนมุงแทนต่อแถว)")]
        [Tooltip("ให้ลูกค้ายืนมุงเป็นกลุ่มรอบจุดจ่ายเงิน แทนการต่อแถวเป็นเส้นตรง (ลำดับการจ่ายเงินยังเรียงตามคนมาก่อน)")]
        [SerializeField] private bool crowdMode = true;

        [Tooltip("จำนวนคนในแถวหน้าสุด (แถวถัดไปลดลงทีละ 1 คน)")]
        [SerializeField] private int crowdColumns = 5;

        [Tooltip("ระยะห่างระหว่างคนในแถว (เมตร)")]
        [SerializeField] private float crowdSpacing = 0.85f;

        [Tooltip("ระยะห่างระหว่างแถว (เมตร)")]
        [SerializeField] private float crowdRowSpacing = 0.6f;

        [Tooltip("สุ่มเยื้องจุดยืนเล็กน้อย (เมตร) ให้ดูไม่เป็นระเบียบเกินไป")]
        [SerializeField] private float crowdJitter = 0.08f;

        // จุดยืนที่จองไว้ของลูกค้าแต่ละคน (ยืนจุดเดิมจนออกไป)
        private readonly CrowdSlots crowdSlots = new CrowdSlots();

        /// <summary>เปิดโหมดยืนมุงอยู่หรือไม่</summary>
        public bool CrowdMode => crowdMode;

        [Header("Checkout (การคิดเงิน)")]
        [Tooltip("เปิด = ต้องมีคนคิดเงิน (ผู้เล่นยืนในวงขาว หรือพนักงานแคชเชียร์) ลูกค้าถึงจะจ่ายได้ / ปิด = ลูกค้าจ่ายเองอัตโนมัติแบบเดิม")]
        [SerializeField] private bool requireOperator = true;

        [Tooltip("ผู้เล่นคิดเงินลูกค้า 1 คนใช้กี่วินาที (Juice King เกาะแรก: 0.75)")]
        [SerializeField] private float payDuration = 0.75f;

        [Tooltip("พนักงานแคชเชียร์คิดเงินลูกค้า 1 คนใช้กี่วินาที (ช้ากว่าผู้เล่น)")]
        [SerializeField] private float staffPayDuration = 1.2f;

        [Tooltip("ขนาดหลอดความคืบหน้าเหนือหัวลูกค้าที่กำลังจ่าย (กว้าง x สูง เมตร)")]
        [SerializeField] private Vector2 payBarSize = new Vector2(0.8f, 0.12f);

        [Tooltip("ความสูงของหลอดจากตัวลูกค้า (เมตร)")]
        [SerializeField] private float payBarHeight = 1.6f;

        [SerializeField] private Color payBarBackColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] private Color payBarFillColor = new Color(0.35f, 0.95f, 0.45f, 1f);

        [Tooltip("สีหลอดตอนไม่มีคนคิดเงิน (ลูกค้ารออยู่)")]
        [SerializeField] private Color payBarWaitingColor = new Color(1f, 0.47f, 0.38f, 1f);

        [Header("Money (เงินที่ได้)")]
        [Tooltip("Prefab เหรียญ (ปล่อยว่างจะเพิ่มเงินเข้า MoneyManager โดยตรง)")]
        [SerializeField] private GameObject coinPrefab;

        [Tooltip("จำนวนเหรียญต่อแถวในกองเงินบนโต๊ะ")]
        [Min(1)] [SerializeField] private int pileColumns = 6;

        [Tooltip("ระยะห่างระหว่างเหรียญในกอง (x = ซ้ายขวา, y = แถวถัดไปขยับขึ้น)")]
        [SerializeField] private Vector2 pileSpacing = new Vector2(0.24f, 0.11f);

        [Tooltip("ตำแหน่งแถวหน้าของกองเงิน เยื้องจากกลางโต๊ะ (เมตร)")]
        [SerializeField] private Vector2 pileOffset = new Vector2(0f, -0.05f);

        [Tooltip("โชว์เหรียญในกองสูงสุดกี่เหรียญ (ที่เกินซ้อนอยู่ที่ช่องสุดท้าย)")]
        [Min(1)] [SerializeField] private int maxPileSlots = 48;

        [Tooltip("เวลาที่เหรียญลอยจากลูกค้ามาลงกอง (วินาที)")]
        [Min(0.01f)] [SerializeField] private float coinFlyTime = 0.25f;

        [Tooltip("เว้นจังหวะระหว่างเหรียญแต่ละเหรียญของลูกค้าคนเดียวกัน (วินาที)")]
        [Min(0f)] [SerializeField] private float coinStagger = 0.08f;

        [Tooltip("เหรียญต้องอยู่ในกองอย่างน้อยกี่วินาทีก่อนลอยเข้าตัวผู้เล่น (ให้เห็นว่าเงินเข้ากองก่อน)")]
        [Min(0f)] [SerializeField] private float collectDelay = 0.3f;

        [Tooltip("เสียงตอนลูกค้าจ่ายเงิน (ถ้ามี)")]
        [SerializeField] private AudioClip paySound;

        [Header("Player Ring (วงขาวจุดยืนคิดเงิน/เก็บเงิน)")]
        [Tooltip("วงขาวบนพื้นหลังโต๊ะ ผู้เล่นยืนในวง = คิดเงินลูกค้า + เหรียญในกองลอยเข้าตัว")]
        [SerializeField] private Transform playerRing;

        [Tooltip("รัศมีวง (เมตร)")]
        [SerializeField] private float playerRingRadius = 0.5f;

        [Tooltip("ยืดเขตวงยืนลงไปทางโต๊ะ (หน่วยโลก) ยืนชิดหลังโต๊ะลึกแค่ไหนก็ยังนับว่าอยู่ในวง ภาพวงไม่ขยับ (-1 = คิดเองจนถึงขอบโต๊ะ)")]
        [SerializeField] private float playerRingReachDown = -1f;

        [Tooltip("สีวงตอนปกติ (สีขาว)")]
        [SerializeField] private Color playerRingColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("สีวงตอนผู้เล่นยืนอยู่ในวง")]
        [SerializeField] private Color playerRingActiveColor = new Color(0.3f, 1f, 0.6f, 0.95f);

        [Tooltip("สีวงตอนมีลูกค้ารอจ่ายแต่ไม่มีคนคิดเงิน (กระพริบเรียกผู้เล่น)")]
        [SerializeField] private Color playerRingNeededColor = new Color(1f, 0.8f, 0.2f, 1f);

        [Header("Events")]
        public UnityEvent<int> onPaid; // จำนวนเงินที่ได้

        private class PileCoin
        {
            public CoinPickup coin;
            public Vector3 start;
            public float startTime;   // เวลาที่เริ่มลอย (รวม stagger)
            public bool landed;
            public float landedTime;
        }

        private readonly List<CustomerAI> queue = new List<CustomerAI>();
        private readonly List<PileCoin> pile = new List<PileCoin>();

        // กลุ่มเรียงชั้นของกองเงิน: ทั้งกองวาดเป็นก้อนเดียวที่ลำดับเดียวกับโต๊ะ (ตัวละครยืนหน้าโต๊ะวาดทับเหรียญทุกเหรียญ)
        private UnityEngine.Rendering.SortingGroup pileGroup;
        private SpriteRenderer playerRingRenderer;
        private Transform playerTransform;
        private bool playerInRing;
        private CashierStaff staff;

        // หลอดความคืบหน้า
        private GameObject payBarRoot;
        private Transform payBarFill;
        private SpriteRenderer payBarFillRenderer;
        private CustomerAI payingCustomer;
        private float payProgress;
        private bool payServing;
        private int payReportFrame = -1;

        public Transform CustomerPayPoint => customerPayPoint != null ? customerPayPoint : transform;

        /// <summary>Prefab เหรียญของโต๊ะนี้ (ว่างได้) ใช้สร้างเหรียญที่หล่นจากที่อื่น</summary>
        public GameObject CoinPrefab => coinPrefab;

        /// <summary>วงขาวที่ผู้เล่น/พนักงานแคชเชียร์ยืน</summary>
        public Transform PlayerRing => playerRing;

        /// <summary>ผู้เล่นยืนอยู่ในวงคิดเงินหรือไม่</summary>
        public bool PlayerInRing => playerInRing;

        /// <summary>มีพนักงานแคชเชียร์ประจำโต๊ะนี้หรือไม่</summary>
        public bool HasStaff => staff != null;

        /// <summary>ตอนนี้มีคนคิดเงินให้ลูกค้าได้หรือไม่</summary>
        public bool CanServe => !requireOperator || playerInRing || HasStaff;

        /// <summary>เวลาคิดเงินต่อคน ตามคนที่คิดอยู่ตอนนี้ (ผู้เล่นเร็วกว่าพนักงาน)</summary>
        public float PayDuration => requireOperator && !playerInRing && HasStaff ? staffPayDuration : payDuration;

        /// <summary>จำนวนลูกค้าที่รอจ่ายเงิน</summary>
        public int QueueCount
        {
            get
            {
                queue.RemoveAll(c => c == null);
                return queue.Count;
            }
        }

        /// <summary>จำนวนเหรียญที่วางรอในกองบนโต๊ะ</summary>
        public int CoinsOnCounter
        {
            get
            {
                pile.RemoveAll(c => c.coin == null);
                return pile.Count;
            }
        }

        /// <summary>พนักงานแคชเชียร์มาประจำ/ออกจากโต๊ะนี้</summary>
        public void SetStaff(CashierStaff cashierStaff)
        {
            staff = cashierStaff;
        }

        /// <summary>
        /// ส่งเหรียญในกอง 1 เหรียญให้ลอยไปหา target (ใช้กับพนักงานที่ตั้งให้เก็บเงินด้วย) คืน true ถ้ามีเหรียญให้ส่ง
        /// </summary>
        public bool AttractOneCoinTo(Transform target)
        {
            if (target == null) return false;
            pile.RemoveAll(c => c.coin == null);
            for (int i = 0; i < pile.Count; i++)
            {
                if (!pile[i].landed) continue;
                SendCoinTo(pile[i].coin, target);
                pile.RemoveAt(i);
                return true;
            }
            return false;
        }

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
            UpdatePile();
            UpdatePayBar();
        }

        /// <summary>
        /// วงขาว: เขียวเมื่อผู้เล่นยืนคิดเงิน / กระพริบเหลืองเมื่อมีลูกค้ารอแต่ไม่มีคนคิดเงิน
        /// </summary>
        private void UpdatePlayerRing()
        {
            if (playerTransform == null)
            {
                var player = FindAnyObjectByType<PlayerCarry>();
                if (player != null) playerTransform = player.transform;
            }

            playerInRing = playerRing != null && playerTransform != null &&
                           YSort.DistanceToRing(YSort.FeetOf(playerTransform), playerRing.position,
                               playerRingReachDown >= 0f ? playerRingReachDown : YSort.ReachDownToSolid(gameObject, playerRing.position)) <= playerRingRadius;

            if (playerRing == null || playerRingRenderer == null) return;

            bool needOperator = requireOperator && !CanServe && QueueCount > 0;
            Color target = playerInRing ? playerRingActiveColor : playerRingColor;
            float pulseSpeed = 2.5f, pulseSize = 0.04f;
            if (needOperator)
            {
                // เรียกผู้เล่น: กระพริบเหลือง-ขาว และเต้นแรงขึ้น
                target = Color.Lerp(playerRingColor, playerRingNeededColor, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
                pulseSpeed = 8f;
                pulseSize = 0.1f;
            }
            playerRingRenderer.color = Color.Lerp(playerRingRenderer.color, target, Time.deltaTime * 10f);

            // ชดเชยสเกลของโต๊ะ ให้วงมีขนาดในโลกตาม playerRingRadius
            Vector3 lossy = transform.lossyScale;
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseSize;
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
            crowdSlots.Release(customer);
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

            // ยืนมุงเป็นกลุ่มก้อนรอบจุดจ่ายเงิน
            if (crowdMode)
            {
                int slot = crowdSlots.GetSlot(customer);
                return CustomerPayPoint.position + (Vector3)CrowdLayout.GetSlotOffset(slot, dir, crowdColumns, crowdSpacing, crowdRowSpacing, crowdJitter);
            }

            return CustomerPayPoint.position + (Vector3)(dir * (index * queueSpacing));
        }

        // ========================== จ่ายเงิน ==========================

        /// <summary>ลูกค้าหัวแถวรายงานความคืบหน้าการจ่ายเงิน (0-1) ทุกเฟรม ใช้วาดหลอดเหนือหัว serving = มีคนคิดเงินอยู่</summary>
        public void ReportPayProgress(CustomerAI customer, float progress, bool serving)
        {
            payingCustomer = customer;
            payProgress = Mathf.Clamp01(progress);
            payServing = serving;
            payReportFrame = Time.frameCount;
        }

        /// <summary>ลูกค้าจ่ายเงินค่าอาหาร 1 จาน (เก็บไว้ให้โค้ดเดิมเรียกได้)</summary>
        public int ReceivePayment(CustomerAI customer, Item dish)
        {
            return ReceivePayment(customer, new List<Item> { dish });
        }

        /// <summary>
        /// ลูกค้าจ่ายเงินค่าอาหารทั้งหมดที่ถืออยู่: เหรียญ 1 เหรียญต่อ 1 จาน ลอยจากตัวลูกค้าไปลงกองบนโต๊ะทีละเหรียญ
        /// </summary>
        public int ReceivePayment(CustomerAI customer, IList<Item> dishes)
        {
            int total = 0;
            Vector3 from = customer != null ? customer.transform.position + new Vector3(0f, 0.6f, 0f) : CustomerPayPoint.position;
            int n = dishes != null ? dishes.Count : 0;

            for (int i = 0; i < Mathf.Max(1, n); i++)
            {
                Item dish = n > 0 ? dishes[i] : null;
                int price = dish != null && dish.SellPrice > 0 ? dish.SellPrice : 10;                total += price;

                if (coinPrefab != null)
                {
                    GameObject coinObj = Instantiate(coinPrefab, from, Quaternion.identity);
                    if (coinObj.TryGetComponent<CoinPickup>(out var pickup))
                    {
                        pickup.Value = price;
                        // เหรียญอยู่ในกองบนโต๊ะ (ไม่หายตามเวลา) รอผู้เล่นเข้าวงถึงจะลอยเข้าตัว
                        pickup.WaitForCollectZone();
                        // อยู่ในกลุ่มกองเงิน เรียงชั้นพร้อมโต๊ะ (ตำแหน่งในโลกคงเดิม ขยับด้วยตำแหน่งโลกทุกเฟรมอยู่แล้ว)
                        coinObj.transform.SetParent(UpdatePileGroup(), true);
                        pile.Add(new PileCoin { coin = pickup, start = from, startTime = Time.time + i * coinStagger });
                    }
                }
                else if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.AddMoney(price);
                }
            }

            AudioManager.Play(SoundId.CustomerPay, paySound, transform.position);

            LeaveQueue(customer);
            onPaid?.Invoke(total);
            return total;
        }

        /// <summary>ตำแหน่งช่องที่ index ในกองเงิน (เรียงแบนบนโต๊ะ)</summary>
        private Vector3 GetPileSlotPosition(int index)
        {
            index = Mathf.Min(index, Mathf.Max(1, maxPileSlots) - 1);
            Collider2D col = GetComponent<Collider2D>();
            Vector3 center = col != null ? col.bounds.center : transform.position;
            Vector2 off = FlatPile.Offset(index, pileColumns, pileSpacing);
            return new Vector3(center.x + pileOffset.x + off.x, center.y + pileOffset.y + off.y, transform.position.z - 0.05f);
        }

        /// <summary>ขยับเหรียญ: ลอยจากลูกค้ามาลงกอง / เลื่อนเข้าช่องใหม่เมื่อกองขยับ / ผู้เล่นอยู่ในวงแล้วลอยเข้าตัว</summary>
        private void UpdatePile()
        {
            pile.RemoveAll(c => c.coin == null);
            if (pile.Count == 0) return;

            UpdatePileGroup();

            for (int i = pile.Count - 1; i >= 0; i--)
            {
                PileCoin c = pile[i];
                Transform t = c.coin.transform;
                Vector3 slot = GetPileSlotPosition(i);

                if (c.coin.TryGetComponent(out SpriteRenderer sr))
                {
                    // ลำดับภายในกลุ่มกองเงิน: แถวหน้าทับแถวหลัง
                    sr.sortingOrder = FlatPile.SortingOffset(Mathf.Min(i, maxPileSlots - 1), pileColumns, maxPileSlots);
                    sr.enabled = Time.time >= c.startTime;
                }

                if (!c.landed)
                {
                    float k = (Time.time - c.startTime) / coinFlyTime;
                    if (k < 0f) { t.position = c.start; continue; }
                    if (k < 1f)
                    {
                        Vector3 p = Vector3.Lerp(c.start, slot, Tween.Evaluate(Ease.OutQuad, k));
                        p.y += Mathf.Sin(k * Mathf.PI) * 0.5f;
                        t.position = p;
                        continue;
                    }
                    c.landed = true;
                    c.landedTime = Time.time;
                    t.position = slot;
                    continue;
                }

                t.position = Vector3.Lerp(t.position, slot, Time.deltaTime * 12f);

                // ผู้เล่นยืนในวง: เหรียญที่ลงกองแล้วครู่หนึ่งลอยเข้าตัว
                if (playerInRing && playerTransform != null && Time.time - c.landedTime >= collectDelay)
                {
                    SendCoinTo(c.coin, playerTransform);
                    pile.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// กลุ่มเรียงชั้นของกองเงิน (สร้างตอนใช้ครั้งแรก): ชั้นและลำดับเดียวกับโต๊ะ วางที่ขอบล่างของโต๊ะ
        /// ต่ำกว่าและใกล้กล้องกว่าโต๊ะนิดเดียว ลำดับเท่ากันจึงวาดหลังโต๊ะ (อยู่บนโต๊ะ)
        /// </summary>
        private Transform UpdatePileGroup()
        {
            if (pileGroup == null)
            {
                var go = new GameObject("Coin Pile");
                go.transform.SetParent(transform, false);
                pileGroup = go.AddComponent<UnityEngine.Rendering.SortingGroup>();
            }

            SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
            if (tableRenderer != null)
            {
                pileGroup.sortingLayerID = tableRenderer.sortingLayerID;
                pileGroup.sortingOrder = tableRenderer.sortingOrder;
                pileGroup.transform.position = new Vector3(transform.position.x, tableRenderer.bounds.min.y - 0.01f, transform.position.z - 0.05f);
            }
            else
            {
                pileGroup.sortingOrder = 0;
                pileGroup.transform.position = transform.position + new Vector3(0f, 0f, -0.05f);
            }
            return pileGroup.transform;
        }

        /// <summary>เหรียญออกจากกองลอยไปหา target: ออกจากกลุ่มกองเงิน และวาดบนสุดระหว่างลอย</summary>
        private static void SendCoinTo(CoinPickup coin, Transform target)
        {
            coin.transform.SetParent(null, true);
            if (coin.TryGetComponent(out SpriteRenderer sr))
            {
                sr.sortingOrder = YSort.Overlay;
                sr.enabled = true;
            }
            coin.AttractTo(target);
        }

        // ========================== หลอดความคืบหน้าการคิดเงิน ==========================

        private void UpdatePayBar()
        {
            bool show = payingCustomer != null && payReportFrame >= Time.frameCount - 1 && (payServing || payProgress > 0f || requireOperator);
            if (!show)
            {
                if (payBarRoot != null && payBarRoot.activeSelf) payBarRoot.SetActive(false);
                return;
            }

            if (payBarRoot == null)
            {
                SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
                int layer = tableRenderer != null ? tableRenderer.sortingLayerID : 0;
                payBarRoot = new GameObject("PayBar");
                payBarRoot.transform.SetParent(transform, false);
                YSort.PutOnTop(payBarRoot.gameObject);   // หลอดจ่ายเงินอยู่บนสุดเสมอ
                WorldUi.MakeShape(payBarRoot.transform, "Back", WorldUi.Pixel, payBarBackColor, payBarSize, Vector3.zero, layer, 70);
                payBarFillRenderer = WorldUi.MakeShape(payBarRoot.transform, "Fill", WorldUi.PixelLeft, payBarFillColor,
                    new Vector2(payBarSize.x, payBarSize.y * 0.7f), new Vector3(-payBarSize.x * 0.5f, 0f, 0f), layer, 71);
                payBarFill = payBarFillRenderer.transform;
            }
            if (!payBarRoot.activeSelf) payBarRoot.SetActive(true);

            WorldUi.KeepWorldScale(payBarRoot.transform);
            payBarRoot.transform.position = payingCustomer.transform.position + new Vector3(0f, payBarHeight, -0.1f);
            payBarRoot.transform.rotation = Quaternion.identity;

            // ไม่มีคนคิดเงิน: หลอดเต็มสีส้มแดงกระพริบ บอกว่าลูกค้ารออยู่
            if (!payServing)
            {
                Color wait = payBarWaitingColor;
                wait.a *= 0.55f + 0.45f * Mathf.Sin(Time.time * 8f);
                payBarFillRenderer.color = wait;
                payBarFill.localScale = new Vector3(payBarSize.x, payBarSize.y * 0.7f, 1f);
            }
            else
            {
                payBarFillRenderer.color = payBarFillColor;
                payBarFill.localScale = new Vector3(payBarSize.x * payProgress, payBarSize.y * 0.7f, 1f);
            }
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
