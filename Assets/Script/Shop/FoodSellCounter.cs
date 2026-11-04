using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.Serialization;

namespace GI351.KitchenGame
{
    [ExecuteAlways]
    [RequireComponent(typeof(Collider2D))]
    public class FoodSellCounter : MonoBehaviour
    {
        [Header("Food Display Slots (จุดโชว์ที่วางอาหารบนโต๊ะ)")]
        [Tooltip("ตำแหน่งวางอาหาร/จานบนโต๊ะแต่ละช่อง (ลาก Transform มาใส่ แต่ละจุดคือจุดโชว์อาหารบนโต๊ะ)")]
        [SerializeField] private Transform[] plateSlots;

        [Tooltip("จุดกึ่งกลางสำหรับวางอาหารบนโต๊ะ (ถ้าไม่ได้กำหนด plateSlots จะวางเรียงรอบจุดนี้อัตโนมัติ)")]
        [SerializeField] private Transform foodDisplayPoint;

        [Tooltip("ตำแหน่งเยื้องสำหรับวางอาหารบนโต๊ะ (ถ้าไม่ได้กำหนด foodDisplayPoint)")]
        [SerializeField] private Vector2 foodDisplayOffset = new Vector2(0f, 0.2f);

        [Tooltip("ระยะห่างระหว่างอาหารแต่ละชิ้นที่วางเรียงบนโต๊ะ (เมตร) กรณีจัดเรียงอัตโนมัติ")]
        [SerializeField] private float foodDisplaySpacing = 0.45f;

        [Tooltip("จำนวนอาหารสูงสุดที่วางรอบนโต๊ะนี้ได้ (ถ้ามี plateSlots จะใช้ตามจำนวนช่อง)")]
        [FormerlySerializedAs("maxFoodOnCounter")]
        [SerializeField] private int maxPlates = 6;

        [Tooltip("ลำดับการวาด (Sorting Order) ของอาหารขณะวางรอบนโต๊ะ เพื่อให้อยู่เหนือหน้าโต๊ะชัดเจน")]
        [SerializeField] private int foodDisplaySortingOrder = 4;

        [Header("Food Deposit Zone (วงวางอาหาร - สีขาว/ฟ้า)")]
        [Tooltip("จุดที่ให้ผู้เล่นยืนวางอาหาร (ถ้าว่างจะใช้ค่า Deposit Zone Offset)")]
        [SerializeField] private Transform foodDepositPoint;

        [Tooltip("ระยะเยื้องจากโต๊ะไปยังจุดวางอาหาร (เช่น (-1.0, -0.8) คืออยู่ด้านหน้าฝั่งซ้ายของโต๊ะ)")]
        [SerializeField] private Vector2 depositZoneOffset = new Vector2(-1.0f, -0.8f);

        [Tooltip("รัศมีของวงกลมวางอาหาร (เมตร)")]
        [SerializeField] private float depositZoneRadius = 0.65f;

        [Tooltip("ระยะที่เดินเข้ามาใกล้โต๊ะแล้ววางอาหารได้ทันที (เมตร)")]
        [SerializeField] private float tableProximityRadius = 2.5f;

        [Tooltip("ถ้าติ๊กถูก ต้องกดปุ่ม E เพื่อวางอาหาร (ถ้าไม่ติ๊ก แค่เดินเข้ามาเหยียบวงหรือเดินชนโต๊ะจะวางอาหารลงโต๊ะทันที)")]
        [SerializeField] private bool requireKeyPress = false;

        [Tooltip("เปิดแสดงวงแหวนวางอาหารบนพื้นในหน้า Game")]
        [SerializeField] private bool showDepositRing = true;

        [Tooltip("สีของวงแหวนวางอาหารตอนปกติ (สีขาว)")]
        [SerializeField] private Color depositRingColor = new Color(1f, 1f, 1f, 0.9f);

        [Tooltip("สีของวงแหวนเมื่อผู้เล่นเข้ามายืนในโซน")]
        [SerializeField] private Color depositRingActiveColor = new Color(0.3f, 1f, 0.6f, 0.95f);

        [Tooltip("ลำดับการวาด (Sorting Order) ของวงแหวนบนพื้นทั้งสองวง")]
        [SerializeField] private int ringSortingOrder = 1;

        [Header("Money Output Zone (วงที่เงินออกมา - สีเหลืองทอง)")]
        [Tooltip("จุดที่เงินจะเด้งออกมา (ลาก Transform มาใส่ หรือปล่อยว่างเพื่อใช้ระยะเยื้องด้านล่าง)")]
        [SerializeField] private Transform moneyOutputPoint;

        [Tooltip("ตำแหน่งเยื้องจากโต๊ะที่เงินจะเด้งออกมา (เช่น (1.0, -0.8) อยู่ด้านหน้าฝั่งขวาของโต๊ะ)")]
        [SerializeField] private Vector2 moneyOutputOffset = new Vector2(1.0f, -0.8f);

        [Tooltip("รัศมีของวงกลมจุดเงินออก (เมตร)")]
        [SerializeField] private float moneyZoneRadius = 0.5f;

