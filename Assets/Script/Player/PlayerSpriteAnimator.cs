using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// สลับภาพตัวละครผู้เล่นตามทิศที่เดิน: เดินหน้า / เดินหลัง / เดินข้าง และท่ายืน (กะพริบตาเป็นระยะ)
    /// ท่ายืนหันหน้า: ค้างเฟรม 0 (ลืมตา) ทุก blinkEvery วินาที เล่นกะพริบ 0 -> 1 (ตาปรือ) -> 2 (หลับตา) แล้วกลับเฟรม 0
    /// ภาพเดินข้างต้องวาดหันขวา (การหันซ้าย PlayerMovement / PlayerAim พลิก flipX ให้เอง)
    /// หยุดเดินแล้ว: ถ้าเดินขึ้นอยู่ จะยืนหันหลัง / เดินข้างอยู่ จะยืนหันข้าง / นอกนั้นยืนหันหน้า
    /// </summary>
    public class PlayerSpriteAnimator : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D body;

        [Header("Frames (ภาพแต่ละท่า)")]
        [Tooltip("ท่ายืนหันหน้า: เฟรม 0 = ลืมตา (ค้างไว้), 1 = ตาปรือ, 2 = หลับตา (เล่น 0 -> 1 -> 2 ตอนกะพริบ) ถ้ามีไม่ถึง 3 เฟรมจะโชว์แค่เฟรม 0")]
        [SerializeField] private Sprite[] idle;

        [Tooltip("เดินลง (หันหน้า): เฟรม 0 = ยืนตรง, 1-2 = ก้าวเท้า")]
        [SerializeField] private Sprite[] walkFront;

        [Tooltip("เดินขึ้น (หันหลัง): เฟรม 0 = ยืนตรง, 1-2 = ก้าวเท้า")]
        [SerializeField] private Sprite[] walkBack;

        [Tooltip("เดินข้าง (วาดหันขวา): เฟรม 0 = ยืนตรง, 1-2 = ก้าวเท้า")]
        [SerializeField] private Sprite[] walkSide;

        [Header("Timing (ความเร็ว)")]
        [Tooltip("เดินกี่เฟรมต่อวินาที")]
        [SerializeField] private float walkFps = 9f;

        [Tooltip("กะพริบตาทุกกี่วินาที (สุ่มในช่วงนี้)")]
        [SerializeField] private Vector2 blinkEvery = new Vector2(2.5f, 4.5f);

        [Tooltip("ตอนกะพริบ โชว์เฟรมตาปรือ / หลับตา เฟรมละกี่วินาที")]
        [SerializeField] private float blinkFrameTime = 0.07f;

        [Tooltip("ความเร็วขั้นต่ำที่นับว่ากำลังเดิน")]
        [SerializeField] private float moveThreshold = 0.15f;

        public enum Facing { Front, Back, Side }

        private Facing facing = Facing.Front;

        /// <summary>ตอนนี้ตัวละครหันทางไหน (หน้า / หลัง / ข้าง) ให้กระบองถือตามทิศ</summary>
        public Facing CurrentFacing => facing;

        private bool attackFacing;

        /// <summary>ระหว่างเหวี่ยงอาวุธ: หันไปทางที่ตี (ไม่หันตามทิศเดิน) จนกว่าจะ ClearAttackFacing</summary>
        public void SetAttackFacing(Facing f)
        {
            attackFacing = true;
            facing = f;
        }

        public void ClearAttackFacing()
        {
            attackFacing = false;
        }
        private float walkTimer;
        private float nextBlink;
        private float blinkStart = -999f;

        // ลำดับเฟรมเดิน: ก้าวซ้าย -> ยืน -> ก้าวขวา -> ยืน
        private static readonly int[] WalkCycle = { 1, 0, 2, 0 };

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            nextBlink = Time.time + Random.Range(blinkEvery.x, blinkEvery.y);
        }

        private void LateUpdate()
        {
            if (spriteRenderer == null) return;

            Vector2 v = body != null ? body.linearVelocity : Vector2.zero;
            bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;

            if (moving)
            {
                if (!attackFacing)
                {
                    facing = Mathf.Abs(v.x) > Mathf.Abs(v.y) * 0.9f ? Facing.Side : (v.y > 0f ? Facing.Back : Facing.Front);
                }
                walkTimer += Time.deltaTime * walkFps;
                Sprite[] set = facing == Facing.Side ? walkSide : facing == Facing.Back ? walkBack : walkFront;
                Set(Pick(set, WalkCycle[(int)walkTimer % WalkCycle.Length]));
                return;
            }

            walkTimer = 0f;

            // ยืนนิ่ง: หันหลัง/หันข้าง ใช้ท่ายืนของทิศนั้น
            if (facing == Facing.Back) { Set(Pick(walkBack, 0)); return; }
            if (facing == Facing.Side) { Set(Pick(walkSide, 0)); return; }

            // หันหน้า: ท่ายืน + กะพริบตา (ระหว่างตีลงใช้ท่ายืนเฉยๆ ไม่กะพริบ)
            if (attackFacing) { Set(Pick(walkFront, 0)); return; }
            if (idle == null || idle.Length == 0) { Set(Pick(walkFront, 0)); return; }
            if (idle.Length < 3) { Set(idle[0]); return; }

            // ค้างเฟรม 0 (ลืมตา) ถึงเวลากะพริบแล้วเล่นเฟรม 1 -> 2 เฟรมละ blinkFrameTime แล้วกลับเฟรม 0
            float now = Time.time;
            float frameTime = Mathf.Max(0.01f, blinkFrameTime);
            if (now >= nextBlink)
            {
                blinkStart = now;
                nextBlink = now + frameTime * (idle.Length - 1) + Random.Range(blinkEvery.x, blinkEvery.y);
            }
            float step = (now - blinkStart) / frameTime;
            Set(idle[step < idle.Length - 1 ? (int)step + 1 : 0]);
        }

        private static Sprite Pick(Sprite[] set, int i)
        {
            if (set == null || set.Length == 0) return null;
            return set[Mathf.Clamp(i, 0, set.Length - 1)];
        }

        private void Set(Sprite s)
        {
            if (s != null && spriteRenderer.sprite != s) spriteRenderer.sprite = s;
        }
    }
}
