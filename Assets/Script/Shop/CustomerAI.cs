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
        }

        private void Start()
        {
            if (wantSpecificFood)
            {
                // สุ่มว่าจะอยากกินไก่ต้ม หรือ หมูต้ม
                preferredDish = Random.value > 0.5f ? ItemType.CookedChicken : ItemType.CookedPig;
            }

            currentPatienceTimer = patienceDuration;
        }

        public void Init(FoodSellCounter counter, Transform exit, CustomerSpawner spawner)
        {
            targetCounter = counter;
            exitPoint = exit;
            spawnerReference = spawner;
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
                        MoveTowards(targetCounter.CustomerWaitPoint.position, OnReachedCounter);
                    }
                    break;

                case CustomerState.WalkingToExit:
                    Vector3 exitPos = exitPoint != null ? exitPoint.position : transform.position + Vector3.down * 10f;
                    MoveTowards(exitPos, OnReachedExit);
                    break;

                case CustomerState.WaitingAtCounter:
                    rb.linearVelocity = Vector2.zero;
                    break;
            }
        }

        private void MoveTowards(Vector3 destination, System.Action onArrived)
        {
            Vector2 currentPos = rb.position;
            Vector2 dir = (Vector2)destination - currentPos;
            float dist = dir.magnitude;

            if (dist <= 0.15f)
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

        private void OnReachedCounter()
        {
            currentState = CustomerState.WaitingAtCounter;
            currentPatienceTimer = patienceDuration;
        }

        private void HandleWaiting()
        {
            // ตรวจสอบว่าที่โต๊ะมีอาหารที่ต้องการหรือไม่
            if (targetCounter != null && targetCounter.HasFoodAvailable(wantSpecificFood ? preferredDish : (ItemType?)null))
            {
                // ซื้ออาหาร
                Item dish = targetCounter.CustomerBuyDish(wantSpecificFood ? preferredDish : (ItemType?)null);
                if (dish != null)
                {
                    // จ่ายเงินให้ผู้เล่นตามราคาขายของอาหารชิ้นนั้น
                    if (MoneyManager.Instance != null)
                    {
                        MoneyManager.Instance.AddMoney(dish.SellPrice);
                    }

                    Destroy(dish.gameObject); // กินอาหาร

                    ShowHappy();
                    currentState = CustomerState.WalkingToExit;
                    return;
                }
            }

            // นับเวลาความอดทน
            currentPatienceTimer -= Time.deltaTime;
            if (currentPatienceTimer <= 0f)
            {
                // หมดความอดทน เดินกลับแบบโมโห
                ShowAngry();
                currentState = CustomerState.WalkingToExit;
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

        private void OnReachedExit()
        {
            if (spawnerReference != null)
            {
                spawnerReference.OnCustomerDespawned(this);
            }
            Destroy(gameObject);
        }
    }
}
