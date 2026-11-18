using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace GI351.KitchenGame
{
    /// <summary>สินค้าหนึ่งชิ้นในร้านค้า</summary>
    [Serializable]
    public class ShopItemData
    {
        [Tooltip("ชื่อสินค้า (ภาษาอังกฤษ เพราะฟอนต์ในตัวไม่มีภาษาไทย)")]
        public string itemName = "Item";

        [Tooltip("คำอธิบายสั้นๆ ใต้ชื่อ (ปล่อยว่างได้)")]
        public string description = "";

        [Tooltip("ราคา (เหรียญ)")]
        public int price = 10;

        [Tooltip("จำนวนที่ซื้อได้ทั้งหมด (0 = ไม่จำกัด) หมดแล้วขึ้น SOLD OUT")]
        public int stock = 0;

        [Tooltip("ยิงเมื่อซื้อสำเร็จ 1 ครั้ง ลากสคริปต์ที่จะให้ของ/ผลมาผูกที่นี่ (ตอนนี้ยังไม่ผูกอะไร)")]
        public UnityEvent onPurchased;

        [NonSerialized] public int sold;
    }

    /// <summary>
    /// ร้านค้าในฉาก: ผู้เล่นเดินเข้าวงแหวนทองหน้าร้าน -> เปิดหน้าซื้อของ (รายการสินค้า + ราคา + ปุ่ม BUY)
    /// ออกจากวง / กด Exit / กด Esc -> ปิดหน้าซื้อ รายการสินค้าอยู่ในลิสต์ Items (ตอนนี้ว่าง ใส่เองใน Inspector)
    /// หน้าซื้อถูกสร้างตอนเล่น (ไม่ต้องลาก Canvas)
    /// </summary>
    public class ShopStall : MonoBehaviour
    {
        [Header("Shop Items (รายการสินค้า)")]
        [Tooltip("ชื่อร้านที่แสดงบนหน้าซื้อ")]
        [SerializeField] private string shopTitle = "SHOP";

        [Tooltip("รายการสินค้า (ตอนนี้ว่าง) เพิ่มสินค้าที่นี่: ชื่อ / ราคา / จำนวนจำกัด / Event ตอนซื้อ")]
        [SerializeField] private List<ShopItemData> items = new List<ShopItemData>();

        [Header("Buy Zone (วงแหวนหน้าร้าน)")]
        [Tooltip("จุดศูนย์กลางวงแหวน (ลาก child Ring มาใส่ ปล่อยว่าง = ใช้ตำแหน่งของ Prefab)")]
        [SerializeField] private Transform zoneCenter;

        [Tooltip("รัศมีวงเปิดร้าน (เมตร) ควรให้ตรงกับขนาดวงแหวนที่เห็น")]
        [SerializeField] private float zoneRadius = 0.8f;

        [Tooltip("เปิดหน้าซื้ออัตโนมัติเมื่อเดินเข้าวง (ถ้าปิด ต้องกด E ในวง)")]
        [SerializeField] private bool openOnEnter = true;

        [Tooltip("ผู้ซื้อ (ปล่อยว่าง = หา Player ที่มี PlayerCarry ให้อัตโนมัติ)")]
        [SerializeField] private Transform buyerOverride;

        [Header("Visuals (ภาพวงแหวน)")]
        [Tooltip("SpriteRenderer ของวงแหวนหน้าร้าน (เปลี่ยนสีตามผู้เล่นอยู่ในวงหรือไม่)")]
        [SerializeField] private SpriteRenderer ringRenderer;

        [SerializeField] private Color ringColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] private Color ringActiveColor = new Color(0.4f, 1f, 0.6f, 0.95f);

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;

        private struct RowView
        {
            public Text price;
            public Text buyText;
            public Image buyImage;
            public Button buyButton;
        }

        private readonly List<RowView> rows = new List<RowView>();
        private Transform cachedBuyer;
        private float nextBuyerSearchTime;
        private bool isBuyerInside;
        private bool dismissedUntilExit;
        private bool isOpen;
        private bool builtUI;
        private int lastMoney = int.MinValue;

        private GameObject canvasObject;
        private GameObject panel;
        private RectTransform listContent;
        private Text moneyText;
        private Text titleText;
        private Text emptyText;

        private static Font uiFont;

        public bool IsOpen => isOpen;
        public bool IsBuyerInside => isBuyerInside;
        public int ItemCount => items != null ? items.Count : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            uiFont = null;
        }

        private Vector3 ZonePosition => zoneCenter != null ? zoneCenter.position : transform.position;

        private void OnValidate()
        {
            if (zoneRadius < 0.1f) zoneRadius = 0.1f;
        }

        private void OnDisable()
        {
            if (isOpen) SetOpen(false);
        }

        private void Update()
        {
            Transform buyer = GetBuyer();
            isBuyerInside = buyer != null && Vector2.Distance(buyer.position, ZonePosition) <= zoneRadius;

            if (!isBuyerInside)
            {
                dismissedUntilExit = false;
                if (isOpen) SetOpen(false);
            }
            else if (!isOpen && !dismissedUntilExit)
            {
                if (openOnEnter || WasInteractKeyPressed()) SetOpen(true);
            }

            if (isOpen)
            {
#if ENABLE_INPUT_SYSTEM
                if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
#else
                if (Input.GetKeyDown(KeyCode.Escape)) Close();
#endif
                int money = CurrentMoney;
                if (money != lastMoney) Refresh();
            }

            if (ringRenderer != null)
            {
                Color target = isBuyerInside ? ringActiveColor : ringColor;
                ringRenderer.color = Color.Lerp(ringRenderer.color, target, Time.unscaledDeltaTime * 10f);
            }
        }

        // ---------------- Public API ----------------

        public void Open() => SetOpen(true);

        /// <summary>ปิดหน้าซื้อ (ถ้ายังยืนอยู่ในวง จะไม่เปิดซ้ำจนกว่าจะเดินออกแล้วเข้าใหม่)</summary>
        public void Close()
        {
            dismissedUntilExit = true;
            SetOpen(false);
        }

        public bool CanBuy(int index)
        {
            if (index < 0 || index >= ItemCount) return false;
            ShopItemData it = items[index];
            if (it.stock > 0 && it.sold >= it.stock) return false;
            return CurrentMoney >= it.price;
        }

        /// <summary>ซื้อสินค้า index (เช็คสต็อก + เงิน) คืน true ถ้าสำเร็จ</summary>
        public bool TryBuy(int index)
        {
            if (index < 0 || index >= ItemCount) return false;
            if (MoneyManager.Instance == null) return false;

            ShopItemData it = items[index];
            if (it.stock > 0 && it.sold >= it.stock) return false;
            if (MoneyManager.Instance.CurrentMoney < it.price) return false;
            if (!MoneyManager.Instance.SpendMoney(it.price)) return false;

            it.sold++;
            Debug.Log("[ShopStall] Bought " + it.itemName + " ($" + it.price + ")");
            it.onPurchased?.Invoke();
            if (isOpen) Refresh();
            return true;
        }

        /// <summary>สร้างรายการสินค้าใหม่ตอนเล่น (ถ้าแก้ลิสต์ Items ผ่านโค้ด)</summary>
        public void RebuildList()
        {
            if (builtUI) BuildRows();
            if (isOpen) Refresh();
        }

        // ---------------- Logic ----------------

        private int CurrentMoney => MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        private Transform GetBuyer()
        {
            if (buyerOverride != null) return buyerOverride;
            if (cachedBuyer != null) return cachedBuyer;
            if (Time.unscaledTime < nextBuyerSearchTime) return null;

            nextBuyerSearchTime = Time.unscaledTime + 0.5f;
            PlayerCarry carry = FindAnyObjectByType<PlayerCarry>();
            cachedBuyer = carry != null ? carry.transform : null;
            return cachedBuyer;
        }

        private void SetOpen(bool open)
        {
            if (open == isOpen) return;

            if (open)
            {
                if (!builtUI) BuildUI();
                isOpen = true;
                panel.SetActive(true);
                BuildRows();
                Refresh();
                onOpened?.Invoke();
            }
            else
            {
                isOpen = false;
                if (panel != null) panel.SetActive(false);
                onClosed?.Invoke();
            }
        }

        private bool WasInteractKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        // ---------------- UI build ----------------

        private void BuildUI()
        {
            builtUI = true;
            EnsureEventSystem();
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvasObject = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            // กล่องร้านกลางจอ (ไม่บังทั้งจอ ให้เห็นฉากด้านหลัง)
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvasRect, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(820f, 700f);
            panel.GetComponent<Image>().color = new Color(0.05f, 0.06f, 0.11f, 0.96f);

            titleText = CreateText(panelRect, "Title", shopTitle, 46, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(36f, -50f), new Vector2(500f, 70f), new Vector2(0f, 0.5f));

            moneyText = CreateText(panelRect, "Money", "$0", 40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f, 1f), TextAnchor.MiddleRight);
            Place(moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(-36f, -50f), new Vector2(260f, 70f), new Vector2(1f, 0.5f));

            // พื้นที่รายการ (เลื่อนได้)
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect), typeof(Image));
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.SetParent(panelRect, false);
            vpRect.anchorMin = new Vector2(0f, 0f);
            vpRect.anchorMax = new Vector2(1f, 1f);
            vpRect.offsetMin = new Vector2(30f, 130f);
            vpRect.offsetMax = new Vector2(-30f, -105f);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            listContent = content.GetComponent<RectTransform>();
            listContent.SetParent(vpRect, false);
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = new Vector2(0f, 0f);
            listContent.offsetMax = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 8f;
            vlg.padding = new RectOffset(8, 8, 8, 8);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = viewport.GetComponent<ScrollRect>();
            scroll.viewport = vpRect;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            emptyText = CreateText(vpRect, "EmptyText", "No items yet", 34, FontStyle.Italic, new Color(0.65f, 0.7f, 0.85f, 1f), TextAnchor.MiddleCenter);
            Stretch(emptyText.rectTransform);

            RectTransform exit = CreateButton(panelRect, "ExitButton", "EXIT", new Vector2(260f, 70f), new Color(1f, 0.2f, 0.45f, 1f), Close);
            exit.anchorMin = exit.anchorMax = new Vector2(0.5f, 0f);
            exit.pivot = new Vector2(0.5f, 0f);
            exit.anchoredPosition = new Vector2(0f, 28f);
        }

        private void BuildRows()
        {
            if (listContent == null) return;

            for (int i = listContent.childCount - 1; i >= 0; i--)
            {
                Destroy(listContent.GetChild(i).gameObject);
            }
            rows.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                ShopItemData it = items[i];

                GameObject row = new GameObject("Row_" + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                RectTransform rowRect = row.GetComponent<RectTransform>();
                rowRect.SetParent(listContent, false);
                row.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 1f);
                row.GetComponent<LayoutElement>().preferredHeight = 84f;

                Text name = CreateText(rowRect, "Name", it.itemName, 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, it.description.Length > 0 ? 14f : 0f), new Vector2(380f, 40f), new Vector2(0f, 0.5f));

                if (!string.IsNullOrEmpty(it.description))
                {
                    Text desc = CreateText(rowRect, "Desc", it.description, 20, FontStyle.Normal, new Color(0.7f, 0.76f, 0.9f, 1f), TextAnchor.MiddleLeft);
                    Place(desc.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, -18f), new Vector2(380f, 28f), new Vector2(0f, 0.5f));
                }

                Text price = CreateText(rowRect, "Price", "$" + it.price, 32, FontStyle.Bold, new Color(1f, 0.92f, 0.3f, 1f), TextAnchor.MiddleRight);
                Place(price.rectTransform, new Vector2(1f, 0.5f), new Vector2(-190f, 0f), new Vector2(150f, 40f), new Vector2(1f, 0.5f));

                RectTransform buy = CreateButton(rowRect, "BuyButton", "BUY", new Vector2(150f, 56f), new Color(0.2f, 0.7f, 1f, 1f), () => TryBuy(index));
                buy.anchorMin = buy.anchorMax = new Vector2(1f, 0.5f);
                buy.pivot = new Vector2(1f, 0.5f);
                buy.anchoredPosition = new Vector2(-16f, 0f);

                rows.Add(new RowView
                {
                    price = price,
                    buyText = buy.GetComponentInChildren<Text>(),
                    buyImage = buy.GetComponent<Image>(),
                    buyButton = buy.GetComponent<Button>()
                });
            }

            if (emptyText != null) emptyText.gameObject.SetActive(items.Count == 0);
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (moneyText != null) moneyText.text = "$" + money;

            for (int i = 0; i < rows.Count && i < items.Count; i++)
            {
                ShopItemData it = items[i];
                RowView v = rows[i];
                bool soldOut = it.stock > 0 && it.sold >= it.stock;
                bool afford = money >= it.price;

                v.buyButton.interactable = !soldOut && afford;
                v.buyImage.color = soldOut ? new Color(0.3f, 0.3f, 0.35f, 1f) : (afford ? new Color(0.2f, 0.7f, 1f, 1f) : new Color(0.25f, 0.3f, 0.45f, 1f));
                v.buyText.text = soldOut ? "SOLD OUT" : "BUY";
                v.buyText.fontSize = soldOut ? 22 : 36;
                v.price.text = it.stock > 0 && !soldOut ? "$" + it.price + "  x" + (it.stock - it.sold) : "$" + it.price;
                v.price.color = afford || soldOut ? new Color(1f, 0.92f, 0.3f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
            }
        }

        // ---------------- UI helpers ----------------

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null || FindAnyObjectByType<EventSystem>() != null) return;

            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static Text CreateText(RectTransform parent, string name, string content, int size, FontStyle style, Color color, TextAnchor align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.font = uiFont;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        private static RectTransform CreateButton(RectTransform parent, string name, string text, Vector2 size, Color color, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = color;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick());

            Text t = CreateText(rt, "Text", text, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return rt;
        }

        private void OnDrawGizmos()
        {
            Vector3 p = ZonePosition;
            Gizmos.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.25f);
            Gizmos.DrawSphere(p, zoneRadius);
            Gizmos.color = ringColor;
            Gizmos.DrawWireSphere(p, zoneRadius);
        }
    }
}
