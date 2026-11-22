using System;
using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// หน้าตาแผงอัปเกรดหม้อ (PotUpgrade) อยู่ใน prefab นี้ทั้งหมด ดับเบิลคลิก prefab แล้วแก้ใน Hierarchy ได้เลย
    /// ข้อความที่มาจากข้อมูล (ชื่อหม้อ ชื่อแถว เลเวล ราคา) โค้ดเขียนทับตอนเล่น ให้แก้ที่ PotUpgrade ของหม้อ
    /// </summary>
    public class PotUpgradePanelView : MonoBehaviour
    {
        /// <summary>ชิ้นส่วนของแถวอัปเกรดหนึ่งแถว</summary>
        [Serializable]
        public class Row
        {
            [Tooltip("ชื่อแถว (มาจาก Price Label / Speed Label ของ PotUpgrade)")]
            public Text nameText;
            public Text levelText;
            [Tooltip("ค่าตอนนี้ -> ค่าหลังอัปเกรด")]
            public Text valuesText;
            public Button button;
            public Image buttonImage;
            [Tooltip("ราคาบนปุ่ม")]
            public Text buttonText;

            public bool IsComplete => levelText != null && valuesText != null && button != null && buttonText != null;
        }

        [Header("Parts (ชิ้นที่โค้ดใช้)")]
        [Tooltip("กล่องแผงที่เปิด/ปิด")]
        public GameObject panel;
        [Tooltip("ชื่อหม้อ")]
        public Text titleText;
        public Text moneyText;
        [Tooltip("แถวอัปเกรดราคาอาหาร")]
        public Row priceRow = new Row();
        [Tooltip("แถวอัปเกรดความเร็ว")]
        public Row speedRow = new Row();

        [Header("Button (สีปุ่มตามสถานะ)")]
        public Color affordColor = new Color(0.25f, 0.75f, 0.35f, 1f);
        [Tooltip("เงินไม่พอ / เต็มแล้ว / ไม่มีเลเวล")]
        public Color disabledColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        public Color buttonTextColor = Color.white;
        [Tooltip("สีราคาบนปุ่มตอนเงินไม่พอ")]
        public Color buttonTextTooExpensiveColor = new Color(1f, 0.5f, 0.5f, 1f);
        public string maxLabel = "MAX";
        [Tooltip("ข้อความบนปุ่มตอนแถวนี้ไม่มีเลเวลให้อัป")]
        public string noLevelsLabel = "-";

        /// <summary>ชิ้นที่ขาดไม่ได้ (คืนชื่อช่องที่ว่าง หรือ null ถ้าครบ)</summary>
        public string FindMissingPart()
        {
            if (panel == null) return "Panel";
            if (!priceRow.IsComplete) return "Price Row";
            if (!speedRow.IsComplete) return "Speed Row";
            return null;
        }

        // ---------------- หน้าตาเริ่มต้น (สร้างด้วยโค้ด) ----------------

        /// <summary>สร้างแผงแบบเริ่มต้น (ใช้ทำ prefab ครั้งแรก และสำรองตอนหม้อยังไม่ได้ใส่ prefab)</summary>
        public static PotUpgradePanelView CreateDefault(Transform parent)
        {
            RectTransform canvasRect = UiBuild.CreateCanvas(parent, "Pot Upgrade Panel", 80);
            PotUpgradePanelView view = canvasRect.gameObject.AddComponent<PotUpgradePanelView>();

            RectTransform panelRect = UiBuild.CreateRect(canvasRect, "Panel", typeof(Image));
            UiBuild.Place(panelRect, new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(980f, 400f), new Vector2(0.5f, 0f));
            panelRect.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.16f, 0.95f);
            view.panel = panelRect.gameObject;

            view.titleText = UiBuild.CreateText(panelRect, "Title", "POT", 44, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, HorizontalWrapMode.Overflow);
            UiBuild.Place(view.titleText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -44f), new Vector2(560f, 60f), new Vector2(0f, 0.5f));

            view.moneyText = UiBuild.CreateText(panelRect, "Money", "$0", 40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f, 1f), TextAnchor.MiddleRight, HorizontalWrapMode.Overflow);
            UiBuild.Place(view.moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -44f), new Vector2(300f, 60f), new Vector2(1f, 0.5f));

            view.priceRow = CreateDefaultRow(panelRect, "Price Row", "Product Price", new Vector2(0f, -100f), view);
            view.speedRow = CreateDefaultRow(panelRect, "Speed Row", "Production Speed", new Vector2(0f, -250f), view);
            return view;
        }

        private static Row CreateDefaultRow(RectTransform parent, string objectName, string label, Vector2 pos, PotUpgradePanelView view)
        {
            RectTransform rowRect = UiBuild.CreateRect(parent, objectName, typeof(Image));
            UiBuild.Place(rowRect, new Vector2(0.5f, 1f), pos, new Vector2(920f, 130f), new Vector2(0.5f, 1f));
            rowRect.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.28f, 1f);

            Row row = new Row();
            row.nameText = UiBuild.CreateText(rowRect, "Name", label, 34, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, HorizontalWrapMode.Overflow);
            UiBuild.Place(row.nameText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(460f, 44f), new Vector2(0f, 0.5f));

            row.levelText = UiBuild.CreateText(rowRect, "Level", "LV 1", 26, FontStyle.Bold, new Color(0.6f, 0.9f, 1f, 1f), TextAnchor.MiddleLeft, HorizontalWrapMode.Overflow);
            UiBuild.Place(row.levelText.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(120f, 36f), new Vector2(0f, 0.5f));

            row.valuesText = UiBuild.CreateText(rowRect, "Values", "$10  ->  $12", 30, FontStyle.Bold, new Color(1f, 0.92f, 0.3f, 1f), TextAnchor.MiddleLeft, HorizontalWrapMode.Overflow);
            UiBuild.Place(row.valuesText.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -70f), new Vector2(420f, 40f), new Vector2(0f, 0.5f));

            row.button = UiBuild.CreateButton(rowRect, "Button", "$50", new Vector2(280f, 90f), view.affordColor, 38);
            UiBuild.Place((RectTransform)row.button.transform, new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(280f, 90f), new Vector2(1f, 0.5f));
            row.buttonImage = row.button.GetComponent<Image>();
            row.buttonText = row.button.GetComponentInChildren<Text>();
            row.buttonText.horizontalOverflow = HorizontalWrapMode.Overflow;
            return row;
        }
    }
}
