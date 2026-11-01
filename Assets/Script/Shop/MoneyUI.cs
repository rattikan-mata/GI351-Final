using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace GI351.KitchenGame
{
    public class MoneyUI : MonoBehaviour
    {
        [Header("UI Text References")]
        [Tooltip("Text Component สำหรับแสดงผลเงิน (รองรับ TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI tmpMoneyText;

        [Tooltip("Text Component ธรรมดา (รองรับ Legacy Text)")]
        [SerializeField] private Text legacyMoneyText;

        [Header("Display Format")]
        [Tooltip("คำนำหน้าข้อความ เช่น $ หรือ เหรียญ: ")]
        [SerializeField] private string prefix = "Money: $";

        [Tooltip("คำลงท้ายข้อความ เช่น G หรือ Baht")]
        [SerializeField] private string suffix = "";

        private void Start()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.onMoneyChanged.AddListener(UpdateMoneyText);
                UpdateMoneyText(MoneyManager.Instance.CurrentMoney);
            }
        }

        private void OnDestroy()
        {
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.onMoneyChanged.RemoveListener(UpdateMoneyText);
            }
        }

        public void UpdateMoneyText(int amount)
        {
            string formattedText = $"{prefix}{amount}{suffix}";

            if (tmpMoneyText != null)
            {
                tmpMoneyText.text = formattedText;
            }

            if (legacyMoneyText != null)
            {
                legacyMoneyText.text = formattedText;
            }
        }
    }
}
