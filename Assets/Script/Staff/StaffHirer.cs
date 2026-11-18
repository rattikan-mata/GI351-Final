using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// อยู่คู่กับร้าน STAFF (ShopStall): เมื่อซื้อสินค้าที่ id ตรงกัน จะจ้างพนักงานมาประจำจุด
    /// ตอนนี้มีแค่พนักงานแคชเชียร์ (Waiter / Hunter ยังล็อกไว้ในร้าน รอระบบให้พนักงานถือของได้)
    /// </summary>
    [RequireComponent(typeof(ShopStall))]
    public class StaffHirer : MonoBehaviour
    {
        [Tooltip("id ของสินค้าในร้านที่หมายถึงพนักงานแคชเชียร์")]
        [SerializeField] private string cashierItemId = "cashier";

        [Tooltip("Prefab พนักงานแคชเชียร์")]
        [SerializeField] private CashierStaff cashierStaffPrefab;

        private ShopStall stall;
        private CashierStaff hiredCashier;

        public CashierStaff HiredCashier => hiredCashier;

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
    }
}
