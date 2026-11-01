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
        [Header("Recipe Settings (ตั้งค่าสูตรอาหาร)")]
        [Tooltip("ชื่อของหม้อ (เช่น หม้อต้มไก่ หรือ หม้อต้มหมู)")]
        [SerializeField] private string potName = "Chicken Boiling Pot";

        [Tooltip("วัตถุดิบที่ต้องใช้ใส่ลงหม้อ")]
        [SerializeField] private ItemType requiredIngredient = ItemType.ChickenMeat;

        [Tooltip("Prefab อาหารปรุงสุกที่จะได้รับเมื่อต้มเสร็จ")]
        [SerializeField] private GameObject cookedDishPrefab;

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
        public UnityEvent onIngredientAdded;
        public UnityEvent onCookingStarted;
        public UnityEvent onCookingFinished;
        public UnityEvent onFoodOutputSpawned;

        // ระบบคิวการทำอาหาร (ต้มทีละ 1 ชิ้น 1 คิว เมื่อสุกแล้วคิวถัดไปจะเริ่มต้มต่อทันที)
        private int queuedIngredientCount = 0;
        private float currentCookTimer = 0f;
        private bool isCurrentlyCooking = false;

        private bool isPlayerInRange;
        private PlayerCarry cachedPlayerCarry;

        // Visual Objects
        private GameObject standZoneObject;
        private SpriteRenderer floorRingRenderer;

        private GameObject outputRingObject;
        private SpriteRenderer outputRingRenderer;
        private float outputRingSpawnPulseTimer = 0f;

        private static Sprite generatedRingSprite;

        public string PotName => potName;
        public int QueuedCount => queuedIngredientCount;
        public int CookingItemsCount => (isCurrentlyCooking ? 1 : 0) + queuedIngredientCount;
        public int MaxCapacity => maxCookingCapacity;
        public bool IsCooking => isCurrentlyCooking;
        public float CurrentProgress => isCurrentlyCooking && cookingDuration > 0f ? Mathf.Clamp01(1f - (currentCookTimer / cookingDuration)) : 0f;
        public float CurrentRemainingTime => isCurrentlyCooking ? Mathf.Max(0f, currentCookTimer) : 0f;
        public float TotalRemainingTime => (isCurrentlyCooking ? currentCookTimer : 0f) + (queuedIngredientCount * cookingDuration);

        private void Awake()
        {
            SetupPotSolidCollider();
            SetupStandZoneAndRing();
            SetupOutputRing();
            UpdateVisuals();
        }

        private void OnEnable()
        {
            SetupPotSolidCollider();
            SetupStandZoneAndRing();
            SetupOutputRing();
            UpdateVisuals();
        }

        private void OnValidate()
        {
            if (standZoneRadius < 0.1f) standZoneRadius = 0.1f;
            if (outputZoneRadius < 0.1f) outputZoneRadius = 0.1f;
            SetupStandZoneAndRing();
            SetupOutputRing();
        }

        /// <summary>
        /// ทำให้ตัวหม้อเป็นของแข็ง (Solid) เดินชนแล้วไม่ทะลุ
        /// </summary>
        private void SetupPotSolidCollider()
        {
            Collider2D mainCol = GetComponent<Collider2D>();
            if (mainCol != null)
            {
                mainCol.isTrigger = false;
            }
        }

        /// <summary>
        /// สร้างโซนและวงแหวนสีขาวบนพื้นสำหรับให้ผู้เล่นยืน (มองเห็นได้ทั้งหน้า Game และ Scene)
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

            // จัดตำแหน่งให้อยู่ตรงจุดยืน GetStandZonePosition() ใน World Space
            standZoneObject.transform.position = GetStandZonePosition();
            standZoneObject.transform.localRotation = Quaternion.identity;

            // ชดเชย Scale ของ Parent เพื่อให้ขนาดของวงกลมใน World Space เท่ากับ standZoneRadius * 2 เป๊ะๆ
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = standZoneRadius * 2f;
            standZoneObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            // 1. เพิ่ม Trigger Collider สำหรับตรวจจับการเหยียบของผู้เล่น
            CircleCollider2D circleCol = standZoneObject.GetComponent<CircleCollider2D>();
            if (circleCol == null)
            {
                circleCol = standZoneObject.AddComponent<CircleCollider2D>();
            }
            circleCol.isTrigger = true;
            circleCol.radius = 0.5f; // เมื่อ LocalScale = targetDiameter รัศมีจะเป็น standZoneRadius พอดี
            circleCol.offset = Vector2.zero;

            // 2. ใส่ Proxy เชื่อมโยง Event กลับมาที่ CookingPot
            PotTriggerZoneProxy proxy = standZoneObject.GetComponent<PotTriggerZoneProxy>();
            if (proxy == null)
            {
                proxy = standZoneObject.AddComponent<PotTriggerZoneProxy>();
            }
            proxy.pot = this;

            // 3. แสดงวงแหวนสีขาวบนพื้นให้เห็นในหน้า Game
            floorRingRenderer = standZoneObject.GetComponent<SpriteRenderer>();
            if (showFloorRing)
            {
                if (floorRingRenderer == null)
                {
                    floorRingRenderer = standZoneObject.AddComponent<SpriteRenderer>();
                }
                floorRingRenderer.enabled = true;

                Sprite ringSpriteToUse = GetRingSprite();
                floorRingRenderer.sprite = ringSpriteToUse;
                floorRingRenderer.color = isPlayerInRange ? ringActiveColor : ringColor;

                // สืบทอด Material และ Sorting Layer เดียวกับ Sprite ของหม้อ เพื่อให้รองรับ URP 2D Lighting สมบูรณ์
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
        /// สร้างวงแหวนสีเหลืองตรงจุดที่อาหารสุกจะหล่นออกมา (มองเห็นได้ทั้งหน้า Game และ Scene)
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
            // ในโหมดแก้ไข (Edit Mode) อัปเดตตำแหน่งและสเกลให้ตรงกับการขยับใน Scene เสมอ
            if (!Application.isPlaying)
            {
                UpdateEditorVisuals();
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

        private void UpdateEditorVisuals()
        {
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

            // เปลี่ยนสีวงแหวนนุ่มนวลเมื่อผู้เล่นเข้ามายืน
            Color targetColor = isPlayerInRange ? ringActiveColor : ringColor;
            floorRingRenderer.color = Color.Lerp(floorRingRenderer.color, targetColor, Time.deltaTime * 10f);

            // ขยาย-หดตัวเบาๆ ให้น่าดึงดูด
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

            // เอฟเฟกต์ดีดเด้งชั่วขณะตอนมีอาหารต้มเสร็จและเด้งออกมา
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

            // สีสว่างขึ้นเล็กน้อยตอนมีอาหารเด้งออกมา
            Color targetCol = popBounce > 0.01f ? Color.Lerp(outputRingColor, Color.white, 0.4f) : outputRingColor;
            outputRingRenderer.color = Color.Lerp(outputRingRenderer.color, targetCol, Time.deltaTime * 12f);
        }

        /// <summary>
        /// อัปเดตเวลาการต้มทีละ 1 ชิ้น (ระบบคิว 1 อัน 1 คิว เมื่อสุกแล้วคิวต่อไปจะเริ่มต้มต่อทันที)
        /// </summary>
        private void UpdateCookingQueue()
        {
            if (!isCurrentlyCooking) return;

            currentCookTimer -= Time.deltaTime;

            // เมื่ออาหารชิ้นปัจจุบันต้มเสร็จสมบูรณ์
            if (currentCookTimer <= 0f)
            {
                SpawnCookedDishAtOutput();
                onCookingFinished?.Invoke();

                if (foodDoneSound != null)
                {
                    AudioSource.PlayClipAtPoint(foodDoneSound, transform.position);
                }

                // ตรวจสอบว่ายังมีคิวอาหารรออยู่หรือไม่
                if (queuedIngredientCount > 0)
                {
                    queuedIngredientCount--;
                    currentCookTimer = cookingDuration; // เริ่มต้มชิ้นถัดไปในคิวทันที
                    isCurrentlyCooking = true;
                }
                else
                {
                    isCurrentlyCooking = false;
                    currentCookTimer = 0f;
                }

                UpdateVisuals();
            }
        }

        /// <summary>
        /// ตรวจสอบและหยิบวัตถุดิบทั้งหมดที่ผู้เล่นมีเข้าคิวในหม้อต้ม (ใส่ได้เรื่อยๆ ไม่ต้องรอทำเสร็จ)
        /// </summary>
        public int TryFeedIngredients(PlayerCarry playerCarry)
        {
            if (playerCarry == null) return 0;

            int fedCount = 0;

            // โยนวัตถุดิบเข้าคิวในหม้อได้เรื่อยๆ จนกว่าคิวจะเต็ม หรือผู้เล่นไม่มีวัตถุดิบชนิดนั้นแล้ว
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

        /// <summary>
        /// เพิ่มวัตถุดิบเข้าคิว ถ้าหม้อว่างจะเริ่มต้มทันที ถ้ากำลังต้มอยู่จะเพิ่มเข้าคิวถัดไป
        /// </summary>
        private void AddIngredientToQueue()
        {
            bool wasIdle = !isCurrentlyCooking;

            if (wasIdle)
            {
                // ถ้าหม้อว่าง เริ่มต้มชิ้นนี้ทันที
                isCurrentlyCooking = true;
                currentCookTimer = cookingDuration;
                onCookingStarted?.Invoke();
            }
            else
            {
                // ถ้าหม้อกำลังต้มอยู่ เพิ่มเข้าคิวรอ
                queuedIngredientCount++;
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

            // กระตุ้นแอนิเมชันขยายวงแหวนสีเหลืองเมื่ออาหารเสร็จ
            outputRingSpawnPulseTimer = 0.35f;

            // มีการกระจายตำแหน่งเล็กน้อยเพื่อให้จานไม่ทับกันสนิทเมื่อออกมา
            Vector2 randomSpread = Random.insideUnitCircle * 0.15f;
            Vector3 finalSpawnPos = spawnPos + new Vector3(randomSpread.x, randomSpread.y, 0f);

            GameObject dishObj = Instantiate(cookedDishPrefab, finalSpawnPos, Quaternion.identity);

            // แรงดีดจานอาหารให้พุ่งออกมาจากหม้อ
            if (foodPopForce > 0f && dishObj.TryGetComponent<Rigidbody2D>(out var dishRb))
            {
                Vector2 popDir = ((Vector2)(finalSpawnPos - transform.position)).normalized;
                if (popDir == Vector2.zero) popDir = Vector2.down;
                dishRb.AddForce(popDir * foodPopForce, ForceMode2D.Impulse);
            }

            onFoodOutputSpawned?.Invoke();
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
            // เปิดเอฟเฟกต์ควันต้มถ้ากำลังมีอาหารต้มอยู่ในหม้อ
            if (cookingEffect != null)
            {
                cookingEffect.SetActive(isCurrentlyCooking);
            }
        }

        private void CheckProximityPlayer()
        {
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
            if (other == null) return;
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
            if (other == null) return;
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
            if (other == null) return;
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
        /// สร้าง Sprite วงแหวนสีขาวความละเอียดสูงแบบเนียนตา (Anti-aliased) อัตโนมัติในหน่วยความจำ
        /// </summary>
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

    /// <summary>
    /// พร็อกซีส่งต่อ Event จาก PlayerStandZone ไปให้ CookingPot
    /// </summary>
    public class PotTriggerZoneProxy : MonoBehaviour
    {
        public CookingPot pot;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerEnter(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerStay(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerExit(other);
        }
    }
}
