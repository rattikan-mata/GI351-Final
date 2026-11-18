using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GI351.KitchenGame
{
    /// <summary>ราคา + ค่าที่ได้ของอัปเกรดหนึ่งเลเวล</summary>
    [Serializable]
    public struct PotUpgradeLevel
    {
        [Tooltip("ราคาที่ต้องจ่ายเพื่ออัปเกรดมาเลเวลนี้ (เหรียญ)")]
        public int cost;

        [Tooltip("ค่าที่ได้หลังอัปเกรด (เป็นค่าสุดท้ายเลย ไม่ใช่ค่าที่บวกเพิ่ม) ราคาอาหาร = เหรียญต่อจาน, ความเร็ว = วินาทีต่อชิ้น")]
        public float value;
    }

    /// <summary>
    /// แผงอัปเกรดของเครื่องทำอาหาร (หม้อ): เดินเข้าวงยืนของหม้อ -> แผงด้านล่างจอเด้งขึ้นมา
    /// มี 2 แถว: ราคาอาหาร (Product Price) และความเร็วการทำ (Production Speed) แต่ละหม้ออัปเกรดแยกกัน
    /// ลิสต์เลเวลเริ่มต้นว่างไว้ ใส่ราคา/ค่าเองใน Inspector (ว่างทั้งคู่ = แผงจะไม่เด้ง)
    /// </summary>
    [RequireComponent(typeof(CookingPot))]
    public class PotUpgrade : MonoBehaviour
    {
        [Header("Product Price (อัปเกรดราคาอาหาร)")]
        [Tooltip("ชื่อแถวบนแผง")]
        [SerializeField] private string priceLabel = "Product Price";

        [Tooltip("เลเวลของราคาอาหาร เรียงจากเลเวล 2 ขึ้นไป: Cost = ราคาอัปเกรด, Value = ราคาขายต่อจานหลังอัปเกรด (ตอนนี้ว่าง ใส่เอง)")]
        [SerializeField] private List<PotUpgradeLevel> priceLevels = new List<PotUpgradeLevel>();

        [Header("Production Speed (อัปเกรดความเร็วทำอาหาร)")]
        [Tooltip("ชื่อแถวบนแผง")]
        [SerializeField] private string speedLabel = "Production Speed";

        [Tooltip("เลเวลของความเร็ว เรียงจากเลเวล 2 ขึ้นไป: Cost = ราคาอัปเกรด, Value = วินาทีต่อชิ้นหลังอัปเกรด (ยิ่งน้อยยิ่งเร็ว) (ตอนนี้ว่าง ใส่เอง)")]
        [SerializeField] private List<PotUpgradeLevel> speedLevels = new List<PotUpgradeLevel>();

        [Header("Panel (แผงอัปเกรด)")]
        [Tooltip("ซ่อนแผงถ้าทั้งสองลิสต์ยังว่าง (ไม่มีอะไรให้อัปเกรด)")]
        [SerializeField] private bool hideWhenNoLevels = true;

        [Tooltip("หน่วงก่อนปิดแผงหลังเดินออกจากวง (วินาที) กันแผงกระพริบเวลายืนตรงขอบวง")]
        [Min(0f)]
        [SerializeField] private float closeDelay = 0.35f;

        [Header("Events")]
        [Tooltip("ยิงเมื่ออัปเกรดสำเร็จ")]
        public UnityEvent onUpgraded;

        private class RowView
        {
            public Text level;
            public Text values;
            public Image buttonImage;
            public Button button;
            public Text buttonText;
        }

        private CookingPot pot;
        private int priceLevel;
        private int speedLevel;
        private float baseSpeed;
        private int basePrice;
        private bool baseCaptured;

        private GameObject canvasObject;
        private GameObject panel;
        private Text titleText;
        private Text moneyText;
        private RowView priceRow;
        private RowView speedRow;
        private bool isOpen;
        private float lastInRangeTime;
        private int lastMoney = int.MinValue;
        private bool builtUI;

        private static Font uiFont;

        public int PriceLevel => priceLevel;
        public int SpeedLevel => speedLevel;
        public bool IsOpen => isOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            uiFont = null;
        }

        private void Awake()
        {
            pot = GetComponent<CookingPot>();
        }

        private void Start()
        {
            CaptureBase();
        }

        private void CaptureBase()
        {
            if (baseCaptured || pot == null) return;
            baseCaptured = true;
            basePrice = pot.CurrentDishPrice;
            baseSpeed = pot.CookingDuration;
        }

        private void OnDisable()
        {
            if (panel != null) panel.SetActive(false);
            isOpen = false;
        }

        private void Update()
        {
            if (!Application.isPlaying || pot == null) return;

            bool hasLevels = priceLevels.Count > 0 || speedLevels.Count > 0;
            bool shouldShow = pot.IsUnlocked && pot.IsPlayerInRange && (hasLevels || !hideWhenNoLevels);

            if (shouldShow)
            {
                // เข้าวงแล้วเปิดทันที และจำเวลาล่าสุดที่ยังอยู่ในวง
                lastInRangeTime = Time.unscaledTime;
                if (!isOpen) SetOpen(true);
            }
            else if (isOpen && Time.unscaledTime - lastInRangeTime >= closeDelay)
            {
                // ออกจากวงนานกว่าเวลาหน่วงแล้วค่อยปิด (กันกระพริบตอนยืนตรงขอบวง)
                SetOpen(false);
            }

            if (isOpen && CurrentMoney != lastMoney) Refresh();
        }

        // ---------------- Logic ----------------

        private int CurrentMoney => MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

        private float CurrentPriceValue => priceLevel <= 0 ? basePrice : priceLevels[Mathf.Min(priceLevel, priceLevels.Count) - 1].value;
        private float CurrentSpeedValue => speedLevel <= 0 ? baseSpeed : speedLevels[Mathf.Min(speedLevel, speedLevels.Count) - 1].value;

        public bool TryUpgradePrice() => TryUpgrade(true);
        public bool TryUpgradeSpeed() => TryUpgrade(false);

        private bool TryUpgrade(bool isPrice)
        {
            CaptureBase();
            List<PotUpgradeLevel> levels = isPrice ? priceLevels : speedLevels;
            int level = isPrice ? priceLevel : speedLevel;
            if (level >= levels.Count || MoneyManager.Instance == null) return false;

            PotUpgradeLevel next = levels[level];
            if (MoneyManager.Instance.CurrentMoney < next.cost) return false;
            if (!MoneyManager.Instance.SpendMoney(next.cost)) return false;

            if (isPrice)
            {
                priceLevel++;
                pot.SetDishPriceOverride(Mathf.RoundToInt(next.value));
            }
            else
            {
                speedLevel++;
                pot.CookingDuration = next.value;
            }

            Debug.Log("[PotUpgrade] " + pot.PotName + " " + (isPrice ? "price" : "speed") + " -> Lv" + ((isPrice ? priceLevel : speedLevel) + 1) + " (" + next.value.ToString("0.##") + ")");
            onUpgraded?.Invoke();
            if (isOpen) Refresh();
            return true;
        }

        private void SetOpen(bool open)
        {
            if (open == isOpen) return;
            isOpen = open;
            if (open)
            {
                CaptureBase();
                if (!builtUI) BuildUI();
                panel.SetActive(true);
                Refresh();
            }
            else if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        // ---------------- UI ----------------

        private void BuildUI()
        {
            builtUI = true;
            EnsureEventSystem();
            if (uiFont == null) uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvasObject = new GameObject("PotUpgradeCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();

            panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.SetParent(canvasRect, false);
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.sizeDelta = new Vector2(980f, 400f);
            panelRect.anchoredPosition = new Vector2(0f, 20f);
            panel.GetComponent<Image>().color = new Color(0.1f, 0.08f, 0.16f, 0.95f);

            titleText = CreateText(panelRect, "Title", "", 44, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Place(titleText.rectTransform, new Vector2(0f, 1f), new Vector2(30f, -44f), new Vector2(560f, 60f), new Vector2(0f, 0.5f));

            moneyText = CreateText(panelRect, "Money", "$0", 40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f, 1f), TextAnchor.MiddleRight);
            Place(moneyText.rectTransform, new Vector2(1f, 1f), new Vector2(-30f, -44f), new Vector2(300f, 60f), new Vector2(1f, 0.5f));

            priceRow = BuildRow(panelRect, priceLabel, new Vector2(0f, -100f), () => TryUpgrade(true));
            speedRow = BuildRow(panelRect, speedLabel, new Vector2(0f, -250f), () => TryUpgrade(false));
        }

        private RowView BuildRow(RectTransform parent, string label, Vector2 pos, Action onClick)
        {
            GameObject row = new GameObject("Row_" + label, typeof(RectTransform), typeof(Image));
            RectTransform rowRect = row.GetComponent<RectTransform>();
            rowRect.SetParent(parent, false);
            rowRect.anchorMin = rowRect.anchorMax = new Vector2(0.5f, 1f);
            rowRect.pivot = new Vector2(0.5f, 1f);
            rowRect.sizeDelta = new Vector2(920f, 130f);
            rowRect.anchoredPosition = pos;
            row.GetComponent<Image>().color = new Color(0.18f, 0.15f, 0.28f, 1f);

            Text name = CreateText(rowRect, "Name", label, 34, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            Place(name.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -28f), new Vector2(460f, 44f), new Vector2(0f, 0.5f));

            Text level = CreateText(rowRect, "Level", "LV 1", 26, FontStyle.Bold, new Color(0.6f, 0.9f, 1f, 1f), TextAnchor.MiddleLeft);
            Place(level.rectTransform, new Vector2(0f, 1f), new Vector2(24f, -70f), new Vector2(120f, 36f), new Vector2(0f, 0.5f));

            Text values = CreateText(rowRect, "Values", "", 30, FontStyle.Bold, new Color(1f, 0.92f, 0.3f, 1f), TextAnchor.MiddleLeft);
            Place(values.rectTransform, new Vector2(0f, 1f), new Vector2(150f, -70f), new Vector2(420f, 40f), new Vector2(0f, 0.5f));

            GameObject btnGo = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.SetParent(rowRect, false);
            btnRect.anchorMin = btnRect.anchorMax = new Vector2(1f, 0.5f);
            btnRect.pivot = new Vector2(1f, 0.5f);
            btnRect.sizeDelta = new Vector2(280f, 90f);
            btnRect.anchoredPosition = new Vector2(-20f, 0f);
            Image img = btnGo.GetComponent<Image>();
            Button btn = btnGo.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => onClick());
            Text btnText = CreateText(btnRect, "Text", "", 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(btnText.rectTransform);

            return new RowView { level = level, values = values, buttonImage = img, button = btn, buttonText = btnText };
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (titleText != null) titleText.text = pot.PotName.ToUpper();
            if (moneyText != null) moneyText.text = "$" + money;

            RefreshRow(priceRow, priceLevels, priceLevel, true, money);
            RefreshRow(speedRow, speedLevels, speedLevel, false, money);
        }

        private void RefreshRow(RowView row, List<PotUpgradeLevel> levels, int level, bool isPrice, int money)
        {
            if (row == null) return;
            row.level.text = "LV " + (level + 1);

            float current = isPrice ? CurrentPriceValue : CurrentSpeedValue;
            string cur = isPrice ? "$" + Mathf.RoundToInt(current) : current.ToString("0.##") + "s";

            if (levels.Count == 0)
            {
                row.values.text = cur;
                row.button.interactable = false;
                row.buttonImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                row.buttonText.text = "-";
                return;
            }

            if (level >= levels.Count)
            {
                row.values.text = cur;
                row.button.interactable = false;
                row.buttonImage.color = new Color(0.3f, 0.3f, 0.35f, 1f);
                row.buttonText.text = "MAX";
                return;
            }

            PotUpgradeLevel next = levels[level];
            string nxt = isPrice ? "$" + Mathf.RoundToInt(next.value) : next.value.ToString("0.##") + "s";
            row.values.text = cur + "  ->  " + nxt;

            bool afford = money >= next.cost;
            row.button.interactable = afford;
            row.buttonImage.color = afford ? new Color(0.25f, 0.75f, 0.35f, 1f) : new Color(0.3f, 0.3f, 0.35f, 1f);
            row.buttonText.text = "$" + next.cost;
            row.buttonText.color = afford ? Color.white : new Color(1f, 0.5f, 0.5f, 1f);
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
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }
    }
}
