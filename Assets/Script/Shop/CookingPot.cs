using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    [ExecuteAlways]
    public class CookingPot : MonoBehaviour
    {
        [Header("Unlock & Purchase Settings (การซื้อปลดล็อกหม้อ)")]
        [Tooltip("ต้องซื้อหม้อนี้ก่อนใช้งานหรือไม่ (ถ้าติ๊กถูก จะต้องซื้อก่อนถึงจะทำงานได้)")]
        [SerializeField] private bool requiresPurchase = false;

        [Tooltip("เปิดระบบซื้อสำหรับหม้อหมู (Pork/Pig Pot) โดยอัตโนมัติ")]
        [SerializeField] private bool autoRequirePurchaseForPork = true;

        [Tooltip("ราคาที่ต้องจ่ายเพื่อซื้อหม้อ (เหรียญ) ใช้กับวงซื้อที่หม้อ (ถ้าซื้อจากร้าน ราคาตั้งที่สินค้าในร้าน)")]
        [SerializeField] private int purchasePrice = 50;

        [Tooltip("ปิด = ไม่มีวงซื้อที่หม้อ ซื้อได้จากร้านเท่านั้น (Shop > Cook) / เปิด = เดินเข้าวงทองหน้าหม้อเพื่อซื้อแบบเดิม")]
        [SerializeField] private bool buyAtPotRing = false;

        [Tooltip("จุดที่ให้ผู้เล่นยืนเพื่อซื้อหม้อ (ลาก Transform มาใส่ หรือปล่อยว่างเพื่อใช้ระยะเยื้อง)")]
        [SerializeField] private Transform purchaseZonePoint;

        [Tooltip("ตำแหน่งเยื้องสำหรับจุดยืนซื้อหม้อ (เมตร) เช่น (0, -0.6) คืออยู่ด้านหน้าหม้อ หรือ (0, 0) คือตรงกลางหม้อ")]
        [SerializeField] private Vector2 purchaseZoneOffset = new Vector2(0f, -0.6f);

        [Tooltip("รัศมีของวงกลมจุดยืนซื้อหม้อ (เมตร)")]
        [SerializeField] private float purchaseZoneRadius = 0.65f;

        [Tooltip("สีของวงแหวนจุดซื้อหม้อ (สีทองอร่าม)")]
        [SerializeField] private Color purchaseRingColor = new Color(1f, 0.85f, 0.2f, 0.95f);

        [Tooltip("ความโปร่งใสของหม้อก่อนซื้อ (0 = ล่องหน, 1 = ทึบแสงปกติ, 0.35 = โปร่งแสง)")]
        [Range(0.05f, 1f)]
        [SerializeField] private float lockedAlpha = 0.35f;

        [Header("Pot Art (ภาพหม้อ)")]
        [Tooltip("ภาพหม้อตอนว่าง/ยังไม่ต้ม (ว่าง = ใช้ภาพที่ SpriteRenderer อยู่แล้ว)")]
        [SerializeField] private Sprite idleSprite;

        [Tooltip("ภาพหม้อตอนกำลังต้ม เช่น ปิดฝา มีไฟ (ว่าง = ใช้ภาพว่างตลอด)")]
        [SerializeField] private Sprite cookingSprite;

        [Header("Pot UI (ป้ายจำนวนในหม้อ + ป้ายสูตร)")]
        [Tooltip("ปิด = วัตถุดิบลอยเข้าปากหม้อแล้วหายเข้าไป ดูจำนวนที่ป้ายแทน / เปิด = วางเรียงบนหม้อแบบเดิม")]
        [SerializeField] private bool showIngredientsOnPot = false;

        [Tooltip("ไอคอนวัตถุดิบบนป้าย (ว่าง = ใช้ภาพของชิ้นแรกที่ใส่หม้อ)")]
        [SerializeField] private Sprite ingredientIcon;

        [Tooltip("ไอคอนอาหารที่ได้บนป้ายสูตร (ว่าง = ใช้ภาพจาก Cooked Dish Prefab)")]
        [SerializeField] private Sprite dishIcon;

        [Tooltip("ป้ายเดียวเหนือหม้อ: วัตถุดิบ + จำนวนในหม้อ → อาหารที่ได้ และพื้นป้ายเป็นหลอดความคืบหน้าการต้มในตัว")]
        [SerializeField] private bool showRecipeSign = true;

        [Tooltip("ตำแหน่งป้าย เยื้องจากกลางหม้อ (เมตร)")]
        [SerializeField] private Vector2 recipeSignOffset = new Vector2(0f, 1.55f);

        [Tooltip("ขนาดป้าย (กว้าง x สูง เมตร)")]
        [SerializeField] private Vector2 potSignSize = new Vector2(1.9f, 0.7f);

        [SerializeField] private Color recipeSignColor = new Color(0.98f, 0.9f, 0.72f, 1f);
        [SerializeField] private Color recipeSignOutlineColor = new Color(0.55f, 0.36f, 0.2f, 1f);
        [SerializeField] private Color recipeArrowColor = new Color(0.62f, 0.4f, 0.55f, 1f);
        [SerializeField] private Color countTextColor = new Color(0.25f, 0.15f, 0.08f, 1f);

        [Tooltip("สีหลอดความคืบหน้าในป้าย (ถาดเต็มจะใช้ Tray Full Bar Color แทน)")]
        [SerializeField] private Color signProgressColor = new Color(0.6f, 0.88f, 0.42f, 1f);

        [Tooltip("ถ้าติ๊กถูก ต้องกดปุ่ม E เพื่อซื้อ (ถ้าไม่ติ๊ก แค่เดินเข้ามาเหยียบวงจะซื้อให้อัตโนมัติถ้าเงินพอ)")]
        [SerializeField] private bool requireKeyPressToBuy = false;

        [Tooltip("เสียงตอนซื้อหม้อสำเร็จ (ถ้ามี)")]
        [SerializeField] private AudioClip purchaseSuccessSound;

        [Header("Recipe Settings (ตั้งค่าสูตรอาหาร)")]
        [Tooltip("ชื่อของหม้อ (เช่น หม้อต้มไก่ หรือ หม้อต้มหมู)")]
        [SerializeField] private string potName = "Chicken Boiling Pot";

        [Tooltip("วัตถุดิบที่ต้องใช้ใส่ลงหม้อ")]
        [SerializeField] private ItemType requiredIngredient = ItemType.ChickenMeat;

        [Tooltip("Prefab อาหารปรุงสุกที่จะได้รับเมื่อต้มเสร็จ")]
        [SerializeField] private GameObject cookedDishPrefab;

        [Tooltip("Prefab อาหารพิเศษที่ได้จากการยัดโจรลงหม้อ (ถ้าว่างจะใช้ Cooked Dish Prefab แล้วย้อมสีทองแทน)")]
        [SerializeField] private GameObject specialDishPrefab;

        [Tooltip("ระยะเวลาในการต้มอาหารแต่ละชิ้นในคิว (วินาที) - จะต้มทีละ 1 ชิ้นเรียงตามคิว")]
        [SerializeField] private float cookingDuration = 3f;

        [Tooltip("จำนวนวัตถุดิบสูงสุดที่ต่อคิวในหม้อพร้อมกันได้ (ใส่ได้เรื่อยๆ ไม่ต้องรอทำเสร็จ)")]
        [SerializeField] private int maxCookingCapacity = 20;

        [Header("Player Stand Zone (ตำแหน่งวงแหวนสีขาวที่ให้ผู้เล่นยืน)")]
        [Tooltip("จุดที่ให้ผู้เล่นยืน (ถ้าว่างจะใช้ค่า Stand Zone Offset)")]
        [SerializeField] private Transform standZonePoint;

        [Tooltip("ระยะเยื้องจากตัวหม้อไปยังจุดที่ให้ผู้เล่นยืนบนพื้น (เช่น (-1.2, 0) คืออยู่ทางซ้ายของหม้อ)")]
        [SerializeField] private Vector2 standZoneOffset = new Vector2(-1.2f, 0f);

        [Tooltip("รัศมีของวงกลมที่ผู้เล่นต้องเข้ามายืน (เมตร)")]
        [SerializeField] private float standZoneRadius = 0.65f;

        [Tooltip("ถ้าติ๊กถูก ต้องกดปุ่ม E ถึงจะโยนเนื้อลงหม้อ (ถ้าไม่ติ๊ก แค่เดินเข้ามาเหยียบวงกลมจะโยนลงทันที)")]
        [SerializeField] private bool requireKeyPress = false;

        [Header("Floor Ring Visual (วงแหวนสีขาวจุดยืนผู้เล่น)")]
        [Tooltip("เปิดแสดงวงแหวนบนพื้นในหน้า Game")]
        [SerializeField] private bool showFloorRing = true;

        [Tooltip("สีของวงแหวนบนพื้นตอนปกติ (สีขาว)")]
        [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("สีของวงแหวนเมื่อผู้เล่นเข้ามายืนในโซน")]
        [SerializeField] private Color ringActiveColor = new Color(0.4f, 1f, 0.6f, 0.95f);

        [Tooltip("ลำดับการวาด (Sorting Order) ของวงแหวนบนพื้น เพื่อให้อยู่บนพื้นกระเบื้องแต่ใต้ตัวละคร")]
        [SerializeField] private int ringSortingOrder = 1;

        [Tooltip("Sprite วงแหวน (ถ้ามีภาพของตัวเองลากมาใส่ได้ หรือปล่อยว่างสคริปต์จะใช้ FloorStandRing.png ให้อัตโนมัติ)")]
        [SerializeField] private Sprite customRingSprite;

        [Header("Output Zone & Yellow Ring (ตำแหน่งและวงแหวนสีเหลืองจุดอาหารออก)")]
        [Tooltip("จุดที่อาหารสุกจะหล่นออกมา (ลาก Transform มาใส่ หรือปล่อยว่างเพื่อใช้ระยะเยื้องด้านล่าง)")]
        [SerializeField] private Transform foodOutputPoint;

        [Tooltip("ตำแหน่งเยื้องจากตัวหม้อที่อาหารจะหล่นออกมา (ถ้าไม่ได้กำหนด foodOutputPoint)")]
        [SerializeField] private Vector2 outputOffset = new Vector2(0f, -0.9f);

        [Tooltip("แรงดีดอาหารออกมาเบาๆ เมื่อต้มเสร็จ")]
        [SerializeField] private float foodPopForce = 1.5f;

        [Tooltip("เปิดแสดงวงแหวนสีเหลืองบนพื้นตรงจุดอาหารออกในหน้า Game")]
        [SerializeField] private bool showOutputRing = true;

        [Tooltip("สีของวงแหวนจุดที่อาหารออกมา (สีเหลืองทอง)")]
        [SerializeField] private Color outputRingColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        [Tooltip("รัศมีของวงแหวนจุดอาหารออก (เมตร)")]
        [SerializeField] private float outputZoneRadius = 0.5f;

        [Header("Output Tray (ถาดอาหารที่ต้มเสร็จ แบบ Juice King)")]
        [Tooltip("เปิด = อาหารที่ต้มเสร็จไปเรียงเป็นกองแบนบนถาดที่จุดอาหารออก ผู้เล่นเดินเข้าวงแล้วดูดเข้ากระเป๋าทีละจาน / ปิด = เด้งออกมาบนพื้นแบบเดิม")]
        [SerializeField] private bool useOutputTray = true;

        [Tooltip("ถาดจุได้กี่จาน เต็มแล้วหม้อหยุดต้มจนกว่าจะมีคนมาเก็บ")]
        [Min(1)] [SerializeField] private int outputTrayCapacity = 20;

        [Tooltip("จำนวนจานต่อแถวบนถาด")]
        [Min(1)] [SerializeField] private int trayColumns = 5;

        [Tooltip("ระยะห่างระหว่างจานบนถาด (x = ซ้ายขวา, y = แถวถัดไปขยับขึ้น)")]
        [SerializeField] private Vector2 traySpacing = new Vector2(0.36f, 0.16f);

        [Tooltip("ความกว้างของจานบนถาด (หน่วยในโลก)")]
        [Min(0.05f)] [SerializeField] private float trayDishWidth = 0.34f;

        [Tooltip("ตำแหน่งกองบนถาด เยื้องจากจุดอาหารออก (เมตร)")]
        [SerializeField] private Vector2 trayOffset = Vector2.zero;

        [Tooltip("ผู้เล่นอยู่ห่างจากกลางถาดไม่เกินเท่านี้ (เมตร) จะเริ่มดูดจานเข้ากระเป๋า")]
        [Min(0.1f)] [SerializeField] private float trayCollectRadius = 0.9f;

        [Tooltip("ดูดจานเข้ากระเป๋า 1 จานทุกกี่วินาที")]
        [Min(0.01f)] [SerializeField] private float trayCollectInterval = 0.1f;

        [Tooltip("เวลาที่จานลอยจากถาดเข้ากระเป๋า (วินาที)")]
        [Min(0.01f)] [SerializeField] private float trayFlyTime = 0.18f;

        [Tooltip("สีหลอดความคืบหน้าตอนถาดเต็ม (หม้อหยุดรอ)")]
        [SerializeField] private Color trayFullBarColor = new Color(1f, 0.47f, 0.38f, 1f);

        [Header("Visual Feedback & Indicators")]
        [Tooltip("GameObject แสดงเอฟเฟกต์ควัน/ฟองน้ำเดือดขณะต้ม")]
        [SerializeField] private GameObject cookingEffect;

        [Tooltip("เสียงตอนโยนวัตถุดิบลงหม้อ (ถ้ามี)")]
        [SerializeField] private AudioClip ingredientDropSound;

        [Tooltip("เสียงตอนอาหารต้มเสร็จแล้วเด้งออกมา (ถ้ามี)")]
        [SerializeField] private AudioClip foodDoneSound;

        [Header("Ingredient Stack (วัตถุดิบลอยไปเรียงบนหม้อ)")]
        [Tooltip("เวลาที่วัตถุดิบ 1 ชิ้นลอยจากตัวผู้เล่นไปถึงหม้อ (วินาที)")]
        [SerializeField] private float ingredientFlyDuration = 0.35f;

        [Tooltip("เว้นช่วงระหว่างการลอยของแต่ละชิ้น (วินาที) ทำให้ลอยไปทีละชิ้น")]
        [SerializeField] private float ingredientLaunchInterval = 0.12f;

        [Tooltip("ความสูงของโค้งตอนลอย (เมตร)")]
        [SerializeField] private float ingredientArcHeight = 0.6f;

        [Tooltip("จุดเริ่มกองชิ้นล่างสุด เยื้องจากกลางหม้อ (เมตร) (0,0) คือกลางหม้อพอดี")]
        [SerializeField] private Vector2 ingredientStackBase = Vector2.zero;

        [Tooltip("จำนวนชิ้นต่อแถวในปากหม้อ (เรียงแบน ไม่ซ้อนสูง)")]
        [Min(1)] [SerializeField] private int ingredientColumns = 4;

        [Tooltip("ระยะห่างระหว่างชิ้นในปากหม้อ (x = ซ้ายขวา, y = แถวถัดไปขยับขึ้น)")]
        [SerializeField] private Vector2 ingredientSpacing = new Vector2(0.22f, 0.1f);

        [Tooltip("ความกว้างของวัตถุดิบตอนอยู่ในหม้อ (หน่วยในโลก)")]
        [Min(0.05f)] [SerializeField] private float ingredientWidth = 0.4f;

        [Tooltip("จำนวนชิ้นสูงสุดที่โชว์บนหม้อ (เกินนี้จะซ่อนไว้แต่ยังอยู่ในคิว)")]
        [SerializeField] private int maxVisibleIngredients = 8;

        [Header("Gizmo Settings")]
        [Tooltip("สีของโซนตรวจจับใน Scene View")]
        [SerializeField] private Color zoneGizmoColor = new Color(0.2f, 0.8f, 1f, 0.25f);

        [Header("Events")]
        public UnityEvent onPotPurchased;
        public UnityEvent onIngredientAdded;
        public UnityEvent onCookingStarted;
        public UnityEvent onCookingFinished;
        public UnityEvent onFoodOutputSpawned;

        // สถานะการปลดล็อก
        private bool isUnlocked = true;
        private Color defaultPotColor = Color.white;
        private SpriteRenderer potSpriteRenderer;
        private Collider2D potSolidCollider;

        // วัตถุโซนยืนซื้อหม้อ
        private GameObject purchaseZoneObject;
        private SpriteRenderer purchaseRingRenderer;
        private TextMesh purchasePriceTextMesh;

        // ระบบคิวการทำอาหาร (ต้มทีละ 1 ชิ้น 1 คิว เมื่อสุกแล้วคิวถัดไปจะเริ่มต้มต่อทันที)
        private int queuedIngredientCount = 0;
        private float currentCookTimer = 0f;
        private bool isCurrentlyCooking = false;
        private bool isCurrentlyCookingSpecial = false;
        private int queuedSpecialDishCount = 0;

        private bool isPlayerInRange;
        private PlayerCarry cachedPlayerCarry;

        // Visual Objects
        private GameObject standZoneObject;
        private SpriteRenderer floorRingRenderer;

        private GameObject outputRingObject;
        private SpriteRenderer outputRingRenderer;
        private float outputRingSpawnPulseTimer = 0f;

        private static Sprite generatedRingSprite;

        // วัตถุดิบที่ลอยไปวางบนหม้อ (ชิ้นแรกคือชิ้นที่กำลังต้ม)
        private class IngredientVisual
        {
            public Item item;
            public Vector3 startPos;
            public Vector3 startScale;
            public float launchTime;
            public bool landed;
        }
        private readonly List<IngredientVisual> ingredientVisuals = new List<IngredientVisual>();
        private float lastIngredientLaunchTime = -999f;

        public bool IsUnlocked => isUnlocked;
        public bool RequiresPurchase => requiresPurchase;
        public int PurchasePrice => purchasePrice;
        public string PotName => potName;

        /// <summary>ผู้เล่นยืนอยู่ในวงยืนของหม้อนี้หรือไม่ (ใช้เปิดแผงอัปเกรด)</summary>
        public bool IsPlayerInRange => isPlayerInRange;

        // ราคาอาหารที่ได้จากอัปเกรด (0 = ยังไม่อัปเกรด ใช้ราคาจาก Prefab อาหาร)
        private int dishPriceOverride;

        /// <summary>เวลาต้มต่อชิ้น (วินาที) ใช้กับอัปเกรดความเร็ว</summary>
        public float CookingDuration
        {
            get => cookingDuration;
            set
            {
                cookingDuration = Mathf.Max(0.05f, value);
                // จานที่กำลังต้มอยู่เร็วขึ้นตามทันที
                if (isCurrentlyCooking) currentCookTimer = Mathf.Min(currentCookTimer, cookingDuration);
            }
        }

        /// <summary>ราคาขายต่อจานของอาหารที่หม้อนี้ทำออกมาตอนนี้</summary>
        public int CurrentDishPrice
        {
            get
            {
                if (dishPriceOverride > 0) return dishPriceOverride;
                if (cookedDishPrefab != null && cookedDishPrefab.TryGetComponent<Item>(out var it) && it.SellPrice > 0) return it.SellPrice;
                return 10;
            }
        }

        /// <summary>ตั้งราคาอาหารที่หม้อนี้ทำออกมา (อัปเกรดราคา) ส่ง 0 = กลับไปใช้ราคาจาก Prefab</summary>
        public void SetDishPriceOverride(int price)
        {
            dishPriceOverride = Mathf.Max(0, price);
        }
        public int QueuedCount => queuedIngredientCount + queuedSpecialDishCount;
        public int CookingItemsCount => (isCurrentlyCooking ? 1 : 0) + queuedIngredientCount + queuedSpecialDishCount;
        public int MaxCapacity => maxCookingCapacity;
        public bool IsCooking => isCurrentlyCooking;
        public float CurrentProgress => isCurrentlyCooking && cookingDuration > 0f ? Mathf.Clamp01(1f - (currentCookTimer / cookingDuration)) : 0f;
        public float CurrentRemainingTime => isCurrentlyCooking ? Mathf.Max(0f, currentCookTimer) : 0f;
        public float TotalRemainingTime => (isCurrentlyCooking ? currentCookTimer : 0f) + ((queuedIngredientCount + queuedSpecialDishCount) * cookingDuration);

        private void Awake()
        {
            InitPotComponents();
            FindExistingZoneObjects();

            // ใน Edit Mode ห้ามเปลี่ยนสี/Collider ของหม้อ ไม่งั้นค่าจะถูกบันทึกลง Scene/Prefab
            if (!Application.isPlaying) return;

            // ตรวจสอบอัตโนมัติ: ถ้าเป็นหม้อหมู ให้เปิดระบบต้องซื้อก่อนใช้งาน
            bool needPurchase = requiresPurchase || (autoRequirePurchaseForPork && IsPigPot());
            if (needPurchase)
            {
                requiresPurchase = true;
                isUnlocked = false;
                ApplyLockedState();
            }
            else
            {
                isUnlocked = true;
                ApplyUnlockedState();
            }
        }

        public static readonly List<CookingPot> AllPots = new List<CookingPot>();

        /// <summary>วัตถุดิบที่หม้อนี้ใช้ (ใช้ดูว่าหม้อนี้ทำอาหารอะไร)</summary>
        public ItemType RequiredIngredient => requiredIngredient;

        /// <summary>อาหารที่หม้อนี้ทำออกมา</summary>
        public ItemType OutputDishType
        {
            get
            {
                if (FoodTypes.TryGetDish(requiredIngredient, out ItemType dish)) return dish;
                return IsPigPot() ? ItemType.CookedPig : ItemType.CookedChicken;
            }
        }

        /// <summary>ภาพและสีของอาหารที่หม้อนี้ทำ (ใช้ทำไอคอนบนป้าย) คืน false ถ้าไม่มีภาพ</summary>
        public bool TryGetDishIcon(out Sprite sprite, out Color color)
        {
            sprite = dishIcon;
            color = Color.white;
            if (sprite != null) return true;
            if (cookedDishPrefab == null) return false;
            SpriteRenderer dsr = cookedDishPrefab.GetComponentInChildren<SpriteRenderer>();
            if (dsr == null || dsr.sprite == null) return false;
            sprite = dsr.sprite;
            color = dsr.color;
            return true;
        }

        private void OnEnable()
        {
            if (!AllPots.Contains(this)) AllPots.Add(this);

            InitPotComponents();
            FindExistingZoneObjects();

            if (!Application.isPlaying) return;

            bool needPurchase = requiresPurchase || (autoRequirePurchaseForPork && IsPigPot());
            if (needPurchase && !isUnlocked)
            {
                ApplyLockedState();
            }
            else
            {
                ApplyUnlockedState();
            }
        }

        private void OnDisable()
        {
            AllPots.Remove(this);
        }

        private void OnValidate()
        {
            if (standZoneRadius < 0.1f) standZoneRadius = 0.1f;
            if (outputZoneRadius < 0.1f) outputZoneRadius = 0.1f;
            if (purchaseZoneRadius < 0.1f) purchaseZoneRadius = 0.1f;

#if UNITY_EDITOR
            // สร้าง/ย้าย GameObject ตรงๆ ใน OnValidate ไม่ได้ (Unity เตือน SendMessage) จึงเลื่อนไปทำหลังจากนี้
            if (Application.isPlaying)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    if (this == null || !Application.isPlaying) return;
                    if (isUnlocked)
                    {
                        SetupStandZoneAndRing();
                        SetupOutputRing();
                    }
                    else
                    {
                        SetupPurchaseZone();
                    }
                };
            }
