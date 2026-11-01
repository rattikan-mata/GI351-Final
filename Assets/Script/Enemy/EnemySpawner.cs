using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings (การตั้งค่าจุดเกิด)")]
        [Tooltip("Prefab ที่ต้องการให้เกิด (เช่น ไก่ หรือ หมู)")]
        [SerializeField] private GameObject prefabToSpawn;

        [Tooltip("จำนวนตัวที่ให้เกิดทันทีตอนเริ่มเกม")]
        [SerializeField] private int initialSpawnCount = 2;

        [Tooltip("จำนวนสูงสุดที่อยู่ในแมพพร้อมกันจากจุดนี้ (ป้องกันไม่ให้เกิดล้น)")]
        [SerializeField] private int maxConcurrentCount = 4;

        [Tooltip("ระยะเวลาเว้นช่วงในการเกิดตัวใหม่ (วินาที)")]
        [SerializeField] private float spawnInterval = 8f;

        [Tooltip("เกิดอัตโนมัติต่อเนื่องเมื่อตัวเก่าตาย")]
        [SerializeField] private bool autoRespawn = true;

        [Header("Spawn Area (ขอบเขตพื้นที่เกิด)")]
        [Tooltip("รัศมีพื้นที่สุ่มตำแหน่งเกิดรอบจุดนี้")]
        [SerializeField] private float spawnRadius = 2.5f;

        [Header("Gizmo Settings")]
        [Tooltip("สีของวงกลมจุดเกิดใน Scene View")]
        [SerializeField] private Color gizmoColor = new Color(0.9f, 0.7f, 0.1f, 0.35f);

        private readonly List<GameObject> activeSpawns = new List<GameObject>();
        private float spawnTimer;

        public int CurrentAliveCount => GetActiveCount();
        public int MaxCount => maxConcurrentCount;

        private void Start()
        {
            // เสกจำนวนเริ่มต้นตอนเริ่มเกม
            for (int i = 0; i < initialSpawnCount; i++)
            {
                if (GetActiveCount() < maxConcurrentCount)
                {
                    SpawnOne();
                }
            }
        }

        private void Update()
        {
            if (!autoRespawn) return;

            // ตรวจสอบว่าจำนวนตัวที่ยังมีชีวิตอยู่ น้อยกว่าขีดจำกัดหรือไม่
            if (GetActiveCount() < maxConcurrentCount)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnOne();
                }
            }
            else
            {
                spawnTimer = 0f;
            }
        }

        /// <summary>
        /// สั่งเสกศัตรู 1 ตัว ณ ตำแหน่งสุ่มภายในรัศมี
        /// </summary>
        public GameObject SpawnOne()
        {
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("[EnemySpawner] ยังไม่ได้กำหนด Prefab To Spawn ใน Inspector!", this);
                return null;
            }

            Vector2 randomOffset = Random.insideUnitCircle * spawnRadius;
            Vector3 spawnPosition = transform.position + new Vector3(randomOffset.x, randomOffset.y, 0f);

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            // เก็บรายการตัวที่ยังมีชีวิตอยู่
            activeSpawns.Add(spawnedObj);
            return spawnedObj;
        }

        /// <summary>
        /// นับจำนวนตัวที่ยังไม่ตาย (ทำความสะอาดตัวที่ถูก Destroy ไปแล้วอัตโนมัติ)
        /// </summary>
        private int GetActiveCount()
        {
            activeSpawns.RemoveAll(item => item == null);
            return activeSpawns.Count;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawSphere(transform.position, spawnRadius);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireSphere(transform.position, spawnRadius);

            // วาดจุดกึ่งกลางของ Spawner
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.3f, 0.3f, 0.1f));
        }
    }
}
