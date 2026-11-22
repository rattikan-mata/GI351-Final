using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    public class HudExtrasView : MonoBehaviour
    {
        [Header("Backpack HUD (มุมซ้ายล่าง)")]
        public GameObject backpackBox;
        public Image itemIcon;
        public Text carryCountText;
        public Text maxBadgeText;
        public Color normalTextColor = Color.white;
        public Color fullTextColor = new Color(1f, 0.35f, 0.35f, 1f);

        [Header("Thief Alert Banner (ด้านบนกลางจอ)")]
        public GameObject thiefAlertBanner;
        public Text thiefAlertText;

        private float alertTimer;

        private void Start()
        {
            if (maxBadgeText != null) maxBadgeText.gameObject.SetActive(false);
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

        public void UpdateBackpack(int current, int max, Sprite icon = null)
        {
            if (itemIcon != null && icon != null) itemIcon.sprite = icon;
            bool isFull = max > 0 && current >= max;

            if (carryCountText != null)
            {
                carryCountText.text = current + " / " + max;
                carryCountText.color = isFull ? fullTextColor : normalTextColor;
            }

            if (maxBadgeText != null) maxBadgeText.gameObject.SetActive(isFull);
        }

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

            RectTransform bag = UiBuild.CreateRect(canvasRect, "Backpack Box", typeof(Image));
            UiBuild.Place(bag, new Vector2(0f, 0f), new Vector2(36f, 36f), new Vector2(270f, 84f), new Vector2(0f, 0f));
            bag.GetComponent<Image>().color = new Color(0.08f, 0.1f, 0.16f, 0.88f);
            view.backpackBox = bag.gameObject;

            RectTransform iconRt = UiBuild.CreateRect(bag, "Item Icon", typeof(Image));
            UiBuild.Place(iconRt, new Vector2(0f, 0.5f), new Vector2(44f, 0f), new Vector2(54f, 54f), new Vector2(0.5f, 0.5f));
            view.itemIcon = iconRt.GetComponent<Image>();

            view.carryCountText = UiBuild.CreateText(bag, "Count Text", "0 / 10", 34, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, HorizontalWrapMode.Overflow);
            UiBuild.Place(view.carryCountText.rectTransform, new Vector2(0f, 0.5f), new Vector2(86f, 0f), new Vector2(160f, 50f), new Vector2(0f, 0.5f));

            view.maxBadgeText = UiBuild.CreateText(bag, "MAX Badge", "MAX!", 24, FontStyle.Bold, new Color(1f, 0.35f, 0.35f, 1f), TextAnchor.MiddleCenter, HorizontalWrapMode.Overflow);
            UiBuild.Place(view.maxBadgeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, 16f), new Vector2(120f, 32f), new Vector2(0.5f, 0.5f));

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