using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.Serialization;

namespace GI351.KitchenGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(Collider2D))]
    public class FoodSellCounter : MonoBehaviour
    {
        [Header("Shelf Dishes (ชั้นนี้ขายอาหารอะไร)")]
        [Tooltip("อาหารที่ชั้นนี้รับวางและขาย (ว่าง = รับทุกอย่าง) เช่น ชั้นหมูใส่แค่ CookedPig / ชั้นไก่ใส่แค่ CookedChicken / ลูกค้าที่อยากกินเมนูไหนจะเดินมาที่ชั้นที่ขายเมนูนั้น / อาหารพิเศษจากโจรวางได้ทุกชั้น")]
        [SerializeField] private List<ItemType> acceptedDishes = new List<ItemType>();

        [Header("Unlock (ปลดล็อกชั้นพร้อมหม้อ)")]
        [Tooltip("ชั้นนี้เปิดใช้เมื่อหม้อนี้ถูกซื้อแล้ว (ว่าง = เปิดตั้งแต่เริ่ม) ระหว่างล็อก: ชั้นจาง เดินทะลุได้ ไม่มีวงวางอาหาร ลูกค้าไม่มา")]
        [SerializeField] private CookingPot unlockWithPot;

        [Tooltip("ความทึบของชั้นตอนยังล็อก (0 = ซ่อนทั้งชั้น)")]
        [Range(0f, 1f)]
        [SerializeField] private float lockedAlpha = 0.3f;

        [Tooltip("ตอนปลดล็อก ชั้นเด้งขยายจากขนาดเท่านี้กลับมาปกติ")]
        [SerializeField] private float unlockPopFrom = 0.6f;

        [Tooltip("เวลาเด้งตอนปลดล็อก (วินาที)")]
        [SerializeField] private float unlockPopTime = 0.35f;

        [Header("Food Display Slots (จุดโชว์ที่วางอาหารบนโต๊ะ)")]
        [Tooltip("ตำแหน่งวางอาหาร/จานบนโต๊ะแต่ละช่อง (ลาก Transform มาใส่ แต่ละจุดคือจุดโชว์อาหารบนโต๊ะ)")]
        [SerializeField] private Transform[] plateSlots;

        [Tooltip("จุดกึ่งกลางสำหรับวางอาหารบนโต๊ะ (ถ้าไม่ได้กำหนด plateSlots จะวางเรียงรอบจุดนี้อัตโนมัติ)")]
        [SerializeField] private Transform foodDisplayPoint;

        [Tooltip("ตำแหน่งเยื้องสำหรับวางอาหารบนโต๊ะ (ถ้าไม่ได้กำหนด foodDisplayPoint)")]
        [SerializeField] private Vector2 foodDisplayOffset = new Vector2(0f, 0.2f);

        [Tooltip("ระยะห่างระหว่างอาหารแต่ละชิ้นที่วางเรียงบนโต๊ะ (เมตร) กรณีจัดเรียงอัตโนมัติ")]
        [SerializeField] private float foodDisplaySpacing = 0.45f;

        [Tooltip("วางอาหารบนโต๊ะได้ไม่จำกัด (ติ๊กถูก = ไม่สนใจ Max Plates)")]
        [SerializeField] private bool unlimitedFood = false;

        [Tooltip("จำนวนอาหารสูงสุดที่วางรอบนโต๊ะนี้ได้ เมื่อปิด Unlimited Food (ถ้ามี plateSlots จะใช้ตามจำนวนช่อง) เต็มแล้วของที่เหลือค้างในกระเป๋า")]
        [FormerlySerializedAs("maxFoodOnCounter")]
        [SerializeField] private int maxPlates = 20;

        [Tooltip("วางอาหารลงโต๊ะ 1 จานทุกกี่วินาที ขณะผู้เล่นยืนในวง (แบบ Juice King 0.15)")]
        [Min(0f)] [SerializeField] private float depositInterval = 0.15f;

        [Tooltip("เวลาที่จานลอยจากผู้เล่นมาลงโต๊ะ (วินาที)")]
        [Min(0.01f)] [SerializeField] private float depositFlyTime = 0.15f;

        [Header("Flat Display (เรียงอาหารบนโต๊ะแบบกองแบน ไม่ซ้อนสูง)")]
        [Tooltip("จำนวนจานต่อแถว")]
        [Min(1)] [SerializeField] private int displayColumns = 5;

        [Tooltip("ระยะห่างระหว่างจาน (x = ซ้ายขวา, y = แถวถัดไปขยับขึ้น)")]
        [SerializeField] private Vector2 displaySpacing = new Vector2(0.44f, 0.16f);

        [Tooltip("ความกว้างของจานบนโต๊ะ (หน่วยในโลก)")]
        [Min(0.05f)] [SerializeField] private float displayDishWidth = 0.38f;

        [Tooltip("โชว์จานบนโต๊ะสูงสุดกี่จาน (ที่เกินยังอยู่บนแผง แต่ซ่อนไว้ ดูจำนวนจริงที่ป้าย)")]
        [Min(1)] [SerializeField] private int maxVisibleDishes = 20;

        [Header("Table Art (โต๊ะมีรูปอาหาร แทนการวางจานทีละชิ้น)")]
        [Tooltip("ปิด = ไม่โชว์จานทีละชิ้นบนโต๊ะ (จานลอยมาลงแล้วหายเข้าโต๊ะ) ใช้รูปโต๊ะที่มีอาหาร + ป้ายจำนวนจานแทน")]
        [SerializeField] private bool showDishesOnTable = false;

        [Tooltip("รูปโต๊ะตอนไม่มีอาหาร (ว่าง = ใช้รูปที่ตั้งไว้ตอนเริ่ม)")]
        [SerializeField] private Sprite emptyTableSprite;

        [Tooltip("รูปโต๊ะตอนมีอาหารอย่างน้อย 1 จาน (ต้องขนาดภาพ/จุดหมุนเดียวกับรูปโต๊ะว่าง)")]
        [SerializeField] private Sprite stockedTableSprite;

        [Header("Stock Badge (ป้ายจำนวนอาหารบนแผง แบบ Juice King)")]
        [Tooltip("ป้ายวงรีบอกจำนวนอาหารที่เหลือบนแผง (ไอคอน + ตัวเลข)")]
        [SerializeField] private bool showStockBadge = true;

        [Tooltip("ตำแหน่งป้าย เยื้องจากกลางโต๊ะ (เมตร)")]
        [SerializeField] private Vector2 stockBadgeOffset = new Vector2(0f, -0.5f);

        [Tooltip("ขนาดป้าย (กว้าง x สูง เมตร)")]
        [SerializeField] private Vector2 stockBadgeSize = new Vector2(0.9f, 0.44f);

        [Tooltip("ไอคอนอาหารบนป้าย (ว่าง = ใช้ภาพอาหารของหม้อที่ทำเมนูนี้ หรือจานแรกที่วาง)")]
        [SerializeField] private Sprite stockIcon;

        [SerializeField] private Color stockBadgeColor = new Color(1f, 0.8f, 0.28f, 1f);
        [SerializeField] private Color stockBadgeOutlineColor = new Color(0.42f, 0.26f, 0.12f, 1f);
        [SerializeField] private Color stockTextColor = new Color(0.25f, 0.15f, 0.08f, 1f);

        [Tooltip("ลำดับการวาด (Sorting Order) ของอาหารขณะวางรอบนโต๊ะ เพื่อให้อยู่เหนือหน้าโต๊ะชัดเจน")]
        [SerializeField] private int foodDisplaySortingOrder = 4;

        [Header("Food Deposit Zone (วงวางอาหาร - สีขาว/ฟ้า)")]
        [Tooltip("จุดที่ให้ผู้เล่นยืนวางอาหาร (ถ้าว่างจะใช้ค่า Deposit Zone Offset)")]
        [SerializeField] private Transform foodDepositPoint;

        [Tooltip("ระยะเยื้องจากโต๊ะไปยังจุดวางอาหาร (เช่น (-1.0, -0.8) คืออยู่ด้านหน้าฝั่งซ้ายของโต๊ะ)")]
        [SerializeField] private Vector2 depositZoneOffset = new Vector2(-1.0f, -0.8f);

        [Tooltip("รัศมีของวงกลมวางอาหาร (เมตร)")]
        [SerializeField] private float depositZoneRadius = 0.65f;

        [Tooltip("ยืดเขตวงวางอาหารลงไปทางโต๊ะ (หน่วยโลก) ยืนชิดหลังโต๊ะลึกแค่ไหนก็ยังวางได้ ภาพวงไม่ขยับ (-1 = คิดเองจนถึงขอบโต๊ะ)")]
        [SerializeField] private float depositReachDown = -1f;

        [Tooltip("ระยะที่เดินเข้ามาใกล้โต๊ะแล้ววางอาหารได้ทันที (เมตร)")]
        [SerializeField] private float tableProximityRadius = 2.5f;

        [Tooltip("ถ้าติ๊กถูก ต้องกดปุ่ม E เพื่อวางอาหาร (ถ้าไม่ติ๊ก แค่เดินเข้ามาเหยียบวงหรือเดินชนโต๊ะจะวางอาหารลงโต๊ะทันที)")]
        [SerializeField] private bool requireKeyPress = false;

        [Tooltip("เปิดแสดงวงแหวนวางอาหารบนพื้นในหน้า Game")]
        [SerializeField] private bool showDepositRing = true;

        [Tooltip("สีของวงแหวนวางอาหารตอนปกติ (สีขาว)")]
        [SerializeField] private Color depositRingColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("สีของวงแหวนเมื่อผู้เล่นเข้ามายืนในโซน")]
        [SerializeField] private Color depositRingActiveColor = new Color(0.3f, 1f, 0.6f, 0.95f);

        [Tooltip("ลำดับการวาด (Sorting Order) ของวงแหวนวางอาหารบนพื้น")]
        [SerializeField] private int ringSortingOrder = 1;

        [Header("Audio & Effects")]
        [Tooltip("เสียงตอนวางอาหารลงโต๊ะ")]
        [SerializeField] private AudioClip depositSound;

        [Header("Customer Settings & Queue (ระบบต่อแถวเข้าคิวลูกค้า)")]
        [Tooltip("จุดที่ลูกค้าหัวแถวจะเดินมารอรับอาหาร")]
        [SerializeField] private Transform customerWaitPoint;

        [Tooltip("ทิศทางการต่อแถวของลูกค้าออกจากจุดรอหน้าโต๊ะ (เช่น (0, -1) คือต่อแถวถอยหลังลงมา)")]
        [SerializeField] private Vector2 queueDirection = new Vector2(0f, -1f);

        [Tooltip("ระยะห่างระหว่างลูกค้าแต่ละคนในแถว (เมตร)")]
        [SerializeField] private float queueSpacing = 0.9f;

        [Header("Crowd (ยืนมุงแทนต่อแถว)")]
        [Tooltip("ให้ลูกค้ายืนมุงเป็นกลุ่มรอบจุดรอ แทนการต่อแถวเป็นเส้นตรง (ลำดับการซื้อยังเรียงตามคนมาก่อน)")]
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

        [Tooltip("จำนวนลูกค้าสูงสุดที่สามารถยืนต่อคิวได้")]
        [SerializeField] private int maxQueueCapacity = 5;

        [Tooltip("จุดต่อคิวแบบกำหนดเอง (ถ้าต้องการระบุจุดยืนของแต่ละคิวเป๊ะๆ ลาก Transform มาใส่ได้ ถ้าปล่อยว่างจะคำนวณตามระยะห่างอัตโนมัติ)")]
        [SerializeField] private Transform[] customQueuePoints;

        [Header("Events")]
        public UnityEvent<int> onDishesCountChanged;

        // Visual Rings
        private GameObject depositRingObject;
        private SpriteRenderer depositRingRenderer;

        private static Sprite cachedRingSprite;

        // รายการอาหารที่วางรอบนโต๊ะ
        private readonly List<Item> dishesOnCounter = new List<Item>();
        private readonly Dictionary<Item, Vector3> foodWorldScales = new Dictionary<Item, Vector3>();
        private bool isPlayerInRange = false;
        private PlayerCarry cachedPlayerCarry;
        private Transform playerTransform;

        // ชั้นขายอาหารทุกอันในฉาก (ใช้ให้ลูกค้าเลือกชั้นตามเมนู)
        private static readonly List<FoodSellCounter> all = new List<FoodSellCounter>();
        public static IReadOnlyList<FoodSellCounter> All
        {
            get
            {
                all.RemoveAll(c => c == null);
                return all;
            }
        }


        // สถานะล็อก (ปลดพร้อมหม้อ)
        private bool lockStateApplied;
        private bool appliedOpen;
        private SpriteRenderer[] shelfRenderers;
        private Color[] shelfBaseColors;
        private bool[] shelfBaseRendererEnabled;
        private Collider2D[] shelfColliders;
        private bool[] shelfBaseColliderEnabled;
        private Vector3 baseLocalScale;
        private float unlockPopTimer = -1f;

        /// <summary>ชั้นเปิดขายแล้วหรือยัง (ไม่ได้ผูกหม้อ = เปิดตลอด)</summary>
        public bool IsOpen => unlockWithPot == null || unlockWithPot.IsUnlocked;
        public CookingPot UnlockWithPot => unlockWithPot;

        public IReadOnlyList<ItemType> AcceptedDishes => acceptedDishes;

        /// <summary>ชั้นนี้รับวาง/ขายอาหารประเภทนี้ไหม (ว่าง = รับทุกอย่าง, อาหารพิเศษจากโจรรับทุกชั้น)</summary>
        public bool Accepts(ItemType type)
        {
            if (type == ItemType.SpecialDish) return true;
            if (acceptedDishes == null || acceptedDishes.Count == 0) return true;
            return acceptedDishes.Contains(type);
        }

        /// <summary>มีชั้นที่ขายเมนูนี้อยู่ในฉากไหม</summary>
        public static bool AnyCounterSells(ItemType dish)
        {
            foreach (FoodSellCounter c in All)
            {
                if (c.isActiveAndEnabled && c.IsOpen && c.Accepts(dish)) return true;
            }
            return false;
        }

        /// <summary>
        /// เลือกชั้นให้ลูกค้า: ต้องขายเมนูที่อยากกิน คิวยังไม่เต็ม แล้วเลือกที่คนรอน้อยสุด
        /// ลูกค้าไม่เจาะจงเมนู (เช่น โจร) เลือกชั้นที่มีอาหารวางเยอะสุดก่อน
        /// คืน null ถ้าไม่มีชั้นที่ขายเมนูนี้เลย
        /// </summary>
        public static FoodSellCounter FindCounterFor(ItemType? dish)
        {
            FoodSellCounter best = null;
            float bestScore = float.PositiveInfinity;
            foreach (FoodSellCounter c in All)
            {
                if (!c.isActiveAndEnabled || !c.IsOpen) continue;
                if (dish.HasValue && !c.Accepts(dish.Value)) continue;

                float score = c.CurrentQueueCount;
                if (c.IsQueueFull) score += 1000f;
                if (!dish.HasValue) score -= c.DishesCount * 10f;
                score += Random.value * 0.5f; // เสมอกันสุ่มเอา

                if (score < bestScore)
                {
                    bestScore = score;
                    best = c;
                }
            }
            return best;
        }

        public int DishesCount => dishesOnCounter.Count;

        /// <summary>ชั้นยังวางอาหารได้อีกกี่จาน (ให้พนักงานเสิร์ฟรู้ว่าควรยกมากี่จาน)</summary>
        public int FreePlates
        {
            get
            {
                if (unlimitedFood) return int.MaxValue;
                dishesOnCounter.RemoveAll(d => d == null);
                return Mathf.Max(0, MaxPlates - dishesOnCounter.Count);
            }
        }
        public int MaxPlates => (plateSlots != null && plateSlots.Length > 0) ? plateSlots.Length : maxPlates;

        /// <summary>เปลี่ยนจำนวนอาหารสูงสุดบนแผง (อัปเกรด Shelf Size จากร้าน)</summary>
        public void SetMaxPlates(int capacity)
        {
            maxPlates = Mathf.Max(1, capacity);
        }
        public Transform CustomerWaitPoint => customerWaitPoint != null ? customerWaitPoint : transform;

        // คิวของลูกค้าที่กำลังยืนรอซื้ออาหาร
        private readonly List<CustomerAI> customerQueue = new List<CustomerAI>();

        public int CurrentQueueCount
        {
            get
            {
                customerQueue.RemoveAll(c => c == null);
                return customerQueue.Count;
            }
        }

        public bool IsQueueFull => CurrentQueueCount >= maxQueueCapacity;

        /// <summary>
        /// ให้ลูกค้าลงทะเบียนเข้าแถวรอคิว
        /// </summary>
        public bool TryJoinQueue(CustomerAI customer)
        {
            if (customer == null) return false;
            customerQueue.RemoveAll(c => c == null);

            if (customerQueue.Contains(customer)) return true;

            if (customerQueue.Count >= maxQueueCapacity) return false;

            customerQueue.Add(customer);
            return true;
        }

        /// <summary>
        /// นำลูกค้าออกจากแถว (เมื่อซื้อเสร็จ หรือหมดความอดทน)
        /// </summary>
        public void LeaveQueue(CustomerAI customer)
        {
            if (customer == null) return;
            crowdSlots.Release(customer);
            customerQueue.Remove(customer);
            customerQueue.RemoveAll(c => c == null);
        }

        /// <summary>
        /// หาตำแหน่งในคิวของลูกค้า (Index 0 คือหัวแถว)
        /// </summary>
        public int GetQueueIndex(CustomerAI customer)
        {
            customerQueue.RemoveAll(c => c == null);
            return customerQueue.IndexOf(customer);
        }

        /// <summary>
        /// คำนวณตำแหน่งใน World Space ที่ลูกค้าคนนี้ควรยืนต่อแถว
        /// </summary>
        public Vector3 GetQueuePosition(CustomerAI customer)
        {
            int index = GetQueueIndex(customer);
            if (index < 0)
            {
                if (TryJoinQueue(customer))
                {
                    index = GetQueueIndex(customer);
                }
                else
                {
                    index = customerQueue.Count;
                }
            }

            // ถ้ากำหนด customQueuePoints ไว้ ให้ใช้ตาม Transform ที่ระบุ
            if (customQueuePoints != null && index < customQueuePoints.Length && customQueuePoints[index] != null)
            {
                return customQueuePoints[index].position;
            }

            // คำนวณตามทิศทางและระยะห่างอัตโนมัติ
            Vector3 basePoint = CustomerWaitPoint.position;
            Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;

            // ยืนมุงเป็นกลุ่มก้อนรอบจุดรอ
            if (crowdMode)
            {
                int slot = crowdSlots.GetSlot(customer);
                return basePoint + (Vector3)CrowdLayout.GetSlotOffset(slot, dir, crowdColumns, crowdSpacing, crowdRowSpacing, crowdJitter);
            }

            return basePoint + (Vector3)(dir * (index * queueSpacing));
        }

        /// <summary>
        /// ลูกค้าคนนี้หยิบอาหารได้ตอนนี้หรือไม่
        /// โหมดต่อแถว: ต้องเป็นหัวแถว
        /// โหมดยืนมุง: ไม่ต้องเป็นหัวแถว ใครก็หยิบได้ถ้ามีอาหารที่อยากกินวางอยู่ โดยคนที่ยืนใกล้เคาน์เตอร์ที่สุดหยิบก่อน
        /// (ไม่งั้นคนหน้าสุดที่อยากกินเมนูที่ยังไม่มี จะขวางทุกคนที่อยากกินเมนูที่มีอยู่)
        /// </summary>
        public bool CanTakeNow(CustomerAI customer)
        {
            if (customer == null) return false;
            if (!crowdMode) return IsHeadOfQueue(customer);

            customerQueue.RemoveAll(c => c == null);
            if (!customerQueue.Contains(customer)) return false;

            int mySlot = crowdSlots.GetSlot(customer);
            float myPatience = customer.RemainingPatience;
            foreach (CustomerAI other in customerQueue)
            {
                if (other == null || other == customer) continue;
                if (other.State != CustomerState.WaitingAtCounter) continue;
                if (!HasFoodAvailable(other.RequestedDish)) continue;

                // คนที่ความอดทนเหลือน้อยกว่า (ใกล้โมโหเดินหนี) ได้ก่อน เท่ากันแล้วค่อยให้คนที่ยืนใกล้เคาน์เตอร์กว่า
                float otherPatience = other.RemainingPatience;
                if (otherPatience < myPatience - 0.01f) return false;
                if (Mathf.Abs(otherPatience - myPatience) <= 0.01f && crowdSlots.GetSlot(other) < mySlot) return false;
            }
            return true;
        }

        /// <summary>
        /// เช็คว่าลูกค้าคนนี้เป็นหัวแถว (ลำดับที่ 0) หรือไม่
        /// </summary>
        public bool IsHeadOfQueue(CustomerAI customer)
        {
            customerQueue.RemoveAll(c => c == null);
            return customerQueue.Count > 0 && customerQueue[0] == customer;
        }

        private void Awake()
        {
            SetupCounterSolidCollider();
            SetupDepositZoneAndRing();
        }

        private void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
            SetupCounterSolidCollider();
            SetupDepositZoneAndRing();
        }

        private void OnDisable()
        {
            all.Remove(this);
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            baseLocalScale = transform.localScale;
            // ชั้นที่ไม่ได้ผูกหม้อ ไม่ต้องยุ่งอะไรเลย (เปิดตลอด)
            if (unlockWithPot != null) ApplyLockState(IsOpen, false);
        }

        /// <summary>
        /// ล็อก: ชั้นจาง (หรือซ่อน) ปิด Collider ทั้งหมด ซ่อนวงวางอาหาร / ปลด: คืนทุกอย่าง แล้วเด้งขยายให้รู้ว่าเปิดแล้ว
        /// ทำเฉพาะตอนเล่น (ไม่แตะค่าในฉาก)
        /// </summary>
        private void ApplyLockState(bool open, bool pop)
        {
            if (shelfRenderers == null)
            {
                var list = new List<SpriteRenderer>();
                foreach (SpriteRenderer r in GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (r == null || r == depositRingRenderer) continue;
                    if (r.GetComponentInParent<Item>() != null) continue; // อาหารบนชั้นไม่นับ
                    list.Add(r);
                }
                shelfRenderers = list.ToArray();
                shelfBaseColors = new Color[shelfRenderers.Length];
                shelfBaseRendererEnabled = new bool[shelfRenderers.Length];
                for (int i = 0; i < shelfRenderers.Length; i++)
                {
                    shelfBaseColors[i] = shelfRenderers[i].color;
                    shelfBaseRendererEnabled[i] = shelfRenderers[i].enabled; // จุด marker ที่ปิดไว้ (Food Point / Circle) ต้องปิดต่อ
                }

                var cols = new List<Collider2D>();
                foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
                {
                    if (c != null && c.GetComponentInParent<Item>() == null) cols.Add(c);
                }
                shelfColliders = cols.ToArray();
                shelfBaseColliderEnabled = new bool[shelfColliders.Length];
                for (int i = 0; i < shelfColliders.Length; i++) shelfBaseColliderEnabled[i] = shelfColliders[i].enabled;
            }

            for (int i = 0; i < shelfRenderers.Length; i++)
            {
                if (shelfRenderers[i] == null) continue;
                Color c = shelfBaseColors[i];
                if (!open) c.a *= lockedAlpha;
                shelfRenderers[i].color = c;
                shelfRenderers[i].enabled = shelfBaseRendererEnabled[i] && (open || lockedAlpha > 0.001f);
            }
            for (int i = 0; i < shelfColliders.Length; i++)
            {
                if (shelfColliders[i] != null) shelfColliders[i].enabled = shelfBaseColliderEnabled[i] && open;
            }
            if (depositRingObject != null) depositRingObject.SetActive(open);

            if (!open) isPlayerInRange = false;

            lockStateApplied = true;
            appliedOpen = open;

            if (open && pop && unlockPopTime > 0f)
            {
                unlockPopTimer = 0f;
                transform.localScale = baseLocalScale * unlockPopFrom;
            }
        }

        private void UpdateLockState()
        {
            if (unlockWithPot == null && !lockStateApplied) return;
            bool open = IsOpen;
            if (!lockStateApplied || open != appliedOpen) ApplyLockState(open, lockStateApplied);

            if (unlockPopTimer >= 0f)
            {
                unlockPopTimer += Time.deltaTime;
                float k = Mathf.Clamp01(unlockPopTimer / unlockPopTime);
                transform.localScale = baseLocalScale * Mathf.LerpUnclamped(unlockPopFrom, 1f, Tween.Evaluate(Ease.OutBack, k));
                if (k >= 1f)
                {
                    transform.localScale = baseLocalScale;
                    unlockPopTimer = -1f;
                }
            }
        }

        private void OnValidate()
        {
            if (depositZoneRadius < 0.1f) depositZoneRadius = 0.1f;
            if (Application.isPlaying)
            {
                SetupDepositZoneAndRing();
            }
        }

        /// <summary>
        /// ทำให้ตัวเคาน์เตอร์ขายอาหารเป็นของแข็ง (Solid) เดินชนแล้วไม่ทะลุ
        /// </summary>
        private void SetupCounterSolidCollider()
        {
            Collider2D mainCol = GetComponent<Collider2D>();
            if (mainCol != null)
            {
                mainCol.isTrigger = false;
            }

            if (customerWaitPoint == null)
            {
                Transform existingWait = transform.Find("CustomerWaitPoint");
                if (existingWait != null)
                {
                    customerWaitPoint = existingWait;
                }
                else
                {
                    var point = new GameObject("CustomerWaitPoint");
                    point.transform.SetParent(transform);
                    point.transform.localPosition = new Vector3(0f, -1f, 0f);
                    customerWaitPoint = point.transform;
                }
            }
        }

        /// <summary>
        /// สร้างโซนและวงแหวนบนพื้นสำหรับวางอาหาร (มองเห็นได้ทั้งหน้า Game และ Scene)
        /// </summary>
        public void SetupDepositZoneAndRing()
        {
            Transform existingZone = transform.Find("FoodDepositZone");
            if (existingZone != null)
            {
                depositRingObject = existingZone.gameObject;
            }
            else
            {
                depositRingObject = new GameObject("FoodDepositZone");
                depositRingObject.transform.SetParent(transform);
            }

            depositRingObject.transform.position = GetDepositPosition();
            depositRingObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = depositZoneRadius * 2f;
            depositRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            // 1. Trigger Collider ตรวจจับการเดินเข้ามาของผู้เล่น
            CircleCollider2D circleCol = depositRingObject.GetComponent<CircleCollider2D>();
            if (circleCol == null)
            {
                circleCol = depositRingObject.AddComponent<CircleCollider2D>();
            }
            circleCol.isTrigger = true;
            circleCol.radius = 0.5f;
            circleCol.offset = Vector2.zero;

            // 2. Proxy รับ Trigger Event (เฉพาะขณะเล่นเกม เพื่อไม่ให้ AddComponent ลง Prefab ในโหมด Editor)
            if (Application.isPlaying)
            {
                SellCounterZoneProxy proxy = depositRingObject.GetComponent<SellCounterZoneProxy>();
                if (proxy == null)
                {
                    proxy = depositRingObject.AddComponent<SellCounterZoneProxy>();
                }
                proxy.counter = this;
            }

            // 3. Sprite Renderer แสดงวงแหวนสีขาว
            depositRingRenderer = depositRingObject.GetComponent<SpriteRenderer>();
            if (showDepositRing)
            {
                if (depositRingRenderer == null)
                {
                    depositRingRenderer = depositRingObject.AddComponent<SpriteRenderer>();
                }
                depositRingRenderer.enabled = true;
                depositRingRenderer.sprite = GetRingSprite();
                depositRingRenderer.color = isPlayerInRange ? depositRingActiveColor : depositRingColor;

                SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
                if (mainRenderer != null)
                {
                    if (mainRenderer.sharedMaterial != null)
                    {
                        depositRingRenderer.sharedMaterial = mainRenderer.sharedMaterial;
                    }
                    depositRingRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                }
                depositRingRenderer.sortingOrder = ringSortingOrder;
            }
            else if (depositRingRenderer != null)
            {
                depositRingRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                UpdateEditorVisuals();
                return;
            }

            // ล็อก/ปลดพร้อมหม้อ: ยังล็อกอยู่ก็ไม่รับของ
            UpdateLockState();
            UpdateStockBadge();
            if (!IsOpen) return;

            // แอนิเมชันของวงแหวน
            UpdateVisualAnimations();
            UpdateFlyIns();

            // ตรวจหา Player ในระยะใกล้เคียงโต๊ะ
            CheckProximityPlayer();

            // ตรวจจับการส่งอาหารอัตโนมัติขณะผู้เล่นยืนอยู่ในโซนหรือใกล้โต๊ะ (ทีละจาน)
            if (isPlayerInRange && cachedPlayerCarry != null)
            {
                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    TryTimedDeposit(cachedPlayerCarry);
                }
            }
        }

        private float nextDepositTime;

        /// <summary>วางอาหาร 1 จานทุก depositInterval วินาที (depositInterval = 0 = วางหมดทีเดียวแบบเดิม)</summary>
        private void TryTimedDeposit(PlayerCarry playerCarry)
        {
            if (depositInterval <= 0f)
            {
                DepositFoodFromPlayer(playerCarry);
                return;
            }
            if (Time.time < nextDepositTime) return;
            if (DepositFoodFromPlayer(playerCarry, 1) > 0) nextDepositTime = Time.time + depositInterval;
        }

        private void CheckProximityPlayer()
        {
            if (cachedPlayerCarry == null)
            {
#if UNITY_2023_1_OR_NEWER
                cachedPlayerCarry = Object.FindAnyObjectByType<PlayerCarry>();
#else
                cachedPlayerCarry = FindObjectOfType<PlayerCarry>();
#endif
                if (cachedPlayerCarry != null)
                {
                    playerTransform = cachedPlayerCarry.transform;
                }
                else
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null)
                    {
                        playerTransform = p.transform;
                        cachedPlayerCarry = p.GetComponentInParent<PlayerCarry>();
                    }
                }
            }

            if (cachedPlayerCarry != null && playerTransform == null)
            {
                playerTransform = cachedPlayerCarry.transform;
            }

            if (playerTransform != null)
            {
                float distToDeposit = YSort.DistanceToRing(YSort.FeetOf(playerTransform), GetDepositPosition(),
                    depositReachDown >= 0f ? depositReachDown : YSort.ReachDownToSolid(gameObject, GetDepositPosition()));
                float distToTable = Vector2.Distance(transform.position, playerTransform.position);

                if (distToDeposit <= depositZoneRadius || distToTable <= tableProximityRadius)
                {
                    isPlayerInRange = true;
                }
                else if (distToDeposit > depositZoneRadius * 1.4f && distToTable > tableProximityRadius * 1.4f)
                {
                    isPlayerInRange = false;
                }
            }
            else
            {
                // Fallback: ตรวจจับ Collider รอบโต๊ะในกรณีไม่มี Reference
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, tableProximityRadius);
                bool found = false;
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    var pc = h.GetComponentInParent<PlayerCarry>();
                    if (pc != null)
                    {
                        cachedPlayerCarry = pc;
                        playerTransform = pc.transform;
                        isPlayerInRange = true;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    isPlayerInRange = false;
                }
            }
        }

        private void UpdateEditorVisuals()
        {
            if (depositRingObject == null || depositRingRenderer == null || depositRingRenderer.sprite == null)
            {
                SetupDepositZoneAndRing();
            }

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            if (depositRingObject != null)
            {
                depositRingObject.transform.position = GetDepositPosition();
                float d1 = depositZoneRadius * 2f;
                depositRingObject.transform.localScale = new Vector3(d1 / pX, d1 / pY, 1f);
            }
        }

        private void UpdateVisualAnimations()
        {
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            // ตรวจสอบ Sprite เสมอให้ไม่ว่างเปล่า
            if (depositRingRenderer != null && depositRingRenderer.sprite == null)
            {
                depositRingRenderer.sprite = GetRingSprite();
            }

            // วงแหวนวางอาหาร (Pulse และเปลี่ยนสีเมื่อผู้เล่นยืน)
            if (depositRingRenderer != null && showDepositRing && depositRingObject != null)
            {
                Color targetCol = isPlayerInRange ? depositRingActiveColor : depositRingColor;
                depositRingRenderer.color = Color.Lerp(depositRingRenderer.color, targetCol, Time.deltaTime * 10f);

                float pulse1 = 1f + Mathf.Sin(Time.time * 2.5f) * 0.04f;
                float d1 = depositZoneRadius * 2f * pulse1;
                depositRingObject.transform.position = GetDepositPosition();
                depositRingObject.transform.localScale = new Vector3(d1 / pX, d1 / pY, 1f);
            }
        }

        private bool WasInteractKeyPressed()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                pressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                pressed = true;
            }
