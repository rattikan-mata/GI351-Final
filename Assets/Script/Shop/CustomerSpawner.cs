using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    public class CustomerSpawner : MonoBehaviour
    {
        [Header("Customer Spawner Settings (ปรับได้ใน Inspector)")]
        [Tooltip("Prefab ของลูกค้าปกติที่มีสคริปต์ CustomerAI")]
        [SerializeField] private GameObject customerPrefab;

        [Tooltip("Prefab สำหรับโจรโดยเฉพาะ (ถ้ากำหนดไว้จะใช้ตัวนี้เสกโจร หากเว้นว่างจะเสกจาก customerPrefab แล้วตั้งเป็นโจร)")]
        [SerializeField] private GameObject thiefPrefab;

        [Tooltip("ระยะเวลาระหว่างการเสกลูกค้าแต่ละคน (วินาที)")]
        [SerializeField] private float spawnInterval = 10f;

        [Tooltip("จำนวนลูกค้าสูงสุดที่อยู่ในฉากพร้อมกัน")]
        [SerializeField] private int maxConcurrentCustomers = 3;

        [Tooltip("โอกาสที่จะเสกเป็นลูกค้าโจรแซงคิว กินไม่จ่าย (เช่น 0.3 คือ 30%)")]
        [Range(0f, 1f)]
        [SerializeField] private float thiefSpawnChance = 0.3f;

        [Header("Thief Limits (จำกัดการเกิดของโจร)")]
        [Tooltip("จำนวนโจรสูงสุดที่อยู่ในฉากพร้อมกัน (0 = ไม่ให้มีโจรเลย)")]
        [Min(0)]
        [SerializeField] private int maxConcurrentThieves = 1;

        [Tooltip("ระยะเวลาขั้นต่ำระหว่างโจรแต่ละตัว นับจากตอนที่โจรตัวก่อนเกิด (วินาที)")]
        [Min(0f)]
        [SerializeField] private float thiefCooldown = 25f;

        [Tooltip("ไม่มีโจรในช่วงแรกของเกม (วินาที) ให้ผู้เล่นตั้งตัวก่อน")]
        [Min(0f)]
        [SerializeField] private float firstThiefDelay = 30f;

        [Tooltip("จำนวนโจรสูงสุดทั้งเกม (0 = ไม่จำกัด)")]
        [Min(0)]
        [SerializeField] private int maxThievesPerGame = 0;

        [Header("Waypoints")]
        [Tooltip("จุดเกิดของลูกค้า (ถ้าว่างจะใช้ตำแหน่งของ Spawner นี้)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("จุดที่ลูกค้าจะเดินกลับไปแล้วหายไป (ถ้าว่างจะใช้จุดเกิด)")]
        [SerializeField] private Transform exitPoint;

        [Tooltip("คอแหลมที่ต่อจากจุดเกิดไปเกาะร้าน: ลูกค้าที่จะข้ามระหว่างฝั่งต่ำกว่าจุดนี้กับฝั่งสูงกว่า ต้องเดินผ่านจุดนี้ก่อน (ไม่เดินตัดน้ำ) ว่าง = เดินตรง")]
        [SerializeField] private Transform pathGate;

        [Tooltip("โต๊ะขายอาหารที่ลูกค้าจะเดินไป")]
        [SerializeField] private FoodSellCounter targetCounter;

        [Header("Order Size (ลูกค้าซื้อคนละกี่จาน)")]
        [Tooltip("ลูกค้าแต่ละคนซื้อสุ่ม 1 ถึงค่านี้ (อัปเกรดได้จากร้าน Big Orders)")]
        [Min(1)] [SerializeField] private int maxDishesPerCustomer = 1;

        [Header("Start (เริ่มปล่อยลูกค้า)")]
        [Tooltip("เปิด = ลูกค้าเริ่มมาเมื่อมีอาหารวางบนแผงครั้งแรก (เปิดเกมมาร้านว่าง) / ปิด = มาตั้งแต่เริ่มเกม")]
        [SerializeField] private bool waitForFirstFood = true;

        [Header("Rhythm (จังหวะการมา: เฉลี่ยตามคนต่อนาที แต่สุ่มมาถี่บ้างเว้นบ้าง)")]
        [Tooltip("โอกาสที่คนถัดไปจะมาติดๆ กัน (มารัว)")]
        [Range(0f, 1f)] [SerializeField] private float burstChance = 0.3f;

        [Tooltip("ช่วงห่างตอนมารัว เทียบกับช่วงปกติ (0.15-0.4 = เร็วกว่าปกติมาก)")]
        [SerializeField] private Vector2 burstGap = new Vector2(0.15f, 0.4f);

        [Tooltip("โอกาสที่จะเว้นว่างนานกว่าปกติ")]
        [Range(0f, 1f)] [SerializeField] private float lullChance = 0.2f;

        [Tooltip("ช่วงห่างตอนเว้น เทียบกับช่วงปกติ")]
        [SerializeField] private Vector2 lullGap = new Vector2(1.6f, 2.3f);

        [Tooltip("ช่วงห่างตอนปกติ เทียบกับช่วงปกติ")]
        [SerializeField] private Vector2 normalGap = new Vector2(0.9f, 1.4f);

        [Header("Adaptive Rate (ลูกค้ามาตามของบนแผง) ปิดไว้: ลูกค้ามาตามเวลาเสมอ ของน้อยก็ยังมา ให้มีคนยืนรอ")]
        [Tooltip("เปิด = ลูกค้ามาถี่ขึ้นตอนแผงมีของเหลือ และมาช้าลงตอนแผงว่าง (ลดคนรอจนเดินหนี)")]
        [SerializeField] private bool adaptToStock = false;

        [Tooltip("ตัวคูณความถี่ตอนแผงไม่มีของเหลือให้ลูกค้าใหม่ (0.35 = มาช้าลงเหลือราว 1/3)")]
        [Range(0.05f, 1f)] [SerializeField] private float emptyRateMultiplier = 0.35f;

        [Tooltip("ตัวคูณความถี่ตอนแผงมีของเหลือเยอะ (2.5 = มาถี่ขึ้น 2.5 เท่า)")]
        [Range(1f, 4f)] [SerializeField] private float fullRateMultiplier = 2.5f;

        [Tooltip("ของเหลือกี่จาน (หลังหักจานที่ลูกค้าที่รออยู่จะซื้อ) ถึงจะมาถี่สุด")]
        [Min(2)] [SerializeField] private int busyStock = 6;

        private readonly List<CustomerAI> activeCustomers = new List<CustomerAI>();
        private float spawnTimer;
        private float nextGap = -1f;
        private bool firstFoodSeen;

        /// <summary>คอแหลมที่ลูกค้าต้องเดินผ่านระหว่างจุดเกิดกับเกาะร้าน (null = ไม่มี)</summary>
        public Transform PathGate => pathGate;

        /// <summary>สุ่มช่วงห่างถึงคนถัดไป: มารัว / เว้น / ปกติ เฉลี่ยแล้วราว spawnInterval</summary>
        private float RollGap()
        {
            float r = Random.value;
            Vector2 range = r < burstChance ? burstGap : (r < burstChance + lullChance ? lullGap : normalGap);
            return spawnInterval * Random.Range(range.x, range.y);
        }

        /// <summary>ความถี่ตอนนี้เทียบกับค่าปกติ (ตามของบนแผง)</summary>
        public float CurrentRateMultiplier { get; private set; } = 1f;

        /// <summary>จำนวนลูกค้าปกติที่เลือกเมนูนี้แล้วและยังรออาหารอยู่ (ทุกจุดเกิด)</summary>
        public static int CountCustomersWaitingFor(ItemType? dish)
        {
            int n = 0;
            foreach (CustomerSpawner s in All)
            {
                if (s == null) continue;
                foreach (CustomerAI c in s.activeCustomers)
                {
                    if (c == null || !c.IsWaitingForFood) continue;
                    if (!dish.HasValue || !c.RequestedDish.HasValue || c.RequestedDish.Value == dish.Value) n++;
                }
            }
            return n;
        }

        /// <summary>จานที่เหลือบนแผงที่เปิดอยู่ หลังหักลูกค้าที่รออยู่ (ติดลบ = คนรอมากกว่าของ)</summary>
        public static int StockSurplus()
        {
            int dishes = 0;
            foreach (FoodSellCounter c in FoodSellCounter.All)
            {
                if (c != null && c.isActiveAndEnabled && c.IsOpen) dishes += c.CountDishes();
            }
            return dishes - CountCustomersWaitingFor(null);
        }

        private float ComputeRateMultiplier()
        {
            if (!adaptToStock) return 1f;
            int surplus = StockSurplus();
            if (surplus <= 0) return emptyRateMultiplier;
            float t = Mathf.Clamp01((surplus - 1) / (float)Mathf.Max(1, busyStock - 1));
            return Mathf.Lerp(1f, fullRateMultiplier, t);
        }

        /// <summary>ทุกจุดเกิดลูกค้าในฉาก</summary>
        public static readonly List<CustomerSpawner> All = new List<CustomerSpawner>();

        /// <summary>จำนวนลูกค้าที่เกิดทั้งหมดตั้งแต่เริ่มเล่น (รวมโจร) ใช้ใน PlaytestLog</summary>
        public static int SpawnedTotal { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SpawnedTotal = 0;
        }

        /// <summary>ระยะเวลาระหว่างลูกค้าแต่ละคน (วินาที)</summary>
        public float SpawnInterval => spawnInterval;

        /// <summary>ลูกค้ากี่คนต่อนาที</summary>
        public float CustomersPerMinute => 60f / Mathf.Max(0.01f, spawnInterval);

        /// <summary>ลูกค้าซื้อได้สูงสุดคนละกี่จาน</summary>
        public int MaxDishesPerCustomer => maxDishesPerCustomer;

        public void SetMaxDishesPerCustomer(int max)
        {
            maxDishesPerCustomer = Mathf.Max(1, max);
        }

        public void SetSpawnInterval(float seconds)
        {
            spawnInterval = Mathf.Max(0.2f, seconds);
        }

        /// <summary>ตั้งความถี่ลูกค้าเป็น "คนต่อนาที" (ใช้กับอัปเกรดในร้าน)</summary>
        public void SetCustomersPerMinute(float perMinute)
        {
            SetSpawnInterval(60f / Mathf.Max(0.1f, perMinute));
        }

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
        }
        private float lastThiefSpawnTime = float.NegativeInfinity;
        private int thievesSpawnedTotal;

        public int ActiveThiefCount
        {
            get
            {
                activeCustomers.RemoveAll(c => c == null);
                int count = 0;
                foreach (var c in activeCustomers)
                {
                    // โจรที่สลบแล้วไม่นับ (กลายเป็นของให้แบกไปต้มแล้ว)
                    if (c.IsThief && !c.IsUnconscious) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// เช็คว่าตอนนี้ยอมให้เกิดโจรได้หรือไม่ ตามค่าที่ตั้งใน Thief Limits
        /// </summary>
        public bool CanSpawnThief()
        {
            if (maxConcurrentThieves <= 0) return false;
            if (Time.timeSinceLevelLoad < firstThiefDelay) return false;
            if (Time.time - lastThiefSpawnTime < thiefCooldown) return false;
            if (maxThievesPerGame > 0 && thievesSpawnedTotal >= maxThievesPerGame) return false;
            return ActiveThiefCount < maxConcurrentThieves;
        }

        private void Awake()
        {
            if (spawnPoint == null) spawnPoint = transform;
            if (exitPoint == null) exitPoint = transform;

            if (targetCounter == null)
            {
#if UNITY_2023_1_OR_NEWER
                targetCounter = Object.FindAnyObjectByType<FoodSellCounter>();
#else
                targetCounter = FindObjectOfType<FoodSellCounter>();
#endif
            }
        }

        private void Update()
        {
            // รอให้มีอาหารวางบนแผงครั้งแรกก่อน แล้วลูกค้าคนแรกมาเกือบทันที
            if (waitForFirstFood && !firstFoodSeen)
            {
                foreach (FoodSellCounter c in FoodSellCounter.All)
                {
                    if (c != null && c.DishesCount > 0) { firstFoodSeen = true; break; }
                }
                if (!firstFoodSeen) { CurrentRateMultiplier = 0f; return; }   // ยังไม่ปล่อยลูกค้า
                nextGap = 1f;
                spawnTimer = 0f;
            }

            // แผงมีของเหลือ = เวลานับเร็วขึ้น (ลูกค้ามาถี่), แผงว่าง = นับช้าลง
            CurrentRateMultiplier = ComputeRateMultiplier();
            if (nextGap < 0f) nextGap = RollGap();
            spawnTimer += Time.deltaTime * CurrentRateMultiplier;
            if (spawnTimer >= nextGap)
            {
                spawnTimer = 0f;
                nextGap = RollGap();
                TrySpawnCustomer();
            }
        }

        /// <summary>
        /// เสกลูกค้าปกติ 1 คนทันที ไม่รอเวลาและไม่ต้องมีอาหารบนแผงก่อน (ใช้ตอนสอนเล่น)
        /// waitForever = ลูกค้าคนนี้รออาหารได้ไม่จำกัด ไม่เดินหนี
        /// </summary>
        public CustomerAI SpawnCustomerNow(bool waitForever)
        {
            activeCustomers.RemoveAll(c => c == null);
            if (customerPrefab == null) return null;
            if (targetCounter == null) targetCounter = Object.FindAnyObjectByType<FoodSellCounter>();
            if (targetCounter == null) return null;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject customerObj = Instantiate(customerPrefab, pos, Quaternion.identity);
            if (!customerObj.TryGetComponent<CustomerAI>(out var customerAI)) return null;

            customerAI.SetAsThief(false);
            customerAI.SetDishesToBuy(1, maxDishesPerCustomer);
            customerAI.SetWaitForever(waitForever);
            customerAI.Init(targetCounter, exitPoint, this);
            customerAI.IgnoreCollisionWithOtherCustomers();
            activeCustomers.Add(customerAI);
            SpawnedTotal++;
            return customerAI;
        }

        public void TrySpawnCustomer()
        {
            // ทำความสะอาด list ที่อาจถูกทำลายไปแล้ว
            activeCustomers.RemoveAll(c => c == null);

            if (activeCustomers.Count >= maxConcurrentCustomers) return;
            if (customerPrefab == null) return;

            if (targetCounter == null)
            {
#if UNITY_2023_1_OR_NEWER
                targetCounter = Object.FindAnyObjectByType<FoodSellCounter>();
#else
                targetCounter = FindObjectOfType<FoodSellCounter>();
#endif
            }

            if (targetCounter == null) return;

            // สุ่มเป็นโจรได้เฉพาะตอนที่ไม่ติดข้อจำกัด ถ้าติดจะกลายเป็นลูกค้าปกติแทน
            bool isThief = Random.value < thiefSpawnChance && CanSpawnThief();

            // ถ้าเป็นลูกค้าปกติ และแถวต่อคิวเต็มแล้ว จะไม่เสกเพิ่ม (แต่โจรสามารถเสกออกมาเพื่อแซงคิวได้เสมอ)
            if (!isThief && AllCountersFull()) return;

            GameObject prefabToUse = (isThief && thiefPrefab != null) ? thiefPrefab : customerPrefab;
            if (prefabToUse == null) prefabToUse = customerPrefab;
            if (prefabToUse == null) return;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject customerObj = Instantiate(prefabToUse, pos, Quaternion.identity);

            if (customerObj.TryGetComponent<CustomerAI>(out var customerAI))
            {
                customerAI.SetAsThief(isThief);
                customerAI.SetDishesToBuy(1, maxDishesPerCustomer);
                customerAI.Init(targetCounter, exitPoint, this);
                customerAI.IgnoreCollisionWithOtherCustomers();
                activeCustomers.Add(customerAI);
                SpawnedTotal++;

                if (isThief)
                {
                    lastThiefSpawnTime = Time.time;
                    thievesSpawnedTotal++;
                }
            }
        }

        /// <summary>ทุกชั้นขายอาหารคิวเต็มหมดแล้วหรือยัง (ลูกค้าแต่ละคนจะเลือกชั้นตามเมนูที่อยากกินเอง)</summary>
        private bool AllCountersFull()
        {
            bool any = false;
            foreach (FoodSellCounter c in FoodSellCounter.All)
            {
                if (!c.isActiveAndEnabled || !c.IsOpen) continue;
                any = true;
                if (!c.IsQueueFull) return false;
            }
            return any ? true : (targetCounter != null && targetCounter.IsQueueFull);
        }

        public void OnCustomerDespawned(CustomerAI customer)
        {
            activeCustomers.Remove(customer);
        }

        private void OnDrawGizmos()
        {
            Vector3 sp = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 ep = exitPoint != null ? exitPoint.position : transform.position;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(sp, 0.5f);

            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(ep, 0.5f);

            if (targetCounter != null)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawLine(sp, targetCounter.CustomerWaitPoint.position);
                Gizmos.color = new Color(1f, 0f, 1f, 0.5f);
                Gizmos.DrawLine(targetCounter.CustomerWaitPoint.position, ep);
            }
        }
    }
}
