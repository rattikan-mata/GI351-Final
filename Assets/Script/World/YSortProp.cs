using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ของที่ตั้งอยู่กับที่ในแมพ (ร้าน โต๊ะ หม้อ แคชเชียร์ ต้นไม้ หิน): ตั้งลำดับวาดตามขอบล่างของภาพ
    /// ภาพลูกที่มีอยู่แล้วเลื่อนตามไปด้วย (ห่างจากภาพหลักเท่าเดิม) ของที่สคริปต์วางทีหลัง (อาหารบนโต๊ะ ฯลฯ)
    /// คิดจากลำดับของภาพหลักอยู่แล้ว จึงอยู่บนโต๊ะถูกต้อง วงบนพื้น (เลขน้อย) ยังอยู่ใต้ตัวละคร
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [ExecuteAlways]
    public class YSortProp : MonoBehaviour
    {
        [Tooltip("ภาพหลัก (ขอบล่าง = จุดที่ตั้งพื้น) ว่างไว้ = ภาพบนตัวนี้หรือลูกตัวแรก")]
        [SerializeField] private SpriteRenderer mainRenderer;

        [Tooltip("ยกจุดตั้งพื้นขึ้นจากขอบล่างของภาพ (หน่วยโลก) เช่น ขาโต๊ะยาว / ภาพมีขอบใสด้านล่าง")]
        [SerializeField] private float baseLift = 0.05f;

        [Tooltip("ภาพลูกที่ลำดับต่ำกว่าภาพหลักเกินค่านี้ = ของบนพื้น (วง/เงา) ไม่ต้องเลื่อนตาม")]
        [SerializeField] private int floorGap = 50;

        [Tooltip("ภาพลูกที่วางบนพื้น (วงยืน วงวางของ) อยู่ใต้ตัวละครเสมอ ไม่เรียงตาม Y")]
        [SerializeField] private SpriteRenderer[] floorRenderers;

        private Vector3 lastPos;

        private void Awake()
        {
            Apply();
        }

        private void Update()
        {
            // ตอนจัดฉากใน Editor ย้ายของแล้วลำดับตามทันที (ในเกมของไม่ขยับ ไม่ต้องคิดซ้ำ)
            if (Application.isPlaying) return;
            if (transform.position != lastPos) Apply();
        }

        [ContextMenu("Apply Y Sort")]
        public void Apply()
        {
            lastPos = transform.position;
            if (mainRenderer == null) mainRenderer = GetComponent<SpriteRenderer>();
            if (mainRenderer == null) mainRenderer = GetComponentInChildren<SpriteRenderer>();
            if (mainRenderer == null || mainRenderer.sprite == null) return;

            int target = YSort.OrderAt(mainRenderer.bounds.min.y + baseLift);
            int delta = target - mainRenderer.sortingOrder;
            if (delta == 0) return;

            int oldMain = mainRenderer.sortingOrder;
            foreach (var r in GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (r == mainRenderer || IsFloor(r)) continue;
                if (r.GetComponentInParent<YSortProp>(true) != this) continue;   // ลูกที่เรียงชั้นเอง (เช่น โต๊ะถาดใต้หม้อ)
                if (r.sortingOrder < oldMain - floorGap) continue;   // วงบนพื้น/เงา อยู่ใต้ตัวละครต่อไป
                r.sortingOrder += delta;
            }
            mainRenderer.sortingOrder = target;
        }

        private bool IsFloor(SpriteRenderer r)
        {
            if (floorRenderers == null) return false;
            foreach (var f in floorRenderers) if (f == r) return true;
            return false;
        }
    }
}
