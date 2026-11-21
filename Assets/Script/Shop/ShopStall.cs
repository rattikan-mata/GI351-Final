using System;
using System.Collections.Generic;
using System.Globalization;
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

        [Tooltip("รหัสสินค้า ให้สคริปต์อื่นรู้ว่าซื้ออะไร (เช่น cashier) ปล่อยว่างได้")]
        public string id = "";

        [Tooltip("แท็บที่สินค้านี้อยู่ ต้องตรงกับชื่อใน ShopStall > Tabs (เช่น Hunt / Cook / Store) ปล่อยว่าง = แท็บแรก")]
        public string tab = "";

        [Tooltip("ล็อกไว้ ยังซื้อไม่ได้ (แสดงในร้านแต่ปุ่มเป็น Coming soon)")]
        public bool locked = false;

        [Tooltip("ข้อความบนปุ่มตอนล็อก")]
        public string lockedText = "SOON";

        [Tooltip("ข้อความบนปุ่มตอนซื้อครบสต็อกแล้ว (เช่น SOLD OUT / OWNED)")]
        public string soldOutText = "SOLD OUT";

        [Header("Levels (สินค้าแบบอัปเลเวล: ใส่ตั้งแต่ 2 เลเวลขึ้นไป = กดซื้อซ้ำเพื่ออัปจนถึง MAX)")]
        [Tooltip("ชื่อค่าที่เปลี่ยนตามเลเวล แสดงใต้ชื่อสินค้า เช่น Damage / Capacity / Speed")]
        public string valueLabel = "";

        [Tooltip("รายการเลเวล ตัวที่ 1 = Lv1 (มีตั้งแต่เริ่ม ไม่ใช้ราคา) ตัวถัดไป = ค่าที่ได้ และราคาที่ต้องจ่ายเพื่ออัปไปเลเวลนั้น ถ้าใส่ 2 ตัวขึ้นไป ช่อง Price / Stock ด้านบนจะไม่ถูกใช้")]
        public List<ShopItemLevel> levels = new List<ShopItemLevel>();

        [Tooltip("เปิดขายถึงเลเวลไหน (0 = ทุกเลเวล) เช่น 3 = อัปได้ถึง Lv3 แล้วปุ่มขึ้น SOON\n" +
                 "เลเวลที่เกินยังเก็บค่าไว้ในลิสต์ (เตรียมไว้สำหรับสัตว์ตัวใหม่) แค่ยังไม่ขาย")]
        [Min(0)] public int levelCap = 0;

        [Tooltip("ยิงเมื่อซื้อสำเร็จ 1 ครั้ง ลากสคริปต์ที่จะให้ของ/ผลมาผูกที่นี่")]
        public UnityEvent onPurchased;

        [NonSerialized] public int sold;
        [NonSerialized] private int currentLevel = 1;

        /// <summary>เป็นสินค้าแบบอัปเลเวลหรือไม่ (มี 2 เลเวลขึ้นไป)</summary>
        public bool IsLeveled => levels != null && levels.Count >= 2;

        public int MaxLevel => IsLeveled ? levels.Count : 1;

        /// <summary>เลเวลปัจจุบัน (เริ่มที่ 1)</summary>
        public int Level
        {
            get => Mathf.Clamp(currentLevel, 1, MaxLevel);
            set => currentLevel = Mathf.Clamp(value, 1, MaxLevel);
        }

        public bool IsMaxLevel => IsLeveled && Level >= levels.Count;

        /// <summary>อัปถึงเลเวลที่เปิดขายแล้ว แต่ยังมีเลเวลถัดไปเตรียมไว้ (ยังไม่เปิดขาย)</summary>
        public bool IsAtLevelCap => IsLeveled && !IsMaxLevel && levelCap > 0 && Level >= levelCap;

        /// <summary>ค่าของเลเวลปัจจุบัน</summary>
        public float CurrentValue => IsLeveled ? levels[Level - 1].value : 0f;

        /// <summary>ค่าของเลเวลถัดไป (ถ้า MAX แล้วคืนค่าปัจจุบัน)</summary>
        public float NextValue => IsLeveled && !IsMaxLevel ? levels[Level].value : CurrentValue;

        /// <summary>ราคาที่ต้องจ่ายตอนนี้ (แบบอัปเลเวล = ราคาไปเลเวลถัดไป, MAX = 0)</summary>
        public int NextPrice => IsLeveled ? (IsMaxLevel ? 0 : levels[Level].price) : price;
    }

    /// <summary>หนึ่งเลเวลของสินค้าแบบอัปเลเวล</summary>
    [Serializable]
    public class ShopItemLevel
    {
        [Tooltip("ค่าที่ได้เมื่ออยู่เลเวลนี้ (เช่น ดาเมจ 30)")]
        public float value;

        [Tooltip("ราคาที่ต้องจ่ายเพื่ออัปมาเลเวลนี้ (Lv1 ไม่ใช้)")]
        public int price;
    }

    /// <summary>
    /// ร้านค้าในฉาก: ผู้เล่นเดินเข้าวงแหวนทองหน้าร้าน -> เปิดหน้าซื้อของ (รายการสินค้า + ราคา + ปุ่ม BUY)
    /// ออกจากวง / กด Exit / กด Esc -> ปิดหน้าซื้อ รายการสินค้าอยู่ในลิสต์ Items (ใส่เองใน Inspector)
    /// สินค้าธรรมดา = ซื้อครั้งเดียว/มีสต็อก ส่วนสินค้าที่ใส่ Levels 2 เลเวลขึ้นไป = กดซื้อซ้ำเพื่ออัปเลเวลจนถึง MAX
    /// หน้าซื้อถูกสร้างตอนเล่น (ไม่ต้องลาก Canvas)
    /// </summary>
    public class ShopStall : MonoBehaviour
    {
        [Header("Shop Items (รายการสินค้า)")]
        [Tooltip("ชื่อร้านที่แสดงบนหน้าซื้อ")]
        [SerializeField] private string shopTitle = "SHOP";

        [Tooltip("รายการสินค้า (ตอนนี้ว่าง) เพิ่มสินค้าที่นี่: ชื่อ / ราคา / จำนวนจำกัด / Event ตอนซื้อ")]
        [SerializeField] private List<ShopItemData> items = new List<ShopItemData>();

        [Tooltip("แท็บแยกหมวดสินค้า เช่น Hunt / Cook / Store (ว่าง = ไม่มีแท็บ แสดงสินค้าทั้งหมด) สินค้าเลือกแท็บที่ช่อง Tab ของแต่ละชิ้น")]
        [SerializeField] private List<string> tabs = new List<string>();

        [Tooltip("ข้อความตอนแท็บนั้นยังไม่มีสินค้า")]
        [SerializeField] private string emptyTabText = "Coming soon";

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
            public Text name;
            public Text desc;
            public Text price;
            public Text buyText;
            public Image buyImage;
            public Button buyButton;
        }

        private readonly List<RowView> rows = new List<RowView>();
        private readonly List<int> rowItemIndex = new List<int>(); // แถวที่ i แสดงสินค้าลำดับไหนในลิสต์ items

        private struct TabView
        {
            public Image image;
            public Text text;
        }

        private readonly List<TabView> tabViews = new List<TabView>();
        private int currentTab;
        private Transform cachedBuyer;
        private float nextBuyerSearchTime;
        private bool isBuyerInside;
        private bool dismissedUntilExit;
        private bool isOpen;
        private bool builtUI;
        private int lastMoney = int.MinValue;

        private string arrowText = "->";

        private GameObject canvasObject;
        private GameObject panel;
        private RectTransform listContent;
        private Text moneyText;
        private Text titleText;
        private Text emptyText;

        private static Font uiFont;

        /// <summary>ยิงเมื่อซื้อสินค้าสำเร็จ (ใช้ให้สคริปต์อื่นทำงานตาม id ของสินค้า เช่น จ้างพนักงาน)</summary>
        public event Action<ShopItemData> ItemPurchased;

        public bool IsOpen => isOpen;
        public bool IsBuyerInside => isBuyerInside;
        public int ItemCount => items != null ? items.Count : 0;

        private bool HasTabs => tabs != null && tabs.Count > 0;

        /// <summary>แท็บที่เปิดอยู่ (ชื่อ) ว่างถ้าร้านนี้ไม่มีแท็บ</summary>
        public string CurrentTab => HasTabs ? tabs[Mathf.Clamp(currentTab, 0, tabs.Count - 1)] : "";

        /// <summary>เปลี่ยนแท็บที่แสดง (ตามลำดับในลิสต์ Tabs)</summary>
        public void SelectTab(int index)
        {
            if (!HasTabs) return;
            currentTab = Mathf.Clamp(index, 0, tabs.Count - 1);
            if (builtUI)
            {
                BuildRows();
                Refresh();
            }
        }

        /// <summary>สินค้าชิ้นนี้อยู่แท็บที่เปิดอยู่ไหม (ช่อง Tab ว่าง หรือชื่อไม่ตรงแท็บไหนเลย = อยู่แท็บแรก)</summary>
        private bool InCurrentTab(ShopItemData it)
        {
            if (!HasTabs || it == null) return true;
            int idx = string.IsNullOrEmpty(it.tab) ? -1 : tabs.FindIndex(t => string.Equals(t, it.tab, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) idx = 0;
            return idx == Mathf.Clamp(currentTab, 0, tabs.Count - 1);
        }

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
            if (it.locked) return false;
            if (it.IsLeveled) return !it.IsMaxLevel && !it.IsAtLevelCap && CurrentMoney >= it.NextPrice;
            if (it.stock > 0 && it.sold >= it.stock) return false;
            return CurrentMoney >= it.price;
        }

        /// <summary>ข้อมูลสินค้าลำดับที่ index (null ถ้าไม่มี)</summary>
        public ShopItemData GetItem(int index)
        {
            return index >= 0 && index < ItemCount ? items[index] : null;
        }

        /// <summary>หาสินค้าจาก id (null ถ้าไม่เจอ)</summary>
        public ShopItemData FindItem(string id)
        {
            if (string.IsNullOrEmpty(id) || items == null) return null;
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] != null && items[i].id == id) return items[i];
            }
            return null;
        }

        /// <summary>ซื้อสินค้า index (เช็คสต็อก + เงิน) คืน true ถ้าสำเร็จ</summary>
        public bool TryBuy(int index)
        {
            if (index < 0 || index >= ItemCount) return false;
            if (MoneyManager.Instance == null) return false;

            ShopItemData it = items[index];
            if (it.locked) return false;

            bool leveled = it.IsLeveled;
            if (leveled)
            {
                if (it.IsMaxLevel || it.IsAtLevelCap) return false;
            }
            else if (it.stock > 0 && it.sold >= it.stock)
            {
                return false;
            }

            // เช็คเงินก่อน ถ้าไม่พอจะไม่หักเงิน
            int cost = leveled ? it.NextPrice : it.price;
            if (MoneyManager.Instance.CurrentMoney < cost) return false;
            if (!MoneyManager.Instance.SpendMoney(cost)) return false;

            it.sold++;
            if (leveled)
            {
                it.Level = it.Level + 1;
                Debug.Log("[ShopStall] Upgraded " + it.itemName + " to Lv" + it.Level + " ($" + cost + ")");
            }
            else
            {
                Debug.Log("[ShopStall] Bought " + it.itemName + " ($" + it.price + ")");
            }
            it.onPurchased?.Invoke();
            ItemPurchased?.Invoke(it);
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
            arrowText = uiFont != null && uiFont.HasCharacter('\u2192') ? "\u2192" : ">";

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
            float tabBarHeight = HasTabs ? 70f : 0f;
            panelRect.sizeDelta = new Vector2(820f, 700f + tabBarHeight);
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
            vpRect.offsetMax = new Vector2(-30f, -105f - tabBarHeight);

            // แถบแท็บใต้ชื่อร้าน
            tabViews.Clear();
            if (HasTabs)
            {
                float gap = 10f;
                float width = (820f - 60f - gap * (tabs.Count - 1)) / tabs.Count;
                for (int i = 0; i < tabs.Count; i++)
                {
                    int tabIndex = i;
                    RectTransform tabBtn = CreateButton(panelRect, "Tab_" + tabs[i], tabs[i].ToUpperInvariant(), new Vector2(width, 58f), new Color(0.16f, 0.18f, 0.28f, 1f), () => SelectTab(tabIndex));
                    tabBtn.anchorMin = tabBtn.anchorMax = new Vector2(0f, 1f);
                    tabBtn.pivot = new Vector2(0f, 1f);
                    tabBtn.anchoredPosition = new Vector2(30f + i * (width + gap), -100f);
                    Text label = tabBtn.GetComponentInChildren<Text>();
                    label.fontSize = 30;
                    tabViews.Add(new TabView { image = tabBtn.GetComponent<Image>(), text = label });
                }
            }
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
            rowItemIndex.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                ShopItemData it = items[i];
                if (!InCurrentTab(it)) continue;

                GameObject row = new GameObject("Row_" + i, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                RectTransform rowRect = row.GetComponent<RectTransform>();
                rowRect.SetParent(listContent, false);
                row.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.22f, 1f);
                row.GetComponent<LayoutElement>().preferredHeight = 84f;

                bool hasDesc = it.IsLeveled || !string.IsNullOrEmpty(it.description);
                Text name = CreateText(rowRect, "Name", it.itemName, 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                Place(name.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, hasDesc ? 14f : 0f), new Vector2(400f, 40f), new Vector2(0f, 0.5f));

                Text desc = null;
                if (hasDesc)
                {
                    desc = CreateText(rowRect, "Desc", it.description ?? "", 20, FontStyle.Normal, new Color(0.7f, 0.76f, 0.9f, 1f), TextAnchor.MiddleLeft);
                    Place(desc.rectTransform, new Vector2(0f, 0.5f), new Vector2(18f, -18f), new Vector2(400f, 28f), new Vector2(0f, 0.5f));
                }

                // ราคาอยู่กลางช่อง (เยื้องขวานิดหน่อยให้พ้นคำอธิบายยาวๆ ทางซ้าย)
                Text price = CreateText(rowRect, "Price", "$" + it.price, 34, FontStyle.Bold, new Color(1f, 0.92f, 0.3f, 1f), TextAnchor.MiddleCenter);
                Place(price.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(150f, 40f), new Vector2(0.5f, 0.5f));

                RectTransform buy = CreateButton(rowRect, "BuyButton", "BUY", new Vector2(150f, 56f), new Color(0.2f, 0.7f, 1f, 1f), () => TryBuy(index));
                buy.anchorMin = buy.anchorMax = new Vector2(1f, 0.5f);
                buy.pivot = new Vector2(1f, 0.5f);
                buy.anchoredPosition = new Vector2(-16f, 0f);

                rowItemIndex.Add(index);
                rows.Add(new RowView
                {
                    name = name,
                    desc = desc,
                    price = price,
                    buyText = buy.GetComponentInChildren<Text>(),
                    buyImage = buy.GetComponent<Image>(),
                    buyButton = buy.GetComponent<Button>()
                });
            }

            if (emptyText != null)
            {
                emptyText.text = HasTabs ? emptyTabText : "No items yet";
                emptyText.gameObject.SetActive(rows.Count == 0);
            }
            RefreshTabs();
        }

        /// <summary>แท็บที่เลือกอยู่เป็นสีฟ้าสว่าง ที่เหลือเป็นสีเข้ม</summary>
        private void RefreshTabs()
        {
            for (int i = 0; i < tabViews.Count; i++)
            {
                bool selected = i == Mathf.Clamp(currentTab, 0, tabs.Count - 1);
                tabViews[i].image.color = selected ? new Color(0.2f, 0.7f, 1f, 1f) : new Color(0.16f, 0.18f, 0.28f, 1f);
                tabViews[i].text.color = selected ? Color.white : new Color(0.65f, 0.7f, 0.85f, 1f);
            }
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (moneyText != null) moneyText.text = "$" + money;

            for (int i = 0; i < rows.Count && i < rowItemIndex.Count; i++)
            {
                ShopItemData it = items[rowItemIndex[i]];
                RowView v = rows[i];
                bool soldOut = it.stock > 0 && it.sold >= it.stock;
                bool afford = money >= it.price;

                if (it.locked)
                {
                    // ยังไม่เปิดขาย: ปุ่มเทา กดไม่ได้
                    v.buyButton.interactable = false;
                    v.buyImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                    v.buyText.text = string.IsNullOrEmpty(it.lockedText) ? "SOON" : it.lockedText;
                    v.buyText.fontSize = 30;
                    v.price.text = "$" + it.price;
                    v.price.color = new Color(0.6f, 0.6f, 0.68f, 1f);
                    continue;
                }

                if (it.IsLeveled)
                {
                    RefreshLeveledRow(it, v, money);
                    continue;
                }

                v.buyButton.interactable = !soldOut && afford;
                v.buyImage.color = soldOut ? new Color(0.3f, 0.3f, 0.35f, 1f) : (afford ? new Color(0.2f, 0.7f, 1f, 1f) : new Color(0.25f, 0.3f, 0.45f, 1f));
                v.buyText.text = soldOut ? (string.IsNullOrEmpty(it.soldOutText) ? "SOLD OUT" : it.soldOutText) : "BUY";
                v.buyText.fontSize = soldOut ? 22 : 36;
                v.price.text = it.stock > 1 && !soldOut ? "$" + it.price + "  x" + (it.stock - it.sold) : "$" + it.price;
                v.price.color = afford || soldOut ? new Color(1f, 0.92f, 0.3f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
            }
        }

        /// <summary>แถวสินค้าแบบอัปเลเวล: "ชื่อ Lv2 → Lv3" / "Damage 30 → 40" / ราคา / ปุ่ม UPGRADE (MAX = ปุ่มเทา กดไม่ได้)</summary>
        private void RefreshLeveledRow(ShopItemData it, RowView v, int money)
        {
            string label = string.IsNullOrEmpty(it.valueLabel) ? "" : it.valueLabel + " ";

            if (it.IsMaxLevel)
            {
                v.name.text = it.itemName + "  Lv" + it.Level + " MAX";
                if (v.desc != null) v.desc.text = label + FormatValue(it.CurrentValue);
                v.price.text = "";
                v.buyButton.interactable = false;
                v.buyImage.color = new Color(0.22f, 0.22f, 0.26f, 1f);
                v.buyText.text = "MAX";
                v.buyText.fontSize = 32;
                return;
            }

            if (it.IsAtLevelCap)
            {
                // เลเวลถัดไปเตรียมไว้แล้วแต่ยังไม่เปิดขาย
                v.name.text = it.itemName + "  Lv" + it.Level;
                if (v.desc != null) v.desc.text = label + FormatValue(it.CurrentValue);
                v.price.text = "";
                v.buyButton.interactable = false;
                v.buyImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                v.buyText.text = string.IsNullOrEmpty(it.lockedText) ? "SOON" : it.lockedText;
                v.buyText.fontSize = 30;
                return;
            }

            int cost = it.NextPrice;
            bool afford = money >= cost;
            v.name.text = it.itemName + "  Lv" + it.Level + " " + arrowText + " Lv" + (it.Level + 1);
            if (v.desc != null) v.desc.text = label + FormatValue(it.CurrentValue) + " " + arrowText + " " + FormatValue(it.NextValue);
            v.price.text = "$" + cost;
            v.price.color = afford ? new Color(1f, 0.92f, 0.3f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
            v.buyButton.interactable = afford;
            v.buyImage.color = afford ? new Color(0.2f, 0.7f, 1f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
            v.buyText.text = "UPGRADE";
            v.buyText.fontSize = 24;
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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
