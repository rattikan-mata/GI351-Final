using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    public class MoneyManager : MonoBehaviour
    {
        public static MoneyManager Instance { get; private set; }

        [Header("Money Settings (ปรับได้ใน Inspector)")]
        [Tooltip("เงินเริ่มต้นของผู้เล่น")]
        [SerializeField] private int startingMoney = 0;

        [Header("Events")]
        public UnityEvent<int> onMoneyChanged; // ส่งค่าเงินปัจจุบันกลับไปอัปเดต UI

        private int currentMoney;

        public int CurrentMoney => currentMoney;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            currentMoney = startingMoney;
        }

        private void Start()
        {
            onMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// เพิ่มเงิน (เมื่อขายอาหาร หรือ รับเงินจากลูกค้า)
        /// </summary>
        public void AddMoney(int amount)
        {
            if (amount <= 0) return;

            currentMoney += amount;
            Debug.Log($"[MoneyManager] Earned +{amount} coins! Current: {currentMoney}");
            onMoneyChanged?.Invoke(currentMoney);
        }

        /// <summary>
        /// ใช้เงิน (ซื้อของ/อัปเกรด) ถ้าเงินพอจะหักและคืนค่า true
        /// </summary>
        public bool SpendMoney(int amount)
        {
            if (amount <= 0) return true;

            if (currentMoney >= amount)
            {
                currentMoney -= amount;
                Debug.Log($"[MoneyManager] Spent -{amount} coins! Current: {currentMoney}");
                onMoneyChanged?.Invoke(currentMoney);
                return true;
            }

            Debug.Log("[MoneyManager] Not enough money!");
            return false;
        }

        /// <summary>
        /// กำหนดค่าเงินโดยตรง
        /// </summary>
        public void SetMoney(int amount)
        {
            currentMoney = Mathf.Max(0, amount);
            onMoneyChanged?.Invoke(currentMoney);
        }
    }
}
