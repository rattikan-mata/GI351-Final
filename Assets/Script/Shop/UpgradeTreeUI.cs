using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace GI351.KitchenGame
{
    /// <summary>ข้อมูลโหนดหนึ่งช่องในผังอัปเกรด</summary>
    [Serializable]
    public class UpgradeNodeData
    {
        [Tooltip("ชื่อที่แสดงบนช่อง (ภาษาอังกฤษ เพราะฟอนต์ในตัวไม่มีภาษาไทย)")]
        public string label = "Upgrade";

        [Tooltip("คำอธิบายตอนเอาเมาส์ชี้ (ปล่อยว่าง = สร้างข้อความจากชนิดผลให้อัตโนมัติ)")]
        public string description = "";

        [Tooltip("ตำแหน่งช่องแนวนอนในผังรังผึ้ง (พิกัด axial q) ช่องข้างเคียงคือ (±1,0) (0,±1) (+1,-1) (-1,+1)")]
        public int q;

        [Tooltip("ตำแหน่งช่องแนวตั้งในผังรังผึ้ง (พิกัด axial r) ค่าเพิ่ม = ลงล่าง")]
        public int r;

        [Tooltip("ราคาที่ต้องจ่าย (เหรียญ)")]
        public int price = 50;

        [Tooltip("ยิงเมื่อซื้อช่องนี้สำเร็จ ลากสคริปต์ที่จะให้ผลอัปเกรดมาผูกที่นี่ (ตอนนี้ยังไม่มีผลอะไร)")]
        public UnityEngine.Events.UnityEvent onPurchased;

        [Tooltip("ติ๊กถ้าเป็นช่องสำรองที่ยังไม่เปิดให้ซื้อ (แสดงเป็น ? ?)")]
        public bool comingSoon = false;

        [Tooltip("ติ๊กถ้าเป็นช่องเริ่มต้น (ถือว่าซื้อแล้วตั้งแต่เริ่มเกม ฟรี)")]
        public bool startsOwned = false;

        public UpgradeNodeData() { }

        public UpgradeNodeData(string label, int q, int r, int price)
        {
            this.label = label;
            this.q = q;
            this.r = r;
            this.price = price;
        }
    }

    /// <summary>
    /// หน้าจออัปเกรดแบบผังหกเหลี่ยม (passive tree): ปุ่มกลางล่างจอ -> กดแล้วเปิดผัง -> ใช้เงินปลดล็อกช่อง
    /// ซื้อได้เฉพาะช่องที่ติดกับช่องที่ซื้อแล้ว (เริ่มจากช่องกลาง) ปุ่ม Exit / Esc / U ปิดหน้าจอ
    /// UI ถูกสร้างตอนเล่น (ไม่ต้องลาก Canvas) ปรับราคา/ค่า/ตำแหน่งช่องได้ที่ลิสต์ Nodes ใน Inspector
    /// </summary>
    public class UpgradeTreeUI : MonoBehaviour
    {
        [Header("Upgrade Nodes (ช่องอัปเกรดทั้งหมด)")]
        [Tooltip("รายการช่องในผัง: ราคา / ผลที่ได้ / ตำแหน่ง (q,r) แก้ได้ทั้งหมด คลิกขวาที่ Component > Reset Nodes To Default เพื่อคืนค่าเริ่มต้น")]
        [SerializeField] private List<UpgradeNodeData> nodes = CreateDefaultNodes();

        [Header("Behaviour (การทำงาน)")]
        [Tooltip("หยุดเกม (Time.timeScale = 0) ขณะเปิดหน้าอัปเกรด")]
        [SerializeField] private bool pauseWhileOpen = true;

        [Tooltip("เห็นช่องไกลแค่ไหนจากช่องที่ซื้อแล้ว: 1 = เห็นเฉพาะช่องที่ซื้อได้, 2 = เห็นเพิ่มอีกชั้นเป็น ? (ไกลกว่านั้นซ่อนไว้)")]
        [Range(1, 4)]
        [SerializeField] private int revealDepth = 2;

        [Tooltip("ปุ่มคีย์ลัดเปิด/ปิดหน้าอัปเกรด")]
#if ENABLE_INPUT_SYSTEM
        [SerializeField] private Key toggleKey = Key.U;
#else
        [SerializeField] private KeyCode toggleKey = KeyCode.U;
#endif

        [Tooltip("ข้อความบนปุ่มกลางล่างจอ")]
        [SerializeField] private string openButtonText = "UPGRADES";

        [Header("Layout (ขนาดผัง)")]
        [Tooltip("รัศมีของหกเหลี่ยมหนึ่งช่อง (พิกเซลที่ความละเอียดอ้างอิง 1920x1080)")]
        [SerializeField] private float cellRadius = 54f;

        [Tooltip("ขนาดหกเหลี่ยมเทียบกับช่องว่าง (1 = ชิดกัน, 0.94 = เว้นช่องไฟเล็กน้อย)")]
        [Range(0.7f, 1f)]
        [SerializeField] private float hexFill = 0.94f;

        [Tooltip("เลื่อนผังขึ้น/ลง (พิกเซล)")]
        [SerializeField] private float treeOffsetY = 50f;

        [Tooltip("ลำดับการวาดของ Canvas (สูง = ทับ UI อื่น)")]
        [SerializeField] private int canvasSortingOrder = 100;

        [Header("Colors (สี)")]
        [SerializeField] private Color ownedBorder = new Color(0.45f, 1f, 0.7f, 1f);
        [SerializeField] private Color ownedFill = new Color(0.1f, 0.55f, 0.4f, 1f);
        [SerializeField] private Color availableBorder = Color.white;
        [SerializeField] private Color availableFill = new Color(0.2f, 0.7f, 1f, 1f);
        [SerializeField] private Color brokeBorder = new Color(0.6f, 0.8f, 1f, 1f);
        [SerializeField] private Color brokeFill = new Color(0.15f, 0.35f, 0.6f, 1f);
        [SerializeField] private Color lockedBorder = new Color(0.32f, 0.32f, 0.4f, 1f);
        [SerializeField] private Color lockedFill = new Color(0.12f, 0.12f, 0.17f, 1f);

        [Header("Events")]
        [Tooltip("ยิงเมื่อซื้อโหนดสำเร็จ ส่ง index ของโหนด")]
        public UnityEngine.Events.UnityEvent<int> onNodePurchased;

        private enum NodeState { Owned, Available, Locked, ComingSoon }

        private class NodeView
        {
            public RectTransform root;
            public Image border;
            public Image fill;
            public Text label;
            public Text price;
        }

        private readonly List<NodeView> views = new List<NodeView>();
        private readonly HashSet<int> owned = new HashSet<int>();
        private readonly Dictionary<Vector2Int, int> indexByCell = new Dictionary<Vector2Int, int>();

        private GameObject canvasObject;
        private GameObject panel;
        private Text moneyText;
        private Text infoText;
        private int hoveredIndex = -1;
        private int lastMoney = int.MinValue;
        private bool isOpen;
        private float previousTimeScale = 1f;
        private bool builtUI;

        private static Font uiFont;
        private static Sprite hexSpriteBorder;
        private static Sprite hexSpriteFill;
        private static bool anyTreeOpen;

        private const string DefaultInfo = "Hover an upgrade to see details. Click to buy.";
        private const float HexTexWidth = 222f;
        private const float HexTexHeight = 256f;

        public bool IsOpen => isOpen;
        public int NodeCount => nodes != null ? nodes.Count : 0;

        /// <summary>true ถ้าหน้าอัปเกรดเปิดอยู่ หรือเมาส์อยู่เหนือ UI (ใช้กันไม่ให้คลิก UI แล้วฟันกระบอง)</summary>
        public static bool IsBlockingGameInput
        {
            get
            {
                if (anyTreeOpen) return true;
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            anyTreeOpen = false;
            hexSpriteBorder = null;
            hexSpriteFill = null;
            uiFont = null;
        }

        // ---------------- Default data ----------------

        private static List<UpgradeNodeData> CreateDefaultNodes()
        {
            // โครงว่างๆ: ช่องกลางเริ่มต้นฟรี + ช่องทดสอบรอบๆ ตามรูปทรงรังผึ้ง (ยังไม่มีผลอัปเกรดจริง)
            // ราคาเพิ่มตามระยะห่างจากช่องกลาง แก้ชื่อ/ราคา/ตำแหน่งได้ใน Inspector
            List<UpgradeNodeData> list = new List<UpgradeNodeData>
            {
                new UpgradeNodeData("SHOP", 0, 0, 0) { startsOwned = true },
            };

            Vector2Int[] cells =
            {
                new Vector2Int(-1, 0), new Vector2Int(1, -1), new Vector2Int(0, 1),
                new Vector2Int(-2, 0), new Vector2Int(2, -2), new Vector2Int(0, 2),
                new Vector2Int(-3, 0), new Vector2Int(3, -3), new Vector2Int(0, 3),
            };
            int[] prices = { 30, 40, 50, 70, 90, 110, 150, 180, 220 };
            for (int i = 0; i < cells.Length; i++)
            {
                list.Add(new UpgradeNodeData("Upgrade", cells[i].x, cells[i].y, prices[i]));
            }

            // ช่องสำรอง ? ? (ปลายแขน)
            Vector2Int[] soon = { new Vector2Int(-4, 0), new Vector2Int(4, -4), new Vector2Int(0, 4) };
            for (int i = 0; i < soon.Length; i++)
            {
                list.Add(new UpgradeNodeData("?", soon[i].x, soon[i].y, 0) { comingSoon = true });
            }
            return list;
        }

        [ContextMenu("Reset Nodes To Default")]
        private void ResetNodesToDefault()
        {
            nodes = CreateDefaultNodes();
        }

        private void Reset()
        {
            nodes = CreateDefaultNodes();
        }

        // ---------------- Lifecycle ----------------

        private void Awake()
        {
            BuildUI();
            SetOpen(false);
        }

        private void OnDisable()
        {
            if (isOpen) SetOpen(false);
        }

        private void OnDestroy()
        {
            if (isOpen) SetOpen(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                if (kb[toggleKey].wasPressedThisFrame) Toggle();
                else if (isOpen && kb.escapeKey.wasPressedThisFrame) Close();
            }
#else
            if (Input.GetKeyDown(toggleKey)) Toggle();
            else if (isOpen && Input.GetKeyDown(KeyCode.Escape)) Close();
#endif

            if (!isOpen) return;

            // เงินเปลี่ยนระหว่างเปิดหน้านี้ (เช่น เหรียญลอยเข้า) ให้รีเฟรชสีราคา
            int money = CurrentMoney;
            if (money != lastMoney) RefreshAll();
        }

        // ---------------- Public API ----------------

        public void Open() => SetOpen(true);
        public void Close() => SetOpen(false);
        public void Toggle() => SetOpen(!isOpen);

        public bool IsOwned(int index) => owned.Contains(index);

        /// <summary>จำนวนช่องที่ซื้อแล้ว (ไม่นับช่องเริ่มต้น)</summary>
        public int PurchasedCount
        {
            get
            {
                int c = 0;
                foreach (int i in owned) if (!nodes[i].startsOwned) c++;
                return c;
            }
        }

        public bool CanPurchase(int index)
        {
            return GetState(index) == NodeState.Available && CurrentMoney >= nodes[index].price;
        }

        /// <summary>ซื้อช่องที่ index (เช็คช่องข้างเคียง + เงิน) คืน true ถ้าสำเร็จ</summary>
        public bool TryPurchase(int index)
        {
            if (!builtUI) BuildUI();
            if (index < 0 || index >= nodes.Count) return false;
            if (GetState(index) != NodeState.Available) return false;
            if (MoneyManager.Instance == null) return false;

            UpgradeNodeData n = nodes[index];
            if (MoneyManager.Instance.CurrentMoney < n.price)
            {
                UiAudioManager.Instance?.PlayCantAfford();
                return false;
            }
            if (!MoneyManager.Instance.SpendMoney(n.price)) return false;

            owned.Add(index);
            n.onPurchased?.Invoke();
            Debug.Log("[UpgradeTree] Purchased " + n.label + " ($" + n.price + ")");
            UiAudioManager.Instance?.PlayUpgrade();
            onNodePurchased?.Invoke(index);
            RefreshAll();
            return true;
        }

        // ---------------- Logic ----------------

        private int CurrentMoney => MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        private static readonly Vector2Int[] NeighborOffsets =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1),
            new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, 1)
        };

        /// <summary>ระยะ (จำนวนช่อง) จากช่องนี้ถึงช่องที่ซื้อแล้วที่ใกล้ที่สุด</summary>
        private int DistanceToOwned(int index)
        {
            UpgradeNodeData n = nodes[index];
            int best = int.MaxValue;
            foreach (int o in owned)
            {
                int dq = n.q - nodes[o].q;
                int dr = n.r - nodes[o].r;
                int d = Mathf.Max(Mathf.Abs(dq), Mathf.Max(Mathf.Abs(dr), Mathf.Abs(dq + dr)));
                if (d < best) best = d;
            }
            return best;
        }

        private NodeState GetState(int index)
        {
            UpgradeNodeData n = nodes[index];
            if (owned.Contains(index)) return NodeState.Owned;
            if (n.comingSoon) return NodeState.ComingSoon;

            Vector2Int cell = new Vector2Int(n.q, n.r);
            for (int i = 0; i < NeighborOffsets.Length; i++)
            {
                if (indexByCell.TryGetValue(cell + NeighborOffsets[i], out int nb) && owned.Contains(nb))
                {
                    return NodeState.Available;
                }
            }
            return NodeState.Locked;
        }

        private string Describe(UpgradeNodeData n)
        {
            return string.IsNullOrEmpty(n.description) ? "No effect yet" : n.description;
        }

        // ---------------- Open / close ----------------

        private void SetOpen(bool open)
        {
            if (!builtUI) BuildUI();

            if (open && !isOpen)
            {
                isOpen = true;
                anyTreeOpen = true;
                hoveredIndex = -1;
                if (pauseWhileOpen)
                {
                    previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
                    Time.timeScale = 0f;
                }
                if (Application.isPlaying) UiAudioManager.Instance?.PlayPanelToggle();
            }
            else if (!open && isOpen)
            {
                isOpen = false;
                anyTreeOpen = false;
                if (pauseWhileOpen) Time.timeScale = previousTimeScale;
                if (Application.isPlaying) UiAudioManager.Instance?.PlayPanelToggle();
            }

            if (panel != null) panel.SetActive(isOpen);
            if (isOpen) RefreshAll();
        }

        // ---------------- UI build ----------------

        private void BuildUI()
        {
            if (builtUI) return;
            builtUI = true;

            EnsureEventSystem();
            EnsureFontAndSprites();

            indexByCell.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                indexByCell[new Vector2Int(nodes[i].q, nodes[i].r)] = i;
                if (nodes[i].startsOwned) owned.Add(i);
            }

            canvasObject = new GameObject("UpgradeTreeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = canvasSortingOrder;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            // ปุ่มเปิด (กลางล่างจอ)
            RectTransform openBtn = CreateButton(canvasRect, "OpenButton", openButtonText, new Vector2(300f, 72f),
                new Color(0.2f, 0.7f, 1f, 1f), Open);
            SetAnchor(openBtn, new Vector2(0.5f, 0f), new Vector2(0f, 48f));

            // แผงผัง (เต็มจอ)
            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvasRect, false);
            Stretch(panelRect);
            Image bg = panel.GetComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.08f, 0.93f);
            bg.raycastTarget = true;

            Text title = CreateText(panelRect, "Title", "UPGRADES", 44, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            SetAnchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -50f), new Vector2(500f, 70f), new Vector2(0f, 0.5f));

            moneyText = CreateText(panelRect, "Money", "$0", 52, FontStyle.Bold, new Color(1f, 0.85f, 0.2f, 1f), TextAnchor.MiddleLeft);
            SetAnchor(moneyText.rectTransform, new Vector2(0f, 0f), new Vector2(40f, 120f), new Vector2(420f, 80f), new Vector2(0f, 0.5f));

            infoText = CreateText(panelRect, "Info", DefaultInfo, 28, FontStyle.Normal, new Color(0.85f, 0.9f, 1f, 1f), TextAnchor.MiddleCenter);
            SetAnchor(infoText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(1300f, 50f), new Vector2(0.5f, 0.5f));

            RectTransform exitBtn = CreateButton(panelRect, "ExitButton", "EXIT", new Vector2(280f, 72f),
                new Color(1f, 0.2f, 0.45f, 1f), Close);
            SetAnchor(exitBtn, new Vector2(0.5f, 0f), new Vector2(0f, 48f));

            // คอนเทนเนอร์ผัง (กลางจอ)
            RectTransform tree = new GameObject("Tree", typeof(RectTransform)).GetComponent<RectTransform>();
            tree.SetParent(panelRect, false);
            tree.anchorMin = tree.anchorMax = new Vector2(0.5f, 0.5f);
            tree.anchoredPosition = new Vector2(0f, treeOffsetY);
            tree.sizeDelta = Vector2.zero;

            BuildNodes(tree);
        }

        private void BuildNodes(RectTransform tree)
        {
            float sqrt3 = Mathf.Sqrt(3f);
            float hexW = sqrt3 * cellRadius * hexFill;
            float hexH = hexW * (HexTexHeight / HexTexWidth);

            views.Clear();
            for (int i = 0; i < nodes.Count; i++)
            {
                UpgradeNodeData n = nodes[i];
                int index = i;

                GameObject go = new GameObject("Node_" + i + "_" + n.label, typeof(RectTransform), typeof(Image), typeof(Button), typeof(EventTrigger));
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.SetParent(tree, false);
                rt.sizeDelta = new Vector2(hexW, hexH);
                rt.anchoredPosition = new Vector2(sqrt3 * cellRadius * (n.q + n.r * 0.5f), -1.5f * cellRadius * n.r);

                Image border = go.GetComponent<Image>();
                border.sprite = hexSpriteBorder;
                border.raycastTarget = true;

                Button btn = go.GetComponent<Button>();
                btn.targetGraphic = border;
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => OnNodeClicked(index));

                EventTrigger trigger = go.GetComponent<EventTrigger>();
                AddTrigger(trigger, EventTriggerType.PointerEnter, () => OnNodeHover(index));
                AddTrigger(trigger, EventTriggerType.PointerExit, () => OnNodeHoverExit(index));

                GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                RectTransform fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.SetParent(rt, false);
                Stretch(fillRt);
                Image fill = fillGo.GetComponent<Image>();
                fill.sprite = hexSpriteFill;
                fill.raycastTarget = false;

                Text label = CreateText(rt, "Label", n.label, 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                SetAnchor(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(hexW * 0.9f, 52f), new Vector2(0.5f, 0.5f));

                Text price = CreateText(rt, "Price", "", 19, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
                SetAnchor(price.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -26f), new Vector2(hexW * 0.9f, 28f), new Vector2(0.5f, 0.5f));

                views.Add(new NodeView { root = rt, border = border, fill = fill, label = label, price = price });
            }
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, Action action)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void OnNodeClicked(int index)
        {
            UiAudioManager.Instance?.PlayClick();
            if (TryPurchase(index))
            {
                UpdateInfo(index);
            }
        }

        private void OnNodeHover(int index)
        {
            hoveredIndex = index;
            UpdateInfo(index);
        }

        private void OnNodeHoverExit(int index)
        {
            if (hoveredIndex == index) hoveredIndex = -1;
            if (infoText != null) infoText.text = DefaultInfo;
        }

        // ---------------- Refresh ----------------

        private void RefreshAll()
        {
            if (!builtUI) return;

            int money = CurrentMoney;
            lastMoney = money;
            if (moneyText != null) moneyText.text = "$" + money;

            for (int i = 0; i < views.Count; i++)
            {
                NodeView v = views[i];
                UpgradeNodeData n = nodes[i];
                NodeState state = GetState(i);
                bool afford = money >= n.price;

                // เห็นแค่ช่องที่ซื้อได้ + อีกชั้นเป็น ? ที่เหลือซ่อนไว้จนกว่าจะเข้าใกล้
                bool visible = DistanceToOwned(i) <= revealDepth;
                v.root.gameObject.SetActive(visible);
                if (!visible) continue;

                switch (state)
                {
                    case NodeState.Owned:
                        v.border.color = ownedBorder;
                        v.fill.color = ownedFill;
                        v.label.text = n.label;
                        v.label.color = Color.white;
                        v.price.text = n.startsOwned ? "" : "OWNED";
                        v.price.color = new Color(0.8f, 1f, 0.9f, 1f);
                        break;

                    case NodeState.Available:
                        v.border.color = afford ? availableBorder : brokeBorder;
                        v.fill.color = afford ? availableFill : brokeFill;
                        v.label.text = n.label;
                        v.label.color = Color.white;
                        v.price.text = "$" + n.price;
                        v.price.color = afford ? new Color(1f, 0.92f, 0.3f, 1f) : new Color(1f, 0.45f, 0.45f, 1f);
                        break;

                    case NodeState.Locked:
                        // ยังซื้อไม่ได้: ซ่อนชื่อกับราคาไว้ เป็นเครื่องหมาย ?
                        v.border.color = lockedBorder;
                        v.fill.color = lockedFill;
                        v.label.text = "?";
                        v.label.color = new Color(0.6f, 0.6f, 0.7f, 1f);
                        v.price.text = "";
                        break;

                    default: // ComingSoon
                        v.border.color = lockedBorder;
                        v.fill.color = lockedFill;
                        v.label.text = "?";
                        v.label.color = new Color(0.6f, 0.6f, 0.7f, 1f);
                        v.price.text = "";
                        break;
                }
            }

            if (hoveredIndex >= 0) UpdateInfo(hoveredIndex);
        }

        private void UpdateInfo(int index)
        {
            if (infoText == null || index < 0 || index >= nodes.Count) return;
            UpgradeNodeData n = nodes[index];

            switch (GetState(index))
            {
                case NodeState.Owned:
                    infoText.text = n.startsOwned ? "Your shop" : n.label + " - " + Describe(n) + " (owned)";
                    break;
                case NodeState.Available:
                    int need = n.price - CurrentMoney;
                    infoText.text = n.label + " - " + Describe(n) + " - $" + n.price + (need > 0 ? "  (need $" + need + " more)" : "");
                    break;
                case NodeState.Locked:
                    infoText.text = "???  Buy a neighbouring upgrade to reveal";
                    break;
                default:
                    infoText.text = "Coming soon...";
                    break;
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

        private static void EnsureFontAndSprites()
        {
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hexSpriteBorder == null) hexSpriteBorder = CreateHexSprite(110.5f);
            if (hexSpriteFill == null) hexSpriteFill = CreateHexSprite(110.5f - 9f);
        }

        /// <summary>สร้าง Sprite หกเหลี่ยมยอดแหลมบน (pointy-top) ขอบเนียน; apothem = ระยะจากกลางถึงขอบ</summary>
        private static Sprite CreateHexSprite(float apothem)
        {
            int w = Mathf.RoundToInt(HexTexWidth);
            int h = Mathf.RoundToInt(HexTexHeight);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[w * h];
            const float c60 = 0.5f;
            const float s60 = 0.8660254f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f - w * 0.5f;
                    float py = y + 0.5f - h * 0.5f;
                    float d = Mathf.Max(Mathf.Abs(px), Mathf.Max(Mathf.Abs(px * c60 + py * s60), Mathf.Abs(-px * c60 + py * s60)));
                    float alpha = Mathf.Clamp01(apothem - d + 0.5f);
                    colors[y * w + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 pos)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = pos;
        }

        private static void SetAnchor(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
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
            ColorBlock cb = btn.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.onClick.AddListener(() => onClick());

            Text t = CreateText(rt, "Text", text, 36, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return rt;
        }
    }
}