        [Tooltip("เปิดแสดงวงแหวนสีเหลืองทองจุดเงินออกในหน้า Game")]
        [SerializeField] private bool showMoneyRing = true;

        [Tooltip("สีของวงแหวนจุดเงินออก (สีเหลืองทอง)")]
        [SerializeField] private Color moneyRingColor = new Color(1f, 0.85f, 0.2f, 0.9f);

        [Tooltip("Prefab เหรียญเงินที่จะเด้งออกมาเมื่อลูกค้าซื้ออาหาร (ถ้าปล่อยว่างจะเพิ่มเงินเข้า MoneyManager โดยตรง)")]
        [SerializeField] private GameObject coinPrefab;

        [Tooltip("แรงดีดเหรียญเงินออกมาจากจุดขาย")]
        [SerializeField] private float moneyPopForce = 2f;

        [Tooltip("ให้เหรียญหล่นวางบนโต๊ะ (สุ่มตำแหน่งบนโต๊ะ) แทนการเด้งออกที่วงเงินออก")]
        [SerializeField] private bool dropMoneyOnCounter = true;

        [Tooltip("ระยะเว้นจากขอบโต๊ะตอนสุ่มตำแหน่งเหรียญ (เมตร)")]
        [SerializeField] private float counterMoneyInset = 0.3f;

        [Header("Audio & Effects")]
        [Tooltip("เสียงตอนลูกค้าซื้ออาหารสำเร็จและเงินเด้งออกมา")]
        [SerializeField] private AudioClip sellSound;

        [Tooltip("เสียงตอนวางอาหารลงโต๊ะ")]
        [SerializeField] private AudioClip depositSound;

        [Header("Customer Settings & Queue (ระบบต่อแถวเข้าคิวลูกค้า)")]
        [Tooltip("จุดที่ลูกค้าหัวแถวจะเดินมารอรับอาหาร")]
        [SerializeField] private Transform customerWaitPoint;

        [Tooltip("ทิศทางการต่อแถวของลูกค้าออกจากจุดรอหน้าโต๊ะ (เช่น (0, -1) คือต่อแถวถอยหลังลงมา)")]
        [SerializeField] private Vector2 queueDirection = new Vector2(0f, -1f);

        [Tooltip("ระยะห่างระหว่างลูกค้าแต่ละคนในแถว (เมตร)")]
        [SerializeField] private float queueSpacing = 0.9f;

        [Tooltip("จำนวนลูกค้าสูงสุดที่สามารถยืนต่อคิวได้")]
        [SerializeField] private int maxQueueCapacity = 5;

        [Tooltip("จุดต่อคิวแบบกำหนดเอง (ถ้าต้องการระบุจุดยืนของแต่ละคิวเป๊ะๆ ลาก Transform มาใส่ได้ ถ้าปล่อยว่างจะคำนวณตามระยะห่างอัตโนมัติ)")]
        [SerializeField] private Transform[] customQueuePoints;

        [Header("Events")]
        public UnityEvent<int> onDishesCountChanged;
        public UnityEvent<Item> onDishSold;
        public UnityEvent<int> onMoneyEarned;

        // Visual Rings
        private GameObject depositRingObject;
        private SpriteRenderer depositRingRenderer;

        private GameObject moneyRingObject;
        private SpriteRenderer moneyRingRenderer;
        private float moneyRingPulseTimer = 0f;

        private static Sprite cachedRingSprite;

        // รายการอาหารที่วางรอบนโต๊ะ
        private readonly List<Item> dishesOnCounter = new List<Item>();
        private readonly Dictionary<Item, Vector3> foodWorldScales = new Dictionary<Item, Vector3>();
        private bool isPlayerInRange = false;
        private PlayerCarry cachedPlayerCarry;
        private Transform playerTransform;

        public Transform[] PlateSlots => plateSlots;
        public int DishesCount => dishesOnCounter.Count;
        public int MaxPlates => (plateSlots != null && plateSlots.Length > 0) ? plateSlots.Length : maxPlates;
        public int MaxFoodCount => MaxPlates;
        public Transform CustomerWaitPoint => customerWaitPoint != null ? customerWaitPoint : transform;

        // คิวของลูกค้าที่กำลังยืนรอซื้ออาหาร
        private readonly List<CustomerAI> customerQueue = new List<CustomerAI>();

        public int CurrentQueueCount
        {
            get
            {
                customerQueue.RemoveAll(c => c == null);
                return customerQueue.Count;
            }
        }

        public bool IsQueueFull => CurrentQueueCount >= maxQueueCapacity;
        public int MaxQueueCapacity => maxQueueCapacity;

        /// <summary>
        /// ให้ลูกค้าลงทะเบียนเข้าแถวรอคิว
        /// </summary>
        public bool TryJoinQueue(CustomerAI customer)
        {
            if (customer == null) return false;
            customerQueue.RemoveAll(c => c == null);

            if (customerQueue.Contains(customer)) return true;

            if (customerQueue.Count >= maxQueueCapacity) return false;

            customerQueue.Add(customer);
            return true;
        }

