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

        [Tooltip("เมื่อเปิดประตูไปโซนถัดไป (ประตูที่อยู่ติดโซนนี้) จำนวนสูงสุดของโซนนี้เพิ่มเป็นเท่านี้ (0 = ไม่เพิ่ม)")]
        [SerializeField] private int maxAnimalsAfterNextOpens = 12;

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

        private AreaGate nextGate;

        private void Start()
        {
            // ประตูไปโซนถัดไป = ประตูถัดไปตามลำดับปลดล็อก (ประตูที่ต้องเปิดประตูของโซนนี้ก่อน)
            // โซนเริ่มต้นที่ไม่มีประตู -> ประตูแรกที่ไม่ต้องเปิดอะไรก่อน (เช่น เกาะไก่ -> ประตูเกาะหมู, เกาะหมู -> ประตูเกาะควาย)
            if (maxAnimalsAfterNextOpens <= 0 || spawner == null) return;
            for (int i = 0; i < all.Count; i++)
            {
                HuntArea other = all[i];
                if (other == null || other == this || other.gate == null || other.gate == gate) continue;
                if (other.gate.RequiredGate != gate) continue;
                nextGate = other.gate;
                break;
            }
            if (nextGate == null) return;
            if (nextGate.IsUnlocked) OnNextGateUnlocked();
            else nextGate.onUnlocked.AddListener(OnNextGateUnlocked);
        }

        private void OnNextGateUnlocked()
        {
            if (spawner != null) spawner.SetMaxCount(Mathf.Max(maxAnimals, maxAnimalsAfterNextOpens));
        }

        private void OnDestroy()
        {
            if (gate != null) gate.onUnlocked.RemoveListener(OnGateUnlocked);
            if (nextGate != null) nextGate.onUnlocked.RemoveListener(OnNextGateUnlocked);
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

        /// <summary>กรอบที่สัตว์เดินได้ในโซนนี้ (ขนาดพื้นที่ - ขอบกันชน)</summary>
        public Rect InnerRect
        {
            get
            {
                Vector2 b = BoundarySize;
                return new Rect((Vector2)transform.position - b * 0.5f, b);
            }
        }

        // ==================== พื้นที่ที่สัตว์วิ่งไปได้ (โซนที่เปิดแล้ว + ทางเชื่อมระหว่างโซน) ====================

        /// <summary>ทางเชื่อมระหว่างสองโซนที่เปิดแล้ว (ช่องประตูที่เปิดแล้ว)</summary>
        public struct Link
        {
            public Rect rect;
            public HuntArea a;
            public HuntArea b;
            public Vector2 Center => rect.center;
            public HuntArea Other(HuntArea from) => from == a ? b : a;
        }

        private static readonly System.Collections.Generic.List<HuntArea> all = new System.Collections.Generic.List<HuntArea>();
        private static readonly System.Collections.Generic.List<Link> links = new System.Collections.Generic.List<Link>();
        private static int linksFrame = -1;

        /// <summary>ยืดช่องประตูไปตามแนวทางเดินเท่านี้ (เมตร) ให้ต่อถึงกรอบของทั้งสองโซน</summary>
        private const float LinkReach = 1.6f;

        public static System.Collections.Generic.IReadOnlyList<HuntArea> All => all;

        private void OnEnable()
        {
            if (!all.Contains(this)) all.Add(this);
            linksFrame = -1;
        }

        private void OnDisable()
        {
            all.Remove(this);
            linksFrame = -1;
        }

        /// <summary>ทางเชื่อมทั้งหมดในตอนนี้ (สร้างใหม่เฟรมละครั้ง ประตูเพิ่งเปิดจะมีทางเชื่อมทันที)</summary>
        public static System.Collections.Generic.List<Link> Links
        {
            get
            {
                if (linksFrame == Time.frameCount) return links;
                linksFrame = Time.frameCount;
                links.Clear();
                for (int i = 0; i < all.Count; i++)
                {
                    HuntArea area = all[i];
                    if (area == null || area.gate == null || !area.IsOpen) continue;
                    if (!area.gate.TryGetPassage(out Rect passage)) continue;

                    // หาโซนอีกฝั่งของประตู: โซนที่เปิดแล้วและอยู่ติดช่องประตู
                    for (int j = 0; j < all.Count; j++)
                    {
                        HuntArea other = all[j];
                        if (other == null || other == area || !other.IsOpen) continue;
                        Rect grown = Grow(other.AreaRect, LinkReach);
                        if (!grown.Overlaps(passage)) continue;

                        // ยืดช่องประตูไปตามแนวที่เชื่อมสองโซน (กว้างเท่าช่องประตูเดิม)
                        Vector2 axis = (Vector2)area.transform.position - (Vector2)other.transform.position;
                        Rect r = passage;
                        if (Mathf.Abs(axis.x) >= Mathf.Abs(axis.y)) { r.xMin -= LinkReach; r.xMax += LinkReach; }
                        else { r.yMin -= LinkReach; r.yMax += LinkReach; }
                        links.Add(new Link { rect = r, a = area, b = other });
                    }
                }
                return links;
            }
        }

        private Rect AreaRect => new Rect((Vector2)transform.position - areaSize * 0.5f, areaSize);

        private static Rect Grow(Rect r, float by) => Rect.MinMaxRect(r.xMin - by, r.yMin - by, r.xMax + by, r.yMax + by);

        /// <summary>โซน (เปิดแล้ว) ที่จุดนี้อยู่ในกรอบเดินได้</summary>
        public static HuntArea FindOpenAreaAt(Vector2 p)
        {
            for (int i = 0; i < all.Count; i++)
            {
                HuntArea a = all[i];
                if (a != null && a.IsOpen && a.InnerRect.Contains(p)) return a;
            }
            return null;
        }

        /// <summary>โซนที่จุดนี้อยู่ในพื้นที่ (ไม่สนว่าเปิดหรือยัง)</summary>
        public static HuntArea FindAreaAt(Vector2 p)
        {
            for (int i = 0; i < all.Count; i++)
            {
                HuntArea a = all[i];
                if (a != null && a.Contains(p)) return a;
            }
            return null;
        }

        /// <summary>จุดนี้อยู่ในพื้นที่ที่สัตว์วิ่งได้หรือไม่ (กรอบโซนที่เปิดแล้ว หรือทางเชื่อม)</summary>
        public static bool IsInsideRoamRegion(Vector2 p)
        {
            if (FindOpenAreaAt(p) != null) return true;
            var ls = Links;
            for (int i = 0; i < ls.Count; i++) if (ls[i].rect.Contains(p)) return true;
            return false;
        }

        /// <summary>กรอบย่อย (โซน/ทางเชื่อม) ที่จุดนี้อยู่ คืน false ถ้าอยู่นอกพื้นที่</summary>
        public static bool TryGetRegionRectAt(Vector2 p, out Rect rect)
        {
            HuntArea a = FindOpenAreaAt(p);
            if (a != null) { rect = a.InnerRect; return true; }
            var ls = Links;
            for (int i = 0; i < ls.Count; i++)
            {
                if (ls[i].rect.Contains(p)) { rect = ls[i].rect; return true; }
            }
            rect = default;
            return false;
        }

        /// <summary>ดึงจุดที่หลุดออกไปกลับเข้าพื้นที่ที่วิ่งได้ (จุดที่ใกล้ที่สุด)</summary>
        public static Vector2 ClampToRoamRegion(Vector2 p)
        {
            Vector2 best = p;
            float bestD = float.PositiveInfinity;
            void Try(Rect r)
            {
                Vector2 c = new Vector2(Mathf.Clamp(p.x, r.xMin, r.xMax), Mathf.Clamp(p.y, r.yMin, r.yMax));
                float d = (c - p).sqrMagnitude;
                if (d < bestD) { bestD = d; best = c; }
            }
            for (int i = 0; i < all.Count; i++)
            {
                HuntArea a = all[i];
                if (a != null && a.IsOpen) Try(a.InnerRect);
            }
            var ls = Links;
            for (int i = 0; i < ls.Count; i++) Try(ls[i].rect);
            return best;
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
