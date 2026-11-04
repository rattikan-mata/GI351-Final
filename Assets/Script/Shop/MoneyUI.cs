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

        [Header("Gain Popup (เลข +เงิน เด้งขึ้นตอนได้เงิน)")]
        [Tooltip("Text สำหรับแสดง +จำนวนเงินที่เพิ่งได้ (ปล่อยว่างถ้าไม่ต้องการ)")]
        [SerializeField] private Text gainPopupText;

        [Tooltip("ระยะเวลาที่เลข +เงิน ค้างอยู่ก่อนจางหาย (วินาที)")]
        [SerializeField] private float gainPopupDuration = 1.2f;

        [Tooltip("ระยะที่เลข +เงิน ลอยขึ้น (พิกเซล)")]
        [SerializeField] private float gainPopupRise = 30f;

        [Tooltip("ขยายตัวเลขเงินชั่วขณะตอนได้เงิน")]
        [SerializeField] private float punchScale = 1.25f;

        private int lastAmount;
        private bool hasLastAmount;
        private int pendingGain;
        private float popupTimer;
        private Vector2 popupBasePos;
        private Color popupBaseColor = Color.white;
        private float punchTimer;
        private MoneyManager subscribedManager;

        private void Start()
        {
            if (gainPopupText != null)
            {
                popupBasePos = gainPopupText.rectTransform.anchoredPosition;
                popupBaseColor = gainPopupText.color;
                gainPopupText.text = "";
            }

            TrySubscribe();
        }

        /// <summary>
        /// ผูกกับ MoneyManager (ถ้ายังไม่พร้อมตอน Start จะลองใหม่ทุกเฟรมจนเจอ)
        /// </summary>
        private void TrySubscribe()
        {
            if (subscribedManager != null || MoneyManager.Instance == null) return;

            subscribedManager = MoneyManager.Instance;
            subscribedManager.onMoneyChanged.AddListener(UpdateMoneyText);
            UpdateMoneyText(subscribedManager.CurrentMoney);
        }

        private void OnDestroy()
        {
            if (subscribedManager != null)
            {
                subscribedManager.onMoneyChanged.RemoveListener(UpdateMoneyText);
            }
        }

        private void Update()
        {
            TrySubscribe();

            // เลข +เงิน ลอยขึ้นแล้วจางหาย
            if (gainPopupText != null && popupTimer > 0f)
            {
                popupTimer -= Time.unscaledDeltaTime;
                float t = 1f - Mathf.Clamp01(popupTimer / Mathf.Max(0.01f, gainPopupDuration));
                gainPopupText.rectTransform.anchoredPosition = popupBasePos + Vector2.up * (gainPopupRise * t);

                Color c = popupBaseColor;
                c.a = popupBaseColor.a * (1f - t * t);
                gainPopupText.color = c;

                if (popupTimer <= 0f)
                {
                    gainPopupText.text = "";
                    pendingGain = 0;
                }
            }

            // ตัวเลขเงินขยายเด้งเบา ๆ
            if (punchTimer > 0f)
            {
                punchTimer -= Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, punchScale, Mathf.Clamp01(punchTimer / 0.2f));
                SetMoneyTextScale(s);
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

            // ได้เงินเพิ่ม -> โชว์ +จำนวน (ถ้าได้ติดกันหลายเหรียญจะรวมยอด)
            if (hasLastAmount && amount > lastAmount)
            {
                ShowGain(amount - lastAmount);
            }

            lastAmount = amount;
            hasLastAmount = true;
        }

        private void ShowGain(int gained)
        {
            punchTimer = 0.2f;

            if (gainPopupText == null) return;

            pendingGain = popupTimer > 0f ? pendingGain + gained : gained;
            gainPopupText.text = $"+{pendingGain}";
            gainPopupText.color = popupBaseColor;
            gainPopupText.rectTransform.anchoredPosition = popupBasePos;
            popupTimer = gainPopupDuration;
        }

        private void SetMoneyTextScale(float s)
        {
            if (tmpMoneyText != null) tmpMoneyText.rectTransform.localScale = Vector3.one * s;
            if (legacyMoneyText != null) legacyMoneyText.rectTransform.localScale = Vector3.one * s;
        }
    }
}
