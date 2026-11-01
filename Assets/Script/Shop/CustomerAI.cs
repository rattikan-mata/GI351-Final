using UnityEngine;

namespace GI351.KitchenGame
{
    public enum CustomerState
    {
        WalkingToCounter,
        WaitingAtCounter,
        WalkingToExit
    }

    [RequireComponent(typeof(Rigidbody2D))]
    public class CustomerAI : MonoBehaviour
    {
        [Header("Customer Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเร็วในการเดินของลูกค้า")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("ความอดทนในการยืนรออาหาร (วินาที)")]
        [SerializeField] private float patienceDuration = 15f;

        [Tooltip("สุ่มอาหารที่อยากกิน (CookedChicken หรือ CookedPig หรือ กินอะไรก็ได้)")]
        [SerializeField] private bool wantSpecificFood = true;

        [SerializeField] private ItemType preferredDish = ItemType.CookedChicken;

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

        private Rigidbody2D rb;
        private CustomerState currentState = CustomerState.WalkingToCounter;
        private float currentPatienceTimer;
        private CustomerSpawner spawnerReference;
        private int lastQueueIndex = -1;

        public CustomerState State => currentState;
        public ItemType PreferredDish => preferredDish;
        public float RemainingPatience => currentPatienceTimer;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            rb.gravityScale = 0f;
            rb.freezeRotation = true;

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
            if (wantSpecificFood)
            {
                // สุ่มว่าจะอยากกินไก่ต้ม หรือ หมูต้ม
                preferredDish = Random.value > 0.5f ? ItemType.CookedChicken : ItemType.CookedPig;
            }

            currentPatienceTimer = patienceDuration;

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

        public void Init(FoodSellCounter counter, Transform exit, CustomerSpawner spawner)
        {
            targetCounter = counter;
            exitPoint = exit;
            spawnerReference = spawner;

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
            if (currentState == CustomerState.WaitingAtCounter)
            {
                HandleWaiting();
            }
        }

        private void FixedUpdate()
        {
            switch (currentState)
            {
                case CustomerState.WalkingToCounter:
                    if (targetCounter != null)
                    {
                        // เดินไปยังตำแหน่งในแถวของตนเอง
                        Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                        MoveTowards(targetSlotPos, OnReachedCounter);
                    }
                    break;

                case CustomerState.WalkingToExit:
                    Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + Vector3.down * 10f;
                    MoveTowards(exitPos, OnReachedExit);
                    break;

                case CustomerState.WaitingAtCounter:
                    if (targetCounter != null)
                    {
                        Vector3 targetSlotPos = targetCounter.GetQueuePosition(this);
                        float distToSlot = Vector2.Distance(rb.position, targetSlotPos);

                        // ถ้าตำแหน่งในแถวขยับไปข้างหน้า ให้เดินตามไปที่ช่องใหม่
                        if (distToSlot > 0.12f)
                        {
                            MoveTowards(targetSlotPos, () =>
                            {
                                if (targetCounter != null) FaceTowards(targetCounter.CustomerWaitPoint.position);
                            });
                        }
                        else
                        {
                            rb.linearVelocity = Vector2.zero;
                            FaceTowards(targetCounter.CustomerWaitPoint.position);
                        }

                        // ตรวจสอบว่าแถวขยับเลื่อนขึ้นหรือไม่ (ถ้าขยับเลื่อนขึ้น รีเซ็ตความอดทนให้ใหม่)
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
            }
        }

        private void MoveTowards(Vector3 destination, System.Action onArrived)
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

            Vector2 vel = dir.normalized * moveSpeed;
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

        private void HandleWaiting()
        {
            if (targetCounter == null) return;

            // ลูกค้าจะซื้ออาหารได้เฉพาะเมื่อ:
            // 1. เป็นหัวแถว (ลำดับที่ 0 ของคิว)
            // 2. เดินมาใกล้จุดรอหน้าโต๊ะแล้ว (ระยะห่าง <= 0.35m)
            if (targetCounter.IsHeadOfQueue(this))
            {
                float distToWaitPoint = Vector2.Distance(rb.position, targetCounter.CustomerWaitPoint.position);
                if (distToWaitPoint <= 0.35f && targetCounter.HasFoodAvailable(wantSpecificFood ? preferredDish : (ItemType?)null))
                {
                    // ซื้ออาหาร (จุดขายจะจัดการหักอาหารและดีดเงินออกมาที่วงเงินออกให้อัตโนมัติ)
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
                // หมดความอดทน เดินกลับแบบโมโห
                ShowAngry();
                LeaveCounterAndExit();
            }
        }

        public void LeaveCounterAndExit()
        {
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
            currentState = CustomerState.WalkingToExit;
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
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
        }

        private void OnDestroy()
        {
            if (targetCounter != null)
            {
                targetCounter.LeaveQueue(this);
            }
        }
    }
}
