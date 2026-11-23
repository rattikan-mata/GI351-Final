using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// อยู่คู่กับร้าน STAFF (ShopStall): เมื่อซื้อสินค้าที่ id ตรงกัน จะจ้างพนักงานมาทำงาน
    /// ตอนนี้มีพนักงานแคชเชียร์ (ยืนคิดเงิน) และพนักงานเสิร์ฟ (ยกอาหารจากหม้อไปวางชั้น)  Hunter ยังล็อกไว้ในร้าน
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

        private ShopStall stall;
        private CashierStaff hiredCashier;
        private WaiterStaff hiredWaiter;

        public CashierStaff HiredCashier => hiredCashier;
        public WaiterStaff HiredWaiter => hiredWaiter;

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
