using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// หน้าตาหน้าซื้อของร้าน (ShopStall) อยู่ใน prefab นี้ทั้งหมด ดับเบิลคลิก prefab แล้วแก้ใน Hierarchy ได้เลย
    /// ข้อความที่มาจากข้อมูล (ชื่อร้าน ชื่อสินค้า ราคา) โค้ดเขียนทับตอนเล่น ให้แก้ที่ ShopStall ใน Inspector
    /// แถวสินค้าและปุ่มแท็บแก้ที่ต้นแบบในกล่อง Templates (ข้างกล่องร้าน) โค้ดก๊อปตามจำนวนสินค้า/แท็บ
    /// ตอนเปิด prefab จะเห็นแถว/แท็บตัวอย่างจากร้านในช่อง Preview Shop (แก้ไม่ได้ ไม่ถูกบันทึก)
    /// </summary>
    public class ShopPanelView : MonoBehaviour
    {
        [Header("Parts (ชิ้นที่โค้ดใช้)")]
        [Tooltip("กล่องหน้าร้านที่เปิด/ปิด")]
        public GameObject panel;
        public Text titleText;
        public Text moneyText;

        [Tooltip("ที่วางแถวสินค้า (มี Vertical Layout Group)")]
        public RectTransform listContent;

        [Tooltip("แถวต้นแบบ (ในกล่อง Templates) โค้ดก๊อปออกมาใส่ List Content ตามจำนวนสินค้า")]
        public ShopRowView rowTemplate;

        [Tooltip("ข้อความตอนแท็บนั้นไม่มีสินค้า")]
        public Text emptyText;
        public Button exitButton;

        [Header("Tabs (ร้านที่ไม่มีแท็บ ปล่อยว่างได้)")]
        [Tooltip("แถบแท็บ (มี Horizontal Layout Group) ซ่อนเองถ้าร้านไม่มีแท็บ")]
        public RectTransform tabBar;

        [Tooltip("ปุ่มแท็บต้นแบบ (ในกล่อง Templates) โค้ดก๊อปออกมาใส่ Tab Bar ตามจำนวนแท็บ")]
        public Button tabTemplate;

        public Color tabSelectedColor = new Color(0.2f, 0.7f, 1f, 1f);
        public Color tabNormalColor = new Color(0.16f, 0.18f, 0.28f, 1f);
        public Color tabSelectedTextColor = Color.white;
        public Color tabNormalTextColor = new Color(0.65f, 0.7f, 0.85f, 1f);

        [Tooltip("รูปปุ่มแท็บที่เลือกอยู่ (ว่าง = ใช้แค่สี)")]
        public Sprite tabSelectedSprite;

        [Tooltip("รูปปุ่มแท็บที่ไม่ได้เลือก (ว่าง = ใช้แค่สี)")]
        public Sprite tabNormalSprite;

        [Header("Buy Button (สีปุ่มซื้อตามสถานะ)")]
        public Color buyColor = new Color(0.2f, 0.7f, 1f, 1f);
        [Tooltip("สินค้าธรรมดาที่เงินไม่พอ")]
        public Color cantAffordColor = new Color(0.25f, 0.3f, 0.45f, 1f);
        [Tooltip("ล็อก / หมด / อัปเลเวลแต่เงินไม่พอ")]
        public Color disabledColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        [Tooltip("อัปเลเวลจนเต็มแล้ว")]
        public Color maxColor = new Color(0.22f, 0.22f, 0.26f, 1f);

        public string buyLabel = "BUY";
        public string upgradeLabel = "UPGRADE";
        public string maxLabel = "MAX";

        [Header("Price Text (สีราคา)")]
        public Color priceColor = new Color(1f, 0.92f, 0.3f, 1f);
        public Color priceTooExpensiveColor = new Color(1f, 0.45f, 0.45f, 1f);
        public Color priceLockedColor = new Color(0.6f, 0.6f, 0.68f, 1f);

        [Header("Templates + Preview (ใช้ตอนแก้ prefab ไม่มีผลตอนเล่น)")]
        [Tooltip("กล่องเก็บต้นแบบ (ซ่อนเองตอนเล่น)")]
        public GameObject templates;

        [Tooltip("ร้านที่ดึงสินค้ามาโชว์เป็นตัวอย่างตอนเปิด prefab นี้ (ลาก prefab Shop Stall มาใส่) ปล่อยว่าง = ไม่โชว์")]
        public ShopStall previewShop;

        [Tooltip("แท็บที่โชว์ตัวอย่าง (0 = แท็บแรก)")]
        [Min(0)] public int previewTab;

        [Tooltip("เงินสมมติตอนโชว์ตัวอย่าง (ดูสีปุ่ม/ราคาตอนเงินพอหรือไม่พอ)")]
        [Min(0)] public int previewMoney = 50;

        /// <summary>ชิ้นที่ขาดไม่ได้ (คืนชื่อช่องที่ว่าง หรือ null ถ้าครบ)</summary>
        public string FindMissingPart()
        {
            if (panel == null) return "Panel";
            if (listContent == null) return "List Content";
            if (rowTemplate == null) return "Row Template";
            if (rowTemplate.nameText == null) return "Row Template > Name Text";
            if (rowTemplate.priceText == null) return "Row Template > Price Text";
            if (rowTemplate.buyButton == null) return "Row Template > Buy Button";
            return null;
        }

        // ---------------- หน้าตาเริ่มต้น (สร้างด้วยโค้ด) ----------------

        /// <summary>สร้างหน้าร้านแบบเริ่มต้น (ใช้ทำ prefab ครั้งแรก และสำรองตอนร้านยังไม่ได้ใส่ prefab)</summary>
        public static ShopPanelView CreateDefault(Transform parent, bool withTabs)
        {
            RectTransform canvasRect = UiBuild.CreateCanvas(parent, "Shop Panel", 90);
            ShopPanelView view = canvasRect.gameObject.AddComponent<ShopPanelView>();

            // กล่องร้านกลางจอ (ไม่บังทั้งจอ ให้เห็นฉากด้านหลัง)
            float tabBarHeight = withTabs ? 70f : 0f;
            RectTransform panelRect = UiBuild.CreateRect(canvasRect, "Panel", typeof(Image));
            UiBuild.Place(panelRect, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 700f + tabBarHeight), new Vector2(0.5f, 0.5f));
            panelRect.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 0.96f);
            view.panel = panelRect.gameObject;

            view.titleText = UiBuild.CreateText(panelRect, "Title", "SHOP", 46, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            UiBuild.Place(view.titleText.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -50f), new Vector2(500f, 70f), new Vector2(0f, 0.5f));

            view.moneyText = UiBuild.CreateText(panelRect, "Money", "0", 40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f, 1f), TextAnchor.MiddleRight);
            UiBuild.Place(view.moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(-36f, -50f), new Vector2(260f, 70f), new Vector2(1f, 0.5f));

            // แถบแท็บใต้ชื่อร้าน
            if (withTabs)
            {
                RectTransform bar = UiBuild.CreateRect(panelRect, "Tab Bar", typeof(HorizontalLayoutGroup));
                UiBuild.Place(bar, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(760f, 58f), new Vector2(0.5f, 1f));
                HorizontalLayoutGroup hlg = bar.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 10f;
                hlg.childControlWidth = true;
                hlg.childControlHeight = true;
                hlg.childForceExpandWidth = true;
                hlg.childForceExpandHeight = true;
                view.tabBar = bar;

            }

            // พื้นที่รายการ (เลื่อนได้)
            RectTransform vpRect = UiBuild.CreateRect(panelRect, "Viewport", typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
            vpRect.anchorMin = new Vector2(0f, 0f);
            vpRect.anchorMax = new Vector2(1f, 1f);
            vpRect.offsetMin = new Vector2(30f, 130f);
            vpRect.offsetMax = new Vector2(-30f, -105f - tabBarHeight);
            vpRect.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            RectTransform content = UiBuild.CreateRect(vpRect, "List Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view.listContent = content;

            ScrollRect scroll = vpRect.GetComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            view.emptyText = UiBuild.CreateText(vpRect, "Empty Text", "Coming soon", 34, FontStyle.Italic, new Color(0.65f, 0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
            UiBuild.Stretch(view.emptyText.rectTransform);
            view.emptyText.gameObject.SetActive(false); // โค้ดเปิดเองตอนแท็บไม่มีสินค้า

            view.exitButton = UiBuild.CreateButton(panelRect, "Exit Button", "EXIT", new Vector2(260f, 70f), new Color(1f, 0.2f, 0.45f, 1f));
            UiBuild.Place((RectTransform)view.exitButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(260f, 70f), new Vector2(0.5f, 0f));

            // กล่องต้นแบบข้างกล่องร้าน
            RectTransform box = CreateTemplatesBox(canvasRect);
            view.templates = box.gameObject;
            view.rowTemplate = CreateDefaultRow(box, view);
            UiBuild.Place((RectTransform)view.rowTemplate.transform, new Vector2(0f, 1f), new Vector2(20f, -60f), new Vector2(744f, 100f), new Vector2(0f, 1f));
            if (withTabs)
            {
                view.tabTemplate = UiBuild.CreateButton(box, "Tab Template", "TAB", new Vector2(240f, 58f), view.tabNormalColor, 30);
                UiBuild.Place((RectTransform)view.tabTemplate.transform, new Vector2(0f, 1f), new Vector2(20f, -180f), new Vector2(240f, 58f), new Vector2(0f, 1f));
            }
            return view;
        }

        /// <summary>กล่อง Templates วางทางขวาของกล่องร้าน (เห็นตอนแก้ prefab ซ่อนตอนเล่น)</summary>
        public static RectTransform CreateTemplatesBox(RectTransform canvasRect)
        {
            RectTransform box = UiBuild.CreateRect(canvasRect, "Templates", typeof(Image));
            UiBuild.Place(box, new Vector2(0.5f, 0.5f), new Vector2(450f, 385f), new Vector2(784f, 260f), new Vector2(0f, 1f));
            box.GetComponent<Image>().color = new Color(0.25f, 0.2f, 0.35f, 0.85f);

            Text label = UiBuild.CreateText(box, "Label", "TEMPLATES - edit these, the list copies them", 24, FontStyle.Bold, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleLeft);
            UiBuild.Place(label.rectTransform, new Vector2(0f, 1f), new Vector2(20f, -30f), new Vector2(740f, 40f), new Vector2(0f, 0.5f));
            return box;
        }

        private static ShopRowView CreateDefaultRow(RectTransform parent, ShopPanelView view)
        {
            // ความสูงแถว = Height ของ RectTransform นี้ (List Content ไม่คุมความสูงลูก)
            RectTransform rowRect = UiBuild.CreateRect(parent, "Row Template", typeof(Image), typeof(ShopRowView));
            rowRect.sizeDelta = new Vector2(rowRect.sizeDelta.x, 100f);
            rowRect.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 1f);
            ShopRowView row = rowRect.GetComponent<ShopRowView>();

            // ชื่อ + คำอธิบาย ซ้อนกันกลางแถว (ซ่อนคำอธิบายแล้วชื่อเลื่อนมากลางเอง)
            RectTransform info = UiBuild.CreateRect(rowRect, "Info", typeof(VerticalLayoutGroup));
            UiBuild.Place(info, new Vector2(0f, 0.5f), new Vector2(18f, 0f), new Vector2(400f, 76f), new Vector2(0f, 0.5f));
            VerticalLayoutGroup vlg = info.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleLeft;
            vlg.spacing = 4f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            row.nameText = UiBuild.CreateText(info, "Name", "Item Name", 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            row.descText = UiBuild.CreateText(info, "Desc", "Description", 20, FontStyle.Normal, new Color(0.7f, 0.76f, 0.9f, 1f), TextAnchor.MiddleLeft);

            // ราคาอยู่กลางช่อง (เยื้องขวานิดหน่อยให้พ้นคำอธิบายยาวๆ ทางซ้าย)
            row.priceText = UiBuild.CreateText(rowRect, "Price", "10", 34, FontStyle.Bold, view.priceColor, TextAnchor.MiddleCenter);
            UiBuild.Place(row.priceText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(150f, 40f), new Vector2(0.5f, 0.5f));

            row.buyButton = UiBuild.CreateButton(rowRect, "Buy Button", "BUY", new Vector2(150f, 56f), view.buyColor);
            UiBuild.Place((RectTransform)row.buyButton.transform, new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(150f, 56f), new Vector2(1f, 0.5f));
            row.buyImage = row.buyButton.GetComponent<Image>();
            row.buyText = row.buyButton.GetComponentInChildren<Text>();
            row.buyText.rectTransform.offsetMin = new Vector2(8f, 0f);
            row.buyText.rectTransform.offsetMax = new Vector2(-8f, 0f);
            UiBuild.BestFit(row.buyText, 14, 36);
            return row;
        }
    }
}