        /// <summary>
        /// นำลูกค้าออกจากแถว (เมื่อซื้อเสร็จ หรือหมดความอดทน)
        /// </summary>
        public void LeaveQueue(CustomerAI customer)
        {
            if (customer == null) return;
            customerQueue.Remove(customer);
            customerQueue.RemoveAll(c => c == null);
        }

        /// <summary>
        /// หาตำแหน่งในคิวของลูกค้า (Index 0 คือหัวแถว)
        /// </summary>
        public int GetQueueIndex(CustomerAI customer)
        {
            customerQueue.RemoveAll(c => c == null);
            return customerQueue.IndexOf(customer);
        }

        /// <summary>
        /// คำนวณตำแหน่งใน World Space ที่ลูกค้าคนนี้ควรยืนต่อแถว
        /// </summary>
        public Vector3 GetQueuePosition(CustomerAI customer)
        {
            int index = GetQueueIndex(customer);
            if (index < 0)
            {
                if (TryJoinQueue(customer))
                {
                    index = GetQueueIndex(customer);
                }
                else
                {
                    index = customerQueue.Count;
                }
            }

            // ถ้ากำหนด customQueuePoints ไว้ ให้ใช้ตาม Transform ที่ระบุ
            if (customQueuePoints != null && index < customQueuePoints.Length && customQueuePoints[index] != null)
            {
                return customQueuePoints[index].position;
            }

            // คำนวณตามทิศทางและระยะห่างอัตโนมัติ
            Vector3 basePoint = CustomerWaitPoint.position;
            Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
            return basePoint + (Vector3)(dir * (index * queueSpacing));
        }

        /// <summary>
        /// เช็คว่าลูกค้าคนนี้เป็นหัวแถว (ลำดับที่ 0) หรือไม่
        /// </summary>
        public bool IsHeadOfQueue(CustomerAI customer)
        {
            customerQueue.RemoveAll(c => c == null);
            return customerQueue.Count > 0 && customerQueue[0] == customer;
        }

        private void Awake()
        {
            SetupCounterSolidCollider();
            SetupDepositZoneAndRing();
            SetupMoneyOutputRing();
            TryAutoAssignCoinPrefab();
        }

        private void OnEnable()
        {
            SetupCounterSolidCollider();
            SetupDepositZoneAndRing();
            SetupMoneyOutputRing();
            TryAutoAssignCoinPrefab();
        }

        private void OnValidate()
        {
            if (depositZoneRadius < 0.1f) depositZoneRadius = 0.1f;
            if (moneyZoneRadius < 0.1f) moneyZoneRadius = 0.1f;
            if (Application.isPlaying)
            {
                SetupDepositZoneAndRing();
                SetupMoneyOutputRing();
            }
            TryAutoAssignCoinPrefab();
        }

        private void TryAutoAssignCoinPrefab()
        {
#if UNITY_EDITOR
            if (coinPrefab == null)
            {
                coinPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Shop/Coin.prefab");
            }
#endif
        }

        /// <summary>
        /// ทำให้ตัวเคาน์เตอร์ขายอาหารเป็นของแข็ง (Solid) เดินชนแล้วไม่ทะลุ
        /// </summary>
        private void SetupCounterSolidCollider()
        {
            Collider2D mainCol = GetComponent<Collider2D>();
            if (mainCol != null)
            {
                mainCol.isTrigger = false;
            }

            if (customerWaitPoint == null)
            {
                Transform existingWait = transform.Find("CustomerWaitPoint");
                if (existingWait != null)
                {
                    customerWaitPoint = existingWait;
                }
                else
                {
                    var point = new GameObject("CustomerWaitPoint");
                    point.transform.SetParent(transform);
                    point.transform.localPosition = new Vector3(0f, -1f, 0f);
                    customerWaitPoint = point.transform;
                }
            }
        }

        /// <summary>
        /// สร้างโซนและวงแหวนบนพื้นสำหรับวางอาหาร (มองเห็นได้ทั้งหน้า Game และ Scene)
        /// </summary>
        public void SetupDepositZoneAndRing()
        {
            Transform existingZone = transform.Find("FoodDepositZone");
            if (existingZone != null)
            {
                depositRingObject = existingZone.gameObject;
            }
            else
            {
                depositRingObject = new GameObject("FoodDepositZone");
                depositRingObject.transform.SetParent(transform);
            }

            depositRingObject.transform.position = GetDepositPosition();
            depositRingObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = depositZoneRadius * 2f;
            depositRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            // 1. Trigger Collider ตรวจจับการเดินเข้ามาของผู้เล่น
            CircleCollider2D circleCol = depositRingObject.GetComponent<CircleCollider2D>();
            if (circleCol == null)
            {
                circleCol = depositRingObject.AddComponent<CircleCollider2D>();
            }
            circleCol.isTrigger = true;
            circleCol.radius = 0.5f;
            circleCol.offset = Vector2.zero;

            // 2. Proxy รับ Trigger Event (เฉพาะขณะเล่นเกม เพื่อไม่ให้ AddComponent ลง Prefab ในโหมด Editor)
            if (Application.isPlaying)
            {
                SellCounterZoneProxy proxy = depositRingObject.GetComponent<SellCounterZoneProxy>();
                if (proxy == null)
                {
                    proxy = depositRingObject.AddComponent<SellCounterZoneProxy>();
                }
                proxy.counter = this;
            }

            // 3. Sprite Renderer แสดงวงแหวนสีขาว
            depositRingRenderer = depositRingObject.GetComponent<SpriteRenderer>();
            if (showDepositRing)
            {
                if (depositRingRenderer == null)
                {
                    depositRingRenderer = depositRingObject.AddComponent<SpriteRenderer>();
                }
                depositRingRenderer.enabled = true;
                depositRingRenderer.sprite = GetRingSprite();
                depositRingRenderer.color = isPlayerInRange ? depositRingActiveColor : depositRingColor;

                SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
                if (mainRenderer != null)
                {
                    if (mainRenderer.sharedMaterial != null)
                    {
                        depositRingRenderer.sharedMaterial = mainRenderer.sharedMaterial;
                    }
                    depositRingRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                }
                depositRingRenderer.sortingOrder = ringSortingOrder;
            }
            else if (depositRingRenderer != null)
            {
                depositRingRenderer.enabled = false;
            }
        }

