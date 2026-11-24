using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// สัตว์เดินแบบกระโดดเด้ง + โยกซ้ายขวา (ใช้ภาพท่ายืนภาพเดียว ไม่ต้องมีเฟรมเดิน)
    /// ค่าตั้งต้นมาจากเดโม Dude Hop Lab ที่ผู้เล่นเลือกเอง: 3 ก้าว/วินาที เด้งสูง 17px โยก 25 องศา (ตัวสูง 60px) เส้นโค้ง abs(sin)
    /// ยิ่งเดินเร็ว ยิ่งเด้งแรง ยืนนิ่งไม่ขยับ แต่ละตัวสุ่มเฟสไม่ให้เด้งพร้อมกัน
    /// ขยับแค่ child "Visual" (ตัวที่ถือ SpriteRenderer) ไม่แตะ root ที่มี collider / Rigidbody2D
    /// และบวกทับตำแหน่งที่ EnemyJuice ตั้ง (ตอนโดนตีสั่น) ได้ ไม่ชนกัน
    /// </summary>
    [DisallowMultipleComponent]
    public class AnimalHop : MonoBehaviour
    {
        [Tooltip("ก้าวต่อวินาที ตอนเดินเต็มความเร็ว")]
        [SerializeField] private float stepsPerSecond = 3f;

        [Tooltip("ความสูงเด้ง เทียบกับความสูงตัว (17px / 60px = 0.28)")]
        [SerializeField] private float hopHeightRatio = 17f / 60f;

        [Tooltip("มุมโยกซ้ายขวาสูงสุด (องศา)")]
        [SerializeField] private float swayDegrees = 25f;

        [Tooltip("ความเร็วที่นับว่า 'เต็ม' (เด้งเต็มที่) 0 = ใช้ความเร็วเดินของ EnemyWander")]
        [SerializeField] private float fullSpeed = 0f;

        [Tooltip("เปลี่ยนระดับการเด้งนุ่มๆ เวลาออกตัว/หยุด (ต่อวินาที)")]
        [SerializeField] private float ampSmoothing = 10f;

        [Tooltip("ภาพที่จะขยับ (ว่าง = หา SpriteRenderer ลูกตัวแรก)")]
        [SerializeField] private Transform visual;

        private Rigidbody2D rb;
        private EnemyWander wander;
        private EnemyHealth health;
        private float phase;
        private float amp;
        private float hopHeight;
        private Vector3 writtenPos;
        private Vector3 lastOffset;
        private bool wrote;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            wander = GetComponent<EnemyWander>();
            health = GetComponent<EnemyHealth>();
            if (visual == null)
            {
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                if (sr != null && sr.transform != transform) visual = sr.transform;
            }

            phase = Random.Range(0f, 2f);
            hopHeight = 0.5f * hopHeightRatio;
            if (visual != null)
            {
                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    hopHeight = sr.sprite.bounds.size.y * Mathf.Abs(visual.lossyScale.y) * hopHeightRatio;
                }
            }
        }

        private void LateUpdate()
        {
            if (visual == null) return;

            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
            float full = fullSpeed > 0f ? fullSpeed : (wander != null ? wander.MoveSpeed : 2f);
            float target = Mathf.Clamp01(speed / Mathf.Max(0.01f, full));
            if (health != null && health.IsDead) target = 0f;
            amp = Mathf.MoveTowards(amp, target, ampSmoothing * Time.deltaTime);

            float u = Time.time * stepsPerSecond + phase;
            float s = Mathf.Sin(u * Mathf.PI);
            float hop = Mathf.Abs(s);

            // ถ้าคนอื่น (EnemyJuice) ตั้งตำแหน่งใหม่ไปแล้ว ใช้ค่านั้นเป็นฐาน ไม่ต้องลบ offset เก่า
            Vector3 pos = visual.localPosition;
            Vector3 basePos = wrote && pos == writtenPos ? pos - lastOffset : pos;

            Vector3 parentScale = visual.parent != null ? visual.parent.lossyScale : Vector3.one;
            float localHop = hopHeight / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y));
            lastOffset = new Vector3(0f, hop * localHop * amp, 0f);
            writtenPos = basePos + lastOffset;
            visual.localPosition = writtenPos;
            wrote = true;

            visual.localRotation = Quaternion.Euler(0f, 0f, s * swayDegrees * amp);
        }

        private void OnDisable()
        {
            if (visual == null || !wrote) return;
            if (visual.localPosition == writtenPos) visual.localPosition -= lastOffset;
            visual.localRotation = Quaternion.identity;
            wrote = false;
        }
    }
}