#endif
            return pressed;
        }

        /// <summary>
        /// รับอาหารสุกจากผู้เล่นมาวางรอบนโต๊ะ (ไม่ต้องใช้จาน อาหารจะวางเรียงรอบนโต๊ะให้ลูกค้ามาซื้อ)
        /// </summary>
        public int DepositFoodFromPlayer(PlayerCarry playerCarry, int maxCount = int.MaxValue)
        {
            if (playerCarry == null || !IsOpen) return 0;

            // กวาดอาหารที่เป็น null ออกไปก่อน (หากมี)
            dishesOnCounter.RemoveAll(d => d == null);

            int depositedCount = 0;

            while (depositedCount < maxCount && (unlimitedFood || dishesOnCounter.Count < MaxPlates))
            {
                // ดึงอาหารสุกที่ชั้นนี้ขาย (ชั้นหมูรับแค่หมู ชั้นไก่รับแค่ไก่ อาหารพิเศษรับทุกชั้น) ชิ้นบนสุดก่อน
                Item cookedItem = null;
                for (int i = playerCarry.CarriedItems.Count - 1; i >= 0; i--)
                {
                    Item it = playerCarry.CarriedItems[i];
                    if (it != null && PlayerCarry.IsCookedFood(it) && Accepts(it.Type))
                    {
                        cookedItem = playerCarry.PopItemOfType(it.Type);
                        break;
                    }
                }

                if (cookedItem != null)
                {
                    depositedCount++;
                    PlaceFoodOnCounter(cookedItem);
                }
                else
                {
                    break;
                }
            }

            if (depositedCount > 0)
            {
                AudioManager.PlayBurst(SoundId.FoodPlace, depositedCount, 0.06f, depositSound, transform.position, 0.04f);
            }

            return depositedCount;
        }

        /// <summary>
        /// วางอาหารลงบนโต๊ะโดยตรง (ไม่ต้องใช้จาน)
        /// </summary>
        public void PlaceFoodOnCounter(Item dish)
        {
            if (dish == null) return;

            // จำขนาดในโลกตอนแบกไว้ เพื่อวางบนโต๊ะด้วยขนาดเท่าเดิม (ใช้กับ plateSlots)
            foodWorldScales[dish] = dish.transform.lossyScale;
            Vector3 startPos = dish.transform.position;

            dishesOnCounter.Add(dish);
            dish.gameObject.SetActive(true);
            dish.SetCarriedState(true);
            dish.transform.SetParent(transform);

            // ลอยจากตัวผู้เล่นมาลงช่องบนโต๊ะ
            if (depositFlyTime > 0f && (plateSlots == null || plateSlots.Length == 0))
            {
                flyingIn[dish] = new FlyIn { start = startPos, startTime = Time.time };
            }

            RearrangeFoodOnCounter();
            onDishesCountChanged?.Invoke(dishesOnCounter.Count);
        }

        /// <summary>
        /// จัดเรียงอาหารทั้งหมดที่วางรอบนโต๊ะให้สวยงามและเป็นระเบียบ
        /// </summary>
        private void RearrangeFoodOnCounter()
        {
            SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            bool hasPlateSlots = plateSlots != null && plateSlots.Length > 0;
            int count = dishesOnCounter.Count;

            for (int i = 0; i < count; i++)
            {
                Item dish = dishesOnCounter[i];
                if (dish == null) continue;

                if (hasPlateSlots && i < plateSlots.Length && plateSlots[i] != null)
                {
                    // 1. วางตามจุดโชว์ plateSlots ที่กำหนดไว้ใน Inspector
                    dish.transform.SetParent(plateSlots[i]);
                    dish.transform.localPosition = new Vector3(0f, 0f, -0.05f);
                    dish.transform.localRotation = Quaternion.identity;
                    dish.transform.localScale = Vector3.one * 0.85f;
                }
                else
                {
                    // 2. กรณีไม่มี plateSlots เรียงเป็นกองแบนบนโต๊ะ (แถวหน้ากระดาน ไม่ซ้อนสูง)
                    dish.transform.SetParent(transform);
                    dish.transform.localRotation = Quaternion.identity;
                    SpriteRenderer dishSr = dish.SpriteRenderer;
                    dish.transform.localScale = dishSr != null
                        ? FlatPile.LocalScaleForWidth(dish.transform, dishSr, displayDishWidth)
                        : new Vector3(0.85f / pX, 0.85f / pY, 1f);

                    // จานที่กำลังลอยมาลงโต๊ะ: Update เป็นคนขยับให้
                    if (!flyingIn.ContainsKey(dish)) dish.transform.position = GetDisplaySlotPosition(i);
                }

                // ปิด Collider และ Rigidbody ขณะวางรอบนโต๊ะเพื่อไม่ให้ชนตัวละคร
                if (dish.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
                if (dish.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.simulated = false;
                    rb.linearVelocity = Vector2.zero;
                }

                if (dish.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    if (tableRenderer != null)
                    {
                        sr.sortingLayerID = tableRenderer.sortingLayerID;
                        // กองแบน: แถวหน้าวาดทับแถวหลัง
                        sr.sortingOrder = Mathf.Max(tableRenderer.sortingOrder + 2, foodDisplaySortingOrder) + FlatPile.SortingOffset(i, displayColumns, maxVisibleDishes);
                    }
                    else
                    {
                        sr.sortingOrder = foodDisplaySortingOrder + FlatPile.SortingOffset(i, displayColumns, maxVisibleDishes);
                    }
                    // โชว์เฉพาะจานในจำนวนที่กำหนด (จานที่กำลังลอยมายังเห็นจนกว่าจะถึง)
                    sr.enabled = showDishesOnTable
                        ? (hasPlateSlots || i < maxVisibleDishes || flyingIn.ContainsKey(dish))
                        : flyingIn.ContainsKey(dish);
                }
            }

            UpdateTableSprite();
        }

        /// <summary>สลับรูปโต๊ะ: มีอาหาร = โต๊ะมีรูปอาหาร, หมด = โต๊ะเปล่า</summary>
        private void UpdateTableSprite()
        {
            if (stockedTableSprite == null) return;
            SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
            if (tableRenderer == null) return;
            if (emptyTableSprite == null) emptyTableSprite = tableRenderer.sprite;
            Sprite target = dishesOnCounter.Count > 0 ? stockedTableSprite : emptyTableSprite;
            if (tableRenderer.sprite != target) tableRenderer.sprite = target;
        }

        // ---------- ป้ายจำนวนอาหารบนแผง ----------

        private GameObject stockBadgeRoot;
        private WorldCountBadge stockBadge;
        private Color stockIconColor = Color.white;
        private int lastStockCount = -1;

        private void UpdateStockBadge()
        {
            bool show = showStockBadge && IsOpen;
            if (!show)
            {
                if (stockBadgeRoot != null && stockBadgeRoot.activeSelf) stockBadgeRoot.SetActive(false);
                return;
            }

            if (stockBadgeRoot == null || stockBadge == null)
            {
                // หลังคอมไพล์ใน Editor ตัวป้ายหาย (ไม่ได้เซฟ) แต่ GameObject ยังอยู่: ลบทิ้งแล้วสร้างใหม่
                if (stockBadgeRoot != null)
                {
                    if (Application.isPlaying) Destroy(stockBadgeRoot); else DestroyImmediate(stockBadgeRoot);
                }
                SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
                int layer = tableRenderer != null ? tableRenderer.sortingLayerID : 0;
                int order = (tableRenderer != null ? tableRenderer.sortingOrder : 0) + 60;
                stockBadgeRoot = new GameObject("StockBadgeRoot");
                stockBadgeRoot.transform.SetParent(transform, false);
                YSort.PutOnTop(stockBadgeRoot.gameObject);   // ป้ายจำนวนอาหารอยู่บนสุดเสมอ
                stockBadge = WorldCountBadge.Create(stockBadgeRoot.transform, "StockBadge", stockBadgeSize,
                    stockBadgeColor, stockBadgeOutlineColor, stockTextColor, layer, order);
                stockBadge.Root.transform.localPosition = new Vector3(stockBadgeOffset.x, stockBadgeOffset.y, 0f);
            }
            if (!stockBadgeRoot.activeSelf) stockBadgeRoot.SetActive(true);

            WorldUi.KeepWorldScale(stockBadgeRoot.transform);
            stockBadgeRoot.transform.position = transform.position + new Vector3(0f, 0f, -0.1f);
            stockBadgeRoot.transform.rotation = Quaternion.identity;

            // ไอคอน: ที่ตั้งไว้ > จานที่อยู่บนแผง > อาหารของหม้อที่ทำเมนูที่แผงนี้ขาย
            if (stockIcon == null)
            {
                foreach (Item d in dishesOnCounter)
                {
                    if (d != null && d.SpriteRenderer != null && d.SpriteRenderer.sprite != null)
                    {
                        stockIcon = d.SpriteRenderer.sprite;
                        stockIconColor = d.SpriteRenderer.color;
                        break;
                    }
                }
                if (stockIcon == null)
                {
                    foreach (CookingPot pot in CookingPot.AllPots)
                    {
                        if (pot == null || !Accepts(pot.OutputDishType)) continue;
                        if (acceptedDishes.Count == 0 && unlockWithPot != null && pot != unlockWithPot) continue;
                        if (pot.TryGetDishIcon(out Sprite s, out Color c))
                        {
                            stockIcon = s;
                            stockIconColor = c;
                            break;
                        }
                    }
                }
            }

            int count = DishesCount;
            stockBadge.Set(stockIcon, stockIconColor, count);
            if (lastStockCount >= 0 && count > lastStockCount) stockBadge.Punch();
            lastStockCount = count;
            stockBadge.Tick();
        }

        // ---------- จานที่กำลังลอยจากผู้เล่นมาลงโต๊ะ ----------

        private struct FlyIn
        {
            public Vector3 start;
            public float startTime;
        }

        private readonly Dictionary<Item, FlyIn> flyingIn = new Dictionary<Item, FlyIn>();
        private readonly List<Item> flyingDone = new List<Item>();

        /// <summary>ตำแหน่งช่องที่ index บนโต๊ะ (กองแบน)</summary>
        private Vector3 GetDisplaySlotPosition(int index)
        {
            // จานที่เกินจำนวนโชว์ ซ้อนอยู่ที่ช่องสุดท้าย (ถูกซ่อนไว้)
            index = Mathf.Min(index, Mathf.Max(1, maxVisibleDishes) - 1);
            Vector2 off = FlatPile.Offset(index, displayColumns, displaySpacing);
            return GetFoodDisplayCenterPosition() + new Vector3(off.x, off.y, -0.05f);
        }

        private void UpdateFlyIns()
        {
            if (flyingIn.Count == 0) return;
            flyingDone.Clear();
            foreach (var kv in flyingIn)
            {
                Item dish = kv.Key;
                int index = dish != null ? dishesOnCounter.IndexOf(dish) : -1;
                if (index < 0) { flyingDone.Add(dish); continue; }

                float k = Mathf.Clamp01((Time.time - kv.Value.startTime) / Mathf.Max(0.01f, depositFlyTime));
                Vector3 slot = GetDisplaySlotPosition(index);
                Vector3 p = Vector3.Lerp(kv.Value.start, slot, Tween.Evaluate(Ease.OutQuad, k));
                p.y += Mathf.Sin(k * Mathf.PI) * 0.3f;
                dish.transform.position = p;
                if (k >= 1f) flyingDone.Add(dish);
            }
            foreach (Item d in flyingDone)
            {
                flyingIn.Remove(d);
                if (d != null)
                {
                    int index = dishesOnCounter.IndexOf(d);
                    if (index >= 0)
                    {
                        d.transform.position = GetDisplaySlotPosition(index);
                        if ((!showDishesOnTable || index >= maxVisibleDishes) && d.SpriteRenderer != null) d.SpriteRenderer.enabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// ลูกค้าหยิบอาหารจากโต๊ะไปถือ (ยังไม่จ่ายเงิน) แล้วค่อยไปจ่ายที่โต๊ะจ่ายเงิน (CashierCounter)
        /// </summary>
        public Item CustomerTakeDish(ItemType? preferredType = null)
        {
            Item selectedDish = RemoveDishForCustomer(preferredType);

            if (selectedDish != null)
            {
                AudioManager.Play(SoundId.FoodPlace, depositSound, transform.position);
            }

            return selectedDish;
        }

        /// <summary>
        /// เลือกและเอาอาหารออกจากโต๊ะให้ลูกค้า (อาหารพิเศษก่อน > เมนูที่อยากกิน > ชิ้นแรกถ้าไม่เจาะจง)
        /// </summary>
        private Item RemoveDishForCustomer(ItemType? preferredType)
        {
            // กวาดอาหารที่ถูกทำลายไปแล้วออกก่อน กัน NullReference ตอนเช็ค Type
            dishesOnCounter.RemoveAll(d => d == null);
            if (dishesOnCounter.Count == 0) return null;

            Item selectedDish = null;

            // 1. ถ้ามีอาหารพิเศษ (SpecialDish) บนโต๊ะ ให้ขายอาหารพิเศษก่อน (ราคาแพง x10)
            for (int i = 0; i < dishesOnCounter.Count; i++)
            {
                if (dishesOnCounter[i] != null && dishesOnCounter[i].Type == ItemType.SpecialDish)
                {
                    selectedDish = dishesOnCounter[i];
                    dishesOnCounter.RemoveAt(i);
                    break;
                }
            }

            // 2. ถ้าไม่มีอาหารพิเศษ ให้หาอาหารตามที่ลูกค้าอยากทาน
            if (selectedDish == null && preferredType.HasValue)
            {
                for (int i = 0; i < dishesOnCounter.Count; i++)
                {
                    if (dishesOnCounter[i] != null && dishesOnCounter[i].Type == preferredType.Value)
                    {
                        selectedDish = dishesOnCounter[i];
                        dishesOnCounter.RemoveAt(i);
                        break;
                    }
                }
            }

            // 3. ถ้าลูกค้าไม่เจาะจงเมนู ให้หยิบชิ้นแรกบนโต๊ะ (ถ้าเจาะจงแล้วไม่มีเมนูนั้น จะไม่ขายอย่างอื่นให้)
            if (selectedDish == null && !preferredType.HasValue && dishesOnCounter.Count > 0)
            {
                selectedDish = dishesOnCounter[0];
                dishesOnCounter.RemoveAt(0);
            }

            if (selectedDish != null)
            {
                selectedDish.transform.SetParent(null);
                foodWorldScales.Remove(selectedDish);
                flyingIn.Remove(selectedDish);
                if (selectedDish.SpriteRenderer != null) selectedDish.SpriteRenderer.enabled = true;

                // จัดตำแหน่งอาหารที่เหลือบนโต๊ะใหม่
                RearrangeFoodOnCounter();
                onDishesCountChanged?.Invoke(dishesOnCounter.Count);
            }

            return selectedDish;
        }

        /// <summary>
        /// โจรฉกอาหารบนโต๊ะไป (กินไม่จ่าย / ไม่มีการเสกเงินออกมา)
        /// </summary>
        public Item ThiefStealDish(CustomerAI thief = null)
        {
            dishesOnCounter.RemoveAll(d => d == null);
            if (dishesOnCounter.Count == 0) return null;

            // ฉกอาหารชิ้นแรกบนโต๊ะ
            Item stolenDish = dishesOnCounter[0];
            dishesOnCounter.RemoveAt(0);

            if (stolenDish != null)
            {
                stolenDish.transform.SetParent(null);
                flyingIn.Remove(stolenDish);
                if (stolenDish.SpriteRenderer != null) stolenDish.SpriteRenderer.enabled = true;   // จานที่ซ่อนอยู่ในโต๊ะ ให้เห็นตอนโจรถือไป
                RearrangeFoodOnCounter();
                onDishesCountChanged?.Invoke(dishesOnCounter.Count);
            }

            return stolenDish;
        }

        public bool HasFoodAvailable(ItemType? preferredType = null)
        {
            if (dishesOnCounter.Count == 0) return false;
            if (!preferredType.HasValue) return true;

            foreach (var dish in dishesOnCounter)
            {
                if (dish != null && (dish.Type == preferredType.Value || dish.Type == ItemType.SpecialDish)) return true;
            }
            return false;
        }

        /// <summary>จำนวนจานบนแผงที่ขายให้ลูกค้าที่อยากกินเมนูนี้ได้ (null = ทุกจาน)</summary>
        public int CountDishes(ItemType? type = null)
        {
            if (!type.HasValue) return dishesOnCounter.Count;
            int n = 0;
            foreach (var dish in dishesOnCounter)
            {
                if (dish != null && (dish.Type == type.Value || dish.Type == ItemType.SpecialDish)) n++;
            }
            return n;
        }

        // ========================== ตำแหน่งและ Ring Sprite ==========================

        public Vector3 GetFoodDisplayCenterPosition()
        {
            if (foodDisplayPoint != null)
            {
                return foodDisplayPoint.position;
            }
            return transform.position + new Vector3(foodDisplayOffset.x, foodDisplayOffset.y, 0f);
        }

        public Vector3 GetDepositPosition()
        {
            if (foodDepositPoint != null)
            {
                return foodDepositPoint.position;
            }
            return transform.position + new Vector3(depositZoneOffset.x, depositZoneOffset.y, 0f);
        }

        private static Sprite generatedRingSprite;

        private Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

#if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefab/Shop/FloorStandRing.png");
            if (loaded != null)
            {
                cachedRingSprite = loaded;
                return cachedRingSprite;
            }
#endif
            return GetOrCreateRingSprite();
        }

        /// <summary>
        /// สร้าง Sprite วงแหวนสีขาวความละเอียดสูงแบบเนียนตา (Anti-aliased) อัตโนมัติในหน่วยความจำ
        /// </summary>
        private static Sprite GetOrCreateRingSprite()
        {
            if (generatedRingSprite != null) return generatedRingSprite;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = (size * 0.5f) - 6f;
            float thickness = 16f;
            float innerR = outerR - thickness;

            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = 0f;

                    if (dist >= innerR && dist <= outerR)
                    {
                        alpha = 1f;
                    }
                    else if (dist > outerR && dist <= outerR + 2.5f)
                    {
                        alpha = Mathf.Clamp01(1f - (dist - outerR) / 2.5f);
                    }
                    else if (dist < innerR && dist >= innerR - 2.5f)
                    {
                        alpha = Mathf.Clamp01((dist - (innerR - 2.5f)) / 2.5f);
                    }
                    else if (dist < innerR - 2.5f)
                    {
                        float t = dist / (innerR - 2.5f);
                        alpha = Mathf.Lerp(0.04f, 0.12f, t);
                    }

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            generatedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return generatedRingSprite;
        }

        // ========================== Trigger & Collision Callbacks ==========================

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleIncomingCollider(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleIncomingCollider(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerEnter(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerStay(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerExit(Collider2D other)
        {
            if (other == null) return;
            var pc = other.GetComponentInParent<PlayerCarry>();
            if (pc != null && cachedPlayerCarry == pc)
            {
                // ปล่อยให้ CheckProximityPlayer ตรวจสอบระยะทางที่แท้จริงจากโต๊ะแทนการตัดทันที
                float distToTable = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 999f;
                if (distToTable > tableProximityRadius)
                {
                    isPlayerInRange = false;
                }
            }
        }

        private void HandleIncomingCollider(Collider2D col)
        {
            if (col == null) return;
            var playerCarry = col.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null)
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    TryTimedDeposit(playerCarry);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // วาดตำแหน่งจุดโชว์ที่วางอาหารบนโต๊ะ (สีเขียว)
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.85f);
            if (plateSlots != null && plateSlots.Length > 0)
            {
                for (int i = 0; i < plateSlots.Length; i++)
                {
                    if (plateSlots[i] != null)
                    {
                        Gizmos.DrawWireSphere(plateSlots[i].position, 0.2f);
                        Gizmos.DrawLine(transform.position, plateSlots[i].position);
                    }
                }
            }
            else
            {
                Vector3 dispPos = GetFoodDisplayCenterPosition();
                Gizmos.DrawWireSphere(dispPos, 0.25f);
                Gizmos.DrawLine(transform.position, dispPos);
            }

            // วาดโซนวางอาหาร (สีฟ้า/ขาว)
            Vector3 depositPos = GetDepositPosition();
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawSphere(depositPos, depositZoneRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(depositPos, depositZoneRadius);
            Gizmos.DrawLine(transform.position, depositPos);

            // วาดแถวต่อคิวของลูกค้า (สีส้ม/ทอง)
            Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.85f);
            Vector3 waitStart = CustomerWaitPoint.position;
            Gizmos.DrawWireSphere(waitStart, 0.25f);

            for (int i = 0; i < maxQueueCapacity; i++)
            {
                Vector3 qPos;
                if (customQueuePoints != null && i < customQueuePoints.Length && customQueuePoints[i] != null)
                {
                    qPos = customQueuePoints[i].position;
                }
                else
                {
                    Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
                    qPos = waitStart + (Vector3)(dir * (i * queueSpacing));
                }

                Gizmos.DrawWireSphere(qPos, 0.18f);

                if (i > 0)
                {
                    Vector3 prevPos;
                    if (customQueuePoints != null && (i - 1) < customQueuePoints.Length && customQueuePoints[i - 1] != null)
                        prevPos = customQueuePoints[i - 1].position;
                    else
                    {
                        Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
                        prevPos = waitStart + (Vector3)(dir * ((i - 1) * queueSpacing));
                    }
                    Gizmos.DrawLine(prevPos, qPos);
                }
            }
        }
    }
}
