using System.Collections;
using UnityEngine;

namespace GI351.KitchenGame
{
    public enum CustomerState
    {
        WalkingToCounter,
        WaitingAtCounter,
        WalkingToExit,
        ThiefFleeing,    // โจรวิ่งหนีพร้อมของที่ขโมยมา
        WalkingToCashier, // ถืออาหารเดินไปต่อแถวที่โต๊ะจ่ายเงิน
        WaitingAtCashier  // ยืนต่อแถวรอจ่ายเงิน
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CustomerAI : MonoBehaviour, IDamageable
    {
        [Header("Customer Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเร็วในการเดินของลูกค้าปกติ")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("ความอดทนในการยืนรออาหาร (วินาที)")]
        [SerializeField] private float patienceDuration = 15f;

        [Tooltip("เปิด = ลูกค้าปกติที่รอนานจนหมดเวลาจะโมโหแล้วเดินกลับ / ปิด = รอได้ไม่จำกัด (โจรยังหมดความอดทนเสมอ ไม่งั้นจะค้างหน้าโต๊ะ)")]
        [SerializeField] private bool normalCustomerLeavesWhenImpatient = false;

        [Tooltip("สุ่มอาหารที่อยากกิน (CookedChicken หรือ CookedPig หรือ กินอะไรก็ได้)")]
        [SerializeField] private bool wantSpecificFood = true;

        [SerializeField] private ItemType preferredDish = ItemType.CookedChicken;

        [Header("Buying & Patience (จำนวนที่ซื้อ / ความอดทน แบบ Juice King)")]
        [Tooltip("ลูกค้าซื้อคนละกี่จาน สุ่มระหว่าง x ถึง y (ซื้อเท่าที่แผงมีตอนนั้น อย่างน้อย 1)")]
        [SerializeField] private Vector2Int dishesToBuy = new Vector2Int(1, 3);

        [Tooltip("ช่วงแรกของเวลารอ (สัดส่วน 0-1) ที่ตัวลูกค้ายังไม่เปลี่ยนสี หลังจากนั้นค่อยๆ แดงจนหมดความอดทน (Juice King: 0.4)")]
        [Range(0f, 1f)] [SerializeField] private float angryStartRatio = 0.4f;

        [Tooltip("สีตัวลูกค้าตอนใกล้หมดความอดทน (ส้มแดง)")]
        [SerializeField] private Color angryColor = new Color(1f, 0.47f, 0.38f, 1f);

        [Header("Obstacle Avoidance (เดินอ้อมโต๊ะ/หม้อ ไม่เดินทะลุ)")]
        [Tooltip("รัศมีตัวลูกค้าที่ใช้เช็คว่าทางเดินชนสิ่งกีดขวางหรือไม่")]
        [SerializeField] private float avoidanceRadius = 0.4f;

        [Tooltip("ระยะเผื่อจากมุมสิ่งกีดขวางตอนเดินอ้อม (เมตร)")]
        [SerializeField] private float avoidanceMargin = 0.15f;

        [Header("Thief Settings (โจรแซงคิว)")]
        [Tooltip("สถานะลูกค้าเป็นโจรแซงคิวหรือไม่")]
        [SerializeField] private bool isThief = false;

        [Tooltip("พลังชีวิตของโจร (กระบองปกติทำ 20 ดาเมจ ตี 1 ครั้งก็ทิ้งของ โยนเงิน แล้วหนี)")]
        [SerializeField] private int thiefHealth = 20;

        [Tooltip("ตัวคูณความเร็วตอนโจรถือของเดินหนี เทียบกับความเร็วเดิน (Move Speed) 0.333 = ช้ากว่าลูกค้าปกติ 3 เท่า ให้ผู้เล่นตามทัน")]
        [SerializeField] private float thiefFleeSpeedMultiplier = 0.333f;

        [Tooltip("โจรขโมยอาหารครั้งละกี่จาน สุ่มระหว่าง x ถึง y (ได้ไม่เกินที่มีบนแผงตอนนั้น)")]
        [SerializeField] private Vector2Int thiefStealDishes = new Vector2Int(1, 3);

        [Tooltip("ระยะเผื่อรอบโซนร้าน (หน่วยโลก) โซนร้าน = กรอบรวมของแผงอาหาร โต๊ะจ่ายเงิน หม้อ และร้านค้า โจรจะรอจนผู้เล่นเดินกลับเข้าโซนนี้ก่อนค่อยหยิบของหนี")]
        [SerializeField] private float shopZonePadding = 3f;

        [Tooltip("ตีโจรแล้วได้เงินตกพื้น = ราคาอาหารที่แพงที่สุดตอนนั้น (หม้อที่ปลดแล้ว) x ค่านี้")]
        [Min(0)] [SerializeField] private int thiefRewardMultiplier = 4;

        [Tooltip("จำนวนเหรียญที่เด้งออกมาตอนตีโจร สุ่มระหว่าง x ถึง y (แบ่งเงินรวมเท่าๆ กัน)")]
        [SerializeField] private Vector2Int thiefRewardCoins = new Vector2Int(4, 6);

        [Header("Target & Path References")]
        [SerializeField] private FoodSellCounter targetCounter;
        [SerializeField] private Transform exitPoint;

        [Tooltip("โต๊ะจ่ายเงิน (ถ้ามี ลูกค้าจะหยิบอาหารจากโต๊ะอาหารแล้วถือมาจ่ายที่นี่ ถ้าว่างจะหาในฉากให้อัตโนมัติ)")]
        [SerializeField] private CashierCounter cashier;

        [Tooltip("ขนาดอาหารตอนลูกค้าถือ (สเกลในโลก)")]
        [SerializeField] private float heldDishScale = 0.84f;

        [Header("Visual Feedback (Optional)")]
        [Tooltip("ไอคอนแสดงตอนได้กินอาหาร (มีความสุข)")]
        [SerializeField] private GameObject happyIcon;

        [Tooltip("ไอคอนแสดงตอนรอนานเกินไป (โมโห)")]
        [SerializeField] private GameObject angryIcon;

        [Tooltip("SpriteRenderer สำหรับกลับด้านหันหน้า")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Damage Visual Feedback (กระพริบแดงเมื่อโดนตี)")]
        [Tooltip("สีตัวละครขณะโดนตี (สีแดง)")]
        [SerializeField] private Color hurtColor = new Color(1f, 0.25f, 0.25f, 1f);

        [Tooltip("ระยะเวลาการกระพริบสีแดง (วินาที)")]
        [SerializeField] private float hurtFlashDuration = 0.18f;

        private Rigidbody2D rb;
        private CustomerState currentState = CustomerState.WalkingToCounter;
        private float currentPatienceTimer;
        private CustomerSpawner spawnerReference;
        private int lastQueueIndex = -1;
        private bool isAngry = false;

        private Color thiefColor = new Color(0.4f, 0.45f, 0.55f, 1f);
        private Color defaultCustomerColor = Color.white;
        private Coroutine hurtFlashCoroutine;

        // อาหารที่ลูกค้าถือไปจ่ายเงิน (ซื้อได้หลายจาน)
        private readonly System.Collections.Generic.List<Item> carriedDishes = new System.Collections.Generic.List<Item>();
        private int wantCount = 1;
        private float payTimer;

        /// <summary>จำนวนจานที่ถืออยู่</summary>
        public int CarriedDishCount => carriedDishes.Count;

        /// <summary>ตั้งจำนวนจานที่ลูกค้าคนนี้จะซื้อ (สุ่มระหว่าง min ถึง max) เรียกก่อน Start (CustomerSpawner เรียกตอนเกิด)</summary>
        public void SetDishesToBuy(int min, int max)
        {
            dishesToBuy = new Vector2Int(Mathf.Max(1, min), Mathf.Max(1, max));
        }

        private bool waitForever;

        /// <summary>true = รออาหารได้ไม่จำกัด ไม่เดินหนี (ลูกค้าคนแรกตอนสอนเล่น)</summary>
        public void SetWaitForever(bool value)
        {
            waitForever = value;
        }
        /// <summary>จำนวนลูกค้าปกติที่รอจนหมดความอดทนแล้วเดินหนี (นับตั้งแต่เริ่มเล่น)</summary>
        public static int LostCustomers { get; private set; }

        /// <summary>โจรที่ถือของที่ขโมยอยู่ (ยังไม่โดนตี ยังไม่หนีพ้น) แถบเตือนบน HUD แสดงค้างไว้ตราบที่ยังมีโจรแบบนี้</summary>
        private static readonly System.Collections.Generic.HashSet<CustomerAI> activeRobbers = new System.Collections.Generic.HashSet<CustomerAI>();
        public static int ActiveRobberCount
        {
            get { activeRobbers.RemoveWhere(r => r == null); return activeRobbers.Count; }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            activeRobbers.Clear();
            LostCustomers = 0;
        }

        // โจร: อาหารที่ขโมยมาอยู่ใน carriedDishes (ถือเหนือหัวแบบลูกค้า) เก็บสเกลเดิมไว้คืนตอนทำหล่นพื้น (ลำดับเดียวกับ carriedDishes)
        // โจรยืนรอผู้เล่นกลับเข้าโซนร้าน (ไม่แย่งคิวลูกค้าปกติ)
        private bool thiefWaitingForPlayer;
        // เหรียญที่กำลังเด้งออกจากตัวโจร (ยังปิด CoinPickup ไว้จนกว่าจะตกถึงพื้น)
        private readonly System.Collections.Generic.List<CoinPickup> hoppingCoins = new System.Collections.Generic.List<CoinPickup>();
        private readonly System.Collections.Generic.List<Vector3> hoppingCoinTargets = new System.Collections.Generic.List<Vector3>();
        private GameObject exclamationObject;

        // จุดอ้อมสิ่งกีดขวางที่กำลังเดินไป (มุมของโต๊ะ/หม้อ)
        private bool hasDetour;
        private Vector2 detourPoint;
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
        private ContactFilter2D obstacleFilter;

        public CustomerState State => currentState;

        /// <summary>เมนูที่ลูกค้าอยากกินโดยเฉพาะ (null = กินอะไรก็ได้ เช่น โจร)</summary>
        public ItemType? RequestedDish => (!isThief && wantSpecificFood) ? preferredDish : (ItemType?)null;
        public ItemType PreferredDish => preferredDish;
        // โจรที่รอผู้เล่นอยู่ ถือว่าใจเย็นที่สุด ไม่บังลูกค้าปกติในโหมดยืนมุง
        public float RemainingPatience => thiefWaitingForPlayer ? float.MaxValue : currentPatienceTimer;

        /// <summary>ลูกค้าปกติที่เลือกเมนูแล้ว กำลังเดินไป/ยืนรอที่แผง ยังไม่ได้อาหาร</summary>
        public bool IsWaitingForFood => !isThief && dishPicked && carriedDishes.Count == 0 &&
            (currentState == CustomerState.WalkingToCounter || currentState == CustomerState.WaitingAtCounter);
        public bool IsThief => isThief;
        /// <summary>โจรที่โดนตีแล้ว (โยนเงินทิ้งแล้วกำลังเดินหนีออกจากร้าน) ไม่นับเป็นโจรที่ยังอันตราย</summary>
        public bool IsCaught => thiefCaught;
        private bool thiefCaught;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                defaultCustomerColor = spriteRenderer.color;
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            // ตรวจเฉพาะ Collider ที่เป็นของแข็ง (ไม่นับวงแหวน/ไอเทมที่เป็น Trigger)
            obstacleFilter = new ContactFilter2D();
            obstacleFilter.useTriggers = false;

            // ตรวจสอบ Collider2D สำหรับรับ Hit โจมตีและเก็บขึ้นหลัง
            var col = GetComponent<CircleCollider2D>();
            if (col == null)
            {
                col = gameObject.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;
            }

            // ป้องกันค่าความอดทน 100000000 จาก Prefab เดิม
            if (patienceDuration > 60f)
            {
                patienceDuration = 15f;
            }

            if (happyIcon != null) happyIcon.SetActive(false);
            if (angryIcon != null) angryIcon.SetActive(false);

            if (targetCounter == null)
            {
#if UNITY_2023_1_OR_NEWER
                targetCounter = Object.FindAnyObjectByType<FoodSellCounter>();
#else
                targetCounter = FindObjectOfType<FoodSellCounter>();
#endif
            }

            if (cashier == null)
            {
                cashier = FindAnyObjectByType<CashierCounter>();
            }
        }

        private void Start()
        {
            if (patienceDuration > 60f)
            {
                patienceDuration = 15f;
            }
            currentPatienceTimer = patienceDuration;

            int lo = Mathf.Max(1, Mathf.Min(dishesToBuy.x, dishesToBuy.y));
            int hi = Mathf.Max(lo, Mathf.Max(dishesToBuy.x, dishesToBuy.y));
            wantCount = Random.Range(lo, hi + 1);

            PickDishAndCounter();

            // เพิกเฉยการชนระหว่างลูกค้าทุกคน เพื่อให้โจรเดินทะลุลูกค้าแซงคิวได้เสมอ
            IgnoreCollisionWithOtherCustomers();

            if (isThief) ApplyThiefAppearance();

            // โจรก็ต่อท้ายแถวตามปกติ เหมือนลูกค้าทั่วไป (ไม่แซงคิว)
            if (targetCounter != null && currentState == CustomerState.WalkingToCounter)
            {
                bool joined = targetCounter.TryJoinQueue(this);
                if (!joined && targetCounter.IsQueueFull)
                {
                    LeaveCounterAndExit();
                }
                else
                {
                    lastQueueIndex = targetCounter.GetQueueIndex(this);
                }
            }
        }

        private bool dishPicked;

        /// <summary>
        /// สุ่มเมนูที่อยากกิน (เฉพาะเมนูที่เครื่องปลดล็อกแล้วและมีชั้นขาย) แล้วเลือกชั้นที่ขายเมนูนั้น
        /// โจรไม่เจาะจงเมนู เลือกชั้นที่มีอาหารวางเยอะสุด ทำครั้งเดียวต่อลูกค้า
        /// </summary>
        private void PickDishAndCounter()
        {
            if (dishPicked) return;
            dishPicked = true;

            if (wantSpecificFood && !isThief)
            {
                ItemType? picked = DishMenu.PickRandomDish();
                if (picked.HasValue)
                {
                    preferredDish = picked.Value;
                    ShowWantBubble();
                }
                else
                {
                    wantSpecificFood = false; // ยังไม่มีเครื่องไหนปลด -> กินอะไรก็ได้
                }
            }

            FoodSellCounter counter = FoodSellCounter.FindCounterFor(RequestedDish);
            if (counter != null) targetCounter = counter;
        }

        public void SetAsThief(bool thief)
        {
            isThief = thief;
            if (isThief)
            {
                wantSpecificFood = false;
                HideWantBubble();
                ApplyThiefAppearance();
            }
        }

        private void ApplyThiefAppearance()
        {
            if (spriteRenderer != null)
            {
                // สีชุดโจร (สีเทาเข้มอมน้ำเงิน) และ Sorting Order สูงกว่าลูกค้าปกติเพื่อให้อยู่หน้าแถวชัดเจน
                spriteRenderer.color = thiefColor;
                spriteRenderer.sortingOrder = 3;
            }

            // โจรมีเครื่องหมาย ! ลอยอยู่เหนือหัวตลอดตั้งแต่เกิด
            ShowExclamationMark();
        }

        public void Init(FoodSellCounter counter, Transform exit, CustomerSpawner spawner)
        {
            targetCounter = counter;
            exitPoint = exit;
            spawnerReference = spawner;

            if (isThief) ApplyThiefAppearance();

            // เลือกเมนู แล้วไปชั้นที่ขายเมนูนั้น (ชั้นหมู / ชั้นไก่)
            PickDishAndCounter();

            // โจรก็ต่อท้ายแถวตามปกติ เหมือนลูกค้าทั่วไป (ไม่แซงคิว)
            if (targetCounter != null)
            {
                bool joined = targetCounter.TryJoinQueue(this);
                if (!joined && targetCounter.IsQueueFull)
                {
                    LeaveCounterAndExit();
                }
                else
                {
                    lastQueueIndex = targetCounter.GetQueueIndex(this);
                }
            }
        }

        private void Update()
        {
            if (carryBadge != null)
            {
                // ในแถวจ่ายเงิน โชว์ป้ายเฉพาะคนหัวแถว (คนข้างหลังยืนชิดกัน ป้ายจะบังกัน)
                // จ่ายเงินแล้วแบกอาหารกลับบ้าน: เหลือแค่จานบนหัว ไม่ต้องมีป้ายจำนวน
                bool showBadge = carriedDishes.Count > 0 && currentState != CustomerState.WalkingToExit &&
                                 (cashier == null || currentState != CustomerState.WaitingAtCashier || cashier.IsHeadOfQueue(this));
                if (carryBadgeRoot.activeSelf != showBadge) carryBadgeRoot.SetActive(showBadge);
                carryBadge.Tick();
            }

            if (currentState == CustomerState.WaitingAtCounter)
            {
                HandleWaiting();
            }
            else if (currentState == CustomerState.WaitingAtCashier)
            {
                HandlePaying();
            }
        }

        private void FixedUpdate()
        {
            switch (currentState)
            {
                case CustomerState.WalkingToCounter:
                    if (targetCounter != null)
                    {
                        // ทั้งลูกค้าปกติและโจร: เดินเข้าประจำช่องท้ายแถวคิว
                        Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                        MoveTowards(targetSlotPos, moveSpeed, OnReachedCounter);
                    }
                    break;

                case CustomerState.WaitingAtCounter:
                    if (targetCounter != null)
                    {
                        Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                        float distToSlot = Vector2.Distance(rb.position, targetSlotPos);

                        if (distToSlot > 0.12f)
                        {
                            MoveTowards(targetSlotPos, moveSpeed, null);
                        }
                        else
                        {
                            rb.linearVelocity = Vector2.zero;
                            // หันหน้าเข้าหาเคาน์เตอร์
                            FaceTowards(targetCounter.transform.position);
                        }

                        int currentQueueIndex = targetCounter.GetQueueIndex(this);
                        // ความอดทนไม่รีเซ็ตตอนคิวขยับ (นับจากเวลาที่แผงไม่มีของ ทุกคนรอพร้อมกัน)
                        if (currentQueueIndex != -1 && currentQueueIndex < lastQueueIndex)
                        {
                            lastQueueIndex = currentQueueIndex;
                        }
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
                    }
                    break;

                case CustomerState.WalkingToCashier:
                    if (cashier != null)
                    {
                        MoveTowards(cashier.GetQueuePosition(this), moveSpeed, () => currentState = CustomerState.WaitingAtCashier);
                    }
                    break;

                case CustomerState.WaitingAtCashier:
                    if (cashier != null)
                    {
                        // แถวขยับขึ้นเมื่อคนหน้าจ่ายเสร็จ ให้เดินตามไปช่องใหม่
                        Vector3 paySlot = cashier.GetQueuePosition(this);
                        if (Vector2.Distance(rb.position, paySlot) > 0.12f)
                        {
                            MoveTowards(paySlot, moveSpeed, null);
                        }
                        else
                        {
                            rb.linearVelocity = Vector2.zero;
                            FaceTowards(cashier.transform.position);
                        }
                    }
                    break;

                case CustomerState.ThiefFleeing:
                    // โจรวิ่งหนีสุดฝีเท้าไปยังทางออกพร้อมเครื่องหมายตกใจ
                    Vector3 thiefExitPos = exitPoint != null ? exitPoint.position : transform.position + Vector3.down * 15f;
                    MoveTowards(thiefExitPos, moveSpeed * thiefFleeSpeedMultiplier, OnReachedExit);
                    break;

                case CustomerState.WalkingToExit:
                    // เดินกลับทางออก (ลูกค้าซื้อเสร็จแล้ว หรือโมโหที่รอนาน)
                    float exitSpeed = isAngry ? moveSpeed * 1.6f : moveSpeed;
                    Vector3 normalExitPos = exitPoint != null ? exitPoint.position : transform.position + Vector3.down * 15f;
                    MoveTowards(normalExitPos, exitSpeed, OnReachedExit);
                    break;
            }
        }

        private void MoveTowards(Vector3 destination, float speed, System.Action onArrived)
        {
            Vector2 currentPos = rb.position;
            Vector2 dir = (Vector2)destination - currentPos;
            float dist = dir.magnitude;

            if (dist <= 0.12f)
            {
                rb.linearVelocity = Vector2.zero;
                hasDetour = false;
                onArrived?.Invoke();
                return;
            }

            // จุดเกิดอยู่บนแหลมที่มีน้ำล้อม: ข้ามระหว่างแหลมกับเกาะร้านต้องผ่านคอแหลม (Path Gate) ก่อน ไม่เดินตัดน้ำ
            Vector2 goal = GetGateTarget(currentPos, destination);

            // ถ้าทางตรงชนโต๊ะ/หม้อ ให้เดินไปมุมของสิ่งกีดขวางก่อน
            Vector2 steerTarget = goal != (Vector2)destination ? goal : GetSteerTarget(currentPos, destination);
            Vector2 vel = (steerTarget - currentPos).normalized * speed;
            rb.linearVelocity = vel;

            if (spriteRenderer != null)
            {
                if (vel.x > 0.05f) spriteRenderer.flipX = false;
                else if (vel.x < -0.05f) spriteRenderer.flipX = true;
            }
        }

        /// <summary>
        /// ถ้าตัวลูกค้ากับปลายทางอยู่คนละฝั่งของคอแหลม (ฝั่งหนึ่งต่ำกว่า Path Gate อีกฝั่งสูงกว่า) ให้เดินไปที่คอแหลมก่อน
        /// </summary>
        private Vector2 GetGateTarget(Vector2 from, Vector2 destination)
        {
            if (spawnerReference == null || spawnerReference.PathGate == null) return destination;
            // ใช้คอแหลมเฉพาะตอนเดินจากจุดเกิดมาแผง และตอนเดินกลับ/วิ่งหนี  แถวจ่ายเงินอยู่บนเกาะ ไม่ต้องอ้อม
            if (currentState != CustomerState.WalkingToCounter && currentState != CustomerState.WalkingToExit &&
                currentState != CustomerState.ThiefFleeing) return destination;
            Vector2 gate = spawnerReference.PathGate.position;
            const float band = 0.15f;
            bool fromBelow = from.y < gate.y - band;
            bool destBelow = destination.y < gate.y - band;
            if (fromBelow == destBelow) return destination;
            if (Vector2.Distance(from, gate) <= 0.2f) return destination;
            return gate;
        }

        /// <summary>
        /// หาจุดที่ควรเดินไปตอนนี้: ปลายทางเลยถ้าทางโล่ง หรือมุมของสิ่งกีดขวางที่ขวางทางอยู่
        /// </summary>
        private Vector2 GetSteerTarget(Vector2 from, Vector2 destination)
        {
            if (hasDetour)
            {
                if (Vector2.Distance(from, detourPoint) <= 0.12f || !IsPathBlocked(from, destination, out _))
                {
                    hasDetour = false;
                }
                else
                {
                    return detourPoint;
                }
            }

            if (!IsPathBlocked(from, destination, out Collider2D obstacle)) return destination;

            // เลือกมุม (ขยายเผื่อขนาดตัวลูกค้า) ที่เดินไปได้และทำให้ระยะรวมสั้นที่สุด
            Bounds b = obstacle.bounds;
            float pad = avoidanceRadius + avoidanceMargin;
            Vector2 min = (Vector2)b.min - Vector2.one * pad;
            Vector2 max = (Vector2)b.max + Vector2.one * pad;
            Vector2[] corners =
            {
                new Vector2(min.x, min.y), new Vector2(min.x, max.y),
                new Vector2(max.x, min.y), new Vector2(max.x, max.y)
            };

            float bestCost = float.MaxValue;
            bool found = false;
            foreach (var corner in corners)
            {
                if (Vector2.Distance(from, corner) <= 0.12f) continue;
                if (IsPathBlocked(from, corner, out _)) continue;

                float cost = Vector2.Distance(from, corner) + Vector2.Distance(corner, destination);
                if (cost < bestCost)
                {
                    bestCost = cost;
                    detourPoint = corner;
                    found = true;
                }
            }

            if (!found) return destination;

            hasDetour = true;
            return detourPoint;
        }

        /// <summary>
        /// เช็คว่าเดินตรงจาก from ไป to แล้วจะชนของแข็งที่อยู่กับที่ (โต๊ะ/หม้อ) หรือไม่
        /// </summary>
        private bool IsPathBlocked(Vector2 from, Vector2 to, out Collider2D obstacle)
        {
            obstacle = null;
            Vector2 dir = to - from;
            float dist = dir.magnitude;
            if (dist < 0.01f) return false;

            int count = Physics2D.CircleCast(from, avoidanceRadius, dir / dist, obstacleFilter, castHits, dist);
            float nearest = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var hit = castHits[i];
                if (hit.collider == null) continue;
                // ข้ามสิ่งที่เคลื่อนที่ได้ (ผู้เล่น สัตว์ ลูกค้าคนอื่น) อ้อมเฉพาะของที่ตั้งอยู่กับที่
                if (hit.rigidbody != null && hit.rigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                if (hit.distance < nearest)
                {
                    nearest = hit.distance;
                    obstacle = hit.collider;
                }
            }
            return obstacle != null;
        }

        private void FaceTowards(Vector3 targetPos)
        {
            if (spriteRenderer != null)
            {
                float diffX = targetPos.x - transform.position.x;
                if (diffX > 0.08f) spriteRenderer.flipX = false;
                else if (diffX < -0.08f) spriteRenderer.flipX = true;
            }
        }

        private void OnReachedCounter()
        {
            currentState = CustomerState.WaitingAtCounter;
            if (targetCounter != null)
            {
                FaceTowards(targetCounter.CustomerWaitPoint.position);
            }
        }

        /// <summary>
        /// โจรฉกอาหารบนโต๊ะโดยไม่จ่ายเงิน แล้วขึ้นเครื่องหมายตกใจพร้อมวิ่งหนี
        /// </summary>
        private void ThiefSnatchFoodAndFlee()
        {
            if (targetCounter == null) return;

            targetCounter.LeaveQueue(this);
            thiefWaitingForPlayer = false;

            // ฉก 1-3 จาน (ได้ไม่เกินที่มีบนแผง) แล้วถือเหนือหัวแบบลูกค้า: เห็นจานเดียว + ป้ายจำนวน
            int lo = Mathf.Max(1, Mathf.Min(thiefStealDishes.x, thiefStealDishes.y));
            int hi = Mathf.Max(lo, Mathf.Max(thiefStealDishes.x, thiefStealDishes.y));
            int toSteal = Random.Range(lo, hi + 1);
            for (int i = 0; i < toSteal; i++)
            {
                Item dish = targetCounter.ThiefStealDish(this);
                if (dish == null) break;   // บนแผงหมดแล้ว
                HoldDish(dish);
            }

            ShowExclamationMark();
            // เสียงเตือนโจร: เล่นจากแถบเตือนบน HUD (UiAudioManager) ที่เดียว ไม่ซ้อนกับ AudioManager
            currentState = CustomerState.ThiefFleeing;
            if (carriedDishes.Count > 0)
            {
                activeRobbers.Add(this);
                HudExtrasView alert = FindAnyObjectByType<HudExtrasView>();
                if (alert != null) alert.ShowThiefAlert();
                else UiAudioManager.Instance?.PlayThiefAlert();
            }
            Debug.Log($"[CustomerAI] โจรฉกอาหารไป {carriedDishes.Count} จาน! เดินหนีแล้ว ตามไปตีให้ทัน!");
        }

        private void HandleWaiting()
        {
            if (targetCounter == null) return;

            // กรณีลูกค้าปกติ
            if (!isThief)
            {
                if (targetCounter.CanTakeNow(this))
                {
                    // ยืนมุง: รับอาหารตรงจุดที่ตัวเองยืน (ใครใกล้เคาน์เตอร์ที่สุดและมีของที่อยากกินหยิบก่อน) / ต่อแถว: ต้องอยู่หัวแถว
                    float distToWaitPoint = Vector2.Distance(rb.position, ServePoint());
                    if (distToWaitPoint <= 0.4f && targetCounter.HasFoodAvailable(wantSpecificFood ? preferredDish : (ItemType?)null))
                    {
                        // หยิบอาหารไปถือ แล้วเดินไปจ่ายเงินที่โต๊ะจ่ายเงิน
                        ItemType? want = wantSpecificFood ? preferredDish : (ItemType?)null;
                        Item dish = targetCounter.CustomerTakeDish(want);
                        if (dish != null)
                        {
                            HoldDish(dish);
                            // ซื้อเพิ่มจนครบจำนวนที่อยากได้ เท่าที่แผงมี
                            while (carriedDishes.Count < wantCount && targetCounter.HasFoodAvailable(want))
                            {
                                Item more = targetCounter.CustomerTakeDish(want);
                                if (more == null) break;
                                HoldDish(more);
                            }
                            ResetPatienceTint();
                            if (cashier != null)
                            {
                                GoToCashier();
                            }
                            else
                            {
                                // ไม่มีโต๊ะจ่ายเงินในฉาก: ถืออาหารกลับไปเลย (ไม่ได้เงิน)
                                Debug.LogWarning("[CustomerAI] ไม่มี CashierCounter ในฉาก ลูกค้าเอาอาหารไปโดยไม่ได้จ่ายเงิน", this);
                                ShowHappy();
                                LeaveCounterAndExit();
                            }
                            return;
                        }
                    }
                }

                // นับเวลาความอดทน (ปิดไว้ = ลูกค้าปกติรอได้ไม่จำกัด / ลูกค้าคนแรกตอนสอนเล่นรอได้ไม่จำกัด)
                if (!normalCustomerLeavesWhenImpatient || waitForever)
                {
                    currentPatienceTimer = patienceDuration;
                    return;
                }

                // นับถอยหลังเฉพาะตอนแผงไม่มีของที่อยากกิน (มีของแล้วแค่รอคิวหยิบ ความอดทนค่อยๆ ฟื้น)
                bool foodThere = targetCounter.HasFoodAvailable(wantSpecificFood ? preferredDish : (ItemType?)null);
                if (foodThere)
                {
                    currentPatienceTimer = Mathf.Min(patienceDuration, currentPatienceTimer + Time.deltaTime * 2f);
                }
                else
                {
                    currentPatienceTimer -= Time.deltaTime;
                }
                UpdatePatienceTint();

                if (currentPatienceTimer <= 0f)
                {
                    // หมดความอดทน โมโหแล้วเดินหนี
                    isAngry = true;
                    LostCustomers++;
                    ShowAngry();
                    AudioManager.Play(SoundId.CustomerAngry);
                    LeaveCounterAndExit();
                }
            }
            else
            {
                // ผู้เล่นออกไปล่าสัตว์อยู่: โจรยืนรอเฉยๆ ยังไม่หยิบของ และไม่หมดความอดทน (รอผู้เล่นกลับเข้าโซนร้านก่อน)
                thiefWaitingForPlayer = !ShopZone.IsPlayerInside(shopZonePadding);
                if (thiefWaitingForPlayer)
                {
                    currentPatienceTimer = patienceDuration;
                    return;
                }

                // โจรรอคิวตามลำดับ พอถึงหัวแถวและมีอาหารบนโต๊ะ ให้ฉกแล้ววิ่งหนี (ไม่จ่ายเงิน)
                if (targetCounter.CanTakeNow(this))
                {
                    float distToWaitPoint = Vector2.Distance(rb.position, ServePoint());
                    if (distToWaitPoint <= 0.4f && targetCounter.DishesCount > 0)
                    {
                        ThiefSnatchFoodAndFlee();
                        return;
                    }
                }

                // โจรก็มีความอดทนเหมือนลูกค้าปกติ: รอนานจนไม่มีอาหารให้ฉก จะเดินกลับมือเปล่า
                currentPatienceTimer -= Time.deltaTime;
                if (currentPatienceTimer <= 0f)
                {
                    isAngry = true;
                    ShowAngry();
                    AudioManager.Play(SoundId.CustomerAngry);
                    LeaveCounterAndExit();
                    Debug.Log("[CustomerAI] โจรรอนานเกินไป ไม่มีอาหารให้ขโมย เดินกลับมือเปล่า");
                }
            }
        }

        /// <summary>จุดที่ลูกค้าต้องยืนเพื่อรับอาหาร: โหมดยืนมุง = จุดของตัวเอง, โหมดต่อแถว = จุดรอหน้าโต๊ะ</summary>
        private Vector3 ServePoint()
        {
            return targetCounter.CrowdMode ? targetCounter.GetQueuePosition(this) : targetCounter.CustomerWaitPoint.position;
        }

        /// <summary>
        /// ถืออาหารไว้เหนือหัว ระหว่างเดินไปจ่ายเงิน
        /// </summary>
        private void HoldDish(Item dish)
        {
            carriedDishes.Add(dish);
            dish.SetCarriedState(true);
            dish.transform.SetParent(transform, true);

            float lossy = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
            dish.transform.localScale = Vector3.one * (heldDishScale / lossy);
            dish.transform.localRotation = Quaternion.identity;

            float headY = 0.9f;
            if (spriteRenderer != null)
            {
                headY = (spriteRenderer.bounds.max.y - transform.position.y) + 0.15f;
            }

            // ถือให้เห็นจานเดียวเหนือหัว (ไม่ซ้อนสูง) จานที่เหลือซ่อนไว้ ดูจำนวนที่ป้าย
            for (int i = 0; i < carriedDishes.Count; i++)
            {
                Item d = carriedDishes[i];
                if (d == null) continue;
                d.transform.position = transform.position + new Vector3(0f, headY, -0.05f);
                if (d.SpriteRenderer != null)
                {
                    // จานใหญ่ขึ้นแล้วก้นจานไม่จมลงไปในหัว: ยกให้ก้นจานอยู่ไม่ต่ำกว่าขอบหัว - 0.1
                    float headTop = transform.position.y + headY - 0.15f;
                    float sink = (headTop - 0.1f) - d.SpriteRenderer.bounds.min.y;
                    if (sink > 0f) d.transform.position += new Vector3(0f, sink, 0f);
                    d.SpriteRenderer.enabled = i == 0;
                    if (spriteRenderer != null)
                    {
                        d.SpriteRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
                        d.SpriteRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
                    }
                }
            }

            UpdateCarryBadge(headY);
        }

        // ป้ายเหนือหัวบอกว่าถือกี่จาน (ไอคอนอาหาร + ตัวเลข แบบ Juice King)
        private GameObject carryBadgeRoot;
        private WorldCountBadge carryBadge;

        private void UpdateCarryBadge(float headY)
        {
            if (carriedDishes.Count == 0)
            {
                if (carryBadgeRoot != null) carryBadgeRoot.SetActive(false);
                return;
            }

            if (carryBadgeRoot == null)
            {
                int layer = spriteRenderer != null ? spriteRenderer.sortingLayerID : 0;
                carryBadgeRoot = new GameObject("CarryBadge");
                YSort.PutOnTop(carryBadgeRoot);   // ป้ายเหนือหัวอยู่บนสุดเสมอ
                carryBadgeRoot.transform.SetParent(transform, false);
                // โจร: ป้ายแดงตัวขาว (ของที่ขโมยมา) / ลูกค้า: ป้ายเหลืองตามปกติ
                carryBadge = isThief
                    ? WorldCountBadge.Create(carryBadgeRoot.transform, "Badge", new Vector2(0.72f, 0.36f),
                        new Color(0.9f, 0.2f, 0.22f, 1f), new Color(0.45f, 0.05f, 0.08f, 1f), Color.white, layer, 55)
                    : WorldCountBadge.Create(carryBadgeRoot.transform, "Badge", new Vector2(0.72f, 0.36f),
                        new Color(1f, 0.8f, 0.28f, 1f), new Color(0.42f, 0.26f, 0.12f, 1f), new Color(0.25f, 0.15f, 0.08f, 1f), layer, 55);
            }
            carryBadgeRoot.SetActive(true);
            WorldUi.KeepWorldScale(carryBadgeRoot.transform);
            // ป้ายอยู่ชิดเหนือจานที่ถือ (จานใหญ่/เล็กแค่ไหนก็ไม่ทับ)
            float badgeY = transform.position.y + headY + 0.42f;
            Item top = carriedDishes[0];
            if (top != null && top.SpriteRenderer != null) badgeY = top.SpriteRenderer.bounds.max.y + 0.22f;
            carryBadgeRoot.transform.position = new Vector3(transform.position.x, badgeY, transform.position.z - 0.1f);

            // โจรถือของ: ย้ายเครื่องหมาย ! ไปไว้ข้างซ้ายของป้าย ไม่ทับกัน (เห็นทั้งของที่ขโมยและรู้ว่าเป็นโจร)
            if (isThief && exclamationObject != null)
            {
                exclamationObject.transform.position = new Vector3(transform.position.x - 0.62f, badgeY, transform.position.z - 0.1f);
            }

            Item first = carriedDishes[0];
            Sprite icon = first != null && first.SpriteRenderer != null ? first.SpriteRenderer.sprite : null;
            Color iconColor = first != null && first.SpriteRenderer != null ? first.SpriteRenderer.color : Color.white;
            carryBadge.Set(icon, iconColor, carriedDishes.Count);
            carryBadge.Punch();
        }

        /// <summary>ย้อมสีตัวลูกค้าตามความอดทนที่เหลือ: ปกติช่วงแรก แล้วค่อยๆ ส้มแดงจนหมด</summary>
        private void UpdatePatienceTint()
        {
            if (spriteRenderer == null || isThief || hurtFlashCoroutine != null || patienceDuration <= 0f) return;
            float waited = 1f - Mathf.Clamp01(currentPatienceTimer / patienceDuration);
            float k = Mathf.InverseLerp(angryStartRatio, 1f, waited);
            spriteRenderer.color = Color.Lerp(defaultCustomerColor, angryColor, k);
        }

        private void ResetPatienceTint()
        {
            if (spriteRenderer == null || isThief || hurtFlashCoroutine != null) return;
            spriteRenderer.color = defaultCustomerColor;
        }

        private void GoToCashier()
        {
            targetCounter.LeaveQueue(this);
            HideWantBubble();
            cashier.JoinQueue(this);
            payTimer = 0f;
            currentState = CustomerState.WalkingToCashier;
        }

        /// <summary>
        /// ยืนหัวแถวที่โต๊ะจ่ายเงินครบเวลา -> จ่ายเงิน แล้วเดินกลับบ้านพร้อมอาหาร
        /// </summary>
        private void HandlePaying()
        {
            if (cashier == null) return;

            if (!cashier.IsHeadOfQueue(this)) return;
            Vector3 payPoint = cashier.CrowdMode ? cashier.GetQueuePosition(this) : cashier.CustomerPayPoint.position;
            if (Vector2.Distance(rb.position, payPoint) > 0.4f) return;

            // ต้องมีคนคิดเงิน (ผู้เล่นยืนในวง หรือพนักงานแคชเชียร์) ไม่งั้นยืนรอ
            float duration = Mathf.Max(0.01f, cashier.PayDuration);
            if (!cashier.CanServe)
            {
                cashier.ReportPayProgress(this, payTimer / duration, false);
                return;
            }

            payTimer += Time.deltaTime;
            cashier.ReportPayProgress(this, payTimer / duration, true);
            if (payTimer < duration) return;

            cashier.ReceivePayment(this, carriedDishes);
            foreach (Item d in carriedDishes)
            {
                if (d != null) Destroy(d.gameObject);
            }
            carriedDishes.Clear();
            if (carryBadgeRoot != null) carryBadgeRoot.SetActive(false);

            ShowHappy();
            currentState = CustomerState.WalkingToExit;
        }

        public void LeaveCounterAndExit()
        {
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
            HideWantBubble();
            currentState = CustomerState.WalkingToExit;
        }

        private GameObject wantBubbleObject;

        [Header("Order Bubble (ป้ายเมนูเหนือหัว)")]
        [Tooltip("รูปฟองความคิดหลังชื่อเมนู (ว่าง = ตัวหนังสือลอยๆ แบบเดิม)")]
        [SerializeField] private Sprite orderBubbleSprite;

        [Tooltip("ความกว้างฟอง (หน่วยโลก)")]
        [SerializeField] private float orderBubbleWidth = 1.5f;

        [Tooltip("สีชื่อเมนูในฟอง")]
        [SerializeField] private Color orderBubbleTextColor = new Color(0.33f, 0.24f, 0.36f, 1f);

        /// <summary>แสดงป้ายเมนูที่ลูกค้าอยากกินเหนือหัว</summary>
        private void ShowWantBubble()
        {
            if (wantBubbleObject == null)
            {
                wantBubbleObject = new GameObject("WantBubble");
                YSort.PutOnTop(wantBubbleObject);   // ป้ายเหนือหัวอยู่บนสุดเสมอ
                wantBubbleObject.transform.SetParent(transform, false);

                float headY = 1.0f;
                if (spriteRenderer != null)
                {
                    float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
                    headY = (spriteRenderer.bounds.max.y - transform.position.y) / scaleY + 0.4f;
                }
                wantBubbleObject.transform.localPosition = new Vector3(0f, headY, -0.1f);

                // ชื่อเมนูอยู่ในลูกของฟอง (ฟองเป็นรูป ตัวหนังสือวางทับ)
                GameObject textObj = wantBubbleObject;
                if (orderBubbleSprite != null)
                {
                    float s = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.x));
                    var bubble = new GameObject("Bubble");
                    bubble.transform.SetParent(wantBubbleObject.transform, false);
                    var bsr = bubble.AddComponent<SpriteRenderer>();
                    bsr.sprite = orderBubbleSprite;
                    bsr.sortingOrder = 49;
                    float w = orderBubbleSprite.bounds.size.x;
                    bubble.transform.localScale = Vector3.one * (w > 0.0001f ? orderBubbleWidth / w / s : 1f);
                    textObj = new GameObject("Text");
                    textObj.transform.SetParent(wantBubbleObject.transform, false);
                    // ให้ตัวหนังสืออยู่กลางส่วนฟองใหญ่ (หางฟองอยู่ซ้ายล่าง)
                    textObj.transform.localPosition = new Vector3(0.05f / s, 0.08f / s, 0f);
                }

                var tm = textObj.AddComponent<TextMesh>();
                tm.text = DishMenu.GetShortName(preferredDish);
                tm.fontSize = 48;
                tm.characterSize = orderBubbleSprite != null ? 0.05f : 0.07f;   // ในฟองตัวเล็กลงให้พอดีฟอง
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                tm.color = orderBubbleSprite != null ? orderBubbleTextColor : DishMenu.GetColor(preferredDish);

                Font font = GameFont.Get();
                if (font != null)
                {
                    tm.font = font;
                    var mr = textObj.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = font.material;
                        mr.sortingOrder = 50;
                    }
                }
            }
            wantBubbleObject.SetActive(true);
        }

        private void HideWantBubble()
        {
            if (wantBubbleObject != null) wantBubbleObject.SetActive(false);
        }

        /// <summary>
        /// ระบบรับการโจมตีจากผู้เล่น (IDamageable)
        /// ตีโดนเฉพาะโจรขโมยอาหาร ตัวจะกระพริบแดงทันทีเมื่อโดนตี และเมื่อ HP หมดจะทิ้งของ โยนเงิน แล้วรีบหนี
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!isThief) return; // ลูกค้าปกติจะไม่ได้รับดาเมจ
            if (thiefCaught) return;   // โดนตีไปแล้ว กำลังหนี ตีซ้ำไม่ได้อะไร

            thiefHealth -= damage;
            Debug.Log($"[CustomerAI] โจรโดนผู้เล่นตี! HP เหลือ: {thiefHealth}");

            // ตัวแดงตอนโดนตี
            FlashHurtColor();

            if (thiefHealth <= 0)
            {
                DropLootAndRunAway();
            }
        }

        /// <summary>
        /// ทำให้ตัวโจรเปลี่ยนเป็นสีแดงชั่วขณะเมื่อโดนตี
        /// </summary>
        private void FlashHurtColor()
        {
            if (spriteRenderer == null) return;

            if (hurtFlashCoroutine != null)
            {
                StopCoroutine(hurtFlashCoroutine);
            }
            hurtFlashCoroutine = StartCoroutine(HurtFlashRoutine());
        }

        private IEnumerator HurtFlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = hurtColor; // เปลี่ยนเป็นสีแดงสด
            }

            yield return new WaitForSeconds(hurtFlashDuration);

            if (spriteRenderer != null)
            {
                // คืนค่าสีเดิมของโจร
                spriteRenderer.color = isThief ? thiefColor : defaultCustomerColor;
            }

            hurtFlashCoroutine = null;
        }

        /// <summary>
        /// โจรโดนตี: ทิ้งอาหารที่ขโมยมา + โยนเงินตกพื้น แล้วรีบเดินหนีออกจากร้าน (ไม่สลบ ไม่ต้องแบกไปไหน)
        /// </summary>
        public void DropLootAndRunAway()
        {
            if (thiefCaught) return;
            thiefCaught = true;
            // ตีโจรโดนแล้ว: หยุดเสียงเตือน (ถ้ายังมีโจรตัวอื่นถือของขโมยอยู่ ปล่อยเสียงเล่นต่อ)
            ReleaseRobber();
            thiefWaitingForPlayer = false;
            if (targetCounter != null) targetCounter.LeaveQueue(this);

            HideExclamationMark();
            AudioManager.Play(SoundId.ThiefKnockout, null, transform.position);

            // อาหารที่ขโมยมาทั้งหมด ตกกระจายลงพื้นรอบตัวโจร ขนาดเท่าตอนถือบนหัว ให้ผู้เล่นเก็บกลับไปวางขายได้
            DropStolenDishes();

            // เงินรางวัลตกพื้น: ราคาอาหารที่แพงที่สุดตอนนี้ x 4 เด้งออกมาเป็นเหรียญรอบตัว (เสียงเดียวกับแคชเชียร์)
            // ตีโดนเมื่อไหร่ก็ได้เงิน (ทั้งก่อนและหลังขโมย)
            SpawnThiefReward();

            // รีบเดินหนีด้วยความเร็วเดินปกติ (ทางเดียวกับลูกค้าที่เดินกลับ / ผ่านคอแหลม)
            isAngry = true;
            currentState = CustomerState.WalkingToExit;
            Debug.Log("[CustomerAI] โจรโดนตี! ทิ้งของ โยนเงิน แล้วรีบหนีไป");
        }

        /// <summary>
        /// ทำอาหารที่โจรขโมยมาหล่นกระจายลงพื้นรอบตัว: เอาออกจากตัวโจร (ขนาดเท่าตอนถือบนหัว) เปิดรูป/Collider ให้เก็บได้
        /// </summary>
        private void DropStolenDishes()
        {
            int n = carriedDishes.Count;
            for (int i = 0; i < n; i++)
            {
                Item dish = carriedDishes[i];
                if (dish == null) continue;

                dish.transform.SetParent(null, true);
                float ang = (i / (float)Mathf.Max(1, n)) * Mathf.PI * 2f + Random.Range(-0.4f, 0.4f);
                float r = Random.Range(0.45f, 0.75f);
                dish.transform.position = transform.position + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
                dish.transform.rotation = Quaternion.identity;
                dish.gameObject.SetActive(true);
                if (dish.SpriteRenderer != null) dish.SpriteRenderer.enabled = true;   // จานที่ซ่อนไว้ (ถือหลายจาน) ให้เห็นทุกจาน
                dish.SetCarriedState(false);
                if (dish.TryGetComponent<Collider2D>(out var dishCol)) dishCol.enabled = true;
            }
            carriedDishes.Clear();
            if (carryBadgeRoot != null) carryBadgeRoot.SetActive(false);
        }

        /// <summary>ราคาขายต่อจานที่แพงที่สุดตอนนี้ (เฉพาะหม้อที่ปลดล็อกแล้ว) ไม่มีหม้อ = 10</summary>
        private static int HighestDishPrice()
        {
            int best = 0;
            foreach (CookingPot pot in CookingPot.AllPots)
            {
                if (pot != null && pot.IsUnlocked) best = Mathf.Max(best, pot.CurrentDishPrice);
            }
            return best > 0 ? best : 10;
        }

        /// <summary>
        /// เงินรางวัลตอนตีโจร: ราคาอาหารที่แพงที่สุด x thiefRewardMultiplier แบ่งเป็นเหรียญ (Prefab เดียวกับแคชเชียร์)
        /// เด้งออกจากตัวโจรไปตกรอบๆ แล้วผู้เล่นเดินเก็บ/ดูดเข้าตัวเหมือนเหรียญทั่วไป
        /// </summary>
        private void SpawnThiefReward()
        {
            int total = HighestDishPrice() * thiefRewardMultiplier;
            if (total <= 0) return;

            AudioManager.Play(SoundId.CustomerPay, null, transform.position);

            CashierCounter cc = cashier != null ? cashier : FindAnyObjectByType<CashierCounter>();
            GameObject coinPrefab = cc != null ? cc.CoinPrefab : null;
            if (coinPrefab == null)
            {
                // ไม่มี Prefab เหรียญ: ให้เงินเข้ากระเป๋าตรงๆ
                if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(total);
                return;
            }

            int lo = Mathf.Max(1, Mathf.Min(thiefRewardCoins.x, thiefRewardCoins.y));
            int hi = Mathf.Max(lo, Mathf.Max(thiefRewardCoins.x, thiefRewardCoins.y));
            int count = Mathf.Clamp(Random.Range(lo, hi + 1), 1, total);
            int share = total / count;
            int remainder = total % count;

            Vector3 from = transform.position + new Vector3(0f, 0.3f, 0f);
            for (int i = 0; i < count; i++)
            {
                GameObject coinObj = Instantiate(coinPrefab, from, Quaternion.identity);
                int value = share + (i < remainder ? 1 : 0);   // รวมทุกเหรียญได้ total พอดี
                if (!coinObj.TryGetComponent<CoinPickup>(out var pickup))
                {
                    if (MoneyManager.Instance != null) MoneyManager.Instance.AddMoney(value);
                    Destroy(coinObj);
                    continue;
                }
                pickup.Value = value;
                pickup.enabled = false;   // ปิดแม่เหล็ก/นับเวลาหายไว้ก่อน จนกว่าจะเด้งถึงพื้น

                float ang = (i / (float)count) * Mathf.PI * 2f + Random.Range(-0.35f, 0.35f);
                float r = Random.Range(0.4f, 1f);
                hoppingCoins.Add(pickup);
                hoppingCoinTargets.Add(transform.position + new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f));
            }

            if (hoppingCoins.Count > 0) StartCoroutine(CoinHopRoutine(from));
        }

        /// <summary>เหรียญเด้งเป็นวงโค้งจากตัวโจรไปตกจุดของตัวเอง</summary>
        private IEnumerator CoinHopRoutine(Vector3 from)
        {
            const float duration = 0.45f;
            const float height = 0.7f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duration);
                for (int i = 0; i < hoppingCoins.Count; i++)
                {
                    CoinPickup coin = hoppingCoins[i];
                    if (coin == null) continue;   // ผู้เล่นเก็บไปกลางอากาศแล้ว
                    Vector3 p = Vector3.Lerp(from, hoppingCoinTargets[i], k);
                    p.y += Mathf.Sin(k * Mathf.PI) * height;
                    coin.transform.position = p;
                }
                yield return null;
            }
            FinishCoinHops();
        }

        /// <summary>วางเหรียญที่ยังเด้งไม่เสร็จลงจุดตก แล้วเปิด CoinPickup (เรียกตอนเด้งจบ หรือโจรถูกปิด/ทำลายกลางทาง)</summary>
        private void FinishCoinHops()
        {
            for (int i = 0; i < hoppingCoins.Count; i++)
            {
                CoinPickup coin = hoppingCoins[i];
                if (coin == null) continue;
                coin.transform.position = hoppingCoinTargets[i];
                coin.enabled = true;
            }
            hoppingCoins.Clear();
            hoppingCoinTargets.Clear();
        }

        private void ShowExclamationMark()
        {
            if (exclamationObject == null)
            {
                exclamationObject = new GameObject("ExclamationMark");
                YSort.PutOnTop(exclamationObject);   // ป้ายเหนือหัวอยู่บนสุดเสมอ
                exclamationObject.transform.SetParent(transform, false);

                // วางไว้เหนือหัวโจรตามขนาดสไปรต์จริง
                float headY = 1.1f;
                if (spriteRenderer != null)
                {
                    float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
                    headY = (spriteRenderer.bounds.max.y - transform.position.y) / scaleY + 0.45f;
                }
                exclamationObject.transform.localPosition = new Vector3(0f, headY, -0.1f);

                var tm = exclamationObject.AddComponent<TextMesh>();
                tm.text = "!";
                tm.fontSize = 72;
                tm.characterSize = 0.1f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.15f, 0.15f, 1f);
                tm.fontStyle = FontStyle.Bold;

                // ต้องกำหนดฟอนต์/วัสดุ และ sorting order เอง ไม่งั้นตัวหนังสือจะมองไม่เห็นหรือถูกสไปรต์บัง
                Font font = GameFont.Get();
                if (font != null)
                {
                    tm.font = font;
                    var mr = exclamationObject.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = font.material;
                        mr.sortingOrder = 50;
                    }
                }
            }
            exclamationObject.SetActive(true);
        }

        private void HideExclamationMark()
        {
            if (exclamationObject != null)
            {
                exclamationObject.SetActive(false);
            }
        }

        private void ShowHappy()
        {
            if (happyIcon != null) happyIcon.SetActive(true);
        }

        private void ShowAngry()
        {
            if (angryIcon != null) angryIcon.SetActive(true);
        }

        /// <summary>เอาโจรตัวนี้ออกจากรายชื่อโจรที่ถือของขโมย ถ้าไม่เหลือแล้วหยุดเสียงเตือน (ตีโดน / หนีพ้น / ออกจากฉาก)</summary>
        private void ReleaseRobber()
        {
            if (activeRobbers.Remove(this) && ActiveRobberCount == 0 && UiAudioManager.Instance != null)
                UiAudioManager.Instance.StopThiefAlert();
        }

        private void OnReachedExit()
        {
            ReleaseRobber();
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
            if (spawnerReference != null)
            {
                spawnerReference.OnCustomerDespawned(this);
            }
            Destroy(gameObject);
        }

        private void OnDisable()
        {
            FinishCoinHops();
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
            if (cashier != null)
            {
                cashier.LeaveQueue(this);
            }
        }

        /// <summary>
        /// เพิกเฉยการชนระหว่างลูกค้าทุกคน เพื่อให้ลูกค้าและโจรสามารถเดินทะลุผ่านกันได้ 100% ไม่ติดขัด
        /// </summary>
        public void IgnoreCollisionWithOtherCustomers()
        {
            var myColliders = GetComponentsInChildren<Collider2D>();
#if UNITY_2023_1_OR_NEWER
            var allCustomers = Object.FindObjectsByType<CustomerAI>();
#else
            var allCustomers = FindObjectsOfType<CustomerAI>();
#endif
            foreach (var other in allCustomers)
            {
                if (other == null || other == this) continue;
                var otherColliders = other.GetComponentsInChildren<Collider2D>();
                foreach (var c1 in myColliders)
                {
                    foreach (var c2 in otherColliders)
                    {
                        if (c1 != null && c2 != null)
                        {
                            Physics2D.IgnoreCollision(c1, c2, true);
                        }
                    }
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            // ถ้าชนโดนลูกค้าหรือโจรตัวอื่น ให้ปิดการชนทันทีเพื่อเดินทะลุกันได้
            if (collision.gameObject.GetComponent<CustomerAI>() != null)
            {
                Physics2D.IgnoreCollision(collision.collider, collision.otherCollider, true);
            }
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (collision.gameObject.GetComponent<CustomerAI>() != null)
            {
                Physics2D.IgnoreCollision(collision.collider, collision.otherCollider, true);
            }
        }

        private void OnDestroy()
        {
            ReleaseRobber();
            // อาหารที่โจรขโมยหนีไปถึงทางออก เป็นลูกของตัวโจร หายไปพร้อมกัน
            FinishCoinHops();
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
            if (cashier != null)
            {
                cashier.LeaveQueue(this);
            }
        }
    }
}

