using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// จัดจุดยืนแบบ "ยืนมุง" หน้าเคาน์เตอร์: แถวหน้าสุดกว้างที่สุด แถวถัดไปแคบลงทีละ 1 คน (วางเหลื่อมกันเอง)
    /// ช่องที่ 0 คือกลางแถวหน้า แล้วไล่ออกซ้าย/ขวา จากนั้นค่อยไปแถวถัดไป
    /// </summary>
    public static class CrowdLayout
    {
        /// <param name="slot">หมายเลขจุดยืน (0 = กลางแถวหน้าสุด ใกล้เคาน์เตอร์ที่สุด)</param>
        /// <param name="dir">ทิศที่ฝูงขยายออกจากเคาน์เตอร์</param>
        /// <param name="frontColumns">จำนวนคนในแถวหน้าสุด (แถวถัดไปลดลงทีละ 1 จนเหลือ 1)</param>
        /// <param name="spacing">ระยะห่างระหว่างคนในแถว (เมตร)</param>
        /// <param name="rowSpacing">ระยะห่างระหว่างแถว (เมตร)</param>
        /// <param name="jitter">ความสุ่มเยื้องเล็กน้อย (เมตร) คงที่ต่อจุด ไม่สั่น</param>
        public static Vector2 GetSlotOffset(int slot, Vector2 dir, int frontColumns, float spacing, float rowSpacing, float jitter)
        {
            slot = Mathf.Max(0, slot);
            frontColumns = Mathf.Max(1, frontColumns);
            dir = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector2.down;
            Vector2 side = new Vector2(-dir.y, dir.x);

            int row = 0, remaining = slot;
            while (true)
            {
                int width = Mathf.Max(1, frontColumns - row);
                if (remaining < width)
                {
                    float x = OrderedColumnX(remaining, width) * spacing;
                    float y = row * rowSpacing;
                    float jx = (Hash(slot * 2 + 1) - 0.5f) * 2f * jitter;
                    float jy = (Hash(slot * 2 + 2) - 0.5f) * 2f * jitter;
                    return side * (x + jx) + dir * (y + jy);
                }
                remaining -= width;
                row++;
                if (row > 1000) return Vector2.zero;
            }
        }

        /// <summary>ตำแหน่งแนวขวาง (หน่วยเป็นช่อง) ของคนที่ k ในแถวกว้าง width โดยเติมจากกลางออกซ้าย/ขวาสลับกัน</summary>
        private static float OrderedColumnX(int k, int width)
        {
            var xs = new List<float>(width);
            for (int i = 0; i < width; i++) xs.Add(i - (width - 1) * 0.5f);
            xs.Sort((a, b) =>
            {
                int c = Mathf.Abs(a).CompareTo(Mathf.Abs(b));
                return c != 0 ? c : a.CompareTo(b);
            });
            return xs[Mathf.Clamp(k, 0, width - 1)];
        }

        private static float Hash(int n)
        {
            unchecked
            {
                uint h = (uint)n * 2654435761u;
                h ^= h >> 15;
                h *= 2246822519u;
                h ^= h >> 13;
                return (h & 0xFFFFu) / 65535f;
            }
        }
    }

    /// <summary>
    /// จองจุดยืนให้ลูกค้าแต่ละคน: มาถึงแล้วได้จุดว่างที่ใกล้เคาน์เตอร์ที่สุด และยืนจุดเดิมจนออกไป (ไม่เลื่อนตามคิว)
    /// </summary>
    public class CrowdSlots
    {
        private readonly Dictionary<CustomerAI, int> slotOf = new Dictionary<CustomerAI, int>();
        private readonly List<CustomerAI> deadKeys = new List<CustomerAI>();

        public int GetSlot(CustomerAI customer)
        {
            Cleanup();
            if (customer == null) return 0;
            if (slotOf.TryGetValue(customer, out int slot)) return slot;

            slot = 0;
            while (slotOf.ContainsValue(slot)) slot++;
            slotOf[customer] = slot;
            return slot;
        }

        public void Release(CustomerAI customer)
        {
            if (customer != null) slotOf.Remove(customer);
            Cleanup();
        }

        private void Cleanup()
        {
            deadKeys.Clear();
            foreach (var k in slotOf.Keys) if (k == null) deadKeys.Add(k);
            foreach (var k in deadKeys) slotOf.Remove(k);
        }
    }
}
