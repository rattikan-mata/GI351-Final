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

        [Header("Obstacle Avoidance (เดินอ้อมโต๊ะ/หม้อ ไม่เดินทะลุ)")]
        [Tooltip("รัศมีตัวลูกค้าที่ใช้เช็คว่าทางเดินชนสิ่งกีดขวางหรือไม่")]
        [SerializeField] private float avoidanceRadius = 0.4f;

        [Tooltip("ระยะเผื่อจากมุมสิ่งกีดขวางตอนเดินอ้อม (เมตร)")]
        [SerializeField] private float avoidanceMargin = 0.15f;

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

        // จุดอ้อมสิ่งกีดขวางที่กำลังเดินไป (มุมของโต๊ะ/หม้อ)
        private bool hasDetour;
        private Vector2 detourPoint;
        private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
        private ContactFilter2D obstacleFilter;

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

            // ตรวจเฉพาะ Collider ที่เป็นของแข็ง (ไม่นับวงแหวน/ไอเทมที่เป็น Trigger)
            obstacleFilter = new ContactFilter2D();
            obstacleFilter.useTriggers = false;

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
                hasDetour = false;
                onArrived?.Invoke();
                return;
            }

            // ถ้าทางตรงชนโต๊ะ/หม้อ ให้เดินไปมุมของสิ่งกีดขวางก่อน
            Vector2 steerTarget = GetSteerTarget(currentPos, destination);
            Vector2 vel = (steerTarget - currentPos).normalized * moveSpeed;
            rb.linearVelocity = vel;

            if (spriteRenderer != null)
            {
                if (vel.x > 0.05f) spriteRenderer.flipX = false;
                else if (vel.x < -0.05f) spriteRenderer.flipX = true;
            }
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
