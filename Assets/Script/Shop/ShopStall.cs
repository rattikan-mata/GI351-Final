using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
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
    /// หน้าตาหน้าซื้ออยู่ใน prefab ช่อง Panel Prefab (Prefab/UI/Shop Panel) แก้ใน Hierarchy ได้
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

        [Header("UI (หน้าตาหน้าซื้อ)")]
        [Tooltip("prefab หน้าซื้อ (Prefab/UI/Shop Panel) ดับเบิลคลิก prefab เพื่อแก้หน้าตาใน Hierarchy\nปล่อยว่าง = สร้างหน้าตาเริ่มต้นด้วยโค้ด")]
        [SerializeField] private ShopPanelView panelPrefab;

        [Header("Events")]
        public UnityEvent onOpened;
        public UnityEvent onClosed;

        private readonly List<ShopRowView> rows = new List<ShopRowView>();
        private readonly List<int> rowItemIndex = new List<int>(); // แถวที่ i แสดงสินค้าลำดับไหนในลิสต์ items

        private readonly List<Button> tabButtons = new List<Button>();
        private int currentTab;
        private Transform cachedBuyer;
        private float nextBuyerSearchTime;
        private bool isBuyerInside;
        private bool dismissedUntilExit;
        private bool isOpen;
        private bool builtUI;
        private int lastMoney = int.MinValue;

        private ShopPanelView view;

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
            UiAudioManager.Instance?.PlayClick();
            currentTab = Mathf.Clamp(index, 0, tabs.Count - 1);
            if (builtUI)
            {
                BuildRows();
                Refresh();
            }
        }

        /// <summary>รายชื่อแท็บ (ว่าง = ร้านนี้ไม่มีแท็บ)</summary>
        public IReadOnlyList<string> Tabs => tabs;

        /// <summary>สินค้าชิ้นนี้อยู่แท็บ tabIndex ไหม (ช่อง Tab ว่าง หรือชื่อไม่ตรงแท็บไหนเลย = อยู่แท็บแรก)</summary>
        public bool IsItemInTab(ShopItemData it, int tabIndex)
        {
            if (!HasTabs || it == null) return true;
            int idx = string.IsNullOrEmpty(it.tab) ? -1 : tabs.FindIndex(t => string.Equals(t, it.tab, StringComparison.OrdinalIgnoreCase));
            if (idx < 0) idx = 0;
            return idx == Mathf.Clamp(tabIndex, 0, tabs.Count - 1);
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
            if (MoneyManager.Instance.CurrentMoney < cost)
            {
                UiAudioManager.Instance?.PlayCantAfford();
                return false;
            }
            if (!MoneyManager.Instance.SpendMoney(cost)) return false;

            it.sold++;
            if (leveled)
            {
                it.Level = it.Level + 1;
                UiAudioManager.Instance?.PlayUpgrade();
                Debug.Log("[ShopStall] Upgraded " + it.itemName + " to Lv" + it.Level + " ($" + cost + ")");
            }
            else
            {
                UiAudioManager.Instance?.PlayBuyOrUnlockZone();
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
            if (Application.isPlaying) UiAudioManager.Instance?.PlayPanelToggle();

            if (open)
            {
                if (!builtUI) BuildUI();
                isOpen = true;
                view.panel.SetActive(true);
                BuildRows();
                Refresh();
                onOpened?.Invoke();
            }
            else
            {
                isOpen = false;
                if (view != null && view.panel != null) view.panel.SetActive(false);
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

        /// <summary>สร้างหน้าซื้อจาก prefab (หรือหน้าตาเริ่มต้นถ้าไม่ได้ใส่ prefab) แล้วผูกปุ่ม</summary>
        private void BuildUI()
        {
            builtUI = true;
            UiBuild.EnsureEventSystem();

            if (panelPrefab != null)
            {
                view = Instantiate(panelPrefab, transform, false);
                view.name = panelPrefab.name;
                string missing = view.FindMissingPart();
                if (missing != null)
                {
                    Debug.LogError("[ShopStall] " + name + ": prefab " + panelPrefab.name + " ขาดช่อง " + missing + " ใช้หน้าตาเริ่มต้นแทน", panelPrefab);
                    Destroy(view.gameObject);
                    view = null;
                }
            }
            if (view == null) view = ShopPanelView.CreateDefault(transform, HasTabs);

            view.panel.SetActive(false);
            if (view.templates != null) view.templates.SetActive(false);
            view.rowTemplate.gameObject.SetActive(false);
            UiBuild.SetText(view.titleText, shopTitle);
            if (view.exitButton != null)
            {
                view.exitButton.onClick.AddListener(() =>
                {
                    UiAudioManager.Instance?.PlayClick();
                    Close();
                });
            }

            BuildTabs();
        }

        /// <summary>ก๊อปปุ่มแท็บต้นแบบ 1 ปุ่มต่อ 1 แท็บ (ร้านที่ไม่มีแท็บ = ซ่อนแถบแท็บ)</summary>
        private void BuildTabs()
        {
            tabButtons.Clear();
            if (view.tabTemplate != null) view.tabTemplate.gameObject.SetActive(false);
            if (view.tabBar != null) view.tabBar.gameObject.SetActive(HasTabs);
            if (!HasTabs) return;

            if (view.tabBar == null || view.tabTemplate == null)
            {
                Debug.LogWarning("[ShopStall] " + name + ": ร้านนี้มีแท็บ แต่ prefab หน้าซื้อไม่มี Tab Bar / Tab Template เลยไม่แสดงแท็บ", this);
                return;
            }

            for (int i = 0; i < tabs.Count; i++)
            {
                int tabIndex = i;
                Button tab = SpawnTab(view, tabs[i]);
                tab.onClick.AddListener(() => SelectTab(tabIndex));
                tabButtons.Add(tab);
            }
        }

        /// <summary>ก๊อปแถวต้นแบบ 1 แถวต่อสินค้า 1 ชิ้นในแท็บที่เปิดอยู่</summary>
        private void BuildRows()
        {
            if (view == null) return;

            for (int i = 0; i < rows.Count; i++)
            {
                if (rows[i] == null) continue;
                rows[i].gameObject.SetActive(false); // ซ่อนทันที (Destroy จริงตอนจบเฟรม)
                Destroy(rows[i].gameObject);
            }
            rows.Clear();
            rowItemIndex.Clear();

            for (int i = 0; i < items.Count; i++)
            {
                int index = i;
                ShopItemData it = items[i];
                if (!IsItemInTab(it, currentTab)) continue;

                ShopRowView row = SpawnRow(view, it, i);
                row.buyButton.onClick.AddListener(() => TryBuy(index));
                rowItemIndex.Add(index);
                rows.Add(row);
            }

            if (view.emptyText != null)
            {
                view.emptyText.text = HasTabs ? emptyTabText : "No items yet";
                view.emptyText.gameObject.SetActive(rows.Count == 0);
            }
            RefreshTabs();
        }

        private void RefreshTabs()
        {
            int selected = Mathf.Clamp(currentTab, 0, tabs.Count - 1);
            for (int i = 0; i < tabButtons.Count; i++)
            {
                ApplyTabState(view, tabButtons[i], i == selected);
            }
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (view == null) return;
            UiBuild.SetText(view.moneyText, "$" + money);

            for (int i = 0; i < rows.Count && i < rowItemIndex.Count; i++)
            {
                ApplyRowState(view, rows[i], items[rowItemIndex[i]], money);
            }
        }

        // ---------------- ใช้ร่วมกันระหว่างตอนเล่น และตัวอย่างตอนแก้ prefab ----------------

        /// <summary>ก๊อปปุ่มแท็บต้นแบบลง Tab Bar แล้วใส่ชื่อแท็บ</summary>
        public static Button SpawnTab(ShopPanelView v, string tabName)
        {
            Button tab = Instantiate(v.tabTemplate, v.tabBar, false);
            tab.name = "Tab_" + tabName;
            tab.gameObject.SetActive(true);
            UiBuild.SetText(tab.GetComponentInChildren<Text>(true), tabName.ToUpperInvariant());
            return tab;
        }

        /// <summary>แท็บที่เลือกอยู่ใช้สี Tab Selected ที่เหลือใช้สี Tab Normal (ตั้งสีได้ที่ prefab หน้าซื้อ)</summary>
        public static void ApplyTabState(ShopPanelView v, Button tab, bool selected)
        {
            Image image = tab.targetGraphic as Image;
            if (image == null) image = tab.GetComponent<Image>();
            if (image != null) image.color = selected ? v.tabSelectedColor : v.tabNormalColor;
            Text label = tab.GetComponentInChildren<Text>(true);
            if (label != null) label.color = selected ? v.tabSelectedTextColor : v.tabNormalTextColor;
        }

        /// <summary>ก๊อปแถวต้นแบบลง List Content แล้วใส่ชื่อ/คำอธิบาย/ราคาของสินค้า</summary>
        public static ShopRowView SpawnRow(ShopPanelView v, ShopItemData it, int itemIndex)
        {
            ShopRowView row = Instantiate(v.rowTemplate, v.listContent, false);
            row.name = "Row_" + itemIndex;
            row.gameObject.SetActive(true);

            row.nameText.text = it.itemName;
            if (row.descText != null)
            {
                bool hasDesc = it.IsLeveled || !string.IsNullOrEmpty(it.description);
                row.descText.text = it.description ?? "";
                row.descText.gameObject.SetActive(hasDesc);
            }
            row.priceText.text = "$" + it.price;
            return row;
        }

        /// <summary>ข้อความ/สีปุ่มซื้อ และสีราคาของแถว ตามสถานะสินค้าและเงินที่มี</summary>
        public static void ApplyRowState(ShopPanelView view, ShopRowView v, ShopItemData it, int money)
        {
            if (it.locked)
            {
                // ยังไม่เปิดขาย: ปุ่มเทา กดไม่ได้
                SetButton(v, false, view.disabledColor, string.IsNullOrEmpty(it.lockedText) ? "SOON" : it.lockedText);
                v.priceText.text = "$" + it.price;
                v.priceText.color = view.priceLockedColor;
                return;
            }

            if (it.IsLeveled)
            {
                ApplyLeveledRowState(view, v, it, money);
                return;
            }

            bool soldOut = it.stock > 0 && it.sold >= it.stock;
            bool afford = money >= it.price;
            string soldOutLabel = string.IsNullOrEmpty(it.soldOutText) ? "SOLD OUT" : it.soldOutText;
            SetButton(v, !soldOut,
                soldOut ? view.disabledColor : (afford ? view.buyColor : view.cantAffordColor),
                soldOut ? soldOutLabel : view.buyLabel);
            v.priceText.text = it.stock > 1 && !soldOut ? "$" + it.price + "  x" + (it.stock - it.sold) : "$" + it.price;
            v.priceText.color = afford || soldOut ? view.priceColor : view.priceTooExpensiveColor;
        }

        private static void SetButton(ShopRowView v, bool interactable, Color color, string label)
        {
            v.buyButton.interactable = interactable;
            if (v.buyImage != null) v.buyImage.color = color;
            UiBuild.SetText(v.buyText, label);
        }

        /// <summary>แถวสินค้าแบบอัปเลเวล: "ชื่อ Lv2 → Lv3" / "Damage 30 → 40" / ราคา / ปุ่ม UPGRADE (MAX = ปุ่มเทา กดไม่ได้)</summary>
        private static void ApplyLeveledRowState(ShopPanelView view, ShopRowView v, ShopItemData it, int money)
        {
            string label = string.IsNullOrEmpty(it.valueLabel) ? "" : it.valueLabel + " ";
            Font font = v.nameText.font;
            string arrow = font != null && font.HasCharacter('→') ? "→" : ">";

            if (it.IsMaxLevel)
            {
                v.nameText.text = it.itemName + "  Lv" + it.Level + " " + view.maxLabel;
                UiBuild.SetText(v.descText, label + FormatValue(it.CurrentValue));
                v.priceText.text = "";
                SetButton(v, false, view.maxColor, view.maxLabel);
                return;
            }

            if (it.IsAtLevelCap)
            {
                // เลเวลถัดไปเตรียมไว้แล้วแต่ยังไม่เปิดขาย
                v.nameText.text = it.itemName + "  Lv" + it.Level;
                UiBuild.SetText(v.descText, label + FormatValue(it.CurrentValue));
                v.priceText.text = "";
                SetButton(v, false, view.disabledColor, string.IsNullOrEmpty(it.lockedText) ? "SOON" : it.lockedText);
                return;
            }

            int cost = it.NextPrice;
            bool afford = money >= cost;
            v.nameText.text = it.itemName + "  Lv" + it.Level + " " + arrow + " Lv" + (it.Level + 1);
            UiBuild.SetText(v.descText, label + FormatValue(it.CurrentValue) + " " + arrow + " " + FormatValue(it.NextValue));
            v.priceText.text = "$" + cost;
            v.priceText.color = afford ? view.priceColor : view.priceTooExpensiveColor;
            SetButton(v, true, afford ? view.buyColor : view.disabledColor, view.upgradeLabel);
        }

        private static string FormatValue(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
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
