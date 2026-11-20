using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวเลขทั้งหมดของ juice ตอนตี/ฆ่าสัตว์ (hit stop, squash, death pop, เนื้อบินโค้ง, camera shake) ปรับได้ที่ asset นี้ที่เดียว
    /// asset จริงอยู่ที่ Assets/Settings/JuiceConfig.asset แล้วลากใส่ช่อง Juice ของ EnemyHealth (Chicken / Pig prefab)
    /// ถ้าช่อง Juice ว่าง จะใช้ค่าเริ่มต้นในโค้ดแทน (ค่าเดียวกับที่ตั้งไว้ด้านล่าง)
    /// </summary>
    [CreateAssetMenu(fileName = "JuiceConfig", menuName = "GI351/Juice Config")]
    public class JuiceConfig : ScriptableObject
    {
        [Header("Pixel Grid (ตารางพิกเซล)")]
        [Tooltip("Pixels Per Unit ของเกม (pixel art ใช้ 16 = 1 พิกเซล 0.0625 หน่วย) ระยะที่ขยับทุกอย่างจะปัดลงตารางนี้")]
        [Min(1f)] public float pixelsPerUnit = 16f;

        [Header("1) Hit Stop (หยุดเกมสั้นๆ ตอนโดนตี)")]
        public bool hitStopEnabled = true;

        [Tooltip("เวลาหยุดตอนตีโดนแต่ยังไม่ตาย (วินาที ตามเวลาจริง)")]
        [Min(0f)] public float hitStopOnHit = 0.05f;

        [Tooltip("เวลาหยุดตอนตีจนตาย (วินาที ตามเวลาจริง)")]
        [Min(0f)] public float hitStopOnKill = 0.09f;

        [Header("1) Squash (บี้ sprite สัตว์ตอนโดนตี ไม่ได้บี้ root ที่มี collider)")]
        public bool squashEnabled = true;

        [Tooltip("สเกลตอนบี้สุด (x กว้างขึ้น, y เตี้ยลง)")]
        public Vector2 squashScale = new Vector2(1.25f, 0.8f);

        [Tooltip("เวลาที่บี้สุด (วินาทีนับจากโดนตี)")]
        [Min(0.001f)] public float squashPeakTime = 0.05f;

        [Tooltip("เวลาที่กลับเป็นปกติ (วินาทีนับจากโดนตี)")]
        [Min(0.002f)] public float squashEndTime = 0.10f;

        [Tooltip("นับเวลา squash แบบ unscaled = เล่นต่อเนื่องแม้ช่วง hit stop (เด้งกลับพอดีตอนเกมเดินต่อ)")]
        public bool squashUnscaledTime = true;

        [Tooltip("ล็อกเท้าไว้ที่เดิมตอนบี้ (ตัวเตี้ยลงแต่ไม่ลอย) ระยะชดเชยปัดลงตารางพิกเซล")]
        public bool squashKeepFeetPlanted = true;

        [Header("2) Death Pop (สัตว์ตาย)")]
        public bool deathPopEnabled = true;

        [Tooltip("ขยายตอนตายเป็นกี่เท่าก่อนหดหาย")]
        [Min(1f)] public float deathPunchScale = 1.15f;

        [Tooltip("เวลาขยาย (วินาที)")]
        [Min(0f)] public float deathPunchTime = 0.12f;

        public Ease deathPunchEase = Ease.OutQuad;

        [Tooltip("เวลาหดเป็น 0 (วินาที) ก่อนจะ destroy")]
        [Min(0f)] public float deathShrinkTime = 0.25f;

        public Ease deathShrinkEase = Ease.InBack;

        [Header("2) Meat Drop (เนื้อที่หล่นบินโค้ง)")]
        [Tooltip("ปิด = เนื้อวางลงพื้นตรงนั้นเลยแบบเดิม")]
        public bool meatFlightEnabled = true;

        [Tooltip("เนื้อโผล่ scale 0 -> 1 ใช้เวลา (วินาที)")]
        [Min(0f)] public float meatSpawnScaleTime = 0.3f;

        public Ease meatSpawnScaleEase = Ease.OutSine;

        [Tooltip("เวลาบินโค้งจนตกพื้น (วินาที)")]
        [Min(0.05f)] public float meatFlightTime = 0.45f;

        [Tooltip("สุ่มเวลาบินเพิ่ม/ลดได้อีก ± กี่วินาที ให้แต่ละชิ้นตกไม่พร้อมกัน (Juice King: 0.4–1.1s)")]
        [Min(0f)] public float meatFlightTimeJitter = 0.1f;

        [Tooltip("การเลื่อนบนพื้นตอนกระเด็น OutQuad = พุ่งออกเร็วแล้วค่อยๆ ช้าลง (Linear = ความเร็วเท่ากันแบบเดิม)")]
        public Ease meatGroundEase = Ease.OutQuad;

        [Tooltip("เนื้อหมุนกี่รอบระหว่างบิน (สุ่มทิศหมุน ตกพื้นแล้วตั้งตรงพอดี) 0 = ไม่หมุน")]
        [Range(0f, 3f)] public float meatSpinTurns = 1f;

        [Tooltip("ระยะที่ root เลื่อนบนพื้น (หน่วย) สุ่มระหว่าง x ถึง y")]
        public Vector2 meatGroundDistance = new Vector2(0.5f, 1f);

        [Tooltip("ความสูงสูงสุดของ sprite ตอนบิน (หน่วย) สุ่มระหว่าง x ถึง y")]
        public Vector2 meatArcHeight = new Vector2(1f, 1.5f);

        [Tooltip("ถ้าตายแล้วดรอปหลายชิ้น แต่ละชิ้นจะแยกทิศเท่าๆ กันแล้วสุ่มเบี่ยงได้อีกกี่องศา (ไม่เกิน 1/4 ของมุมที่แบ่ง)")]
        [Range(0f, 90f)] public float meatDirectionJitter = 25f;

        [Tooltip("เด้งหลังตกพื้นครั้งแรก สูงกี่เปอร์เซ็นต์ของความสูงที่บิน (0.3 = 30%)")]
        [Range(0f, 1f)] public float meatBounceHeightPercent = 0.3f;

        [Tooltip("เวลาเด้ง 1 ครั้ง (วินาที)")]
        [Min(0f)] public float meatBounceTime = 0.15f;

        [Tooltip("สเกลตอน squash ที่ตกพื้นจริง (x กว้างขึ้น, y เตี้ยลง)")]
        public Vector2 meatLandSquashScale = new Vector2(1.2f, 0.8f);

        [Tooltip("เวลา squash ตอนตกพื้น (วินาที)")]
        [Min(0f)] public float meatLandSquashTime = 0.1f;

        [Tooltip("ขนาดเงาใต้เนื้อ กว้าง x สูง (หน่วย)")]
        public Vector2 meatShadowSize = new Vector2(0.6f, 0.22f);

        [Tooltip("สีเงา (ดำโปร่ง)")]
        public Color meatShadowColor = new Color(0f, 0f, 0f, 0.35f);

        [Tooltip("ขยับเงาขึ้น/ลงจากขอบล่างของเนื้อ (หน่วย)")]
        public float meatShadowYOffset = 0f;

        [Tooltip("เนื้อสูงขึ้นแล้วเงาเล็กลงเหลือกี่เท่า (เมื่อสูงสุด)")]
        [Range(0.2f, 1f)] public float meatShadowMinScale = 0.55f;

        [Tooltip("ให้เก็บเนื้อได้หลังตกพื้นเท่านั้น (ปิด collider ตอนบิน) ปิดช่องนี้ = เก็บได้ระหว่างบิน")]
        public bool meatPickupOnlyAfterLanding = true;

        [Header("4) Magnet Pickup (เนื้อดูดเข้าตัวแบบ Juice King)")]
        [Tooltip("เนื้อที่ตกจากสัตว์จะลอยเข้าหาผู้เล่นเองเมื่อเข้าใกล้ (ใช้กับเนื้อที่ดรอปจากสัตว์เท่านั้น ของที่กด Q ทิ้งไม่ดูด)")]
        public bool magnetEnabled = true;

        [Tooltip("ระยะที่เริ่มดูด (หน่วย จากกลางตัวผู้เล่น)")]
        [Min(0f)] public float magnetRadius = 2f;

        [Tooltip("ตกพื้นแล้วรอกี่วินาทีก่อนดูดได้ (ให้เห็นจังหวะกระเด็นก่อน)")]
        [Min(0f)] public float magnetDelayAfterLanding = 0.15f;

        [Tooltip("ก่อนพุ่ง: ป๊อปขยายเป็นกี่เท่า (ท่าเตรียมตัว)")]
        [Min(1f)] public float magnetAnticipationScale = 1.2f;

        [Tooltip("ก่อนพุ่ง: เวลาป๊อป (วินาที)")]
        [Min(0f)] public float magnetAnticipationTime = 0.08f;

        [Tooltip("เวลาพุ่งเข้าตัว (วินาที) Juice King ใช้ 0.5")]
        [Min(0.05f)] public float magnetFlyTime = 0.28f;

        [Tooltip("InQuad / InCubic = เริ่มช้าแล้วเร่งเข้าตัว (รู้สึกเหมือนถูกดูด)")]
        public Ease magnetFlyEase = Ease.InQuad;

        [Tooltip("ความโค้งตอนพุ่ง (หน่วย) 0 = เส้นตรง")]
        [Min(0f)] public float magnetArcHeight = 0.5f;

        [Tooltip("ระหว่างพุ่งหดเหลือกี่เท่าตอนถึงตัว")]
        [Range(0.1f, 1f)] public float magnetEndScale = 0.7f;

        [Tooltip("ถึงตัวแล้ว กองของบนหลังเด้งขยายเป็นกี่เท่า (Juice King: punch 1.15)")]
        [Min(1f)] public float magnetStackPunchScale = 1.15f;

        [Tooltip("เวลาเด้งของกองบนหลัง (วินาที) (Juice King: 0.12)")]
        [Min(0f)] public float magnetStackPunchTime = 0.12f;

        [Header("3) Camera Shake (กล้องสั่น หน่วยเป็นพิกเซลของเกม)")]
        public bool shakeEnabled = true;

        [Tooltip("ตอนตีโดน: ความแรง (พิกเซล)")]
        [Min(0f)] public float shakeOnHitPixels = 1f;

        [Tooltip("ตอนตีโดน: ระยะเวลา (วินาที ตามเวลาจริง)")]
        [Min(0f)] public float shakeOnHitTime = 0.1f;

        [Tooltip("ตอนฆ่า: ความแรง (พิกเซล) สุ่มระหว่าง x ถึง y")]
        public Vector2 shakeOnKillPixels = new Vector2(2f, 3f);

        [Tooltip("ตอนฆ่า: ระยะเวลา (วินาที ตามเวลาจริง)")]
        [Min(0f)] public float shakeOnKillTime = 0.15f;
    }
}
