using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// อยู่คู่กับร้าน STAFF (ShopStall): เมื่อซื้อสินค้าที่ id ตรงกัน จะจ้างพนักงานมาทำงาน
    /// มีพนักงานแคชเชียร์ (ยืนคิดเงิน) พนักงานเสิร์ฟ (ยกอาหารจากหม้อไปวางชั้น) และพนักงานล่าสัตว์โซนละคน (ล่าแล้วเอาเนื้อมาใส่หม้อ ซื้อได้เมื่อเปิดโซน)
    /// </summary>
    [RequireComponent(typeof(ShopStall))]
    public class StaffHirer : MonoBehaviour
    {
        [Header("Cashier (พนักงานแคชเชียร์)")]
        [Tooltip("id ของสินค้าในร้านที่หมายถึงพนักงานแคชเชียร์")]
        [SerializeField] private string cashierItemId = "cashier";

        [Tooltip("Prefab พนักงานแคชเชียร์")]
        [SerializeField] private CashierStaff cashierStaffPrefab;

        [Header("Waiter (พนักงานเสิร์ฟ)")]
        [Tooltip("id ของสินค้าในร้านที่หมายถึงพนักงานเสิร์ฟ")]
        [SerializeField] private string waiterItemId = "waiter";

        [Tooltip("Prefab พนักงานเสิร์ฟ")]
        [SerializeField] private WaiterStaff waiterStaffPrefab;

        [Tooltip("จุดที่พนักงานเสิร์ฟโผล่ตอนจ้าง เทียบกับร้านนี้ (เมตร)")]
        [SerializeField] private Vector2 waiterSpawnOffset = new Vector2(0f, -1f);

        [System.Serializable]
        public class HunterSlot
        {
            [Tooltip("id ของสินค้าในร้านที่หมายถึงลูกจ้างล่าสัตว์คนนี้")]
            public string itemId = "hunter";

            [Tooltip("ประจำโซนที่สัตว์ดรอปเนื้อนี้ (ซื้อได้เมื่อเปิดประตูโซนนั้นแล้ว)")]
            public ItemType meat = ItemType.ChickenMeat;

            [Tooltip("ถือเนื้อได้ครั้งละกี่ชิ้น (0 = ใช้ค่าใน Prefab ลูกจ้าง)")]
            [Min(0)] public int carryCapacity;

            [Tooltip("ดาเมจต่อการตี (0 = ใช้ค่าใน Prefab ลูกจ้าง) ไก่เลือด 30 หมู 100 ควาย 150")]
            [Min(0)] public int damage;

            [Tooltip("ตีได้ทุกกี่วินาที (0 = ใช้ค่าใน Prefab ลูกจ้าง 0.8)")]
            [Min(0f)] public float attackInterval;

            [Tooltip("คูณความเร็วเดิน (1 = เท่า Prefab, 1.15 = เร็วขึ้น 15%)")]
            [Min(0f)] public float moveSpeedMultiplier = 1f;
        }

        [Header("Hunter (พนักงานล่าสัตว์ โซนละคน)")]
        [Tooltip("ลูกจ้างล่าสัตว์แต่ละโซน: id สินค้าในร้าน -> ชนิดเนื้อของโซน  ราคาตั้งที่ ShopStall > Items")]
        [SerializeField] private System.Collections.Generic.List<HunterSlot> hunterSlots = new System.Collections.Generic.List<HunterSlot>
        {
            new HunterSlot { itemId = "hunter", meat = ItemType.ChickenMeat, carryCapacity = 4, damage = 30 },
            new HunterSlot { itemId = "hunter_pig", meat = ItemType.PigMeat, carryCapacity = 12, damage = 100 },
            new HunterSlot { itemId = "hunter_buffalo", meat = ItemType.BuffaloMeat, carryCapacity = 8, damage = 150 }
        };

        [Tooltip("Prefab พนักงานล่าสัตว์")]
        [SerializeField] private HunterStaff hunterStaffPrefab;

        [Tooltip("จุดที่พนักงานล่าสัตว์โผล่ตอนจ้าง เทียบกับร้านนี้ (เมตร)")]
        [SerializeField] private Vector2 hunterSpawnOffset = new Vector2(0f, -1f);

        [Tooltip("ข้อความบนปุ่มตอนโซนยังไม่เปิด")]
        [SerializeField] private string zoneLockedText = "OPEN ZONE";

        private ShopStall stall;
        private CashierStaff hiredCashier;
        private WaiterStaff hiredWaiter;
        private readonly System.Collections.Generic.Dictionary<string, HunterStaff> hiredHunters = new System.Collections.Generic.Dictionary<string, HunterStaff>();

        public CashierStaff HiredCashier => hiredCashier;
        public WaiterStaff HiredWaiter => hiredWaiter;
        public int HiredHunterCount => hiredHunters.Count;

        private void Start()
        {
            // ล็อกลูกจ้างล่าสัตว์ของโซนที่ยังไม่เปิด แล้วปลดเองตอนเปิดประตู
            foreach (HunterSlot slot in hunterSlots)
            {
                ShopItemData item = stall != null ? stall.FindItem(slot.itemId) : null;
                HuntArea area = FindArea(slot.meat);
                if (item == null || area == null || area.IsOpen || area.Gate == null) continue;

                item.locked = true;
                item.lockedText = zoneLockedText;
                area.Gate.onUnlocked.AddListener(() =>
                {
                    item.locked = false;
                    if (stall != null) stall.RefreshIfOpen();
                });
            }
        }

        private static HuntArea FindArea(ItemType meat)
        {
            foreach (HuntArea a in HuntArea.All)
            {
                if (a != null && a.DropsMeat(meat)) return a;
            }
            return null;
        }

        private void Awake()
        {
            stall = GetComponent<ShopStall>();
        }

        private void OnEnable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            if (stall != null) stall.ItemPurchased += HandlePurchased;
        }

        private void OnDisable()
        {
            if (stall != null) stall.ItemPurchased -= HandlePurchased;
        }

        private void HandlePurchased(ShopItemData item)
        {
            if (item == null) return;
            if (item.id == cashierItemId) HireCashier();
            else if (item.id == waiterItemId) HireWaiter();
            else
            {
                foreach (HunterSlot slot in hunterSlots)
                {
                    if (item.id == slot.itemId) { HireHunter(slot); break; }
                }
            }
        }

        /// <summary>จ้างลูกจ้างล่าสัตว์ของโซนตาม id สินค้า (ใช้ตอนทดสอบ)</summary>
        public HunterStaff HireHunter(string itemId)
        {
            foreach (HunterSlot slot in hunterSlots)
            {
                if (slot.itemId == itemId) return HireHunter(slot);
            }
            return null;
        }

        /// <summary>จ้างพนักงานล่าสัตว์ประจำโซน: โผล่ข้างร้านนี้แล้วเดินไปล่าเอง (โซนละคน)</summary>
        private HunterStaff HireHunter(HunterSlot slot)
        {
            if (hiredHunters.TryGetValue(slot.itemId, out HunterStaff existing) && existing != null) return existing;
            if (hunterStaffPrefab == null)
            {
                Debug.LogWarning("[StaffHirer] ยังไม่ได้ใส่ Prefab พนักงานล่าสัตว์", this);
                return null;
            }

            HunterStaff hunter = Instantiate(hunterStaffPrefab);
            hunter.name = "Hunter Staff (" + slot.meat + ")";
            hunter.Init(transform.position + (Vector3)hunterSpawnOffset, slot.meat, slot.carryCapacity, slot.damage,
                slot.attackInterval, slot.moveSpeedMultiplier);
            hiredHunters[slot.itemId] = hunter;
            HuntArea area = FindArea(slot.meat);
            if (area != null) area.SetHunterHired();
            Debug.Log("[StaffHirer] Hired a hunter for " + slot.meat);
            return hunter;
        }

        /// <summary>จ้างพนักงานแคชเชียร์มายืนที่โต๊ะจ่ายเงิน (จ้างได้คนเดียว)</summary>
        public CashierStaff HireCashier()
        {
            if (hiredCashier != null) return hiredCashier;
            if (cashierStaffPrefab == null)
            {
                Debug.LogWarning("[StaffHirer] ยังไม่ได้ใส่ Prefab พนักงานแคชเชียร์", this);
                return null;
            }

            CashierCounter counter = FindAnyObjectByType<CashierCounter>();
            if (counter == null)
            {
                Debug.LogWarning("[StaffHirer] ไม่พบโต๊ะจ่ายเงิน (CashierCounter) ในฉาก", this);
                return null;
            }

            hiredCashier = Instantiate(cashierStaffPrefab);
            hiredCashier.name = "Cashier Staff";
            hiredCashier.Init(counter);
            Debug.Log("[StaffHirer] Hired a cashier");
            return hiredCashier;
        }

        /// <summary>จ้างพนักงานเสิร์ฟ: โผล่ข้างร้านนี้แล้วเดินไปยกอาหารเอง (จ้างได้คนเดียว)</summary>
        public WaiterStaff HireWaiter()
        {
            if (hiredWaiter != null) return hiredWaiter;
            if (waiterStaffPrefab == null)
            {
                Debug.LogWarning("[StaffHirer] ยังไม่ได้ใส่ Prefab พนักงานเสิร์ฟ", this);
                return null;
            }

            hiredWaiter = Instantiate(waiterStaffPrefab);
            hiredWaiter.name = "Waiter Staff";
            hiredWaiter.Init(transform.position + (Vector3)waiterSpawnOffset);
            Debug.Log("[StaffHirer] Hired a waiter");
            return hiredWaiter;
        }
    }
}
