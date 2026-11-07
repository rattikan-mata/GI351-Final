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

        [Header("Boundary (ขอบเขตที่สัตว์จะไม่เดินออกไป)")]
        [Tooltip("ล็อกสัตว์ที่เกิดจากจุดนี้ให้อยู่ในกรอบสี่เหลี่ยมด้านล่าง (เดินสุ่ม/โดนตีกระเด็นก็ไม่หลุดออกไป)")]
        [SerializeField] private bool confineToBoundary = true;

        [Tooltip("ขนาดกรอบ กว้าง x สูง (เมตร) มีจุดกึ่งกลางอยู่ที่ Spawner")]
        [SerializeField] private Vector2 boundarySize = new Vector2(8f, 6f);

        [Tooltip("เลื่อนจุดกึ่งกลางกรอบออกจากตำแหน่ง Spawner (เมตร)")]
        [SerializeField] private Vector2 boundaryOffset = Vector2.zero;

        [Tooltip("สีของกรอบใน Scene View")]
        [SerializeField] private Color boundaryGizmoColor = new Color(1f, 0.35f, 0.2f, 0.9f);

        [Header("Gizmo Settings")]
        [Tooltip("สีของวงกลมจุดเกิดใน Scene View")]
        [SerializeField] private Color gizmoColor = new Color(0.9f, 0.7f, 0.1f, 0.35f);

        private readonly List<GameObject> activeSpawns = new List<GameObject>();
        private float spawnTimer;

        public int CurrentAliveCount => GetActiveCount();
        public int MaxCount => maxConcurrentCount;
        public Vector2 BoundaryCenter => (Vector2)transform.position + boundaryOffset;
        public Vector2 BoundarySize => boundarySize;

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

            if (confineToBoundary)
            {
                // ไม่ให้เกิดนอกกรอบ
                Vector2 half = boundarySize * 0.5f;
                Vector2 c = BoundaryCenter;
                spawnPosition.x = Mathf.Clamp(spawnPosition.x, c.x - half.x, c.x + half.x);
                spawnPosition.y = Mathf.Clamp(spawnPosition.y, c.y - half.y, c.y + half.y);
            }

            GameObject spawnedObj = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);

            // ให้ทุกตัวจากจุดนี้ใช้กรอบเดียวกัน
            if (confineToBoundary && spawnedObj.TryGetComponent<EnemyWander>(out var wander))
            {
                wander.SetBoundary(BoundaryCenter, boundarySize);
            }

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

            // วาดกรอบขอบเขต
            if (confineToBoundary)
            {
                Vector2 c = BoundaryCenter;
                Gizmos.color = boundaryGizmoColor;
                Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0f), new Vector3(boundarySize.x, boundarySize.y, 0.1f));
                Gizmos.color = new Color(boundaryGizmoColor.r, boundaryGizmoColor.g, boundaryGizmoColor.b, 0.08f);
                Gizmos.DrawCube(new Vector3(c.x, c.y, 0f), new Vector3(boundarySize.x, boundarySize.y, 0.1f));
            }
        }
    }
}
