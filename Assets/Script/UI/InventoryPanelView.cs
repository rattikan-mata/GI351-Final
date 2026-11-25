using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    /// <summary>
    /// แผง Inventory บน HUD: แสดงของที่ผู้เล่นแบกอยู่ จัดกลุ่มตามชนิด (รูป + จำนวน "x3") และ "แบก/ความจุ" ด้านล่างการ์ด
    /// วางบน RectTransform ลูกของ HUD Canvas แล้วสร้าง UI ของตัวเองตอน Awake (กรอบ, ช่องของ, ตัวหนังสือ)
    /// อัปเดตเฉพาะตอนของเปลี่ยน (ฟัง PlayerCarry.onCarryCountChanged + เช็คสำรองทุก 0.25 วินาที)
    /// </summary>
    [DisallowMultipleComponent]
    public class InventoryPanelView : MonoBehaviour
    {
        private const string VisualName = "Inventory Visual";
        private const int MaxPoolSize = 24;

        [Header("Frame (กรอบการ์ด)")]
        [Tooltip("รูปกรอบแผง (UI_InventoryPanel) มีหัวสีเขียวเขียนคำว่า Inventory อยู่แล้ว")]
        [SerializeField] private Sprite frameSprite;

        [Tooltip("ขนาดแผง (พิกเซลอ้างอิง 1920x1080) ค่าเริ่ม 260x348 = สัดส่วนเดียวกับรูป 297x398")]
        [SerializeField] private Vector2 panelSize = new Vector2(290f, 390f);

        [Tooltip("สัดส่วนความสูงของแถบหัวสีเขียว (ส่วนบนของรูป) เนื้อหาจะเริ่มใต้แถบนี้")]
        [Range(0f, 0.5f)]
        [SerializeField] private float headerHeightRatio = 0.18f;

        [Header("Placement (ตำแหน่งบนจอ)")]
        [Tooltip("ติ๊ก = ตั้ง anchor/ตำแหน่ง/ขนาดของ RectTransform ตามค่าข้างล่างตอนเริ่ม / ไม่ติ๊ก = ใช้ค่าที่จัดไว้ใน RectTransform เอง")]
        [SerializeField] private bool applyPlacement = true;

        [Tooltip("จุดยึดบนจอ (0,0.5) = ชิดซ้ายกลางจอ")]
        [SerializeField] private Vector2 anchor = new Vector2(1f, 1f);

        [Tooltip("จุดหมุนของแผง (0,0.5) = ขอบซ้ายกลางแผง")]
        [SerializeField] private Vector2 pivot = new Vector2(1f, 1f);

        [Tooltip("ระยะห่างจากจุดยึด (พิกเซลอ้างอิง) เช่น (24,0) = ห่างขอบซ้าย 24")]
        [SerializeField] private Vector2 anchoredPosition = new Vector2(-32f, -133f);

        [Header("Layout (การจัดช่องของ)")]
        [Tooltip("ระยะขอบในเนื้อการ์ด: x = ซ้าย, y = บน (ใต้แถบหัว), z = ขวา, w = ล่าง (เหนือบรรทัดจำนวน)")]
        [SerializeField] private Vector4 bodyPadding = new Vector4(16f, 10f, 16f, 6f);

        [Tooltip("ความสูงบรรทัด 'แบก/ความจุ' ด้านล่างการ์ด")]
        [SerializeField] private float footerHeight = 26f;

        [Tooltip("จำนวนคอลัมน์ของช่องของ")]
        [Range(1, 4)]
        [SerializeField] private int columns = 1;

        [Tooltip("ต้องแสดงได้อย่างน้อยกี่ชนิด (ถ้าที่ไม่พอ ช่องจะย่อลงให้พอ)")]
        [SerializeField] private int minVisibleTypes = 4;

        [Tooltip("แบกของหลายชนิดเกินจำนวนที่การ์ดปกติใส่ได้ การ์ดยืดลงทีละแถวได้ถึงกี่ชนิด (รูปกรอบเป็น 9-slice หัว/มุมไม่เสียทรง)")]
        [SerializeField] private int maxVisibleTypes = 8;

        [Tooltip("ขนาดรูปของ (พิกเซลอ้างอิง)")]
        [SerializeField] private float iconSize = 64f;

        [Tooltip("ระยะห่างระหว่างแถว")]
        [SerializeField] private float rowSpacing = 6f;

        [Tooltip("ระยะห่างระหว่างคอลัมน์")]
        [SerializeField] private float columnSpacing = 8f;

        [Tooltip("แสดงชื่อของตัวเล็กใต้ตัวเลขจำนวน")]
        [SerializeField] private bool showItemNames = true;

        [Header("Text (ตัวหนังสือ)")]
        [Tooltip("ฟอนต์ (ว่าง = ฟอนต์ในตัว LegacyRuntime)")]
        [SerializeField] private Font font;

        [SerializeField] private int countFontSize = 26;
        [SerializeField] private int nameFontSize = 20;
        [SerializeField] private int footerFontSize = 20;
        [SerializeField] private int emptyFontSize = 22;

        [Tooltip("สีตัวหนังสือ (น้ำตาลแบบหน้าร้าน)")]
        [SerializeField] private Color textColor = new Color(0.08f, 0.07f, 0.06f, 1f);

        [Tooltip("ใส่ขอบตัวหนังสือ (ตาม mockup: ตัวหนังสือดำบนการ์ดครีม ไม่มีขอบ)")]
        [SerializeField] private bool useOutline = false;

        [Tooltip("แสดงบรรทัด 'แบก/ความจุ' ด้านล่างการ์ด (หัวผู้เล่นมีเลขนี้อยู่แล้ว)")]
        [SerializeField] private bool showFooter = false;

        [Tooltip("สีขอบตัวหนังสือ")]
        [SerializeField] private Color outlineColor = new Color(0.2f, 0.12f, 0.06f, 0.7f);

        [Tooltip("ระยะขอบตัวหนังสือ (พิกเซล)")]
        [SerializeField] private Vector2 outlineDistance = new Vector2(1f, -1f);

        [Tooltip("สีบรรทัด 'แบก/ความจุ' ตอนกระเป๋าเต็ม")]
        [SerializeField] private Color fullColor = new Color(0.78f, 0.2f, 0.14f, 1f);

        [Tooltip("ข้อความตอนไม่ได้แบกอะไร")]
        [SerializeField] private string emptyText = "Empty";

        [System.Serializable]
        public struct DisplayName
        {
            public ItemType type;
            public string name;
        }

        [Tooltip("ชื่อที่โชว์ในแผงตาม mockup (เปลี่ยนเฉพาะในแผงนี้ ชื่อไอเทมที่อื่นไม่เปลี่ยน) ชนิดที่ไม่อยู่ในลิสต์ = ใช้ชื่อไอเทมเดิม\nเนื้อดิบ = ชื่อสัตว์สั้นๆ / อาหาร = ลงท้าย -baww (Wild Meatbaww)")]
        [SerializeField] private List<DisplayName> displayNames = new List<DisplayName>
        {
            new DisplayName { type = ItemType.ChickenMeat, name = "Chicken" },
            new DisplayName { type = ItemType.PigMeat, name = "Pork" },
            new DisplayName { type = ItemType.BuffaloMeat, name = "Buffalo" },
            new DisplayName { type = ItemType.CookedChicken, name = "Chick-baww" },
            new DisplayName { type = ItemType.CookedPig, name = "Pig-baww" },
            new DisplayName { type = ItemType.CookedBuffalo, name = "Buff-baww" },
            new DisplayName { type = ItemType.MixedDish, name = "Mix-baww" },
            new DisplayName { type = ItemType.SpecialDish, name = "Golden-baww" },
            new DisplayName { type = ItemType.UnconsciousThief, name = "Thief" },
        };

        private string NameFor(Item item)
        {
            for (int i = 0; i < displayNames.Count; i++)
            {
                if (displayNames[i].type == item.Type && !string.IsNullOrEmpty(displayNames[i].name)) return displayNames[i].name;
            }
            return item.ItemName;
        }

        [Header("Toggle (เปิด/ปิดแผง)")]
#if ENABLE_INPUT_SYSTEM
        [Tooltip("ปุ่มเปิด/ปิดแผง (Input System)")]
        [SerializeField] private Key toggleKey = Key.I;
#else
        [Tooltip("ปุ่มเปิด/ปิดแผง")]
        [SerializeField] private KeyCode toggleKey = KeyCode.I;
#endif

        [Tooltip("แสดงแผงตั้งแต่เริ่มเกม")]
        [SerializeField] private bool startVisible = true;

        [Tooltip("เช็คสำรองทุกกี่วินาที (หาผู้เล่นใหม่ / จำนวนเปลี่ยนโดยไม่มี event)")]
        [SerializeField] private float pollInterval = 0.25f;

        // ---- UI ที่สร้างเอง ----
        private GameObject visual;
        private Image frameImage;
        private RectTransform contentRect;
        private Text emptyLabel;
        private Text footerLabel;
        private Cell[] cells = new Cell[0];

        // ---- ข้อมูล ----
        private PlayerCarry carry;
        private UnityEvent<int, int> subscribedEvent;
        private bool dirty = true;
        private float pollTimer;
        private int lastCount = -1;
        private int lastMax = -1;

        // ใช้ซ้ำทุกครั้งที่ refresh (ไม่ new ทุกเฟรม)
        private readonly List<ItemType> groupTypes = new List<ItemType>();
        private readonly List<int> groupCounts = new List<int>();
        private readonly List<Item> groupFirst = new List<Item>();
        private static readonly Dictionary<int, string> CountStrings = new Dictionary<int, string>();

        private class Cell
        {
            public GameObject root;
            public Image icon;
            public Text count;
            public Text name;
        }

        /// <summary>แผงกำลังแสดงอยู่ไหม</summary>
        public bool IsVisible => visual != null && visual.activeSelf;

        /// <summary>
        /// สร้างแผง Inventory ใต้ Canvas พร้อม RectTransform แล้วคืนคอมโพเนนต์ (ใช้จาก editor script ใส่ลง HUD prefab ได้)
        /// </summary>
        public static InventoryPanelView CreateDefault(Transform canvas, Sprite frame)
        {
            var go = new GameObject("Inventory Panel", typeof(RectTransform));
            go.layer = canvas != null ? canvas.gameObject.layer : go.layer;
            go.SetActive(false); // เลื่อน Awake ไว้จนกว่าจะใส่รูปกรอบเสร็จ (ตอนเล่นเกม)
            if (canvas != null) go.transform.SetParent(canvas, false);

            var view = go.AddComponent<InventoryPanelView>();
            view.frameSprite = frame;
            view.ApplyPlacement();
            go.SetActive(true);
            return view;
        }

        private void Awake()
        {
            if (font == null) font = GameFont.Get();
            if (applyPlacement) ApplyPlacement();
            Build();
            SetVisible(startVisible);
        }

        private void OnEnable()
        {
            dirty = true;
            pollTimer = 0f;
            if (carry != null) Subscribe(carry);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>ตั้ง anchor/pivot/ตำแหน่ง/ขนาดของแผงตามค่าใน Inspector</summary>
        private void ApplyPlacement()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = panelSize;
            rt.localScale = Vector3.one;
        }

        // ================= สร้าง UI =================

        private void Build()
        {
            // ถ้ามีของเก่าค้างอยู่ (เช่นบันทึกจากตอนเล่น) ลบทิ้งแล้วสร้างใหม่ให้ตรงกับค่าปัจจุบัน
            Transform old = transform.Find(VisualName);
            if (old != null)
            {
                old.SetParent(null, false);
                Destroy(old.gameObject);
            }

            // กรอบการ์ด เต็มแผง
            visual = new GameObject(VisualName, typeof(RectTransform));
            visual.layer = gameObject.layer;
            var vrt = (RectTransform)visual.transform;
            vrt.SetParent(transform, false);
            Stretch(vrt);

            frameImage = visual.AddComponent<Image>();
            frameImage.sprite = frameSprite;
            // กรอบที่ตั้งขอบ 9-slice ไว้ = ยืดได้โดยหัวเขียวและมุมโค้งคมชัดเท่าเดิม (การ์ดยืดลงตอนมีของหลายชนิด)
            frameImage.type = frameSprite != null && frameSprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            frameImage.preserveAspect = false;
            frameImage.raycastTarget = false;
            if (frameSprite == null) frameImage.color = new Color(0.99f, 0.96f, 0.89f, 1f); // ไม่มีรูป = การ์ดครีมเปล่า

            float headerH = panelSize.y * headerHeightRatio;
            float padL = bodyPadding.x, padT = bodyPadding.y, padR = bodyPadding.z, padB = bodyPadding.w;
            float footerHeight = showFooter ? this.footerHeight : 0f;

            // พื้นที่เนื้อหาใต้แถบหัว เหนือบรรทัดจำนวน
            contentRect = NewRect("Content", vrt);
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(padL, padB + footerHeight);
            contentRect.offsetMax = new Vector2(-padR, -(headerH + padT));

            // ข้อความตอนว่าง อยู่กลางเนื้อการ์ด
            emptyLabel = NewText("Empty", contentRect, emptyFontSize, TextAnchor.MiddleCenter);
            Stretch(emptyLabel.rectTransform);
            emptyLabel.text = emptyText;

            // บรรทัด "แบก/ความจุ" ด้านล่างการ์ด
            var footerRt = NewRect("Footer", vrt);
            footerRt.anchorMin = new Vector2(0f, 0f);
            footerRt.anchorMax = new Vector2(1f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.offsetMin = new Vector2(padL, padB);
            footerRt.offsetMax = new Vector2(-padR, padB + footerHeight);
            footerLabel = NewText("Count", footerRt, footerFontSize, TextAnchor.MiddleCenter);
            Stretch(footerLabel.rectTransform);
            footerRt.gameObject.SetActive(showFooter);

            BuildCells(panelSize.x - padL - padR, panelSize.y - headerH - padT - padB - footerHeight);
        }

        private void BuildCells(float contentW, float contentH)
        {
            int cols = Mathf.Max(1, columns);
            int minRows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, minVisibleTypes) / (float)cols));
            float cellW = Mathf.Max(16f, (contentW - columnSpacing * (cols - 1)) / cols);

            // ช่องสูงเท่ารูป แต่ย่อลงถ้าที่ไม่พอให้ได้อย่างน้อย minRows แถว
            float fitH = (contentH - rowSpacing * (minRows - 1)) / minRows;
            float cellH = Mathf.Max(16f, Mathf.Min(iconSize, fitH));
            int rows = Mathf.Max(minRows, Mathf.FloorToInt((contentH + rowSpacing) / (cellH + rowSpacing)));
            int growRows = Mathf.CeilToInt(Mathf.Max(1, maxVisibleTypes) / (float)cols);
            int total = Mathf.Min(MaxPoolSize, Mathf.Max(rows, growRows) * cols);
            baseRows = rows;
            rowStep = cellH + rowSpacing;

            float iconPx = Mathf.Min(iconSize, cellH);
            cells = new Cell[total];
            for (int i = 0; i < total; i++)
            {
                int r = i / cols, c = i % cols;
                var cell = new Cell();

                var crt = NewRect("Cell " + i, contentRect);
                crt.anchorMin = crt.anchorMax = new Vector2(0f, 1f);
                crt.pivot = new Vector2(0f, 1f);
                crt.sizeDelta = new Vector2(cellW, cellH);
                crt.anchoredPosition = new Vector2(c * (cellW + columnSpacing), -r * (cellH + rowSpacing));
                cell.root = crt.gameObject;

                // รูปของ ชิดซ้ายของช่อง
                var irt = NewRect("Icon", crt);
                irt.anchorMin = irt.anchorMax = new Vector2(0f, 0.5f);
                irt.pivot = new Vector2(0f, 0.5f);
                irt.sizeDelta = new Vector2(iconPx, iconPx);
                irt.anchoredPosition = Vector2.zero;
                cell.icon = irt.gameObject.AddComponent<Image>();
                cell.icon.preserveAspect = true;
                cell.icon.raycastTarget = false;

                if (cols == 1 && showItemNames)
                {
                    // แบบรายการตาม mockup: รูปใหญ่ซ้าย / "x3" ตัวหนาเกาะมุมขวาบนของรูป / ชื่อของตัวปกติทางขวา
                    cell.count = NewText("Count", crt, countFontSize, TextAnchor.UpperLeft);
                    var crt2 = cell.count.rectTransform;
                    crt2.anchorMin = crt2.anchorMax = new Vector2(0f, 1f);
                    crt2.pivot = new Vector2(0f, 1f);
                    crt2.sizeDelta = new Vector2(70f, countFontSize + 6f);
                    crt2.anchoredPosition = new Vector2(iconPx * 0.82f, 4f);

                    cell.name = NewText("Name", crt, nameFontSize, TextAnchor.MiddleLeft);
                    cell.name.fontStyle = FontStyle.Normal;
                    // ชื่อยาว (เช่น Raw Chicken Wing) ย่อตัวหนังสือให้พอดีการ์ด ไม่ล้นขอบ
                    cell.name.horizontalOverflow = HorizontalWrapMode.Wrap;
                    cell.name.resizeTextForBestFit = true;
                    cell.name.resizeTextMinSize = 12;
                    cell.name.resizeTextMaxSize = nameFontSize;
                    var nrt2 = cell.name.rectTransform;
                    nrt2.anchorMin = Vector2.zero;
                    nrt2.anchorMax = Vector2.one;
                    nrt2.offsetMin = new Vector2(iconPx + 30f, 0f);
                    nrt2.offsetMax = new Vector2(-4f, -cellH * 0.2f);

                    cell.root.SetActive(false);
                    cells[i] = cell;
                    continue;
                }

                // ตัวเลข "x3" ทางขวาของรูป (มีชื่อ = ครึ่งบน, ชื่อ = ครึ่งล่าง)
                float textLeft = iconPx + 4f;
                cell.count = NewText("Count", crt, countFontSize, showItemNames ? TextAnchor.LowerLeft : TextAnchor.MiddleLeft);
                var trt = cell.count.rectTransform;
                trt.anchorMin = new Vector2(0f, showItemNames ? 0.45f : 0f);
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(textLeft, 0f);
                trt.offsetMax = Vector2.zero;

                if (showItemNames)
                {
                    cell.name = NewText("Name", crt, nameFontSize, TextAnchor.UpperLeft);
                    cell.name.fontStyle = FontStyle.Normal;
                    var nrt = cell.name.rectTransform;
                    nrt.anchorMin = Vector2.zero;
                    nrt.anchorMax = new Vector2(1f, 0.45f);
                    nrt.offsetMin = new Vector2(textLeft, 0f);
                    nrt.offsetMax = Vector2.zero;
                }

                cell.root.SetActive(false);
                cells[i] = cell;
            }
        }

        private RectTransform NewRect(string objName, Transform parent)
        {
            var go = new GameObject(objName, typeof(RectTransform));
            go.layer = gameObject.layer;
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private Text NewText(string objName, Transform parent, int size, TextAnchor align)
        {
            var rt = NewRect(objName, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = textColor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            if (useOutline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = outlineColor;
                o.effectDistance = outlineDistance;
            }
            return t;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        // ================= เปิด/ปิด =================

        /// <summary>เปิด/ปิดเฉพาะภาพของแผง คอมโพเนนต์ยังทำงานต่อ (ฟังปุ่มได้)</summary>
        public void SetVisible(bool show)
        {
            if (visual == null) return;
            visual.SetActive(show);
            if (show) dirty = true;
        }

        public void ToggleVisible()
        {
            SetVisible(!IsVisible);
        }

        private bool TogglePressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (toggleKey == Key.None || Keyboard.current == null) return false;
            return Keyboard.current[toggleKey].wasPressedThisFrame;
#else
            return toggleKey != KeyCode.None && Input.GetKeyDown(toggleKey);
#endif
        }

        // ================= ข้อมูล =================

        private void Update()
        {
            if (TogglePressed()) ToggleVisible();

            // เช็คสำรอง: ผู้เล่นเกิดทีหลัง HUD / ถูกทำลาย / จำนวนเปลี่ยนโดยไม่มี event
            pollTimer -= Time.unscaledDeltaTime;
            if (pollTimer <= 0f)
            {
                pollTimer = pollInterval;
                if (carry == null || !carry.isActiveAndEnabled) FindCarry();
                if (carry != null && (carry.CurrentCarriedCount != lastCount || carry.MaxCarryCapacity != lastMax)) dirty = true;
            }
        }

        private void LateUpdate()
        {
            if (!dirty || !IsVisible) return;
            dirty = false;
            Refresh();
        }

        private void FindCarry()
        {
            PlayerCarry found = null;
            for (int i = 0; i < PlayerCarry.All.Count; i++)
            {
                if (PlayerCarry.All[i] != null) { found = PlayerCarry.All[i]; break; }
            }
            if (found == null) found = FindAnyObjectByType<PlayerCarry>();
            // เจอตัวเดิม (หรือยังไม่มีผู้เล่นเหมือนเดิม) = ไม่ต้องทำอะไร กัน refresh ซ้ำทุก 0.25 วินาทีตอนไม่มีผู้เล่น
            // เทียบ reference จริง: ผู้เล่นที่ถูกทำลาย (== null แบบ Unity) ต้องนับว่า "เปลี่ยน" เพื่อล้างแผง
            if (ReferenceEquals(found, carry) && (found == null || subscribedEvent != null)) return;

            Unsubscribe();
            carry = found;
            if (carry != null) Subscribe(carry);
            dirty = true;
        }

        private void Subscribe(PlayerCarry target)
        {
            Unsubscribe();
            if (target.onCarryCountChanged == null) target.onCarryCountChanged = new UnityEvent<int, int>();
            subscribedEvent = target.onCarryCountChanged;
            subscribedEvent.AddListener(OnCarryCountChanged);
        }

        private void Unsubscribe()
        {
            // จำ event ไว้เอง ถอดได้แม้ผู้เล่นถูกทำลายไปแล้ว
            if (subscribedEvent != null) subscribedEvent.RemoveListener(OnCarryCountChanged);
            subscribedEvent = null;
        }

        private void OnCarryCountChanged(int count, int max)
        {
            dirty = true;
        }

        private int baseRows = 1;
        private float rowStep;

        /// <summary>การ์ดยืดลงทีละแถวเมื่อของหลายชนิดเกินที่การ์ดปกติใส่ได้ (หดกลับเมื่อของน้อยลง)</summary>
        private void FitHeight(int shownTypes)
        {
            var rt = transform as RectTransform;
            if (rt == null) return;
            int cols = Mathf.Max(1, columns);
            int rowsNeeded = Mathf.CeilToInt(shownTypes / (float)cols);
            int extra = Mathf.Max(0, rowsNeeded - baseRows);
            float h = panelSize.y + extra * rowStep;
            if (!Mathf.Approximately(rt.sizeDelta.y, h)) rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
        }

        private void Refresh()
        {
            groupTypes.Clear();
            groupCounts.Clear();
            groupFirst.Clear();

            int carried = 0, max = 0;
            if (carry != null)
            {
                carried = carry.CurrentCarriedCount;
                max = carry.MaxCarryCapacity;

                // จัดกลุ่มตามชนิด เรียงตามลำดับที่เจอก่อน
                IReadOnlyList<Item> items = carry.CarriedItems;
                for (int i = 0; i < items.Count; i++)
                {
                    Item it = items[i];
                    if (it == null) continue;
                    int idx = groupTypes.IndexOf(it.Type);
                    if (idx < 0)
                    {
                        groupTypes.Add(it.Type);
                        groupCounts.Add(1);
                        groupFirst.Add(it);
                    }
                    else groupCounts[idx]++;
                }
            }
            lastCount = carried;
            lastMax = max;

            int shown = Mathf.Min(groupTypes.Count, cells.Length);
            FitHeight(shown);
            for (int i = 0; i < cells.Length; i++)
            {
                Cell cell = cells[i];
                bool on = i < shown;
                if (cell.root.activeSelf != on) cell.root.SetActive(on);
                if (!on) continue;

                Item first = groupFirst[i];
                SpriteRenderer sr = first.SpriteRenderer;
                Sprite sprite = sr != null ? sr.sprite : null;
                Color col = sr != null ? sr.color : Color.white;
                col.a = 1f; // ของในกระเป๋าอาจกำลังกระพริบ/จาง ให้รูปในแผงทึบเสมอ
                cell.icon.sprite = sprite;
                cell.icon.color = col;
                cell.icon.enabled = sprite != null;

                cell.count.text = CountText(groupCounts[i]);
                if (cell.name != null) cell.name.text = NameFor(first);
            }

            emptyLabel.text = emptyText;
            emptyLabel.gameObject.SetActive(groupTypes.Count == 0);

            footerLabel.text = carry != null ? carried + "/" + max : "";
            footerLabel.color = carry != null && max > 0 && carried >= max ? fullColor : textColor;
        }

        private static string CountText(int n)
        {
            if (!CountStrings.TryGetValue(n, out string s))
            {
                s = "x" + n;
                CountStrings[n] = s;
            }
            return s;
        }
    }
}
