using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ส่วนเสริมบน HUD: แสดงแถบแจ้งเตือนสีแดงด้านบนกลางจอเมื่อมีโจรหยิบอาหารแล้วหนี ค้างไว้จนกว่าจะตีโจรได้หรือโจรหนีพ้น
    /// </summary>
    public class HudExtrasView : MonoBehaviour
    {
        [Header("Thief Alert Banner (ด้านบนกลางจอ)")]
        public GameObject thiefAlertBanner;
        public Text thiefAlertText;

        [Tooltip("โชว์แถบค้างไว้อย่างน้อยกี่วินาที (กันแถบแวบแล้วหายถ้าตีโจรทันทีที่ขโมย)")]
        [SerializeField] private float minShowTime = 1f;

        private float shownAt = -999f;
        private bool alerting;

        private void Start()
        {
            HideDuringCameraPan.Attach(gameObject);   // กล้องดึงจอ = ซ่อนแถบเตือนชั่วคราว
            if (thiefAlertBanner != null) thiefAlertBanner.SetActive(false);
        }

        private void Update()
        {
            if (!alerting || thiefAlertBanner == null) return;

            // แถบค้างไว้จนกว่าจะไม่มีโจรถือของขโมยแล้ว: ตีโจรได้ (ทิ้งของหนีไป) หรือโจรหนีพ้นจอ
            if (CustomerAI.ActiveRobberCount == 0 && Time.unscaledTime - shownAt >= minShowTime)
            {
                alerting = false;
                thiefAlertBanner.SetActive(false);
            }
        }

        /// <summary>เรียกตอนโจรหยิบอาหารแล้วหนีไม่จ่ายเงิน: แถบค้างจนกว่าจะตีโจรได้หรือโจรหนีไป (duration ไม่ใช้แล้ว เก็บไว้ให้โค้ดเดิมเรียกได้)</summary>
        public void ShowThiefAlert(string message = "THIEF! Hit them to recover meat!", float duration = 0f)
        {
            if (thiefAlertBanner != null) thiefAlertBanner.SetActive(true);
            if (thiefAlertText != null) thiefAlertText.text = message;
            alerting = true;
            shownAt = Time.unscaledTime;
            UiAudioManager.Instance?.PlayThiefAlert();
        }

        public static HudExtrasView CreateDefault(Transform parent)
        {
            RectTransform canvasRect = UiBuild.CreateCanvas(parent, "HUD Extras Canvas", 15);
            HudExtrasView view = canvasRect.gameObject.AddComponent<HudExtrasView>();

            // แถบแจ้งเตือนโจร (กลางบน)
            RectTransform alert = UiBuild.CreateRect(canvasRect, "Thief Alert Banner", typeof(Image));
            UiBuild.Place(alert, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(620f, 64f), new Vector2(0.5f, 1f));
            alert.GetComponent<Image>().color = new Color(0.85f, 0.15f, 0.22f, 0.94f);
            view.thiefAlertBanner = alert.gameObject;

            view.thiefAlertText = UiBuild.CreateText(alert, "Alert Text", "THIEF! Hit them to recover meat!", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            UiBuild.Stretch(view.thiefAlertText.rectTransform);

            return view;
        }
    }
}