        /// <summary>
        /// สร้างวงแหวนสีเหลืองทองตรงจุดที่เงิน/เหรียญจะเด้งออกมา (มองเห็นได้ทั้งหน้า Game และ Scene)
        /// </summary>
        public void SetupMoneyOutputRing()
        {
            Transform existingRing = transform.Find("MoneyOutputRing");
            if (existingRing != null)
            {
                moneyRingObject = existingRing.gameObject;
            }
            else
            {
                moneyRingObject = new GameObject("MoneyOutputRing");
                moneyRingObject.transform.SetParent(transform);
            }

            moneyRingObject.transform.position = GetMoneyOutputPosition();
            moneyRingObject.transform.localRotation = Quaternion.identity;

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;
            float targetDiameter = moneyZoneRadius * 2f;
            moneyRingObject.transform.localScale = new Vector3(targetDiameter / pX, targetDiameter / pY, 1f);

            moneyRingRenderer = moneyRingObject.GetComponent<SpriteRenderer>();
            if (showMoneyRing)
            {
                if (moneyRingRenderer == null)
                {
                    moneyRingRenderer = moneyRingObject.AddComponent<SpriteRenderer>();
                }
                moneyRingRenderer.enabled = true;
                moneyRingRenderer.sprite = GetRingSprite();
                moneyRingRenderer.color = moneyRingColor;

                SpriteRenderer mainRenderer = GetComponent<SpriteRenderer>();
                if (mainRenderer != null)
                {
                    if (mainRenderer.sharedMaterial != null)
                    {
                        moneyRingRenderer.sharedMaterial = mainRenderer.sharedMaterial;
                    }
                    moneyRingRenderer.sortingLayerID = mainRenderer.sortingLayerID;
                }
                moneyRingRenderer.sortingOrder = ringSortingOrder;
            }
            else if (moneyRingRenderer != null)
            {
                moneyRingRenderer.enabled = false;
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                UpdateEditorVisuals();
                return;
            }

            // แอนิเมชันของวงแหวน
            UpdateVisualAnimations();

            // ตรวจหา Player ในระยะใกล้เคียงโต๊ะ
            CheckProximityPlayer();

            // ตรวจจับการส่งอาหารอัตโนมัติขณะผู้เล่นยืนอยู่ในโซนหรือใกล้โต๊ะ
            if (isPlayerInRange && cachedPlayerCarry != null)
            {
                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    DepositFoodFromPlayer(cachedPlayerCarry);
                }
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
                if (cachedPlayerCarry != null)
                {
                    playerTransform = cachedPlayerCarry.transform;
                }
                else
                {
                    var p = GameObject.FindGameObjectWithTag("Player");
                    if (p != null)
                    {
                        playerTransform = p.transform;
                        cachedPlayerCarry = p.GetComponentInParent<PlayerCarry>();
                    }
                }
            }

            if (cachedPlayerCarry != null && playerTransform == null)
            {
                playerTransform = cachedPlayerCarry.transform;
            }

            if (playerTransform != null)
            {
                float distToDeposit = Vector2.Distance(GetDepositPosition(), playerTransform.position);
                float distToTable = Vector2.Distance(transform.position, playerTransform.position);

                if (distToDeposit <= depositZoneRadius || distToTable <= tableProximityRadius)
                {
                    isPlayerInRange = true;
                }
                else if (distToDeposit > depositZoneRadius * 1.4f && distToTable > tableProximityRadius * 1.4f)
                {
                    isPlayerInRange = false;
                }
            }
            else
            {
                // Fallback: ตรวจจับ Collider รอบโต๊ะในกรณีไม่มี Reference
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, tableProximityRadius);
                bool found = false;
                foreach (var h in hits)
                {
                    if (h == null) continue;
                    var pc = h.GetComponentInParent<PlayerCarry>();
                    if (pc != null)
                    {
                        cachedPlayerCarry = pc;
                        playerTransform = pc.transform;
                        isPlayerInRange = true;
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    isPlayerInRange = false;
                }
            }
        }

