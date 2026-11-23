using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พนักงานแคชเชียร์ (แบบ Juice King): ยืนประจำโต๊ะจ่ายเงิน แล้ว "คิดเงินลูกค้าแทนผู้เล่น" ผู้เล่นไม่ต้องยืนในวงเอง
    /// อ่อนกว่าผู้เล่น: คิดเงินช้ากว่า (ตั้งที่ CashierCounter > Staff Pay Duration) ไม่เดิน และสู้โจรไม่ได้
    /// เงินยังกองอยู่บนโต๊ะ ผู้เล่นต้องเดินมาเก็บเอง (เปิด Collect Coins ถ้าอยากให้พนักงานเก็บให้ด้วย)
    /// </summary>
    public class CashierStaff : StaffBase
    {
        [Header("Work (การทำงาน)")]
        [Tooltip("ให้พนักงานเก็บเหรียญในกองให้ด้วย (ปิด = ผู้เล่นเดินมาเก็บเงินเอง)")]
        [SerializeField] private bool collectCoins = false;

        [Tooltip("เก็บเหรียญ 1 เหรียญทุกกี่วินาที (ใช้เมื่อเปิด Collect Coins)")]
        [SerializeField] private float collectInterval = 0.6f;

        [Tooltip("ยืนเยื้องจากกลางวงขาวเล็กน้อย ไม่บังผู้เล่นตอนเดินเข้าวง (เมตร)")]
        [SerializeField] private Vector2 standOffset = new Vector2(0.55f, 0f);

        private CashierCounter counter;
        private float collectTimer;

        public CashierCounter Counter => counter;

        /// <summary>เริ่มงานที่โต๊ะจ่ายเงินนี้</summary>
        public void Init(CashierCounter cashier)
        {
            counter = cashier;
            if (counter == null) return;

            counter.SetStaff(this);
            if (counter.PlayerRing != null)
            {
                transform.position = counter.PlayerRing.position + (Vector3)standOffset;
            }
        }

        private void OnDisable()
        {
            if (counter != null && counter.HasStaff) counter.SetStaff(null);
        }

        private void Update()
        {
            UpdateVisual();

            if (counter == null || !collectCoins) return;

            collectTimer += Time.deltaTime;
            if (collectTimer >= collectInterval)
            {
                collectTimer = 0f;
                counter.AttractOneCoinTo(transform);
            }
        }
    }
}
