using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ส่วนเสริมบน HUD: แสดงแถบแจ้งเตือนสีแดงด้านบนกลางจอเมื่อมีโจรหยิบอาหารแล้วหนีไม่จ่ายเงิน
    /// </summary>
    public class HudExtrasView : MonoBehaviour
    {
        [Header("Thief Alert Banner (ด้านบนกลางจอ)")]
        public GameObject thiefAlertBanner;
        public Text thiefAlertText;

        private float alertTimer;

        private void Start()
        {
            if (thiefAlertBanner != null) thiefAlertBanner.SetActive(false);
        }

        private void Update()
        {
            if (alertTimer > 0f)
            {
                alertTimer -= Time.deltaTime;
                if (alertTimer <= 0f && thiefAlertBanner != null)
                    thiefAlertBanner.SetActive(false);
            }
        }

        /// <summary>เรียกตอนโจรหยิบอาหารแล้วหนีไม่จ่ายเงิน</summary>
        public void ShowThiefAlert(string message = "THIEF! Hit them to recover meat!", float duration = 3f)
        {
            if (thiefAlertBanner != null) thiefAlertBanner.SetActive(true);
            if (thiefAlertText != null) thiefAlertText.text = message;
            alertTimer = duration;
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