        private void UpdateEditorVisuals()
        {
            if (depositRingObject == null || depositRingRenderer == null || depositRingRenderer.sprite == null)
            {
                SetupDepositZoneAndRing();
            }

            if (moneyRingObject == null || moneyRingRenderer == null || moneyRingRenderer.sprite == null)
            {
                SetupMoneyOutputRing();
            }

            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            if (depositRingObject != null)
            {
                depositRingObject.transform.position = GetDepositPosition();
                float d1 = depositZoneRadius * 2f;
                depositRingObject.transform.localScale = new Vector3(d1 / pX, d1 / pY, 1f);
            }

            if (moneyRingObject != null)
            {
                moneyRingObject.transform.position = GetMoneyOutputPosition();
                float d2 = moneyZoneRadius * 2f;
                moneyRingObject.transform.localScale = new Vector3(d2 / pX, d2 / pY, 1f);
            }
        }

        private void UpdateVisualAnimations()
        {
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            // ตรวจสอบ Sprite เสมอให้ไม่ว่างเปล่า
            if (depositRingRenderer != null && depositRingRenderer.sprite == null)
            {
                depositRingRenderer.sprite = GetRingSprite();
            }
            if (moneyRingRenderer != null && moneyRingRenderer.sprite == null)
            {
                moneyRingRenderer.sprite = GetRingSprite();
            }

            // 1. วงแหวนวางอาหาร (Pulse และเปลี่ยนสีเมื่อผู้เล่นยืน)
            if (depositRingRenderer != null && showDepositRing && depositRingObject != null)
            {
                Color targetCol = isPlayerInRange ? depositRingActiveColor : depositRingColor;
                depositRingRenderer.color = Color.Lerp(depositRingRenderer.color, targetCol, Time.deltaTime * 10f);

                float pulse1 = 1f + Mathf.Sin(Time.time * 2.5f) * 0.04f;
                float d1 = depositZoneRadius * 2f * pulse1;
                depositRingObject.transform.position = GetDepositPosition();
                depositRingObject.transform.localScale = new Vector3(d1 / pX, d1 / pY, 1f);
            }

            // 2. วงแหวนเงินออก (Pulse และ Pop Bounce ตอนเงินเด้ง)
            if (moneyRingRenderer != null && showMoneyRing && moneyRingObject != null)
            {
                float popBounce = 0f;
                if (moneyRingPulseTimer > 0f)
                {
                    moneyRingPulseTimer -= Time.deltaTime;
                    popBounce = Mathf.Sin((moneyRingPulseTimer / 0.35f) * Mathf.PI) * 0.2f;
                }

                float pulse2 = 1f + Mathf.Sin(Time.time * 2f) * 0.03f + popBounce;
                float d2 = moneyZoneRadius * 2f * pulse2;
                moneyRingObject.transform.position = GetMoneyOutputPosition();
                moneyRingObject.transform.localScale = new Vector3(d2 / pX, d2 / pY, 1f);

                Color targetCol = popBounce > 0.01f ? Color.Lerp(moneyRingColor, Color.white, 0.5f) : moneyRingColor;
                moneyRingRenderer.color = Color.Lerp(moneyRingRenderer.color, targetCol, Time.deltaTime * 12f);
            }
        }