#endif
        }

        public bool IsPigPot()
        {
            string pName = potName != null ? potName.ToLower() : "";
            string gName = gameObject != null ? gameObject.name.ToLower() : "";
            return requiredIngredient == ItemType.PigMeat || pName.Contains("pig") || pName.Contains("pork") || gName.Contains("pig") || gName.Contains("pork");
        }

        /// <summary>
        /// ดึง child วงแหวนที่บันทึกอยู่ใน Scene/Prefab แล้ว เพื่อให้ซ่อน/แสดงได้ตั้งแต่ Awake
        /// </summary>
        private void FindExistingZoneObjects()
        {
            if (standZoneObject == null)
            {
                Transform t = transform.Find("PlayerStandZone");
                if (t != null) standZoneObject = t.gameObject;
            }
            if (outputRingObject == null)
            {
                Transform t = transform.Find("FoodOutputRing");
                if (t != null) outputRingObject = t.gameObject;
            }
            if (purchaseZoneObject == null)
            {
                Transform t = transform.Find("PotPurchaseZone");
                if (t != null) purchaseZoneObject = t.gameObject;
            }
        }

        private void InitPotComponents()
        {
            if (potSpriteRenderer == null)
            {
                potSpriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (potSpriteRenderer != null)
            {
                if (defaultPotColor == Color.white || defaultPotColor.a < 0.8f)
                {
                    defaultPotColor = potSpriteRenderer.color;
                    if (defaultPotColor.a < 0.8f) defaultPotColor.a = 1f;
                }
            }

            if (potSolidCollider == null)
            {
                potSolidCollider = GetComponent<Collider2D>();
            }
        }

        /// <summary>
        /// สถานะหม้อก่อนซื้อ: หม้อโปร่งแสง, เดินทะลุได้, ซ่อนจุดปรุงอาหาร และเปิดจุดยืนซื้อหม้อ
        /// </summary>
        private void ApplyLockedState()
        {
            InitPotComponents();

            // 1. หม้อโปร่งใส (Transparent)
            if (potSpriteRenderer != null)
            {
                Color c = defaultPotColor;
                c.a = lockedAlpha;
                potSpriteRenderer.color = c;
            }

            // 2. เดินทะลุได้ก่อนซื้อ (Trigger / ไม่บล็อกทางเดิน)
            if (potSolidCollider != null)
            {
                potSolidCollider.isTrigger = true;
            }

            // 3. ปิดการใช้งานจุดยืนปรุงอาหารและจุดอาหารออก
            if (standZoneObject != null) standZoneObject.SetActive(false);
            if (outputRingObject != null) outputRingObject.SetActive(false);

            // 4. เปิดจุดยืนซื้อหม้อ (เฉพาะถ้าให้ซื้อที่หม้อ ไม่งั้นซื้อจากร้าน)
            if (Application.isPlaying)
            {
                if (buyAtPotRing)
                {
                    SetupPurchaseZone();
                    if (purchaseZoneObject != null) purchaseZoneObject.SetActive(true);
                }
                else if (purchaseZoneObject != null)
                {
                    purchaseZoneObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// สถานะหม้อหลังซื้อ: หม้อทึบแสงปกติ, เดินชนไม่ทะลุ, ปิดจุดซื้อ และเปิดวงแหวนปรุงอาหารพร้อมใช้งาน
        /// </summary>
        private void ApplyUnlockedState()
        {
            InitPotComponents();

            // 1. หม้อทึบแสงปกติ (Opaque)
            if (potSpriteRenderer != null)
            {
                potSpriteRenderer.color = defaultPotColor;
            }

            // 2. เดินชนไม่ทะลุ (Solid)
            if (potSolidCollider != null)
            {
                potSolidCollider.isTrigger = false;
                potSolidCollider.enabled = true;
            }

            // 3. ปิดจุดยืนซื้อหม้อ
            if (purchaseZoneObject != null)
            {
                purchaseZoneObject.SetActive(false);
            }

            // 4. เปิดใช้งานวงแหวนปรุงอาหารและจุดอาหารออก
            if (Application.isPlaying)
            {
                SetupStandZoneAndRing();
                SetupOutputRing();
            }

            if (standZoneObject != null) standZoneObject.SetActive(true);
            if (outputRingObject != null) outputRingObject.SetActive(true);

            UpdateVisuals();
        }

        /// <summary>
        /// สร้างจุดยืนสำหรับซื้อหม้อ พร้อมวงแหวนสีทองและป้ายราคา ($XX)
        /// </summary>
        private void SetupPurchaseZone()
        {
            Transform existingZone = transform.Find("PotPurchaseZone");

            // ซื้อจากร้านแทน: ไม่สร้างวงซื้อ (ถ้ามีอยู่แล้วให้ซ่อน)
            if (!buyAtPotRing)
            {
                if (existingZone != null)
                {
                    purchaseZoneObject = existingZone.gameObject;
                    purchaseZoneObject.SetActive(false);
                }
                return;
            }

            if (existingZone != null)
            {
                purchaseZoneObject = existingZone.gameObject;
            }
            else
            {
                purchaseZoneObject = new GameObject("PotPurchaseZone");
                purchaseZoneObject.transform.SetParent(transform);
            }

            purchaseZoneObject.transform.position = GetPurchaseZonePosition();
            purchaseZoneObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = purchaseZoneRadius * 2f;
            purchaseZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            // วงแหวนสีทองบนพื้น
            purchaseRingRenderer = purchaseZoneObject.GetComponent<SpriteRenderer>();
            if (purchaseRingRenderer == null)
            {
                purchaseRingRenderer = purchaseZoneObject.AddComponent<SpriteRenderer>();
            }
            purchaseRingRenderer.enabled = true;
            purchaseRingRenderer.sprite = GetRingSprite();
            purchaseRingRenderer.color = purchaseRingColor;
            purchaseRingRenderer.sortingOrder = ringSortingOrder;

            // ข้อความแสดงราคา ($50)
            Transform existingText = purchaseZoneObject.transform.Find("PriceText");
            GameObject textObj = existingText != null ? existingText.gameObject : new GameObject("PriceText");
            textObj.transform.SetParent(purchaseZoneObject.transform);
            textObj.transform.localPosition = Vector3.zero;
            textObj.transform.localRotation = Quaternion.identity;
            textObj.transform.localScale = new Vector3(1f / (targetDiameter / pX), 1f / (targetDiameter / pY), 1f);

            purchasePriceTextMesh = textObj.GetComponent<TextMesh>();
            if (purchasePriceTextMesh == null)
            {
                purchasePriceTextMesh = textObj.AddComponent<TextMesh>();
            }
            purchasePriceTextMesh.text = $"${purchasePrice}";
            purchasePriceTextMesh.fontSize = 44;
            purchasePriceTextMesh.characterSize = 0.06f;
            purchasePriceTextMesh.alignment = TextAlignment.Center;
            purchasePriceTextMesh.anchor = TextAnchor.MiddleCenter;
            purchasePriceTextMesh.color = Color.yellow;

            MeshRenderer mr = textObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingOrder = ringSortingOrder + 2;
            }
        }

        public Vector3 GetPurchaseZonePosition()
        {
            if (purchaseZonePoint != null)
            {
                return purchaseZonePoint.position;
            }
            return transform.position + new Vector3(purchaseZoneOffset.x, purchaseZoneOffset.y, 0f);
        }

        /// <summary>
        /// สร้างโซนและวงแหวนสีขาวบนพื้นสำหรับให้ผู้เล่นยืนทำอาหาร
        /// </summary>
        public void SetupStandZoneAndRing()
        {
            Transform existingZone = transform.Find("PlayerStandZone");
            if (existingZone != null)
            {
                standZoneObject = existingZone.gameObject;
            }
            else
            {
                standZoneObject = new GameObject("PlayerStandZone");
                standZoneObject.transform.SetParent(transform);
            }

            standZoneObject.transform.position = GetStandZonePosition();
            standZoneObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = standZoneRadius * 2f;
            standZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            CircleCollider2D circleCol = standZoneObject.GetComponent<CircleCollider2D>();
            if (circleCol == null)
            {
                circleCol = standZoneObject.AddComponent<CircleCollider2D>();
            }
            circleCol.isTrigger = true;
            circleCol.radius = 0.5f;
            circleCol.offset = Vector2.zero;

            if (Application.isPlaying)
            {
                PotTriggerZoneProxy proxy = standZoneObject.GetComponent<PotTriggerZoneProxy>();
                if (proxy == null)
                {
                    proxy = standZoneObject.AddComponent<PotTriggerZoneProxy>();
                }
                proxy.pot = this;
            }

            floorRingRenderer = standZoneObject.GetComponent<SpriteRenderer>();
            if (showFloorRing)
            {
                if (floorRingRenderer == null)
                {
                    floorRingRenderer = standZoneObject.AddComponent<SpriteRenderer>();
                }
                floorRingRenderer.enabled = true;
                floorRingRenderer.sprite = GetRingSprite();
                floorRingRenderer.color = isPlayerInRange ? ringActiveColor : ringColor;

                SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
                if (potRenderer != null)
                {
                    if (potRenderer.sharedMaterial != null)
                    {
                        floorRingRenderer.sharedMaterial = potRenderer.sharedMaterial;
                    }
                    floorRingRenderer.sortingLayerID = potRenderer.sortingLayerID;
                }
                floorRingRenderer.sortingOrder = ringSortingOrder;
            }
            else if (floorRingRenderer != null)
            {
                floorRingRenderer.enabled = false;
            }
        }

        /// <summary>
        /// สร้างวงแหวนสีเหลืองตรงจุดที่อาหารสุกจะหล่นออกมา
        /// </summary>
        public void SetupOutputRing()
        {
            Transform existingRing = transform.Find("FoodOutputRing");
            if (existingRing != null)
            {
                outputRingObject = existingRing.gameObject;
            }
            else
            {
                outputRingObject = new GameObject("FoodOutputRing");
                outputRingObject.transform.SetParent(transform);
            }

            outputRingObject.transform.position = GetOutputPosition();
            outputRingObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = outputZoneRadius * 2f;
            outputRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            outputRingRenderer = outputRingObject.GetComponent<SpriteRenderer>();
            if (showOutputRing)
            {
                if (outputRingRenderer == null)
                {
                    outputRingRenderer = outputRingObject.AddComponent<SpriteRenderer>();
                }
                outputRingRenderer.enabled = true;
                outputRingRenderer.sprite = GetRingSprite();
                outputRingRenderer.color = outputRingColor;

                SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
                if (potRenderer != null)
                {
                    if (potRenderer.sharedMaterial != null)
                    {
                        outputRingRenderer.sharedMaterial = potRenderer.sharedMaterial;
                    }
                    outputRingRenderer.sortingLayerID = potRenderer.sortingLayerID;
                }
                outputRingRenderer.sortingOrder = ringSortingOrder;
            }
            else if (outputRingRenderer != null)
            {
                outputRingRenderer.enabled = false;
            }
        }

        /// <summary>สลับภาพหม้อ ว่าง <-> กำลังต้ม (ถ้าตั้งภาพไว้)</summary>
        private void UpdatePotSprite()
        {
            if (potSpriteRenderer == null || (idleSprite == null && cookingSprite == null)) return;
            Sprite want = isCurrentlyCooking && cookingSprite != null ? cookingSprite : idleSprite;
            if (want != null && potSpriteRenderer.sprite != want) potSpriteRenderer.sprite = want;
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                UpdateEditorVisuals();
                return;
            }

            // ถ้ายังไม่ได้ซื้อหม้อ ให้ทำงานเฉพาะระบบจุดยืนซื้อหม้อเท่านั้น
            if (!isUnlocked)
            {
                if (buyAtPotRing) UpdatePurchaseZoneLogic();
                return;
            }

            // อัปเดตเวลาการต้มอาหารทีละ 1 ชิ้นตามคิว (ถาดเต็ม = หยุดรอ)
            UpdateCookingQueue();
            UpdateTrayCollect();

            // วัตถุดิบลอยไปเรียงบนหม้อ + ป้ายบนหม้อ (จำนวน + หลอดความคืบหน้าในป้าย)
            UpdateIngredientVisuals();
            UpdatePotSprite();
            UpdatePotUI();

            // แอนิเมชันของวงแหวนสีขาว (จุดยืนผู้เล่น)
            UpdateFloorRingAnimation();

            // แอนิเมชันของวงแหวนสีเหลือง (จุดอาหารออก)
            UpdateOutputRingAnimation();

            // ตรวจหา Player ในระยะใกล้เคียงจุดยืน
            CheckProximityPlayer();

            // ตรวจจับการรับวัตถุดิบอัตโนมัติขณะผู้เล่นยืนอยู่ในโซน
            if (isPlayerInRange && cachedPlayerCarry != null)
            {
                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    TryFeedIngredients(cachedPlayerCarry);
                }
            }
        }

        private void UpdatePurchaseZoneLogic()
        {
            if (purchaseZoneObject == null)
            {
                SetupPurchaseZone();
                if (purchaseZoneObject == null) return;
            }

            // แอนิเมชันวงแหวนสีทองเต้นเป็นจังหวะ Pulse
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = purchaseZoneRadius * 2f * pulse;
            purchaseZoneObject.transform.position = GetPurchaseZonePosition();
            purchaseZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            // ตรวจจับผู้เล่น
            if (cachedPlayerCarry == null)
            {
#if UNITY_2023_1_OR_NEWER
                cachedPlayerCarry = Object.FindAnyObjectByType<PlayerCarry>();
#else
                cachedPlayerCarry = FindObjectOfType<PlayerCarry>();
#endif
            }

            if (cachedPlayerCarry == null) return;

            Vector3 playerPos = cachedPlayerCarry.transform.position;
            float dist = Vector2.Distance(playerPos, GetPurchaseZonePosition());
            bool isPlayerInBuyZone = dist <= purchaseZoneRadius;

            int currentMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            bool canAfford = currentMoney >= purchasePrice;

            // ปรับสีข้อความราคาตามเงินของผู้เล่น (ถ้าพอ = เหลือง/เขียว, ถ้าไม่พอ = แดง)
            if (purchasePriceTextMesh != null)
            {
                purchasePriceTextMesh.color = canAfford ? (isPlayerInBuyZone ? Color.green : Color.yellow) : Color.red;
            }

            if (purchaseRingRenderer != null)
            {
                purchaseRingRenderer.color = isPlayerInBuyZone ? (canAfford ? Color.green : new Color(1f, 0.4f, 0.4f, 0.9f)) : purchaseRingColor;
            }

            // เมื่อผู้เล่นยืนในโซนซื้อ
            if (isPlayerInBuyZone)
            {
                if (!requireKeyPressToBuy || WasInteractKeyPressed())
                {
                    TryBuyPot();
                }
            }
        }

        /// <summary>
        /// ตรวจสอบเงินและซื้อหม้อ
        /// </summary>
        public bool TryBuyPot()
        {
            if (isUnlocked) return true;

            if (MoneyManager.Instance != null)
            {
                // เช็คเงินก่อน ไม่งั้น SpendMoney จะ log "Not enough money!" ทุกเฟรมที่ยืนในวง
                if (MoneyManager.Instance.CurrentMoney < purchasePrice) return false;

                if (MoneyManager.Instance.SpendMoney(purchasePrice))
                {
                    UiAudioManager.Instance?.PlayBuyOrUnlockZone();
                    UnlockPot();
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// ปลดล็อกหม้อสำเร็จ: เปลี่ยนหม้อเป็นทึบแสง, เดินชนไม่ทะลุ และเปิดใช้งานระบบทำอาหารทันที
        /// </summary>
        public void UnlockPot()
        {
            if (isUnlocked) return;
            isUnlocked = true;

            if (purchaseSuccessSound != null)
            {
                AudioSource.PlayClipAtPoint(purchaseSuccessSound, transform.position);
            }

            ApplyUnlockedState();
            onPotPurchased?.Invoke();
            Debug.Log($"[CookingPot] {potName} Unlocked & Purchased successfully for {purchasePrice} coins!");
        }

        private void UpdateEditorVisuals()
        {
            // ไม่เปลี่ยนสีหม้อใน Edit Mode (จะถูกบันทึกลง Scene) ใช้ Gizmo วงซื้อหม้อแทนการพรีวิว
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            if (standZoneObject != null)
            {
                standZoneObject.transform.position = GetStandZonePosition();
                float targetDiameter = standZoneRadius * 2f;
                standZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);
            }

            if (outputRingObject != null)
            {
                outputRingObject.transform.position = GetOutputPosition();
                float targetDiameter = outputZoneRadius * 2f;
                outputRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);
            }
        }

        private bool WasInteractKeyPressed()
        {
            bool pressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                pressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && Input.GetKeyDown(KeyCode.E))
            {
                pressed = true;
            }
#endif

            return pressed;
        }

        private void UpdateFloorRingAnimation()
        {
            if (floorRingRenderer == null || !showFloorRing || standZoneObject == null) return;

            Color targetColor = isPlayerInRange ? ringActiveColor : ringColor;
            floorRingRenderer.color = Color.Lerp(floorRingRenderer.color, targetColor, Time.deltaTime * 10f);

            float pulse = 1f + Mathf.Sin(Time.time * 2.5f) * 0.04f;
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = standZoneRadius * 2f * pulse;

            standZoneObject.transform.position = GetStandZonePosition();
            standZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);
        }

        private void UpdateOutputRingAnimation()
        {
            if (outputRingRenderer == null || !showOutputRing || outputRingObject == null) return;

            float popBounce = 0f;
            if (outputRingSpawnPulseTimer > 0f)
            {
                outputRingSpawnPulseTimer -= Time.deltaTime;
                popBounce = Mathf.Sin((outputRingSpawnPulseTimer / 0.35f) * Mathf.PI) * 0.15f;
            }

            float pulse = 1f + Mathf.Sin(Time.time * 2f) * 0.03f + popBounce;
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = outputZoneRadius * 2f * pulse;

            outputRingObject.transform.position = GetOutputPosition();
            outputRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            Color targetCol = popBounce > 0.01f ? Color.Lerp(outputRingColor, Color.white, 0.4f) : outputRingColor;
            outputRingRenderer.color = Color.Lerp(outputRingRenderer.color, targetCol, Time.deltaTime * 12f);
        }

        private void UpdateCookingQueue()
        {
            if (!isCurrentlyCooking) return;

            // ถาดเต็ม: หยุดต้มชิ้นปัจจุบันไว้ก่อน (หลอดเปลี่ยนสี) จนกว่าผู้เล่นจะมาเก็บ
            if (IsTrayFull) return;

            currentCookTimer -= Time.deltaTime;

            if (currentCookTimer <= 0f)
            {
                // ชิ้นที่ต้มเสร็จ (ชิ้นล่างสุดบนหม้อ) หายไปกลายเป็นอาหาร  อาหารพิเศษไม่มีชิ้นบนหม้อ
                if (!isCurrentlyCookingSpecial)
                {
                    RemoveFirstIngredientVisual();
                }

                if (isCurrentlyCookingSpecial)
                {
                    SpawnSpecialCookedDishAtOutput();
                }
                else
                {
                    SpawnCookedDishAtOutput();
                }

                onCookingFinished?.Invoke();

                if (foodDoneSound != null)
                {
                    AudioSource.PlayClipAtPoint(foodDoneSound, transform.position);
                }

                // ตรวจสอบคิวชิ้นถัดไป (จานพิเศษหรือจานธรรมดา)
                if (queuedSpecialDishCount > 0)
                {
                    queuedSpecialDishCount--;
                    isCurrentlyCookingSpecial = true;
                    currentCookTimer = cookingDuration;
                    isCurrentlyCooking = true;
                }
                else if (queuedIngredientCount > 0)
                {
                    queuedIngredientCount--;
                    isCurrentlyCookingSpecial = false;
                    currentCookTimer = cookingDuration;
                    isCurrentlyCooking = true;
                }
                else
                {
                    isCurrentlyCooking = false;
                    isCurrentlyCookingSpecial = false;
                    currentCookTimer = 0f;
                }

                UpdateVisuals();
            }
        }

        public int TryFeedIngredients(PlayerCarry playerCarry)
        {
            if (!isUnlocked) return 0;
            if (playerCarry == null) return 0;

            int fedCount = 0;

            // 1. ตรวจสอบว่าผู้เล่นแบก "โจรที่สลบ" (UnconsciousThief) มาหรือไม่ (สามารถจับยัดเข้าหม้อไหนก็ได้!)
            while (CookingItemsCount < maxCookingCapacity && playerCarry.HasItemOfType(ItemType.UnconsciousThief))
            {
                Item thief = playerCarry.PopItemOfType(ItemType.UnconsciousThief);
                if (thief != null)
                {
                    Destroy(thief.gameObject);
                    AddSpecialIngredientToQueue();
                    fedCount++;
                    Debug.Log($"[CookingPot] ยัดโจรเข้า {potName} สำเร็จ! กำลังปรุงอาหารพิเศษราคาแพง x10...");
                }
                else
                {
                    break;
                }
            }

            // 2. ป้อนวัตถุดิบเนื้อปกติประจำหม้อ (เนื้อไก่ หรือ เนื้อหมู)
            while (CookingItemsCount < maxCookingCapacity && playerCarry.HasItemOfType(requiredIngredient))
            {
                Item meat = playerCarry.PopItemOfType(requiredIngredient);
                if (meat != null)
                {
                    AddIngredientVisual(meat);
                    AddIngredientToQueue();
                    fedCount++;
                }
                else
                {
                    break;
                }
            }

            return fedCount;
        }

        private void AddIngredientToQueue()
        {
            if (!isUnlocked) return;

            bool wasIdle = !isCurrentlyCooking;

            if (wasIdle)
            {
                isCurrentlyCooking = true;
                isCurrentlyCookingSpecial = false;
                currentCookTimer = cookingDuration;
                onCookingStarted?.Invoke();
            }
            else
            {
                queuedIngredientCount++;
            }

            UpdateVisuals();
            onIngredientAdded?.Invoke();

            if (ingredientDropSound != null)
            {
                AudioSource.PlayClipAtPoint(ingredientDropSound, transform.position);
            }
        }

        // ========================== วัตถุดิบลอยไปเรียงบนหม้อ ==========================

        /// <summary>
        /// เก็บวัตถุดิบไว้แสดงบนหม้อแทนการทำลายทันที จะลอยจากตัวผู้เล่นไปเรียงบนหม้อทีละชิ้น
        /// </summary>
        private void AddIngredientVisual(Item meat)
        {
            // ปิดการเก็บ/ชน/สลายตัวของไอเทม ขณะอยู่บนหม้อ
            meat.SetCarriedState(true);
            meat.transform.SetParent(transform, true);

            float launch = Mathf.Max(Time.time, lastIngredientLaunchTime + ingredientLaunchInterval);
            lastIngredientLaunchTime = launch;

            ingredientVisuals.Add(new IngredientVisual
            {
                item = meat,
                startPos = meat.transform.position,
                startScale = meat.transform.lossyScale,
                launchTime = launch
            });

            if (meat.SpriteRenderer != null)
            {
                SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
                if (potRenderer != null)
                {
                    meat.SpriteRenderer.sortingLayerID = potRenderer.sortingLayerID;
                    meat.SpriteRenderer.sortingOrder = potRenderer.sortingOrder + 2;
                }
            }
        }

        private void RemoveFirstIngredientVisual()
        {
            if (ingredientVisuals.Count == 0) return;

            IngredientVisual first = ingredientVisuals[0];
            ingredientVisuals.RemoveAt(0);
            if (first.item != null)
            {
                Destroy(first.item.gameObject);
            }
        }

        /// <summary>
        /// ตำแหน่งชิ้นที่ index ในกองแนวตั้งบนหม้อ (ชิ้น 0 อยู่ล่างสุดและต้มก่อน)
        /// </summary>
        private Vector3 GetIngredientSlotPosition(int index)
        {
            // กองแบนในปากหม้อ (ไม่ซ้อนสูง)
            Vector2 off = FlatPile.Offset(index, ingredientColumns, ingredientSpacing);
            return transform.position
                + new Vector3(ingredientStackBase.x + off.x, ingredientStackBase.y + off.y, -0.05f);
        }

        private void UpdateIngredientVisuals()
        {
            ingredientVisuals.RemoveAll(v => v.item == null);

            int visibleCount = Mathf.Min(ingredientVisuals.Count, Mathf.Max(1, maxVisibleIngredients));
            Vector3 lossy = transform.lossyScale;

            SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
            int baseIngredientOrder = (potRenderer != null ? potRenderer.sortingOrder : 0) + 2;

            for (int i = 0; i < ingredientVisuals.Count; i++)
            {
                IngredientVisual v = ingredientVisuals[i];
                Transform t = v.item.transform;
                bool visible = i < visibleCount;

                // ขนาดในปากหม้อ: กว้าง ingredientWidth (ชดเชยสเกลของหม้อให้เอง)
                Vector3 displayLocalScale = v.item.SpriteRenderer != null
                    ? FlatPile.LocalScaleForWidth(t, v.item.SpriteRenderer, ingredientWidth)
                    : new Vector3(
                        v.startScale.x / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                        v.startScale.y / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                        1f);

                if (v.item.SpriteRenderer != null)
                {
                    // ก่อนถึงคิวลอยยังเห็นอยู่ตรงตำแหน่งเดิม หลังลอยแล้วเห็นเฉพาะชิ้นที่อยู่ในจำนวนโชว์
                    v.item.SpriteRenderer.enabled = Time.time < v.launchTime || visible;
                }

                // ยังไม่ถึงคิวลอย: ค้างอยู่ที่ตำแหน่งเดิม
                if (Time.time < v.launchTime) continue;

                float flyProgress = ingredientFlyDuration > 0f ? (Time.time - v.launchTime) / ingredientFlyDuration : 1f;

                // ไม่โชว์ของบนหม้อ: ลอยเข้าปากหม้อแล้วหดหายเข้าไป (ดูจำนวนที่ป้ายแทน)
                if (!showIngredientsOnPot)
                {
                    Vector3 mouth = transform.position + new Vector3(ingredientStackBase.x, ingredientStackBase.y, -0.05f);
                    if (flyProgress < 1f)
                    {
                        float e = Mathf.SmoothStep(0f, 1f, flyProgress);
                        Vector3 p = Vector3.Lerp(v.startPos, mouth, e);
                        p.y += Mathf.Sin(flyProgress * Mathf.PI) * ingredientArcHeight;
                        t.position = p;
                        // ช่วงท้ายหดลงเหมือนตกลงไปในหม้อ
                        float shrink = flyProgress < 0.7f ? 1f : Mathf.Lerp(1f, 0.2f, (flyProgress - 0.7f) / 0.3f);
                        t.localScale = displayLocalScale * shrink;
                        if (v.item.SpriteRenderer != null)
                        {
                            v.item.SpriteRenderer.enabled = true;
                            v.item.SpriteRenderer.sortingOrder = baseIngredientOrder + 10;
                        }
                    }
                    else
                    {
                        t.position = mouth;
                        t.localScale = displayLocalScale;
                        if (v.item.SpriteRenderer != null) v.item.SpriteRenderer.enabled = false;
                        if (!v.landed)
                        {
                            v.landed = true;
                            PunchCountBadge();
                        }
                    }
                    t.localRotation = Quaternion.identity;
                    continue;
                }

                if (!visible)
                {
                    // ชิ้นที่เกินจำนวนโชว์ ซ่อนไว้กลางหม้อ เมื่อมีที่ว่างจะเลื่อนออกมาเอง
                    t.position = transform.position;
                    t.localScale = displayLocalScale;
                    continue;
                }

                Vector3 slot = GetIngredientSlotPosition(i);

                // กองแบน: แถวหน้าวาดทับแถวหลัง
                if (v.item.SpriteRenderer != null)
                {
                    v.item.SpriteRenderer.sortingOrder = baseIngredientOrder + FlatPile.SortingOffset(i, ingredientColumns, maxVisibleIngredients);
                }
                float progress = ingredientFlyDuration > 0f ? (Time.time - v.launchTime) / ingredientFlyDuration : 1f;

                if (progress < 1f)
                {
                    // ลอยเป็นโค้งจากตัวผู้เล่นไปยังช่องบนหม้อ
                    float eased = Mathf.SmoothStep(0f, 1f, progress);
                    Vector3 pos = Vector3.Lerp(v.startPos, slot, eased);
                    pos.y += Mathf.Sin(progress * Mathf.PI) * ingredientArcHeight;
                    t.position = pos;
                    t.localScale = displayLocalScale;
                }
                else
                {
                    // ถึงแล้ว: เลื่อนเข้าช่องนุ่ม ๆ (ช่องขยับเมื่อชิ้นแรกต้มเสร็จ)
                    t.position = Vector3.Lerp(t.position, slot, Time.deltaTime * 12f);
                    t.localScale = displayLocalScale;
                }
                t.localRotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// เพิ่มวัตถุดิบพิเศษ (โจรที่สลบ) เข้าคิวทำอาหารพิเศษ
        /// </summary>
        private void AddSpecialIngredientToQueue()
        {
            if (!isUnlocked) return;

            bool wasIdle = !isCurrentlyCooking;

            if (wasIdle)
            {
                isCurrentlyCooking = true;
                isCurrentlyCookingSpecial = true;
                currentCookTimer = cookingDuration;
                onCookingStarted?.Invoke();
            }
            else
            {
                queuedSpecialDishCount++;
            }

            UpdateVisuals();
            onIngredientAdded?.Invoke();

            if (ingredientDropSound != null)
            {
                AudioSource.PlayClipAtPoint(ingredientDropSound, transform.position);
            }
        }

        private void SpawnCookedDishAtOutput()
        {
            if (cookedDishPrefab == null) return;

            Vector3 spawnPos = GetOutputPosition();
            outputRingSpawnPulseTimer = 0.35f;

            Vector2 randomSpread = Random.insideUnitCircle * 0.15f;
            Vector3 finalSpawnPos = spawnPos + new Vector3(randomSpread.x, randomSpread.y, 0f);

            GameObject dishObj = Instantiate(cookedDishPrefab, finalSpawnPos, Quaternion.identity);

            // ใช้ราคาที่อัปเกรดไว้ (ถ้ามี)
            if (dishPriceOverride > 0 && dishObj.TryGetComponent<Item>(out var pricedItem))
            {
                pricedItem.SetSellPrice(dishPriceOverride);
            }

            if (useOutputTray && dishObj.TryGetComponent<Item>(out var trayItem))
            {
                AddToTray(trayItem);
                onFoodOutputSpawned?.Invoke();
                return;
            }

            if (foodPopForce > 0f && dishObj.TryGetComponent<Rigidbody2D>(out var dishRb))
            {
                Vector2 popDir = ((Vector2)(finalSpawnPos - transform.position)).normalized;
                if (popDir == Vector2.zero) popDir = Vector2.down;
                dishRb.AddForce(popDir * foodPopForce, ForceMode2D.Impulse);
            }

            onFoodOutputSpawned?.Invoke();
        }

        /// <summary>
        /// ผลิตอาหารพิเศษจากโจร: ราคาแพง x10 จากไก่ (ไก่ 10 เหรียญ -> อาหารพิเศษ 100 เหรียญ)
        /// เปล่งออร่าสีเหลืองทองอร่าม
        /// </summary>
        private void SpawnSpecialCookedDishAtOutput()
        {
            GameObject specialPrefab = specialDishPrefab != null ? specialDishPrefab : cookedDishPrefab;
            if (specialPrefab == null) return;

            Vector3 spawnPos = GetOutputPosition();
            outputRingSpawnPulseTimer = 0.35f;

            Vector2 randomSpread = Random.insideUnitCircle * 0.15f;
            Vector3 finalSpawnPos = spawnPos + new Vector3(randomSpread.x, randomSpread.y, 0f);

            GameObject dishObj = Instantiate(specialPrefab, finalSpawnPos, Quaternion.identity);

            // ตั้งค่าไอเทมให้เป็นอาหารพิเศษราคาแพง x10
            if (dishObj.TryGetComponent<Item>(out var item))
            {
                item.SetItemType(ItemType.SpecialDish);
                item.SetItemName("Special Golden Dish");
                item.SetSellPrice(100);
                item.SetItemColor(new Color(1f, 0.85f, 0.12f, 1f));
            }

            if (dishObj.TryGetComponent<CookedDish>(out var cookedDish))
            {
                cookedDish.SetDishName("Special Dish (x10)");
                cookedDish.SetDishQuality(5);
            }

            // ย้อมสีทองอร่ามให้เห็นเด่นชัดว่าเป็นอาหารชั้นยอด
            if (dishObj.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = new Color(1f, 0.85f, 0.12f, 1f);
            }

            if (useOutputTray && dishObj.TryGetComponent<Item>(out var trayItem))
            {
                AddToTray(trayItem);
                onFoodOutputSpawned?.Invoke();
                return;
            }

            if (foodPopForce > 0f && dishObj.TryGetComponent<Rigidbody2D>(out var dishRb))
            {
                Vector2 popDir = ((Vector2)(finalSpawnPos - transform.position)).normalized;
                if (popDir == Vector2.zero) popDir = Vector2.down;
                dishRb.AddForce(popDir * foodPopForce, ForceMode2D.Impulse);
            }

            onFoodOutputSpawned?.Invoke();
            Debug.Log("[CookingPot] ปรุงเสร็จแล้ว! อาหารพิเศษ Special Golden Dish (100 coins) กระเด็นออกมาจากหม้อ!");
        }

        // ========================== ถาดอาหารที่ต้มเสร็จ ==========================

        private readonly List<Item> trayItems = new List<Item>();
        private float nextTrayCollectTime;

        /// <summary>จำนวนจานบนถาด</summary>
        public int TrayCount
        {
            get
            {
                trayItems.RemoveAll(i => i == null);
                return trayItems.Count;
            }
        }

        /// <summary>ถาดเต็ม (หม้อหยุดต้มรอ)</summary>
        public bool IsTrayFull => useOutputTray && TrayCount >= outputTrayCapacity;

        private Vector3 TrayCenter => GetOutputPosition() + new Vector3(trayOffset.x, trayOffset.y, 0f);

        /// <summary>วางจานที่ต้มเสร็จลงถาด (เรียงแบน ไม่ซ้อนสูง) แล้วเด้งโผล่</summary>
        private void AddToTray(Item dish)
        {
            dish.SetCarriedState(true); // ปิด collider / ฟิสิกส์ / การสลายตัว ระหว่างอยู่บนถาด
            dish.transform.SetParent(null, true);
            trayItems.Add(dish);
            LayoutTray();
            StartCoroutine(TrayPopIn(dish.transform));
        }

        private void LayoutTray()
        {
            trayItems.RemoveAll(i => i == null);
            SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
            int layer = potRenderer != null ? potRenderer.sortingLayerID : 0;
            int baseOrder = (potRenderer != null ? potRenderer.sortingOrder : 0) + 3;
            Vector3 center = TrayCenter;

            for (int i = 0; i < trayItems.Count; i++)
            {
                Item it = trayItems[i];
                Vector2 off = FlatPile.Offset(i, trayColumns, traySpacing);
                it.transform.position = new Vector3(center.x + off.x, center.y + off.y, center.z - 0.05f);
                it.transform.localRotation = Quaternion.identity;
                if (it.SpriteRenderer != null)
                {
                    it.transform.localScale = FlatPile.LocalScaleForWidth(it.transform, it.SpriteRenderer, trayDishWidth);
                    it.SpriteRenderer.sortingLayerID = layer;
                    it.SpriteRenderer.sortingOrder = baseOrder + FlatPile.SortingOffset(i, trayColumns, outputTrayCapacity);
                }
            }
        }

        private IEnumerator TrayPopIn(Transform t)
        {
            if (t == null) yield break;
            Vector3 target = t.localScale;
            yield return Tween.Run(0.18f, k =>
            {
                if (t == null) return;
                float s = k < 0.6f ? Mathf.Lerp(0.3f, 1.2f, k / 0.6f) : Mathf.Lerp(1.2f, 1f, (k - 0.6f) / 0.4f);
                t.localScale = target * s;
            });
            if (t != null) t.localScale = target;
        }

        /// <summary>ผู้เล่นเข้าใกล้ถาด: ดูดจานเข้ากระเป๋าทีละจาน (จานแถวหลังสุดก่อน)</summary>
        private void UpdateTrayCollect()
        {
            if (!useOutputTray || Time.time < nextTrayCollectTime) return;
            if (TrayCount == 0) return;

            Vector2 center = TrayCenter;
            PlayerCarry target = null;
            float best = trayCollectRadius * trayCollectRadius;
            foreach (PlayerCarry pc in PlayerCarry.All)
            {
                if (pc == null || !pc.CanCarry()) continue;
                float d = ((Vector2)pc.transform.position - center).sqrMagnitude;
                if (d <= best) { best = d; target = pc; }
            }
            if (target == null || !target.TryReserveIncoming()) return;

            int last = trayItems.Count - 1;
            Item dish = trayItems[last];
            trayItems.RemoveAt(last);
            nextTrayCollectTime = Time.time + trayCollectInterval;
            StartCoroutine(FlyTrayDishTo(dish, target));
        }

        private IEnumerator FlyTrayDishTo(Item dish, PlayerCarry target)
        {
            Transform t = dish.transform;
            Vector3 start = t.position;
            Vector3 startScale = t.localScale;
            float time = 0f;
            while (time < trayFlyTime && dish != null && target != null && target.isActiveAndEnabled)
            {
                float k = Mathf.Clamp01(time / trayFlyTime);
                float e = Tween.Evaluate(Ease.InQuad, k);
                Vector3 end = target.NextStackWorldPosition;
                Vector3 p = Vector3.LerpUnclamped(start, end, e) + Vector3.up * (0.35f * Mathf.Sin(k * Mathf.PI));
                t.position = new Vector3(p.x, p.y, start.z);
                t.localScale = startScale * Mathf.Lerp(1f, 0.7f, k);
                yield return null;
                time += Time.deltaTime;
            }

            if (dish == null) { if (target != null) target.ReleaseIncoming(); yield break; }
            t.localScale = startScale;

            if (target != null && target.isActiveAndEnabled)
            {
                // PickUpReservedItem คืนช่องที่จองไว้ให้เองทั้งตอนสำเร็จและไม่สำเร็จ
                if (target.PickUpReservedItem(dish, 0.7f))
                {
                    LayoutTray();
                    yield break;
                }
            }
            else if (target != null)
            {
                target.ReleaseIncoming();
            }

            // เก็บไม่ได้ (ผู้เล่นหาย/เต็ม) วางกลับถาด
            dish.SetCarriedState(true);
            trayItems.Add(dish);
            LayoutTray();
        }

        // ========================== ป้ายบนหม้อ (วัตถุดิบ + จำนวน → อาหาร, พื้นป้ายเป็นหลอดความคืบหน้า) ==========================

        private GameObject potUIRoot;
        private GameObject recipeSign;
        private SpriteRenderer recipeRawRenderer;
        private SpriteRenderer recipeDishRenderer;
        private SpriteRenderer signProgressRenderer;
        private TextMesh signCountText;
        private int lastSignCount = int.MinValue;
        private float signPunchStart = -10f;
        private const float SignPunchTime = 0.16f;
        private const float SignProgressInset = 0.07f;

        /// <summary>จำนวนที่อยู่ในหม้อจริง (ไม่นับชิ้นที่ยังลอยไม่ถึงปากหม้อ)</summary>
        private int DisplayedPotCount()
        {
            int count = CookingItemsCount;
            if (!showIngredientsOnPot)
            {
                for (int i = 0; i < ingredientVisuals.Count; i++)
                {
                    if (ingredientVisuals[i].item != null && !ingredientVisuals[i].landed) count--;
                }
            }
            return Mathf.Max(0, count);
        }

        private void UpdatePotUI()
        {
            if (!showRecipeSign)
            {
                if (potUIRoot != null) potUIRoot.SetActive(false);
                return;
            }

            SetupPotUI();
            if (!potUIRoot.activeSelf) potUIRoot.SetActive(true);

            // หน่วยของป้ายเป็นเมตรในโลก ไม่ว่าหม้อจะถูกสเกลเท่าไหร่
            Vector3 lossy = transform.lossyScale;
            potUIRoot.transform.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                1f);
            potUIRoot.transform.position = transform.position + new Vector3(0f, 0f, -0.1f);
            potUIRoot.transform.rotation = Quaternion.identity;

            // ไอคอนสำรอง: วัตถุดิบชิ้นแรกที่ใส่ / ภาพจาก prefab อาหาร
            if (ingredientIcon == null)
            {
                foreach (var v in ingredientVisuals)
                {
                    if (v.item != null && v.item.SpriteRenderer != null && v.item.SpriteRenderer.sprite != null)
                    {
                        ingredientIcon = v.item.SpriteRenderer.sprite;
                        break;
                    }
                }
            }
            if (dishIcon == null && cookedDishPrefab != null)
            {
                SpriteRenderer dsr = cookedDishPrefab.GetComponentInChildren<SpriteRenderer>();
                if (dsr != null)
                {
                    dishIcon = dsr.sprite;
                    // ภาพชั่วคราวที่ย้อมสีใน prefab (เช่น หกเหลี่ยมสีเหลือง) ใช้สีเดียวกันบนป้าย
                    if (recipeDishRenderer != null) recipeDishRenderer.color = dsr.color;
                }
            }

            float iconBox = potSignSize.y * 0.68f;
            WorldUi.FitIcon(recipeRawRenderer, ingredientIcon, iconBox);
            WorldUi.FitIcon(recipeDishRenderer, dishIcon, iconBox);

            // จำนวนในหม้อ (ข้างไอคอนวัตถุดิบ) ว่างเมื่อหม้อว่าง
            int count = DisplayedPotCount();
            if (count != lastSignCount)
            {
                signCountText.text = count > 0 ? count.ToString() : "";
                lastSignCount = count;
            }

            // หลอดความคืบหน้าในป้าย: ยืดจากซ้ายไปขวา ถาดเต็มเป็นสีส้มแดง (หม้อหยุดรอให้มาเก็บ)
            float progress = !isCurrentlyCooking ? 0f : (IsTrayFull ? 1f : CurrentProgress);
            float innerW = potSignSize.x - SignProgressInset * 2f;
            float innerH = potSignSize.y - SignProgressInset * 2f;
            signProgressRenderer.enabled = progress > 0.001f;
            signProgressRenderer.transform.localScale = new Vector3(innerW * progress, innerH, 1f);
            signProgressRenderer.color = IsTrayFull ? trayFullBarColor : signProgressColor;

            // เด้งตอนของตกลงหม้อ
            float k = (Time.unscaledTime - signPunchStart) / SignPunchTime;
            float s = k >= 0f && k < 1f ? Mathf.Lerp(1f, 1.15f, Mathf.Sin(k * Mathf.PI)) : 1f;
            recipeSign.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>ป้ายเด้งตอนของตกลงหม้อ</summary>
        private void PunchCountBadge()
        {
            signPunchStart = Time.unscaledTime;
        }

        private void SetupPotUI()
        {
            if (potUIRoot != null) return;

            SpriteRenderer potRenderer = GetComponent<SpriteRenderer>();
            int layer = potRenderer != null ? potRenderer.sortingLayerID : 0;
            int order = (potRenderer != null ? potRenderer.sortingOrder : 0) + 50;

            potUIRoot = new GameObject("PotUI");
            potUIRoot.transform.SetParent(transform, false);

            const float outline = 0.06f;
            float w = potSignSize.x, h = potSignSize.y;

            // ป้ายเดียว: [วัตถุดิบ 9 ▸ อาหาร]
            recipeSign = new GameObject("RecipeSign");
            recipeSign.transform.SetParent(potUIRoot.transform, false);
            recipeSign.transform.localPosition = new Vector3(recipeSignOffset.x, recipeSignOffset.y, 0f);

            WorldUi.MakeShape(recipeSign.transform, "Outline", WorldUi.RoundRect, recipeSignOutlineColor,
                new Vector2(w + outline * 2f, h + outline * 2f), Vector3.zero, layer, order);
            WorldUi.MakeShape(recipeSign.transform, "Fill", WorldUi.RoundRect, recipeSignColor, potSignSize, Vector3.zero, layer, order + 1);

            // หลอดความคืบหน้า (pivot ซ้าย) อยู่ใต้ไอคอน
            signProgressRenderer = WorldUi.MakeShape(recipeSign.transform, "Progress", WorldUi.PixelLeft, signProgressColor,
                new Vector2(0f, h - SignProgressInset * 2f), new Vector3(-w * 0.5f + SignProgressInset, 0f, 0f), layer, order + 2);
            signProgressRenderer.enabled = false;

            recipeRawRenderer = WorldUi.MakeShape(recipeSign.transform, "Raw", null, Color.white, Vector2.one, new Vector3(-w * 0.32f, 0f, 0f), layer, order + 3);

            var textGo = new GameObject("Count");
            textGo.transform.SetParent(recipeSign.transform, false);
            textGo.transform.localPosition = new Vector3(-w * 0.1f, 0.01f, 0f);
            signCountText = textGo.AddComponent<TextMesh>();
            signCountText.anchor = TextAnchor.MiddleCenter;
            signCountText.alignment = TextAlignment.Center;
            signCountText.fontStyle = FontStyle.Bold;
            signCountText.fontSize = 64;
            signCountText.characterSize = h * 0.11f;
            signCountText.color = countTextColor;
            var mr = textGo.GetComponent<MeshRenderer>();
            mr.sortingLayerID = layer;
            mr.sortingOrder = order + 3;

            float arrow = h * 0.36f;
            WorldUi.MakeShape(recipeSign.transform, "Arrow", WorldUi.Arrow, recipeArrowColor, new Vector2(arrow, arrow), new Vector3(w * 0.08f, 0f, 0f), layer, order + 3);
            recipeDishRenderer = WorldUi.MakeShape(recipeSign.transform, "Dish", null, Color.white, Vector2.one, new Vector3(w * 0.3f, 0f, 0f), layer, order + 3);
        }


        public Vector3 GetStandZonePosition()
        {
            if (standZonePoint != null)
            {
                return standZonePoint.position;
            }
            return transform.position + new Vector3(standZoneOffset.x, standZoneOffset.y, 0f);
        }

        public Vector3 GetStandZoneLocalPosition()
        {
            if (standZonePoint != null)
            {
                return transform.InverseTransformPoint(standZonePoint.position);
            }
            return new Vector3(standZoneOffset.x, standZoneOffset.y, 0f);
        }

        public Vector3 GetOutputPosition()
        {
            if (foodOutputPoint != null)
            {
                return foodOutputPoint.position;
            }
            return transform.position + new Vector3(outputOffset.x, outputOffset.y, 0f);
        }

        private void UpdateVisuals()
        {
            if (cookingEffect != null)
            {
                cookingEffect.SetActive(isUnlocked && isCurrentlyCooking);
            }
        }

        private void CheckProximityPlayer()
        {
            if (!isUnlocked) return;

            if (cachedPlayerCarry == null)
            {
#if UNITY_2023_1_OR_NEWER
                cachedPlayerCarry = Object.FindAnyObjectByType<PlayerCarry>();
#else
                cachedPlayerCarry = FindObjectOfType<PlayerCarry>();
#endif
            }

            if (cachedPlayerCarry != null)
            {
                float distToStand = Vector2.Distance(GetStandZonePosition(), cachedPlayerCarry.transform.position);
                if (distToStand <= standZoneRadius)
                {
                    isPlayerInRange = true;
                }
                else if (distToStand > standZoneRadius * 1.4f)
                {
                    isPlayerInRange = false;
                }
            }
        }

        public void OnZoneTriggerEnter(Collider2D other)
        {
            if (!isUnlocked || other == null) return;
            var playerCarry = other.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null)
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    TryFeedIngredients(playerCarry);
                }
            }
        }

        public void OnZoneTriggerStay(Collider2D other)
        {
            if (!isUnlocked || other == null) return;
            var playerCarry = other.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null)
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    TryFeedIngredients(playerCarry);
                }
            }
        }

        public void OnZoneTriggerExit(Collider2D other)
        {
            if (!isUnlocked || other == null) return;
            var playerCarry = other.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null && cachedPlayerCarry == playerCarry)
            {
                float distToStand = Vector2.Distance(GetStandZonePosition(), playerCarry.transform.position);
                if (distToStand > standZoneRadius)
                {
                    isPlayerInRange = false;
                }
            }
        }

        private Sprite GetRingSprite()
        {
            if (customRingSprite != null) return customRingSprite;

#if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefab/Shop/FloorStandRing.png");
            if (loaded != null)
            {
                customRingSprite = loaded;
                return loaded;
            }
#endif
            return GetOrCreateRingSprite();
        }

        /// <summary>
        /// Sprite วงแหวนที่ใช้ร่วมกับเครื่องอื่น (เช่น เครื่องผสมเนื้อ) ให้หน้าตาเหมือนวงของหม้อ
        /// </summary>
        public static Sprite GetSharedRingSprite()
        {
#if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefab/Shop/FloorStandRing.png");
            if (loaded != null) return loaded;
#endif
            return GetOrCreateRingSprite();
        }

        private static Sprite GetOrCreateRingSprite()
        {
            if (generatedRingSprite != null) return generatedRingSprite;

            int size = 256;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float outerR = (size * 0.5f) - 6f;
            float thickness = 16f;
            float innerR = outerR - thickness;

            Color[] colors = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    float alpha = 0f;

                    if (dist >= innerR && dist <= outerR)
                    {
                        alpha = 1f;
                    }
                    else if (dist > outerR && dist <= outerR + 2.5f)
                    {
                        alpha = Mathf.Clamp01(1f - (dist - outerR) / 2.5f);
                    }
                    else if (dist < innerR && dist >= innerR - 2.5f)
                    {
                        alpha = Mathf.Clamp01((dist - (innerR - 2.5f)) / 2.5f);
                    }
                    else if (dist < innerR - 2.5f)
                    {
                        float t = dist / (innerR - 2.5f);
                        alpha = Mathf.Lerp(0.04f, 0.12f, t);
                    }

                    colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            tex.SetPixels(colors);
            tex.Apply();

            generatedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            return generatedRingSprite;
        }

        private void OnDrawGizmos()
        {
            bool needPurchase = !isUnlocked || (!Application.isPlaying && (requiresPurchase || (autoRequirePurchaseForPork && IsPigPot())));

            if (needPurchase && buyAtPotRing)
            {
                // วาดตำแหน่งจุดยืนซื้อหม้อ (สีเหลืองทอง / ส้ม)
                Vector3 buyPos = GetPurchaseZonePosition();
                Gizmos.color = new Color(purchaseRingColor.r, purchaseRingColor.g, purchaseRingColor.b, 0.3f);
                Gizmos.DrawSphere(buyPos, purchaseZoneRadius);
                Gizmos.color = purchaseRingColor;
                Gizmos.DrawWireSphere(buyPos, purchaseZoneRadius);
                Gizmos.DrawLine(transform.position, buyPos);
            }
            else
            {
                // วาดตำแหน่งโซนและวงแหวนที่ผู้เล่นต้องเข้ามายืน (สีฟ้าใส)
                Vector3 zonePos = GetStandZonePosition();
                Gizmos.color = zoneGizmoColor;
                Gizmos.DrawSphere(zonePos, standZoneRadius);
                Gizmos.color = new Color(zoneGizmoColor.r, zoneGizmoColor.g, zoneGizmoColor.b, 0.9f);
                Gizmos.DrawWireSphere(zonePos, standZoneRadius);
                Gizmos.DrawLine(transform.position, zonePos);

                // วาดจุดที่อาหารจะหล่นออกมา (สีเหลืองทอง)
                Vector3 outPos = GetOutputPosition();
                Gizmos.color = new Color(outputRingColor.r, outputRingColor.g, outputRingColor.b, 0.35f);
                Gizmos.DrawSphere(outPos, outputZoneRadius);
                Gizmos.color = outputRingColor;
                Gizmos.DrawWireSphere(outPos, outputZoneRadius);
                Gizmos.DrawLine(transform.position, outPos);
            }
        }
    }
}
