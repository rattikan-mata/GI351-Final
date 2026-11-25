using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ฟอนต์หลักของเกม (Fredoka) ที่เดียว: ข้อความที่โค้ดสร้างเอง (UI Text / TextMesh ในโลก) ดึงจากที่นี่
    /// ไฟล์อยู่ที่ Assets/Resources/Fonts (โหลดตอนเล่นได้) หาไม่เจอ = ใช้ฟอนต์ในตัวของ Unity แทน
    /// ฟอนต์: Fredoka (SIL Open Font License, ดู Resources/Fonts/OFL.txt) ไม่มีตัวอักษรไทย
    /// </summary>
    public static class GameFont
    {
        /// <summary>ชื่อไฟล์ใน Resources (ไม่ต้องใส่นามสกุล)</summary>
        public const string ResourcePath = "Fonts/Fredoka-SemiBold";

        private static Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            font = null;
        }

        /// <summary>ฟอนต์หลักของเกม</summary>
        public static Font Get()
        {
            if (font == null) font = Resources.Load<Font>(ResourcePath);
            if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        /// <summary>ใส่ฟอนต์หลักให้ TextMesh ในโลก (ต้องเปลี่ยน material ของ MeshRenderer ตามฟอนต์ด้วย ไม่งั้นตัวหนังสือเป็นกล่อง)</summary>
        public static void Apply(TextMesh textMesh)
        {
            if (textMesh == null) return;
            Font f = Get();
            if (f == null) return;
            textMesh.font = f;
            var mr = textMesh.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = f.material;
        }
    }
}
