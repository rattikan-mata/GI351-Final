using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    /// <summary>
    /// แผงอัปเกรดของแผงขายอาหาร (FoodSellCounter) และโต๊ะจ่ายเงิน (CashierCounter) แบบเดียวกับ PotUpgrade ของหม้อ
    /// เดินเข้าวงของแผง/โต๊ะ -> แผงด้านล่างจอเด้งขึ้นมา (ใช้ prefab เดียวกับหม้อ: PotUpgradePanelView)
    ///   Shelf   : แถว A = จำนวนจานที่แผงนี้จุได้ (อัปแยกแต่ละแผง) แถว B ไม่ใช้ (ซ่อน)
    ///   Cashier : แถว A = ลูกค้ากี่คนต่อนาที (CustomerSpawner ทุกตัว), แถว B = ลูกค้าซื้อได้สูงสุดคนละกี่จาน
    /// ชนิดเลือกเองจากคอมโพเนนต์บน GameObject เดียวกัน (มี FoodSellCounter = Shelf, มี CashierCounter = Cashier)
    /// ตอนเริ่มเกม apply ค่า Base (Lv1) ให้ ลิสต์เลเวลว่าง = แถวนั้นอัปไม่ได้ (แถว B ว่าง = ซ่อนแถว)
    /// </summary>
    public class StationUpgrade : MonoBehaviour
    {
        public enum Station
        {
            Shelf,
            Cashier
        }

        /// <summary>ค่าที่แถวอัปเกรดเปลี่ยน</summary>
        public enum Stat
        {
            Auto,                   // ตามชนิดจุด: Shelf แถว A = ความจุ (แถว B ไม่ใช้) / Cashier แถว A = ลูกค้า แถว B = จานต่อคน
            None,                   // แถวนี้ไม่ใช้ (ซ่อน)
            ShelfCapacity,          // จำนวนจานที่แผงนี้จุได้ (แผงนี้แผงเดียว)
            CustomersPerMinute,     // ลูกค้ากี่คนต่อนาที (ทุก CustomerSpawner)
            MaxDishesPerCustomer    // ลูกค้าซื้อได้สูงสุดคนละกี่จาน (ทุก CustomerSpawner)
        }

        /// <summary>ข้อมูลแถวอัปเกรดหนึ่งแถว</summary>
        [Serializable]
        public class Row
        {
            [Tooltip("ชื่อแถวบนแผง")]
            public string label = "";

            [Tooltip("ค่าที่แถวนี้เปลี่ยน (Auto = ตามชนิดจุด) เช่น แผงไก่ แถว B = Customers Per Minute ย้ายอัปเกรดลูกค้ามาไว้ที่แผง")]
            public Stat stat = Stat.Auto;

            [Tooltip("ค่าตอนเลเวล 1 (apply ให้ตอนเริ่มเกม)")]
            public float baseValue = 1f;

            [Tooltip("เลเวลเรียงจากเลเวล 2 ขึ้นไป: Cost = ราคาอัปเกรด, Value = ค่าหลังอัปเกรด (ค่าสุดท้ายเลย ไม่ใช่ค่าที่บวกเพิ่ม)")]
            public List<PotUpgradeLevel> levels = new List<PotUpgradeLevel>();

            [Tooltip("ข้อความต่อท้ายค่าบนแผง เช่น \"/min\" หรือ \" max\" (ว่างได้)")]
            public string valueSuffix = "";

            [Tooltip("รูปแบบตัวเลขบนแผง (เช่น \"0\" = จำนวนเต็ม, \"0.#\" = ทศนิยม 1 ตำแหน่ง)")]
            public string valueFormat = "0.#";
        }

        [Tooltip("ชนิดของจุดอัปเกรด (เลือกเองจากคอมโพเนนต์: มี FoodSellCounter = Shelf, มี CashierCounter = Cashier) ค่านี้ใช้เมื่อหาไม่เจอทั้งคู่")]
        [SerializeField] private Station station = Station.Cashier;

        [Header("Row A (Shelf = ความจุแผง, Cashier = ลูกค้าต่อนาที)")]
        [Tooltip("แถวบน: Shelf = จำนวนจานสูงสุดบนแผงนี้, Cashier = ลูกค้ากี่คนต่อนาที (ทุก CustomerSpawner)")]
        [SerializeField] private Row rowA = new Row { label = "Customers", baseValue = 8f, valueSuffix = "/min", valueFormat = "0.#" };

        [Header("Row B (Cashier = จานสูงสุดต่อคน, Shelf ไม่ใช้)")]
        [Tooltip("แถวล่าง: Cashier = ลูกค้าซื้อได้สูงสุดคนละกี่จาน (ทุก CustomerSpawner) ลิสต์ว่าง = ซ่อนแถวนี้ / Shelf ไม่ใช้ (ซ่อนเสมอ)")]
        [SerializeField] private Row rowB = new Row { label = "Big Orders", baseValue = 1f, valueSuffix = " max", valueFormat = "0" };

        [Tooltip("แคชเชียร์: เปิดแผงอัปเกรดได้เฉพาะหลังจ้างพนักงานแคชเชียร์แล้ว (ร้าน Staff)")]
        [SerializeField] private bool cashierNeedsStaff = true;

        [Header("Panel (แผงอัปเกรด)")]
        [Tooltip("ซ่อนแผงถ้าทั้งสองลิสต์ยังว่าง (ไม่มีอะไรให้อัปเกรด)")]
        [SerializeField] private bool hideWhenNoLevels = true;

        [Tooltip("หน่วงก่อนเปิดแผงหลังเดินเข้าวง (วินาที) 0 = เปิดทันที")]
        [Min(0f)]
        [SerializeField] private float openDelay = 0f;

        [Tooltip("หน่วงก่อนปิดแผงหลังเดินออกจากวง (วินาที) กันแผงกระพริบเวลายืนตรงขอบวง")]
        [Min(0f)]
        [SerializeField] private float closeDelay = 0.35f;

        [Tooltip("prefab แผงอัปเกรด (Prefab/UI/Pot Upgrade Panel ใช้ร่วมกับหม้อ)\nปล่อยว่าง = สร้างหน้าตาเริ่มต้นด้วยโค้ด")]
        [SerializeField] private PotUpgradePanelView panelPrefab;

        [Header("Events")]
        [Tooltip("ยิงเมื่ออัปเกรดสำเร็จ")]
        public UnityEvent onUpgraded;

        private FoodSellCounter shelf;
        private CashierCounter cashier;
        private int levelA;
        private int levelB;
        private bool baseApplied;

        private PotUpgradePanelView view;
        private bool isOpen;
        private float lastInRangeTime;
        private float enterRangeTime = -1f;
        private int lastMoney = int.MinValue;
        private bool builtUI;

        public Station CurrentStation => station;
        /// <summary>เลเวลแถว A ตอนนี้ (0 = Lv1)</summary>
        public int LevelA => levelA;
        /// <summary>เลเวลแถว B ตอนนี้ (0 = Lv1)</summary>
        public int LevelB => levelB;
        public bool IsOpen => isOpen;

        /// <summary>จุดนี้อัปเกรดได้แล้วไหม (แผงอาหาร: เปิดขายแล้ว / แคชเชียร์: จ้างพนักงานแล้ว)</summary>
        public bool IsAvailable
        {
            get
            {
                if (station == Station.Shelf) return shelf != null && shelf.IsOpen;
                return cashier != null && (!cashierNeedsStaff || cashier.HasStaff);
            }
        }

        /// <summary>ผู้เล่นเคยเปิดแผงนี้แล้ว (สอนเล่น: ชี้ให้มาดูครั้งเดียว)</summary>
        public bool EverOpened { get; private set; }

        /// <summary>ราคาอัปเกรดถูกสุดที่ยังซื้อได้ (ไม่มีให้อัปแล้ว = -1)</summary>
        public int CheapestNextCost
        {
            get
            {
                int best = -1;
                if (levelA < rowA.levels.Count) best = rowA.levels[levelA].cost;
                if (UsesRowB && levelB < rowB.levels.Count)
                {
                    int c = rowB.levels[levelB].cost;
                    if (best < 0 || c < best) best = c;
                }
                return best;
            }
        }

        /// <summary>ใส่ค่าเริ่มต้นตามชนิดตอนเพิ่มคอมโพเนนต์ใน Editor</summary>
        private void Reset()
        {
            DetectStation();
            if (station == Station.Shelf)
            {
                rowA = new Row { label = "Shelf Capacity", baseValue = 20f, valueSuffix = "", valueFormat = "0" };
                rowB = new Row { label = "", baseValue = 0f, valueSuffix = "", valueFormat = "0" };
            }
            else
            {
                rowA = new Row { label = "Customers", baseValue = 8f, valueSuffix = "/min", valueFormat = "0.#" };
                rowB = new Row { label = "Big Orders", baseValue = 1f, valueSuffix = " max", valueFormat = "0" };
            }
        }

        private void Awake()
        {
            DetectStation();
        }

        private void Start()
        {
            ApplyBase();
        }

        private void DetectStation()
        {
            shelf = GetComponent<FoodSellCounter>();
            cashier = GetComponent<CashierCounter>();
            if (shelf != null) station = Station.Shelf;
            else if (cashier != null) station = Station.Cashier;
        }

        /// <summary>apply ค่า Lv1 ของทุกแถว (ครั้งเดียวตอนเริ่ม)</summary>
        private void ApplyBase()
        {
            if (baseApplied) return;
            baseApplied = true;
            ApplyRow(true, rowA.baseValue);
            if (UsesRowB) ApplyRow(false, rowB.baseValue);

            // กันตั้งค่าผิด: แผงที่ใช้ Plate Slots จะจุได้เท่าจำนวนช่องเสมอ อัปเกรดความจุจะไม่มีผล
            if (StatFor(true) == Stat.ShelfCapacity && shelf != null && rowA.levels.Count > 0 && shelf.MaxPlates != Mathf.Max(1, Mathf.RoundToInt(rowA.baseValue)))
            {
                Debug.LogWarning("[StationUpgrade] " + name + ": แผงนี้ใช้ Plate Slots (" + shelf.MaxPlates + " ช่อง) อัปเกรด " + rowA.label + " จะไม่มีผล", this);
            }
        }

        private void OnDisable()
        {
            if (view != null && view.panel != null) view.panel.SetActive(false);
            StationCameraZoom.Release(this);
            isOpen = false;
            enterRangeTime = -1f;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            bool hasLevels = rowA.levels.Count > 0 || (UsesRowB && rowB.levels.Count > 0);
            bool shouldShow = IsPlayerInStation && (hasLevels || !hideWhenNoLevels);

            if (shouldShow)
            {
                // จำเวลาที่เข้าวง แล้วเปิดเมื่อครบเวลาหน่วง (0 = ทันที)
                lastInRangeTime = Time.unscaledTime;
                if (enterRangeTime < 0f) enterRangeTime = Time.unscaledTime;
                if (!isOpen && Time.unscaledTime - enterRangeTime >= openDelay) SetOpen(true);
            }
            else
            {
                enterRangeTime = -1f;
                if (isOpen && Time.unscaledTime - lastInRangeTime >= closeDelay)
                {
                    // ออกจากวงนานกว่าเวลาหน่วงแล้วค่อยปิด (กันกระพริบตอนยืนตรงขอบวง)
                    SetOpen(false);
                }
            }

            if (isOpen && CurrentMoney != lastMoney) Refresh();
        }

        /// <summary>ผู้เล่นอยู่ในวงของแผง/โต๊ะไหม (แผงขายต้องเปิดขายแล้วด้วย)</summary>
        private bool IsPlayerInStation
        {
            get
            {
                if (station == Station.Shelf) return shelf != null && shelf.IsOpen && shelf.IsPlayerInRange;
                return IsAvailable && cashier.PlayerInRing;   // แคชเชียร์: ต้องจ้างพนักงานแคชเชียร์ก่อน
            }
        }

        /// <summary>แถว B ใช้ได้ไหม</summary>
        private bool UsesRowB => StatFor(false) != Stat.None;

        /// <summary>ค่าที่แถวนี้เปลี่ยนจริง (Auto = ตามชนิดจุดแบบเดิม)</summary>
        private Stat StatFor(bool isA)
        {
            Stat s = isA ? rowA.stat : rowB.stat;
            if (s != Stat.Auto) return s;
            if (station == Station.Shelf) return isA ? Stat.ShelfCapacity : Stat.None;
            return isA ? Stat.CustomersPerMinute : Stat.MaxDishesPerCustomer;
        }

        // ---------------- Logic ----------------

        private int CurrentMoney => MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        private static float CurrentValue(Row row, int level) => level <= 0 ? row.baseValue : row.levels[Mathf.Min(level, row.levels.Count) - 1].value;

        public bool TryUpgradeA() => TryUpgrade(true);
        public bool TryUpgradeB() => TryUpgrade(false);

        private bool TryUpgrade(bool isA)
        {
            if (!isA && !UsesRowB) return false;
            ApplyBase();
            Row row = isA ? rowA : rowB;
            int level = isA ? levelA : levelB;
            if (level >= row.levels.Count || MoneyManager.Instance == null) return false;

            PotUpgradeLevel next = row.levels[level];
            if (MoneyManager.Instance.CurrentMoney < next.cost)
            {
                UiAudioManager.Instance?.PlayCantAfford();
                if (view != null) JuiceCantAfford(isA ? view.priceRow : view.speedRow);
                return false;
            }
            if (!MoneyManager.Instance.SpendMoney(next.cost)) return false;

            if (isA) levelA++;
            else levelB++;
            ApplyRow(isA, next.value);

            Debug.Log("[StationUpgrade] " + name + " " + row.label + " -> Lv" + ((isA ? levelA : levelB) + 1) + " (" + next.value.ToString("0.##") + ")");
            UiAudioManager.Instance?.PlayPurchase();   // เสียงคิดเงิน
            if (view != null) JuiceUpgraded(isA ? view.priceRow : view.speedRow);
            onUpgraded?.Invoke();
            if (isOpen) Refresh();
            return true;
        }

        /// <summary>ใส่ค่าของแถวลงเกม</summary>
        private void ApplyRow(bool isA, float value)
        {
            switch (StatFor(isA))
            {
                case Stat.ShelfCapacity:
                    // ความจุของแผงนี้แผงเดียว
                    if (shelf != null) shelf.SetMaxPlates(Mathf.RoundToInt(value));
                    break;
                case Stat.CustomersPerMinute:
                    foreach (CustomerSpawner s in FindObjectsByType<CustomerSpawner>(FindObjectsInactive.Include)) s.SetCustomersPerMinute(value);
                    break;
                case Stat.MaxDishesPerCustomer:
                    foreach (CustomerSpawner s in FindObjectsByType<CustomerSpawner>(FindObjectsInactive.Include)) s.SetMaxDishesPerCustomer(Mathf.RoundToInt(value));
                    break;
            }
        }

        private void SetOpen(bool open)
        {
            if (open == isOpen) return;
            if (Application.isPlaying) UiAudioManager.Instance?.PlayPanelToggle();
            isOpen = open;
            if (open)
            {
                EverOpened = true;
                ApplyBase();
                if (!builtUI) BuildUI();
                view.panel.SetActive(true);
                StationCameraZoom.Focus(this, transform);   // ซูมกล้องเข้าหาเครื่องแบบ Juice King
                Refresh();
            }
            else
            {
                if (view != null && view.panel != null) view.panel.SetActive(false);
                StationCameraZoom.Release(this);
            }
        }

        // ---------------- จุ้ย ----------------

        /// <summary>อัปสำเร็จ: ปุ่ม / เลข LV / ค่า เด้ง และตัวเครื่องในฉากยืดหดดึ๋ง</summary>
        private void JuiceUpgraded(PotUpgradePanelView.Row row)
        {
            // แคชเชียร์: ยืดหดแค่ภาพเครื่อง ไม่ใช่ตัวหลัก (กองเหรียญบนโต๊ะจะได้ไม่บิดเบี้ยวค้าง)
            Transform art = transform.Find("Cashier Art");
            if (art != null) UiJuice.Squash(art);
            else
            {
                // แผงอาหาร: เด้งจบแล้วจัดจานใหม่ เผื่อมีจานถูกวาง/หยิบระหว่างที่แผงยืดหดอยู่ (กันจานเบี้ยวค้าง)
                FoodSellCounter counter = GetComponent<FoodSellCounter>();
                UiJuice.Squash(transform, onDone: counter != null ? counter.RelayoutFood : (System.Action)null);
            }
            if (row == null) return;
            if (row.button != null) UiJuice.Punch(row.button.transform, 0.16f);
            if (row.levelText != null) UiJuice.Punch(row.levelText.transform, 0.45f, 0.4f);
            if (row.valuesText != null) UiJuice.Punch(row.valuesText.transform, 0.3f, 0.4f);
        }

        /// <summary>เงินไม่พอ: ปุ่มสั่นซ้ายขวา</summary>
        private void JuiceCantAfford(PotUpgradePanelView.Row row)
        {
            if (row != null && row.button != null) UiJuice.Shake(row.button.transform);
        }

        // ---------------- UI ----------------

        /// <summary>สร้างแผงจาก prefab (หรือหน้าตาเริ่มต้นถ้าไม่ได้ใส่ prefab) แล้วผูกปุ่ม</summary>
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
                    Debug.LogError("[StationUpgrade] " + name + ": prefab " + panelPrefab.name + " ขาดช่อง " + missing + " ใช้หน้าตาเริ่มต้นแทน", panelPrefab);
                    Destroy(view.gameObject);
                    view = null;
                }
            }
            if (view == null) view = PotUpgradePanelView.CreateDefault(transform);

            view.panel.SetActive(false);
            HideDuringCameraPan.Attach(view.gameObject);   // กล้องดึงจอ = ซ่อนแผงชั่วคราว
            UiBuild.SetText(view.priceRow.nameText, rowA.label);
            UiBuild.SetText(view.speedRow.nameText, rowB.label);
            view.priceRow.button.onClick.AddListener(() => TryUpgrade(true));
            view.speedRow.button.onClick.AddListener(() => TryUpgrade(false));

            // รูปเหรียญอีแปะแทน $: เงินที่มี / ค่าอัปเกรดบนปุ่ม (ค่าในแถวเป็นจำนวน ไม่ใส่เหรียญ)
            CoinTextIcon.Attach(view.moneyText);
            CoinTextIcon.Attach(view.priceRow.buttonText);
            CoinTextIcon.Attach(view.speedRow.buttonText);

            // แถว B ว่าง (หรือเป็น Shelf) = ซ่อนทั้งแถว
            if (!UsesRowB || rowB.levels.Count == 0) HideRow(view.speedRow);
        }

        /// <summary>ซ่อนแถวบนแผง: หา parent ร่วมที่ใกล้สุดของชื่อแถวกับปุ่ม (ไม่ซ่อนตัวแผงเอง) หาไม่เจอ = ซ่อนทีละชิ้น</summary>
        private void HideRow(PotUpgradePanelView.Row row)
        {
            Transform root = FindRowRoot(row);
            if (root != null)
            {
                root.gameObject.SetActive(false);
                SlidePanelDownForMissingRow(root as RectTransform);
                return;
            }
            if (row.nameText != null) row.nameText.gameObject.SetActive(false);
            if (row.levelText != null) row.levelText.gameObject.SetActive(false);
            if (row.valuesText != null) row.valuesText.gameObject.SetActive(false);
            if (row.button != null) row.button.gameObject.SetActive(false);
        }

        /// <summary>
        /// แผงมีแถวเดียว (เช่น Shelf): เลื่อนแผงลงเท่าระยะห่างระหว่างแถว A กับแถว B ให้ช่องว่างใต้แถว A ไปอยู่นอกจอ
        /// (แผงติดขอบล่างจอ ขอบล่างของกรอบล้นจออยู่แล้ว) ไม่ให้แผงบังฉากเกินจำเป็น
        /// </summary>
        private void SlidePanelDownForMissingRow(RectTransform hiddenRow)
        {
            if (hiddenRow == null || view == null || view.panel == null || view.priceRow.nameText == null) return;
            RectTransform rowA = FindRowRoot(view.priceRow) as RectTransform;
            RectTransform panelRect = view.panel.transform as RectTransform;
            if (rowA == null || panelRect == null || rowA.parent != hiddenRow.parent) return;
            float step = Mathf.Abs(hiddenRow.anchoredPosition.y - rowA.anchoredPosition.y);
            panelRect.anchoredPosition -= new Vector2(0f, step);
        }

        private Transform FindRowRoot(PotUpgradePanelView.Row row)
        {
            if (row.nameText == null || row.button == null || view.panel == null) return null;
            Transform panelT = view.panel.transform;

            // เก็บ parent ทั้งหมดของชื่อแถวจนถึงแผง (ไม่รวมแผง)
            HashSet<Transform> nameAncestors = new HashSet<Transform>();
            for (Transform t = row.nameText.transform; t != null && t != panelT; t = t.parent)
            {
                nameAncestors.Add(t);
                if (t.parent == null) return null; // ชื่อแถวไม่ได้อยู่ใต้แผง
            }

            // ไล่ขึ้นจากปุ่มจนเจอตัวที่อยู่ในชุดเดียวกัน
            for (Transform t = row.button.transform; t != null && t != panelT; t = t.parent)
            {
                if (nameAncestors.Contains(t)) return t;
            }
            return null;
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (view == null) return;
            UiBuild.SetText(view.titleText, station == Station.Shelf ? name.ToUpper() : "CASHIER");
            UiBuild.SetText(view.moneyText, money.ToString());

            RefreshRow(view.priceRow, rowA, levelA, money);
            if (UsesRowB) RefreshRow(view.speedRow, rowB, levelB, money);
        }

        private void RefreshRow(PotUpgradePanelView.Row row, Row data, int level, int money)
        {
            row.levelText.text = "LV " + (level + 1);

            string cur = FormatValue(data, CurrentValue(data, level));

            if (data.levels.Count == 0 || level >= data.levels.Count)
            {
                // ไม่มีเลเวลให้อัป หรืออัปจนเต็มแล้ว
                row.valuesText.text = cur;
                SetButton(row, false, view.disabledColor, data.levels.Count == 0 ? view.noLevelsLabel : view.maxLabel, view.buttonTextColor);
                CoinTextIcon.SetVisible(row.buttonText, false);
                return;
            }

            PotUpgradeLevel next = data.levels[level];
            row.valuesText.text = cur;   // บอกค่าตอนนี้เฉยๆ (ไม่มีลูกศร)

            bool afford = money >= next.cost;
            SetButton(row, true, afford ? view.affordColor : view.disabledColor, next.cost.ToString(),
                afford ? view.buttonTextColor : view.buttonTextTooExpensiveColor);
            CoinTextIcon.SetVisible(row.buttonText, true);
        }

        private static string FormatValue(Row data, float value)
        {
            string format = string.IsNullOrEmpty(data.valueFormat) ? "0.#" : data.valueFormat;
            return value.ToString(format) + data.valueSuffix;
        }

        private static void SetButton(PotUpgradePanelView.Row row, bool interactable, Color color, string label, Color textColor)
        {
            row.button.interactable = interactable;
            if (row.buttonImage != null) row.buttonImage.color = color;
            row.buttonText.text = label;
            row.buttonText.color = textColor;
        }
    }
}
