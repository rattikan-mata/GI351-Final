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

        [Header("Waypoints")]
        [Tooltip("จุดเกิดของลูกค้า (ถ้าว่างจะใช้ตำแหน่งของ Spawner นี้)")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("จุดที่ลูกค้าจะเดินกลับไปแล้วหายไป (ถ้าว่างจะใช้จุดเกิด)")]
        [SerializeField] private Transform exitPoint;

        [Tooltip("โต๊ะขายอาหารที่ลูกค้าจะเดินไป")]
        [SerializeField] private FoodSellCounter targetCounter;

        private readonly List<CustomerAI> activeCustomers = new List<CustomerAI>();
        private float spawnTimer;

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
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                TrySpawnCustomer();
            }
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

            bool isThief = Random.value < thiefSpawnChance;

            // ถ้าเป็นลูกค้าปกติ และแถวต่อคิวเต็มแล้ว จะไม่เสกเพิ่ม (แต่โจรสามารถเสกออกมาเพื่อแซงคิวได้เสมอ)
            if (!isThief && targetCounter.IsQueueFull) return;

            GameObject prefabToUse = (isThief && thiefPrefab != null) ? thiefPrefab : customerPrefab;
            if (prefabToUse == null) prefabToUse = customerPrefab;
            if (prefabToUse == null) return;

            Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject customerObj = Instantiate(prefabToUse, pos, Quaternion.identity);

            if (customerObj.TryGetComponent<CustomerAI>(out var customerAI))
            {
                customerAI.SetAsThief(isThief);
                customerAI.Init(targetCounter, exitPoint, this);
                customerAI.IgnoreCollisionWithOtherCustomers();
                activeCustomers.Add(customerAI);
            }
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
