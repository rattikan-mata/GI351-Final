using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ร้านเริ่มเล็ก แล้วพื้นดินขยายออกทีละช่วงเมื่อซื้อหม้อ (Stage ละหม้อ)
    /// - ช่วงที่ซื้อแล้ว: เป็นพื้นจริง ของในช่วง (หม้อ/ชั้น) แสดงปกติ
    /// - ช่วงถัดไป: โชว์พื้นจางๆ (Preview Tilemap) บนน้ำ + หม้อ/ชั้นแบบล็อก (โปร่งแสง) ให้รู้ว่าซื้ออะไรต่อแล้วร้านจะโตไปทางไหน
    /// - ช่วงที่ไกลกว่านั้น: ซ่อนทั้งพื้นและของ
    /// ซื้อหม้อของช่วงหลังก่อนช่วงหน้า พื้นช่วงก่อนหน้าก็จะเปิดให้ด้วย (ไม่มีเกาะลอยกลางน้ำ)
    /// ในฉาก (Edit mode) วาดพื้นทุกช่วงไว้ให้เห็นภาพรวมได้ ตอนเริ่มเกมสคริปต์จะตัดช่วงที่ยังไม่ซื้อออกเอง
    /// </summary>
    public class ShopExpansion : MonoBehaviour
    {
        [Serializable]
        public class Stage
        {
            [Tooltip("ชื่อช่วง (ไว้ดูใน Inspector)")]
            public string name = "Stage";

            [Tooltip("หม้อที่ซื้อแล้วช่วงนี้จะเปิด (ว่าง = เปิดตั้งแต่เริ่ม)")]
            public CookingPot unlockWithPot;

            [Tooltip("ช่องพื้นของช่วงนี้ (หน่วย cell ของ Tilemap: x, y = มุมซ้ายล่าง, w, h)")]
            public RectInt[] cells = new RectInt[0];

            [Tooltip("ของที่อยู่บนพื้นช่วงนี้ (หม้อ ชั้นขาย ฯลฯ) ซ่อนไว้จนกว่าช่วงนี้จะเป็นช่วงถัดไป")]
            public GameObject[] objects = new GameObject[0];

            public bool IsOpen => unlockWithPot == null || unlockWithPot.IsUnlocked;
        }

        [Header("Tilemaps")]
        [SerializeField] private Tilemap floor;
        [SerializeField] private Tilemap water;

        [Tooltip("Tilemap แยกไว้โชว์พื้นจางๆ ของช่วงถัดไป (ว่าง = ไม่โชว์)")]
        [SerializeField] private Tilemap preview;

        [Tooltip("ว่าง = ใช้ tile หญ้าที่เจอใน Floor")]
        [SerializeField] private TileBase landTile;

        [Tooltip("ว่าง = ใช้ tile น้ำที่เจอใน Water")]
        [SerializeField] private TileBase waterTile;

        [SerializeField] private Color previewColor = new Color(1f, 1f, 1f, 0.35f);

        [Header("Stages (เรียงจากใกล้ไปไกล)")]
        [SerializeField] private List<Stage> stages = new List<Stage>();

        [Header("Juice")]
        [Tooltip("เวลาที่พื้นค่อยๆ งอกจากซ้ายไปขวา (วินาที)")]
        [SerializeField] private float revealDuration = 0.5f;

        [Tooltip("เวลาที่หม้อ/ชั้นเด้งขึ้นมา (วินาที)")]
        [SerializeField] private float popDuration = 0.3f;

        private int appliedMaxOpen = int.MinValue;
        private readonly Dictionary<GameObject, Vector3> baseScales = new Dictionary<GameObject, Vector3>();

        public IReadOnlyList<Stage> Stages => stages;

        private void Start()
        {
            ResolveTiles();
            if (preview != null) preview.color = previewColor;
            Apply(ComputeMaxOpen(), false);
        }

        private void Update()
        {
            int maxOpen = ComputeMaxOpen();
            if (maxOpen != appliedMaxOpen) Apply(maxOpen, true);
        }

        private void ResolveTiles()
        {
            if (landTile == null && floor != null) landTile = FirstTile(floor);
            if (waterTile == null && water != null) waterTile = FirstTile(water);
        }

        private static TileBase FirstTile(Tilemap tm)
        {
            tm.CompressBounds();
            foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
            {
                TileBase t = tm.GetTile(p);
                if (t != null) return t;
            }
            return null;
        }

        /// <summary>ช่วงที่ไกลที่สุดที่เปิดแล้ว (-1 = ยังไม่มีช่วงไหนเปิด)</summary>
        private int ComputeMaxOpen()
        {
            int max = -1;
            for (int i = 0; i < stages.Count; i++)
            {
                if (stages[i] != null && stages[i].IsOpen) max = i;
            }
            return max;
        }

        private void Apply(int maxOpen, bool animate)
        {
            int previous = appliedMaxOpen;
            appliedMaxOpen = maxOpen;

            var newlyReal = new List<Vector3Int>();
            for (int i = 0; i < stages.Count; i++)
            {
                Stage s = stages[i];
                if (s == null) continue;
                bool real = i <= maxOpen;
                bool next = i == maxOpen + 1;
                bool wasReal = previous != int.MinValue && i <= previous;

                foreach (RectInt r in s.cells)
                {
                    foreach (Vector2Int c in r.allPositionsWithin)
                    {
                        var cell = new Vector3Int(c.x, c.y, 0);
                        if (real)
                        {
                            if (animate && !wasReal) newlyReal.Add(cell);
                            else SetLand(cell);
                        }
                        else
                        {
                            SetWater(cell);
                            if (preview != null) preview.SetTile(cell, next ? landTile : null);
                        }
                    }
                }

                // ของบนพื้น: ช่วงที่เปิดแล้ว + ช่วงถัดไป (แบบล็อก) แสดง / ไกลกว่านั้นซ่อน
                bool show = real || next;
                foreach (GameObject go in s.objects)
                {
                    if (go == null) continue;
                    bool wasActive = go.activeSelf;
                    if (wasActive != show) go.SetActive(show);
                    if (show && !wasActive && animate) StartCoroutine(Pop(go));
                }
            }

            if (newlyReal.Count > 0) StartCoroutine(RevealSweep(newlyReal));
        }

        private void SetLand(Vector3Int cell)
        {
            if (floor != null) floor.SetTile(cell, landTile);
            if (water != null) water.SetTile(cell, null);
            if (preview != null) preview.SetTile(cell, null);
        }

        private void SetWater(Vector3Int cell)
        {
            if (floor != null) floor.SetTile(cell, null);
            if (water != null) water.SetTile(cell, waterTile);
        }

        /// <summary>พื้นใหม่งอกจากซ้ายไปขวา (ทีละคอลัมน์)</summary>
        private IEnumerator RevealSweep(List<Vector3Int> cells)
        {
            int minX = int.MaxValue, maxX = int.MinValue;
            foreach (Vector3Int c in cells) { minX = Mathf.Min(minX, c.x); maxX = Mathf.Max(maxX, c.x); }
            int columns = maxX - minX + 1;
            float step = columns > 1 ? revealDuration / columns : 0f;

            for (int x = minX; x <= maxX; x++)
            {
                foreach (Vector3Int c in cells) if (c.x == x) SetLand(c);
                if (step > 0f) yield return new WaitForSeconds(step);
            }
            JuiceDirector.Shake(3f, 0.18f, 16f);
        }

        /// <summary>ของที่เพิ่งโผล่ เด้งจากเล็กไปขนาดจริง</summary>
        private IEnumerator Pop(GameObject go)
        {
            if (!baseScales.TryGetValue(go, out Vector3 baseScale))
            {
                baseScale = go.transform.localScale;
                baseScales[go] = baseScale;
            }
            float t = 0f;
            while (t < popDuration && go != null)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / popDuration);
                // ขึ้นเกินนิดแล้วกลับ (overshoot)
                float s = k < 0.7f ? Mathf.Lerp(0.2f, 1.12f, k / 0.7f) : Mathf.Lerp(1.12f, 1f, (k - 0.7f) / 0.3f);
                go.transform.localScale = baseScale * s;
                yield return null;
            }
            if (go != null) go.transform.localScale = baseScale;
        }

        private void OnDrawGizmosSelected()
        {
            if (floor == null) return;
            for (int i = 0; i < stages.Count; i++)
            {
                Stage s = stages[i];
                if (s == null) continue;
                Gizmos.color = Color.HSVToRGB((i * 0.17f) % 1f, 0.7f, 1f);
                foreach (RectInt r in s.cells)
                {
                    Vector3 a = floor.CellToWorld(new Vector3Int(r.xMin, r.yMin, 0));
                    Vector3 b = floor.CellToWorld(new Vector3Int(r.xMax, r.yMax, 0));
                    Gizmos.DrawWireCube((a + b) * 0.5f, b - a);
                }
            }
        }
    }
}