namespace GI351.KitchenGame
{
    /// <summary>
    /// โซนร้าน (เกาะแรกที่มีแผงอาหาร โต๊ะจ่ายเงิน หม้อ และร้านค้า) ใช้เช็คว่าผู้เล่นอยู่ที่ร้านหรือออกไปล่าสัตว์
    /// กรอบคำนวณครั้งเดียวต่อฉาก (คำนวณใหม่เมื่อเปลี่ยนฉาก) จุดที่อยู่ในโซนล่าสัตว์ (HuntArea) ไม่นับเป็นโซนร้าน
    /// </summary>
    public static class ShopZone
    {
        private static bool computed;
        private static bool hasBounds;
        private static Bounds cachedBounds;
        private static UnityEngine.SceneManagement.Scene cachedScene;
        private static PlayerMovement player;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            computed = false;
            hasBounds = false;
            cachedScene = default;
            player = null;
        }

        /// <summary>ผู้เล่นอยู่ในโซนร้านหรือไม่ (ไม่มีผู้เล่น/ไม่มีของร้านในฉาก = ถือว่าอยู่ ไม่ให้โจรค้างรอตลอดไป)</summary>
        public static bool IsPlayerInside(float padding)
        {
            if (player == null) player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null) return true;
            return Contains(YSort.FeetOf(player.transform), padding);   // ใช้ตำแหน่งเท้าผู้เล่น (YSortActor) ไม่มี = ตำแหน่งตัว
        }

        /// <summary>จุดนี้อยู่ในโซนร้าน (ขยายขอบออกไป padding หน่วยโลก) หรือไม่</summary>
        public static bool Contains(Vector2 point, float padding)
        {
            if (!TryGetBounds(out Bounds b)) return true;
            Vector3 min = b.min;
            Vector3 max = b.max;
            if (point.x < min.x - padding || point.x > max.x + padding || point.y < min.y - padding || point.y > max.y + padding) return false;

            // ขอบที่ขยายออกไปล้นเข้าโซนล่าสัตว์ ไม่นับว่าอยู่ร้าน
            foreach (HuntArea area in HuntArea.All)
            {
                if (area != null && area.Contains(point)) return false;
            }
            return true;
        }

        private static bool TryGetBounds(out Bounds bounds)
        {
            UnityEngine.SceneManagement.Scene scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!computed || scene != cachedScene)
            {
                cachedScene = scene;
                computed = true;
                hasBounds = false;
                Encapsulate(Object.FindObjectsByType<FoodSellCounter>(FindObjectsInactive.Include));
                Encapsulate(Object.FindObjectsByType<CashierCounter>(FindObjectsInactive.Include));
                Encapsulate(Object.FindObjectsByType<CookingPot>(FindObjectsInactive.Include));
                Encapsulate(Object.FindObjectsByType<ShopStall>(FindObjectsInactive.Include));
            }
            bounds = cachedBounds;
            return hasBounds;
        }

        private static void Encapsulate(Component[] parts)
        {
            if (parts == null) return;
            foreach (Component c in parts)
            {
                if (c == null) continue;
                // ร้านค้า/หม้อที่ตั้งอยู่ในโซนล่าสัตว์ ไม่ใช่ส่วนของร้าน
                if (InsideHuntArea(c.transform.position)) continue;

                bool any = false;
                if (c.gameObject.activeInHierarchy)
                {
                    // ใช้ Collider ที่เป็นของแข็งก่อน (ไม่นับวงแหวน Trigger ที่กว้าง) ถ้าไม่มีค่อยใช้รูป
                    foreach (Collider2D col in c.GetComponentsInChildren<Collider2D>())
                    {
                        if (col == null || !col.enabled || col.isTrigger) continue;
                        Add(col.bounds);
                        any = true;
                    }
                    if (!any)
                    {
                        foreach (SpriteRenderer sr in c.GetComponentsInChildren<SpriteRenderer>())
                        {
                            if (sr == null || !sr.enabled || sr.sprite == null) continue;
                            Add(sr.bounds);
                            any = true;
                        }
                    }
                }
                if (!any) Add(new Bounds(c.transform.position, Vector3.zero));
            }
        }

        private static void Add(Bounds b)
        {
            if (!hasBounds)
            {
                cachedBounds = b;
                hasBounds = true;
            }
            else
            {
                cachedBounds.Encapsulate(b);
            }
        }

        private static bool InsideHuntArea(Vector2 point)
        {
            foreach (HuntArea area in HuntArea.All)
            {
                if (area != null && area.Contains(point)) return true;
            }
            return false;
        }
    }
}
