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

        [Tooltip("ราคาที่ต้องจ่ายเพื่อซื้อหม้อ (เหรียญ)")]
        [SerializeField] private int purchasePrice = 50;

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

        [Header("Visual Feedback & Indicators")]
        [Tooltip("GameObject แสดงเอฟเฟกต์ควัน/ฟองน้ำเดือดขณะต้ม")]
        [SerializeField] private GameObject cookingEffect;

        [Tooltip("เสียงตอนโยนวัตถุดิบลงหม้อ (ถ้ามี)")]
        [SerializeField] private AudioClip ingredientDropSound;

        [Tooltip("เสียงตอนอาหารต้มเสร็จแล้วเด้งออกมา (ถ้ามี)")]
        [SerializeField] private AudioClip foodDoneSound;

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

        public bool IsUnlocked => isUnlocked;
        public bool RequiresPurchase => requiresPurchase;
        public int PurchasePrice => purchasePrice;
        public string PotName => potName;
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

        private void OnEnable()
        {
            if (!AllPots.Contains(this)) AllPots.Add(this);

            InitPotComponents();

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

            if (Application.isPlaying)
            {
                if (isUnlocked)
                {
                    SetupStandZoneAndRing();
                    SetupOutputRing();
                }
                else
                {
                    SetupPurchaseZone();
                }
            }
        }

        public bool IsPigPot()
        {
            string pName = potName != null ? potName.ToLower() : "";
            string gName = gameObject != null ? gameObject.name.ToLower() : "";
            return requiredIngredient == ItemType.PigMeat || pName.Contains("pig") || pName.Contains("pork") || gName.Contains("pig") || gName.Contains("pork");
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

            // 4. เปิดจุดยืนซื้อหม้อ
            if (Application.isPlaying)
            {
                SetupPurchaseZone();
                if (purchaseZoneObject != null) purchaseZoneObject.SetActive(true);
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
                UpdatePurchaseZoneLogic();
                return;
            }

            // อัปเดตเวลาการต้มอาหารทีละ 1 ชิ้นตามคิว
            UpdateCookingQueue();

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
                if (MoneyManager.Instance.SpendMoney(purchasePrice))
                {
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
            InitPotComponents();

            bool needPurchase = requiresPurchase || (autoRequirePurchaseForPork && IsPigPot());

            // แสดงตัวอย่างความโปร่งใสใน Scene View
            if (potSpriteRenderer != null)
            {
                Color c = defaultPotColor;
                c.a = needPurchase ? lockedAlpha : 1f;
                potSpriteRenderer.color = c;
            }

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

            currentCookTimer -= Time.deltaTime;

            if (currentCookTimer <= 0f)
            {
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
                    Destroy(meat.gameObject);
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

            if (foodPopForce > 0f && dishObj.TryGetComponent<Rigidbody2D>(out var dishRb))
            {
                Vector2 popDir = ((Vector2)(finalSpawnPos - transform.position)).normalized;
                if (popDir == Vector2.zero) popDir = Vector2.down;
                dishRb.AddForce(popDir * foodPopForce, ForceMode2D.Impulse);
            }

            onFoodOutputSpawned?.Invoke();
            Debug.Log("[CookingPot] ปรุงเสร็จแล้ว! อาหารพิเศษ Special Golden Dish (100 coins) กระเด็นออกมาจากหม้อ!");
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

            if (needPurchase)
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
