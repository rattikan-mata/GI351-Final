using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
    /// หน้าตาแผงอยู่ใน prefab ช่อง Panel Prefab (Prefab/UI/Pot Upgrade Panel) แก้ใน Hierarchy ได้
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

        [Tooltip("prefab แผงอัปเกรด (Prefab/UI/Pot Upgrade Panel) ดับเบิลคลิก prefab เพื่อแก้หน้าตาใน Hierarchy\nปล่อยว่าง = สร้างหน้าตาเริ่มต้นด้วยโค้ด")]
        [SerializeField] private PotUpgradePanelView panelPrefab;

        [Header("Events")]
        [Tooltip("ยิงเมื่ออัปเกรดสำเร็จ")]
        public UnityEvent onUpgraded;

        private CookingPot pot;
        private int priceLevel;
        private int speedLevel;
        private float baseSpeed;
        private int basePrice;
        private bool baseCaptured;

        private PotUpgradePanelView view;
        private bool isOpen;
        private float lastInRangeTime;
        private int lastMoney = int.MinValue;
        private bool builtUI;

        public int PriceLevel => priceLevel;
        public int SpeedLevel => speedLevel;
        public bool IsOpen => isOpen;

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

            // กันตั้งค่าผิด: อัปเกรดราคาแล้วราคาต้องสูงขึ้น (เช่น ก๊อปตารางหม้อไก่มาใส่หม้อหมูที่ราคาฐานสูงกว่า)
            float prev = basePrice;
            for (int i = 0; i < priceLevels.Count; i++)
            {
                if (priceLevels[i].value <= prev)
                {
                    Debug.LogWarning("[PotUpgrade] " + pot.PotName + ": Product Price Lv" + (i + 2) + " = $" + priceLevels[i].value.ToString("0.##") + " ไม่สูงกว่าเลเวลก่อนหน้า ($" + prev.ToString("0.##") + ") อัปเกรดแล้วราคาจะไม่ขึ้น", this);
                }
                prev = priceLevels[i].value;
            }
        }

        private void OnDisable()
        {
            if (view != null && view.panel != null) view.panel.SetActive(false);
            StationCameraZoom.Release(this);
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
            if (MoneyManager.Instance.CurrentMoney < next.cost)
            {
                UiAudioManager.Instance?.PlayCantAfford();
                if (view != null) JuiceCantAfford(isPrice ? view.priceRow : view.speedRow);
                return false;
            }
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
            UiAudioManager.Instance?.PlayPurchase();   // เสียงคิดเงิน
            if (view != null) JuiceUpgraded(isPrice ? view.priceRow : view.speedRow);
            onUpgraded?.Invoke();
            if (isOpen) Refresh();
            return true;
        }

        private void SetOpen(bool open)
        {
            if (open == isOpen) return;
            if (Application.isPlaying) UiAudioManager.Instance?.PlayPanelToggle();
            isOpen = open;
            if (open)
            {
                CaptureBase();
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
            UiJuice.Squash(transform);
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
                    Debug.LogError("[PotUpgrade] " + name + ": prefab " + panelPrefab.name + " ขาดช่อง " + missing + " ใช้หน้าตาเริ่มต้นแทน", panelPrefab);
                    Destroy(view.gameObject);
                    view = null;
                }
            }
            if (view == null) view = PotUpgradePanelView.CreateDefault(transform);

            view.panel.SetActive(false);
            HideDuringCameraPan.Attach(view.gameObject);   // กล้องดึงจอ = ซ่อนแผงชั่วคราว
            UiBuild.SetText(view.priceRow.nameText, priceLabel);
            UiBuild.SetText(view.speedRow.nameText, speedLabel);
            view.priceRow.button.onClick.AddListener(() => TryUpgrade(true));
            view.speedRow.button.onClick.AddListener(() => TryUpgrade(false));

            // รูปเหรียญอีแปะแทน $: เงินที่มี / ราคาสินค้า (แถวราคา) / ค่าอัปเกรดบนปุ่ม
            CoinTextIcon.Attach(view.moneyText);
            CoinTextIcon.Attach(view.priceRow.valuesText);
            CoinTextIcon.Attach(view.priceRow.buttonText);
            CoinTextIcon.Attach(view.speedRow.buttonText);
        }

        private void Refresh()
        {
            int money = CurrentMoney;
            lastMoney = money;
            if (view == null) return;
            UiBuild.SetText(view.titleText, pot.PotName.ToUpper());
            UiBuild.SetText(view.moneyText, money.ToString());

            RefreshRow(view.priceRow, priceLevels, priceLevel, true, money);
            RefreshRow(view.speedRow, speedLevels, speedLevel, false, money);
        }

        private void RefreshRow(PotUpgradePanelView.Row row, List<PotUpgradeLevel> levels, int level, bool isPrice, int money)
        {
            row.levelText.text = "LV " + (level + 1);

            float current = isPrice ? CurrentPriceValue : CurrentSpeedValue;
            string cur = isPrice ? Mathf.RoundToInt(current).ToString() : current.ToString("0.##") + "s";
            CoinTextIcon.SetVisible(row.valuesText, isPrice); // เหรียญ 1 อันหน้าราคา เฉพาะแถวราคาสินค้า

            if (levels.Count == 0 || level >= levels.Count)
            {
                // ไม่มีเลเวลให้อัป หรืออัปจนเต็มแล้ว
                row.valuesText.text = cur;
                SetButton(row, false, view.disabledColor, levels.Count == 0 ? view.noLevelsLabel : view.maxLabel, view.buttonTextColor);
                CoinTextIcon.SetVisible(row.buttonText, false);
                return;
            }

            PotUpgradeLevel next = levels[level];
            row.valuesText.text = cur;   // บอกค่าตอนนี้เฉยๆ (ไม่มีลูกศร)

            bool afford = money >= next.cost;
            SetButton(row, true, afford ? view.affordColor : view.disabledColor, next.cost.ToString(),
                afford ? view.buttonTextColor : view.buttonTextTooExpensiveColor);
            CoinTextIcon.SetVisible(row.buttonText, true);
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
