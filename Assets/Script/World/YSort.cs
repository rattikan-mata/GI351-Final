using UnityEngine;
using UnityEngine.Rendering;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เรียงชั้นภาพตามความสูงของเท้า (Y-sort): ของที่เท้าอยู่ต่ำกว่าบนจอ วาดทับของที่อยู่สูงกว่า
    /// ตัวละครและของในแมพใช้ช่วงเลข Base +- (Y * PerUnit) ส่วนพื้น/วงบนพื้นใช้เลขน้อย (อยู่ใต้ทุกอย่าง)
    /// ป้ายลอย/ตัวเลข/เอฟเฟกต์ใช้ Overlay ขึ้นไป (อยู่บนทุกอย่าง)
    /// </summary>
    public static class YSort
    {
        /// <summary>เลขกลางของช่วงตัวละคร/ของในแมพ</summary>
        public const int Base = 5000;

        /// <summary>ความละเอียด: 1 หน่วยโลก = 20 ขั้น (ห่างกัน 0.05 หน่วยก็เรียงถูก)</summary>
        public const float PerUnit = 20f;

        /// <summary>ป้ายลอย ตัวเลข เอฟเฟกต์ อยู่เหนือตัวละครและของทุกชิ้น</summary>
        public const int Overlay = 20000;

        /// <summary>ลำดับวาดของสิ่งที่เท้าอยู่ที่ footY (ยิ่งต่ำยิ่งอยู่หน้า)</summary>
        public static int OrderAt(float footY)
        {
            return Base - Mathf.RoundToInt(footY * PerUnit);
        }

        /// <summary>ตำแหน่งเท้าของตัวละคร (มี YSortActor) ไม่มี = ตำแหน่งของวัตถุ ใช้เช็กการยืนในวงบนพื้น</summary>
        public static Vector2 FeetOf(Transform t)
        {
            if (t == null) return Vector2.zero;
            var actor = t.GetComponent<YSortActor>();
            return actor != null ? actor.FootPosition : (Vector2)t.position;
        }

        /// <summary>
        /// ระยะจากเท้าถึงวงที่ "ยืดลงไป" ทาง -Y ได้ reachDown หน่วย (ภาพวงไม่ขยับ)
        /// ใช้กับวงที่อยู่เหนือโต๊ะ: เดินอ้อมไปยืนชิดหลังโต๊ะลึกแค่ไหนก็ยังนับว่าอยู่ในวง
        /// </summary>
        public static float DistanceToRing(Vector2 feet, Vector2 ringCenter, float reachDown)
        {
            float y = Mathf.Clamp(feet.y, ringCenter.y - Mathf.Max(0f, reachDown), ringCenter.y);
            return Vector2.Distance(feet, new Vector2(ringCenter.x, y));
        }

        /// <summary>
        /// ระยะที่ต้องยืดวงลงไปให้ถึงจุดที่เท้าเดินเข้าไปได้ลึกสุดหลังโต๊ะ (ขอบบนของคอลไลเดอร์ทึบของโต๊ะ)
        /// ไม่มีคอลไลเดอร์ทึบ = 0
        /// </summary>
        public static float ReachDownToSolid(GameObject table, Vector2 ringCenter)
        {
            if (table == null) return 0f;
            float top = float.NegativeInfinity;
            foreach (var c in table.GetComponents<Collider2D>())
            {
                if (c.isTrigger || !c.enabled) continue;
                top = Mathf.Max(top, c.bounds.max.y);
            }
            return float.IsNegativeInfinity(top) ? 0f : Mathf.Max(0f, ringCenter.y - top);
        }

        /// <summary>
        /// ให้วัตถุนี้ (และลูกทั้งหมด) วาดอยู่บนสุดเสมอ แม้เป็นลูกของตัวละครที่ถูกเรียงตาม Y
        /// ใช้กับป้ายเหนือหัว แถบเลือด ตัวนับของที่ถือ
        /// </summary>
        public static void PutOnTop(GameObject go, int order = 0)
        {
            if (go == null) return;
            var group = go.GetComponent<SortingGroup>();
            if (group == null) group = go.AddComponent<SortingGroup>();
            group.sortAtRoot = true;
            group.sortingOrder = Overlay + order;
        }
    }
}
