using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// โซนล่าสัตว์หนึ่งโซน รวมทุกอย่างไว้ที่เดียว: ขนาดพื้นที่, จุดเกิดสัตว์, จำนวนสูงสุด, เวลาเกิดใหม่ และประตูที่ต้องเปิดก่อน
    /// ค่าในนี้จะทับค่าของ EnemySpawner ตอนเริ่มเกม (กรอบที่สัตว์เดินไม่ออก = ขนาดพื้นที่ - ขอบกันชน)
    /// ถ้ามีประตู (AreaGate) ที่ยังไม่เปิด จุดเกิดจะถูกปิดไว้จนกว่าประตูจะเปิด
    /// </summary>
    public class HuntArea : MonoBehaviour
    {
        [Header("Area (พื้นที่)")]
        [Tooltip("ชื่อโซน (ไว้ดูใน Inspector)")]
        [SerializeField] private string areaName = "Hunting Area";

        [Tooltip("ขนาดพื้นที่ กว้าง x สูง (เมตร) กึ่งกลางอยู่ที่ตำแหน่งของ object นี้")]
        [SerializeField] private Vector2 areaSize = new Vector2(13f, 12f);

        [Tooltip("เว้นขอบด้านในไม่ให้สัตว์เดินชิดขอบ/น้ำเกินไป (เมตร)")]
        [SerializeField] private float edgePadding = 0.4f;

        [Header("Animals (สัตว์)")]
        [Tooltip("จุดเกิดสัตว์ของโซนนี้ (จะถูกย้ายมากลางโซน)")]
        [SerializeField] private EnemySpawner spawner;

        [Tooltip("จำนวนสัตว์สูงสุดในโซนพร้อมกัน")]
        [SerializeField] private int maxAnimals = 6;

        [Tooltip("จำนวนสัตว์ตอนเริ่ม (หรือตอนเปิดประตู)")]
        [SerializeField] private int initialAnimals = 4;

        [Tooltip("เกิดใหม่ทุกกี่วินาที เมื่อจำนวนยังไม่เต็ม")]
        [SerializeField] private float respawnInterval = 3f;

        [Header("Unlock (การปลดล็อก)")]
        [Tooltip("ประตูที่ต้องจ่ายเงินเปิดก่อนเข้าโซนนี้ (ว่าง = เปิดตั้งแต่เริ่ม) ก่อนเปิดจะไม่มีสัตว์เกิด")]
        [SerializeField] private AreaGate gate;

        [SerializeField] private Color gizmoColor = new Color(0.3f, 1f, 0.4f, 0.9f);

        public string AreaName => areaName;
        public Vector2 AreaSize => areaSize;
        public Vector2 BoundarySize => new Vector2(Mathf.Max(0.5f, areaSize.x - edgePadding * 2f), Mathf.Max(0.5f, areaSize.y - edgePadding * 2f));
        public EnemySpawner Spawner => spawner;
        public AreaGate Gate => gate;
        public bool IsOpen => gate == null || gate.IsUnlocked;

        private void Awake()
        {
            if (spawner != null)
            {
                spawner.transform.position = transform.position;
                Vector2 b = BoundarySize;
                float radius = Mathf.Max(0.5f, Mathf.Min(b.x, b.y) * 0.5f - 0.5f);
                spawner.Configure(b, maxAnimals, respawnInterval, initialAnimals, radius);

                if (!IsOpen)
                {
                    spawner.gameObject.SetActive(false);
                }
            }

            if (gate != null && !gate.IsUnlocked)
            {
                gate.onUnlocked.AddListener(OnGateUnlocked);
            }
        }

        private void OnDestroy()
        {
            if (gate != null) gate.onUnlocked.RemoveListener(OnGateUnlocked);
        }

        private void OnGateUnlocked()
        {
            if (spawner != null) spawner.gameObject.SetActive(true);
        }

        /// <summary>จุดนี้อยู่ในโซนหรือไม่</summary>
        public bool Contains(Vector2 point)
        {
            Vector2 c = transform.position;
            Vector2 h = areaSize * 0.5f;
            return point.x >= c.x - h.x && point.x <= c.x + h.x && point.y >= c.y - h.y && point.y <= c.y + h.y;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = gizmoColor;
            Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.06f);
            Gizmos.DrawCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0.1f));
        }
    }
}
