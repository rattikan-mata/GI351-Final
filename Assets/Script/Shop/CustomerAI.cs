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
        Unconscious      // โดนตีสลบแล้ว (สามารถยกขึ้นหลังได้)
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CustomerAI : MonoBehaviour, IDamageable
    {
        [Header("Customer Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเร็วในการเดินของลูกค้าปกติ")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("ความอดทนในการยืนรออาหาร (วินาที)")]
        [SerializeField] private float patienceDuration = 15f;

        [Tooltip("สุ่มอาหารที่อยากกิน (CookedChicken หรือ CookedPig หรือ กินอะไรก็ได้)")]
        [SerializeField] private bool wantSpecificFood = true;

        [SerializeField] private ItemType preferredDish = ItemType.CookedChicken;

        [Header("Thief Settings (โจรแซงคิว)")]
        [Tooltip("สถานะลูกค้าเป็นโจรแซงคิวหรือไม่")]
        [SerializeField] private bool isThief = false;

        [Tooltip("พลังชีวิตของโจร (กระบองปกติทำ 20 ดาเมจ ตี 1 ครั้งจะสลบทันที)")]
        [SerializeField] private int thiefHealth = 20;

        [Tooltip("ตัวคูณความเร็วในการวิ่งหนีของโจร")]
        [SerializeField] private float thiefFleeSpeedMultiplier = 2.2f;

        [Header("Target & Path References")]
        [SerializeField] private FoodSellCounter targetCounter;
        [SerializeField] private Transform exitPoint;

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

        // โจรและไอเทมที่ถูกขโมย
        private Item stolenDish;
        private GameObject exclamationObject;
        private GameObject dizzyObject;

        public CustomerState State => currentState;
        public ItemType PreferredDish => preferredDish;
        public float RemainingPatience => currentPatienceTimer;
        public bool IsThief => isThief;
        public bool IsUnconscious => currentState == CustomerState.Unconscious;

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
        }

        private void Start()
        {
            if (patienceDuration > 60f)
            {
                patienceDuration = 15f;
            }
            currentPatienceTimer = patienceDuration;

            if (wantSpecificFood && !isThief)
            {
                // สุ่มเมนูที่อยากกิน เฉพาะเมนูที่เครื่องปลดล็อกแล้ว (โจรกินได้ทุกอย่างจึงไม่ต้องสุ่ม)
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

            // เพิกเฉยการชนระหว่างลูกค้าทุกคน เพื่อให้โจรเดินทะลุลูกค้าแซงคิวได้เสมอ
            IgnoreCollisionWithOtherCustomers();

            if (isThief)
            {
                ApplyThiefAppearance();
                // โจรจะไม่ต่อคิว แต่จะเดินตรงไปที่หน้าสุดของแถวคิวทันที
                currentState = CustomerState.WalkingToCounter;
            }
            else if (targetCounter != null && currentState == CustomerState.WalkingToCounter)
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

            if (isThief)
            {
                ApplyThiefAppearance();
                currentState = CustomerState.WalkingToCounter;
            }
            else if (targetCounter != null)
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
            if (currentState == CustomerState.Unconscious) return;

            if (currentState == CustomerState.WaitingAtCounter)
            {
                HandleWaiting();
            }
        }

        private void FixedUpdate()
        {
            if (currentState == CustomerState.Unconscious)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            switch (currentState)
            {
                case CustomerState.WalkingToCounter:
                    if (targetCounter != null)
                    {
                        if (isThief)
                        {
                            // ลงทะเบียนแซงคิว เพื่อให้ลูกค้าในแถวขยับถอยหลังไปหนึ่งช่อง
                            targetCounter.RegisterThief(this);

                            // โจรแซงคิว: วิ่งตรงไปยังหัวแถว (ช่องแรก) ส่วนลูกค้าถอยหลังให้
                            Vector3 frontPos = targetCounter.CustomerWaitPoint.position;

                            MoveTowards(frontPos, moveSpeed * 1.35f, OnThiefReachedCounter);

                            // หากวิ่งเข้ามาใกล้โต๊ะแล้ว และมีอาหารอยู่ ให้ฉกทันที
                            float distToWait = Vector2.Distance(rb.position, frontPos);
                            if (distToWait <= 0.45f && targetCounter.DishesCount > 0)
                            {
                                ThiefSnatchFoodAndFlee();
                            }
                        }
                        else
                        {
                            // ลูกค้าปกติ: เดินเข้าประจำช่องในแถวคิว
                            Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                            MoveTowards(targetSlotPos, moveSpeed, OnReachedCounter);
                        }
                    }
                    break;

                case CustomerState.WaitingAtCounter:
                    if (isThief)
                    {
                        // โจรยืนรอหน้าสุดของแถวคิว ถ้ามีอาหารวางลงเมื่อไหร่ ให้ฉกแล้ววิ่งหนีทันที
                        if (targetCounter != null)
                        {
                            Vector3 frontPos = targetCounter.CustomerWaitPoint.position;
                            rb.linearVelocity = Vector2.zero;
                            rb.position = Vector2.MoveTowards(rb.position, frontPos, Time.fixedDeltaTime * 3f);
                            FaceTowards(targetCounter.CustomerWaitPoint.position);

                            if (targetCounter.DishesCount > 0)
                            {
                                ThiefSnatchFoodAndFlee();
                            }
                        }
                    }
                    else if (targetCounter != null)
                    {
                        Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                        float distToSlot = Vector2.Distance(rb.position, targetSlotPos);

                        if (distToSlot > 0.12f)
                        {
                            MoveTowards(targetSlotPos, moveSpeed, () =>
                            {
                                if (targetCounter != null) FaceTowards(targetCounter.CustomerWaitPoint.position);
                            });
                        }
                        else
                        {
                            rb.linearVelocity = Vector2.zero;
                            FaceTowards(targetCounter.CustomerWaitPoint.position);
                        }

                        int currentQueueIndex = targetCounter.GetQueueIndex(this);
                        if (currentQueueIndex != -1 && currentQueueIndex < lastQueueIndex)
                        {
                            currentPatienceTimer = patienceDuration;
                            lastQueueIndex = currentQueueIndex;
                        }
                    }
                    else
                    {
                        rb.linearVelocity = Vector2.zero;
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
                onArrived?.Invoke();
                return;
            }

            Vector2 vel = dir.normalized * speed;
            rb.linearVelocity = vel;

            if (spriteRenderer != null)
            {
                if (vel.x > 0.05f) spriteRenderer.flipX = false;
                else if (vel.x < -0.05f) spriteRenderer.flipX = true;
            }
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

        private void OnThiefReachedCounter()
        {
            currentState = CustomerState.WaitingAtCounter;
            if (targetCounter != null)
            {
                FaceTowards(targetCounter.CustomerWaitPoint.position);
                if (targetCounter.DishesCount > 0)
                {
                    ThiefSnatchFoodAndFlee();
                }
            }
        }

        /// <summary>
        /// โจรฉกอาหารบนโต๊ะโดยไม่จ่ายเงิน แล้วขึ้นเครื่องหมายตกใจพร้อมวิ่งหนี
        /// </summary>
        private void ThiefSnatchFoodAndFlee()
        {
            if (targetCounter == null) return;

            targetCounter.UnregisterThief(this);
            stolenDish = targetCounter.ThiefStealDish(this);
            if (stolenDish != null)
            {
                // ซ่อนอาหารที่ฉกไว้บนตัวโจร
                stolenDish.transform.SetParent(transform);
                stolenDish.transform.localPosition = new Vector3(0f, 0.2f, -0.05f);
                if (stolenDish.TryGetComponent<Collider2D>(out var dishCol)) dishCol.enabled = false;
            }

            ShowExclamationMark();
            currentState = CustomerState.ThiefFleeing;
            Debug.Log("[CustomerAI] โจรฉกอาหารไปแล้ว! วิ่งหนีเร็ว!");
        }

        private void HandleWaiting()
        {
            if (targetCounter == null) return;

            // กรณีลูกค้าปกติ
            if (!isThief)
            {
                if (targetCounter.IsHeadOfQueue(this))
                {
                    float distToWaitPoint = Vector2.Distance(rb.position, targetCounter.CustomerWaitPoint.position);
                    if (distToWaitPoint <= 0.4f && targetCounter.HasFoodAvailable(wantSpecificFood ? preferredDish : (ItemType?)null))
                    {
                        Item dish = targetCounter.CustomerBuyDish(wantSpecificFood ? preferredDish : (ItemType?)null);
                        if (dish != null)
                        {
                            ShowHappy();
                            LeaveCounterAndExit();
                            return;
                        }
                    }
                }

                // นับเวลาความอดทน
                currentPatienceTimer -= Time.deltaTime;
                if (currentPatienceTimer <= 0f)
                {
                    // หมดความอดทน โมโหแล้วเดินหนี
                    isAngry = true;
                    ShowAngry();
                    LeaveCounterAndExit();
                }
            }
        }

        public void LeaveCounterAndExit()
        {
            if (targetCounter != null && !isThief)
            {
                targetCounter.LeaveQueue(this);
            }
            HideWantBubble();
            currentState = CustomerState.WalkingToExit;
        }

        private GameObject wantBubbleObject;

        /// <summary>แสดงป้ายเมนูที่ลูกค้าอยากกินเหนือหัว</summary>
        private void ShowWantBubble()
        {
            if (wantBubbleObject == null)
            {
                wantBubbleObject = new GameObject("WantBubble");
                wantBubbleObject.transform.SetParent(transform, false);

                float headY = 1.0f;
                if (spriteRenderer != null)
                {
                    float scaleY = Mathf.Max(0.0001f, Mathf.Abs(transform.lossyScale.y));
                    headY = (spriteRenderer.bounds.max.y - transform.position.y) / scaleY + 0.4f;
                }
                wantBubbleObject.transform.localPosition = new Vector3(0f, headY, -0.1f);

                var tm = wantBubbleObject.AddComponent<TextMesh>();
                tm.text = DishMenu.GetShortName(preferredDish);
                tm.fontSize = 48;
                tm.characterSize = 0.07f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                tm.color = DishMenu.GetColor(preferredDish);

                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    tm.font = font;
                    var mr = wantBubbleObject.GetComponent<MeshRenderer>();
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
        /// ตีโดนเฉพาะโจรขโมยอาหาร ตัวจะกระพริบแดงทันทีเมื่อโดนตี และเมื่อ HP หมดจะสลบ
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (!isThief) return; // ลูกค้าปกติจะไม่ได้รับดาเมจ
            if (currentState == CustomerState.Unconscious) return;

            thiefHealth -= damage;
            Debug.Log($"[CustomerAI] โจรโดนผู้เล่นตี! HP เหลือ: {thiefHealth}");

            // ตัวแดงตอนโดนตี
            FlashHurtColor();

            if (thiefHealth <= 0)
            {
                KnockOut();
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
        /// โจรถูกตีจนสลบ: หยุดการเคลื่อนที่ นอนราบกับพื้น และกลายเป็นไอเทมที่ยกขึ้นหลังได้
        /// </summary>
        public void KnockOut()
        {
            currentState = CustomerState.Unconscious;
            if (targetCounter != null) targetCounter.UnregisterThief(this);
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;

            HideExclamationMark();
            ShowDizzyIndicator();

            // พลิกตัวนอนราบลงกับพื้น
            transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            // ถ้ามีอาหารที่ขโมยมา ให้ตกหล่นลงพื้นข้างตัวโจร
            if (stolenDish != null)
            {
                stolenDish.transform.SetParent(null);
                stolenDish.transform.position = transform.position + Vector3.right * 0.6f;
                stolenDish.gameObject.SetActive(true);
                if (stolenDish.TryGetComponent<Collider2D>(out var dishCol)) dishCol.enabled = true;
                if (stolenDish.TryGetComponent<Item>(out var dishItem)) dishItem.SetCarriedState(false);
                stolenDish = null;
            }

            // ติดตั้ง Item component ให้ตัวโจรกลายเป็นไอเทมประเภท UnconsciousThief เพื่อให้ PlayerCarry ยกขึ้นหลังได้
            Item itemComp = GetComponent<Item>();
            if (itemComp == null)
            {
                itemComp = gameObject.AddComponent<Item>();
            }
            itemComp.SetItemType(ItemType.UnconsciousThief);
            itemComp.SetItemName("Unconscious Thief");
            itemComp.SetSellPrice(0); // เอาไปต้มทำอาหารพิเศษ

            // เปิด Trigger Collider เพื่อให้เดินชนแล้วเก็บขึ้นหลังได้
            var col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.enabled = true;
                col.isTrigger = true;
                col.radius = 0.5f;
            }

            Debug.Log("[CustomerAI] โจรโดนตีสลบแล้ว! สามารถยกขึ้นหลังไปยัดใส่หม้อต้มได้!");
        }

        private void ShowExclamationMark()
        {
            if (exclamationObject == null)
            {
                exclamationObject = new GameObject("ExclamationMark");
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
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

        private void ShowDizzyIndicator()
        {
            if (dizzyObject == null)
            {
                dizzyObject = new GameObject("DizzyIndicator");
                dizzyObject.transform.SetParent(transform);
                dizzyObject.transform.localPosition = new Vector3(0f, 0.8f, -0.1f);
                var tm = dizzyObject.AddComponent<TextMesh>();
                tm.text = "💫";
                tm.fontSize = 52;
                tm.characterSize = 0.06f;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.color = new Color(1f, 0.9f, 0.2f, 1f);
                tm.fontStyle = FontStyle.Bold;
            }
            dizzyObject.SetActive(true);
        }

        private void ShowHappy()
        {
            if (happyIcon != null) happyIcon.SetActive(true);
        }

        private void ShowAngry()
        {
            if (angryIcon != null) angryIcon.SetActive(true);
        }

        private void OnReachedExit()
        {
            if (targetCounter != null && !isThief)
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
            if (targetCounter != null && !isThief)
            {
                targetCounter.LeaveQueue(this);
            }
        }

        /// <summary>
        /// เพิกเฉยการชนระหว่างลูกค้าทุกคน เพื่อให้ลูกค้าและโจรสามารถเดินทะลุผ่านกันได้ 100% ไม่ติดขัด
        /// </summary>
        public void IgnoreCollisionWithOtherCustomers()
        {
            var myColliders = GetComponentsInChildren<Collider2D>();
#if UNITY_2023_1_OR_NEWER
            var allCustomers = Object.FindObjectsByType<CustomerAI>(FindObjectsSortMode.None);
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
            if (targetCounter != null && !isThief)
            {
                targetCounter.LeaveQueue(this);
            }
            if (targetCounter != null && isThief)
            {
                targetCounter.UnregisterThief(this);
            }
        }
    }
}
