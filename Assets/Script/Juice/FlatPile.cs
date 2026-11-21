using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// จัดของเป็น "กองแบน" (ไม่ซ้อนสูง): เรียงเป็นแถวหน้ากระดาน แถวถัดไปอยู่ด้านหลัง (สูงขึ้นนิดเดียว) และเยื้องครึ่งช่องสลับกัน
    /// ใช้ร่วมกันที่ถาดอาหารข้างหม้อ / แผงขาย / วัตถุดิบบนหม้อ ให้ทุกที่หน้าตาเดียวกัน
    /// ชิ้นที่ 0 อยู่แถวหน้าสุดและถูกวาดทับแถวหลัง
    /// </summary>
    public static class FlatPile
    {
        /// <summary>ตำแหน่งของชิ้นที่ index เทียบกับจุดกลางแถวหน้า</summary>
        public static Vector2 Offset(int index, int columns, Vector2 spacing)
        {
            columns = Mathf.Max(1, columns);
            int row = index / columns;
            int col = index % columns;
            float stagger = (row % 2 == 0 ? -0.25f : 0.25f) * spacing.x;
            float x = (col - (columns - 1) * 0.5f) * spacing.x + stagger;
            return new Vector2(x, row * spacing.y);
        }

        /// <summary>ลำดับการวาด: แถวหน้าทับแถวหลัง (คืนค่าที่บวกกับ sortingOrder ฐาน)</summary>
        public static int SortingOffset(int index, int columns, int capacity)
        {
            columns = Mathf.Max(1, columns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, capacity) / (float)columns));
            int row = index / columns;
            int col = index % columns;
            return Mathf.Max(0, rows - 1 - row) * columns + col;
        }

        /// <summary>สเกล local ที่ทำให้ sprite ของชิ้นนี้กว้าง worldWidth หน่วยในโลก (ชดเชยสเกลของ parent)</summary>
        public static Vector3 LocalScaleForWidth(Transform item, SpriteRenderer sr, float worldWidth)
        {
            if (item == null || sr == null || sr.sprite == null || worldWidth <= 0f) return item != null ? item.localScale : Vector3.one;
            float spriteW = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
            // สเกลระหว่าง item กับ renderer (เช่น sprite อยู่ใน child ที่มีสเกลของตัวเอง)
            Vector3 inner = sr.transform == item ? Vector3.one : Vector3.Scale(sr.transform.localScale, Vector3.one);
            Vector3 parentLossy = item.parent != null ? item.parent.lossyScale : Vector3.one;
            float world = worldWidth / (spriteW * Mathf.Max(0.0001f, Mathf.Abs(inner.x)));
            return new Vector3(
                world / Mathf.Max(0.0001f, Mathf.Abs(parentLossy.x)),
                world / Mathf.Max(0.0001f, Mathf.Abs(parentLossy.y)),
                1f);
        }
    }
}