        private bool WasInteractKeyPressed()
        {
            bool pressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
            {
                pressed = true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (!pressed && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return)))
            {
                pressed = true;
            }
#endif
            return pressed;
        }

        /// <summary>
        /// รับอาหารสุกจากผู้เล่นมาวางรอบนโต๊ะ (ไม่ต้องใช้จาน อาหารจะวางเรียงรอบนโต๊ะให้ลูกค้ามาซื้อ)
        /// </summary>
        public int DepositFoodFromPlayer(PlayerCarry playerCarry)
        {
            if (playerCarry == null) return 0;

            // กวาดอาหารที่เป็น null ออกไปก่อน (หากมี)
            dishesOnCounter.RemoveAll(d => d == null);

            int depositedCount = 0;

            while (dishesOnCounter.Count < MaxPlates)
            {
                // ดึงอาหารสุกชิ้นใดก็ได้ที่ผู้เล่นถืออยู่ (ไก่ต้ม, หมูต้ม, หรืออาหารปรุงสุกใดๆ)
                Item cookedItem = playerCarry.PopAnyCookedFood();

                // สำรอง: หาก PopAnyCookedFood ไม่พบ ลองค้นหาตามประเภทแบบเจาะจง
                if (cookedItem == null)
                {
                    if (playerCarry.HasItemOfType(ItemType.CookedChicken))
                    {
                        cookedItem = playerCarry.PopItemOfType(ItemType.CookedChicken);
                    }
                    else if (playerCarry.HasItemOfType(ItemType.CookedPig))
                    {
                        cookedItem = playerCarry.PopItemOfType(ItemType.CookedPig);
                    }
                    else
                    {
                        // ลองค้นหาไอเทมใดๆ ในตัวผู้เล่นที่สุกแล้ว (ไม่ใช่เนื้อดิบ)
                        for (int i = playerCarry.CarriedItems.Count - 1; i >= 0; i--)
                        {
                            var it = playerCarry.CarriedItems[i];
                            if (it != null && PlayerCarry.IsCookedFood(it))
                            {
                                cookedItem = playerCarry.PopItemOfType(it.Type);
                                break;
                            }
                        }
                    }
                }

                if (cookedItem != null)
                {
                    depositedCount++;
                    PlaceFoodOnCounter(cookedItem);

                    if (depositSound != null)
                    {
                        AudioSource.PlayClipAtPoint(depositSound, transform.position);
                    }
                }
                else
                {
                    break;
                }
            }

            return depositedCount;
        }

        /// <summary>
        /// วางอาหารลงบนโต๊ะโดยตรง (ไม่ต้องใช้จาน)
        /// </summary>
        public void PlaceFoodOnCounter(Item dish)
        {
            if (dish == null) return;

            // จำขนาดในโลกตอนแบกไว้ เพื่อวางบนโต๊ะด้วยขนาดเท่าเดิม
            foodWorldScales[dish] = dish.transform.lossyScale;

            dishesOnCounter.Add(dish);
            dish.gameObject.SetActive(true);
            dish.SetCarriedState(true);
            dish.transform.SetParent(transform);

            RearrangeFoodOnCounter();
            onDishesCountChanged?.Invoke(dishesOnCounter.Count);
        }

        /// <summary>
        /// จัดเรียงอาหารทั้งหมดที่วางรอบนโต๊ะให้สวยงามและเป็นระเบียบ
        /// </summary>
        private void RearrangeFoodOnCounter()
        {
            SpriteRenderer tableRenderer = GetComponent<SpriteRenderer>();
            Vector3 lossy = transform.lossyScale;
            float pX = Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f;
            float pY = Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f;

            bool hasPlateSlots = plateSlots != null && plateSlots.Length > 0;
            int count = dishesOnCounter.Count;

            for (int i = 0; i < count; i++)
            {
                Item dish = dishesOnCounter[i];
                if (dish == null) continue;

                if (hasPlateSlots && i < plateSlots.Length && plateSlots[i] != null)
                {
                    // 1. วางตามจุดโชว์ plateSlots ที่กำหนดไว้ใน Inspector
                    dish.transform.SetParent(plateSlots[i]);
                    dish.transform.localPosition = new Vector3(0f, 0f, -0.05f);
                    dish.transform.localRotation = Quaternion.identity;
                    dish.transform.localScale = Vector3.one * 0.85f;
                }
                else
                {
                    // 2. กรณีไม่มี plateSlots กองซ้อนขึ้นแนวตั้งจากกลางโต๊ะ ขนาดและระยะเท่ากองบนหลังผู้เล่น
                    Vector3 step = cachedPlayerCarry != null ? cachedPlayerCarry.WorldStackOffset : new Vector3(0f, foodDisplaySpacing, -0.02f);
                    Vector3 worldSlotPos = GetFoodDisplayCenterPosition() + new Vector3(0f, 0f, -0.05f) + step * i;

                    Vector3 worldScale = foodWorldScales.TryGetValue(dish, out var s) ? s : Vector3.one * 0.85f;

                    dish.transform.SetParent(transform);
                    dish.transform.position = worldSlotPos;
                    dish.transform.localRotation = Quaternion.identity;
                    dish.transform.localScale = new Vector3(worldScale.x / pX, worldScale.y / pY, 1f);
                }

                // ปิด Collider และ Rigidbody ขณะวางรอบนโต๊ะเพื่อไม่ให้ชนตัวละคร
                if (dish.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
                if (dish.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.simulated = false;
                    rb.linearVelocity = Vector2.zero;
                }

                if (dish.TryGetComponent<SpriteRenderer>(out var sr))
                {
                    if (tableRenderer != null)
                    {
                        sr.sortingLayerID = tableRenderer.sortingLayerID;
                        // ชิ้นที่อยู่สูงกว่าวาดทับชิ้นที่อยู่ต่ำกว่า
                        sr.sortingOrder = Mathf.Max(tableRenderer.sortingOrder + 2, foodDisplaySortingOrder) + i;
                    }
                    else
                    {
                        sr.sortingOrder = foodDisplaySortingOrder + i;
                    }
                    sr.enabled = true;
                }
            }
        }

        /// <summary>
        /// เมื่อลูกค้าเดินมาซื้ออาหาร:
        /// 1. หักอาหารออกจากโต๊ะ
        /// 2. ดีดเหรียญเงิน/เงินออกมาที่วงเงินออก (Money Output Zone)
        /// </summary>
        public Item CustomerBuyDish(ItemType? preferredType = null)
        {
            // กวาดอาหารที่ถูกทำลายไปแล้วออกก่อน กัน NullReference ตอนเช็ค Type
            dishesOnCounter.RemoveAll(d => d == null);
            if (dishesOnCounter.Count == 0) return null;

            Item selectedDish = null;
            if (preferredType.HasValue)
            {
                for (int i = 0; i < dishesOnCounter.Count; i++)
                {
                    if (dishesOnCounter[i].Type == preferredType.Value)
                    {
                        selectedDish = dishesOnCounter[i];
                        dishesOnCounter.RemoveAt(i);
                        break;
                    }
                }
            }

            if (selectedDish == null && dishesOnCounter.Count > 0)
            {
                selectedDish = dishesOnCounter[0];
                dishesOnCounter.RemoveAt(0);
            }

            if (selectedDish != null)
            {
                selectedDish.transform.SetParent(null);

                // จัดตำแหน่งอาหารที่เหลือบนโต๊ะใหม่
                RearrangeFoodOnCounter();

                // ลูกค้าจ่ายเงิน -> ดีดเงินออกมาที่วงแหวนสีเหลือง
                SpawnMoneyFromSale(selectedDish.SellPrice);

                if (sellSound != null)
                {
                    AudioSource.PlayClipAtPoint(sellSound, transform.position);
                }

                onDishSold?.Invoke(selectedDish);
                onDishesCountChanged?.Invoke(dishesOnCounter.Count);

                // ทำลายอาหาร (ลูกค้าทาน/รับไป)
                foodWorldScales.Remove(selectedDish);
                Destroy(selectedDish.gameObject);
            }

            return selectedDish;
        }

        /// <summary>
        /// ดีดเหรียญเงินออกมาที่วงเงินออก (Money Output Zone)
        /// </summary>
        private void SpawnMoneyFromSale(int earnedMoney)
        {
            if (earnedMoney <= 0) earnedMoney = 10;
            Vector3 finalSpawnPos;
            if (dropMoneyOnCounter)
            {
                finalSpawnPos = GetRandomPointOnCounter();
            }
            else
            {
                Vector2 randomSpread = Random.insideUnitCircle * 0.15f;
                finalSpawnPos = GetMoneyOutputPosition() + new Vector3(randomSpread.x, randomSpread.y, 0f);
                moneyRingPulseTimer = 0.35f;
            }

            if (coinPrefab != null)
            {
                // สร้างเหรียญจริงดีดออกมา
                GameObject coinObj = Instantiate(coinPrefab, finalSpawnPos, Quaternion.identity);
                if (coinObj.TryGetComponent<CoinPickup>(out var coinPickup))
                {
                    coinPickup.Value = earnedMoney;
                }

                if (!dropMoneyOnCounter && coinObj.TryGetComponent<Rigidbody2D>(out var coinRb) && moneyPopForce > 0f)
                {
                    Vector2 popDir = ((Vector2)(finalSpawnPos - transform.position)).normalized;
                    if (popDir == Vector2.zero) popDir = Vector2.down;
                    popDir = Quaternion.Euler(0, 0, Random.Range(-25f, 25f)) * popDir;
                    coinRb.AddForce(popDir * moneyPopForce, ForceMode2D.Impulse);
                }
            }
            else
            {
                // ถ้าไม่มี Coin Prefab ให้เพิ่มเงินเข้า MoneyManager ให้โดยตรง
                if (MoneyManager.Instance != null)
                {
                    MoneyManager.Instance.AddMoney(earnedMoney);
                }
            }

            onMoneyEarned?.Invoke(earnedMoney);
        }

        /// <summary>
        /// สุ่มตำแหน่งบนพื้นผิวโต๊ะ (เว้นระยะจากขอบตาม counterMoneyInset)
        /// </summary>
        private Vector3 GetRandomPointOnCounter()
        {
            Collider2D mainCol = GetComponent<Collider2D>();
            if (mainCol == null) return transform.position;

            Bounds b = mainCol.bounds;
            float halfW = Mathf.Max(0f, b.extents.x - counterMoneyInset);
            float halfH = Mathf.Max(0f, b.extents.y - counterMoneyInset);
            return new Vector3(
                b.center.x + Random.Range(-halfW, halfW),
                b.center.y + Random.Range(-halfH, halfH),
                transform.position.z);
        }

        public bool HasFoodAvailable(ItemType? preferredType = null)
        {
            if (dishesOnCounter.Count == 0) return false;
            if (!preferredType.HasValue) return true;

            foreach (var dish in dishesOnCounter)
            {
                if (dish != null && dish.Type == preferredType.Value) return true;
            }
            return false;
        }

        /// <summary>
        /// สำหรับดึงอาหารกลับคืนลงกระเป๋าผู้เล่น (ถ้าต้องการ)
        /// </summary>
        public Item TakeFoodFromCounter()
        {
            dishesOnCounter.RemoveAll(d => d == null);
            if (dishesOnCounter.Count == 0) return null;

            int lastIdx = dishesOnCounter.Count - 1;
            Item dish = dishesOnCounter[lastIdx];
            dishesOnCounter.RemoveAt(lastIdx);
            foodWorldScales.Remove(dish);

            dish.transform.SetParent(null);
            dish.SetCarriedState(false);

            if (dish.TryGetComponent<Collider2D>(out var col)) col.enabled = true;
            if (dish.TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = true;

            RearrangeFoodOnCounter();
            onDishesCountChanged?.Invoke(dishesOnCounter.Count);
            return dish;
        }

        // Backward compatibility
        public void PlaceDishOnCounter(Item dish) => PlaceFoodOnCounter(dish);
        public Item TakeDishFromCounter() => TakeFoodFromCounter();
        public void Interact(PlayerCarry playerCarry) => DepositFoodFromPlayer(playerCarry);

        // ========================== ตำแหน่งและ Ring Sprite ==========================

        public Vector3 GetFoodDisplayCenterPosition()
        {
            if (foodDisplayPoint != null)
            {
                return foodDisplayPoint.position;
            }
            return transform.position + new Vector3(foodDisplayOffset.x, foodDisplayOffset.y, 0f);
        }

        public Vector3 GetDepositPosition()
        {
            if (foodDepositPoint != null)
            {
                return foodDepositPoint.position;
            }
            return transform.position + new Vector3(depositZoneOffset.x, depositZoneOffset.y, 0f);
        }

        public Vector3 GetMoneyOutputPosition()
        {
            if (moneyOutputPoint != null)
            {
                return moneyOutputPoint.position;
            }
            return transform.position + new Vector3(moneyOutputOffset.x, moneyOutputOffset.y, 0f);
        }

        private static Sprite generatedRingSprite;

        private Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

#if UNITY_EDITOR
            Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Prefab/Shop/FloorStandRing.png");
            if (loaded != null)
            {
                cachedRingSprite = loaded;
                return cachedRingSprite;
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

        // ========================== Trigger & Collision Callbacks ==========================

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleIncomingCollider(collision.collider);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleIncomingCollider(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerEnter(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerStay(Collider2D other)
        {
            HandleIncomingCollider(other);
        }

        public void OnZoneTriggerExit(Collider2D other)
        {
            if (other == null) return;
            var pc = other.GetComponentInParent<PlayerCarry>();
            if (pc != null && cachedPlayerCarry == pc)
            {
                // ปล่อยให้ CheckProximityPlayer ตรวจสอบระยะทางที่แท้จริงจากโต๊ะแทนการตัดทันที
                float distToTable = playerTransform != null ? Vector2.Distance(transform.position, playerTransform.position) : 999f;
                if (distToTable > tableProximityRadius)
                {
                    isPlayerInRange = false;
                }
            }
        }

        private void HandleIncomingCollider(Collider2D col)
        {
            if (col == null) return;
            var playerCarry = col.GetComponentInParent<PlayerCarry>();
            if (playerCarry != null)
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                if (!requireKeyPress || WasInteractKeyPressed())
                {
                    DepositFoodFromPlayer(playerCarry);
                }
            }
        }

        private void OnDrawGizmos()
        {
            // วาดตำแหน่งจุดโชว์ที่วางอาหารบนโต๊ะ (สีเขียว)
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.85f);
            if (plateSlots != null && plateSlots.Length > 0)
            {
                for (int i = 0; i < plateSlots.Length; i++)
                {
                    if (plateSlots[i] != null)
                    {
                        Gizmos.DrawWireSphere(plateSlots[i].position, 0.2f);
                        Gizmos.DrawLine(transform.position, plateSlots[i].position);
                    }
                }
            }
            else
            {
                Vector3 dispPos = GetFoodDisplayCenterPosition();
                Gizmos.DrawWireSphere(dispPos, 0.25f);
                Gizmos.DrawLine(transform.position, dispPos);
            }

            // วาดโซนวางอาหาร (สีฟ้า/ขาว)
            Vector3 depositPos = GetDepositPosition();
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
            Gizmos.DrawSphere(depositPos, depositZoneRadius);
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Gizmos.DrawWireSphere(depositPos, depositZoneRadius);
            Gizmos.DrawLine(transform.position, depositPos);

            // วาดโซนเงินออก (สีเหลืองทอง)
            Vector3 moneyPos = GetMoneyOutputPosition();
            Gizmos.color = new Color(moneyRingColor.r, moneyRingColor.g, moneyRingColor.b, 0.35f);
            Gizmos.DrawSphere(moneyPos, moneyZoneRadius);
            Gizmos.color = moneyRingColor;
            Gizmos.DrawWireSphere(moneyPos, moneyZoneRadius);
            Gizmos.DrawLine(transform.position, moneyPos);

            // วาดแถวต่อคิวของลูกค้า (สีส้ม/ทอง)
            Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.85f);
            Vector3 waitStart = CustomerWaitPoint.position;
            Gizmos.DrawWireSphere(waitStart, 0.25f);

            for (int i = 0; i < maxQueueCapacity; i++)
            {
                Vector3 qPos;
                if (customQueuePoints != null && i < customQueuePoints.Length && customQueuePoints[i] != null)
                {
                    qPos = customQueuePoints[i].position;
                }
                else
                {
                    Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
                    qPos = waitStart + (Vector3)(dir * (i * queueSpacing));
                }

                Gizmos.DrawWireSphere(qPos, 0.18f);

                if (i > 0)
                {
                    Vector3 prevPos;
                    if (customQueuePoints != null && (i - 1) < customQueuePoints.Length && customQueuePoints[i - 1] != null)
                        prevPos = customQueuePoints[i - 1].position;
                    else
                    {
                        Vector2 dir = queueDirection.sqrMagnitude > 0.001f ? queueDirection.normalized : Vector2.down;
                        prevPos = waitStart + (Vector3)(dir * ((i - 1) * queueSpacing));
                    }
                    Gizmos.DrawLine(prevPos, qPos);
                }
            }
        }
    }
}
