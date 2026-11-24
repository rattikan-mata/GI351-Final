using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    /// <summary>
    /// ทิศที่ผู้เล่นหัน: หันตามเมาส์ ค่าเริ่มต้นคือหันแค่ซ้าย/ขวา (เมาส์อยู่ฝั่งไหนของตัวก็หันฝั่งนั้น เดินขึ้นลงก็ใช้ท่าข้างเหมือนกัน)
    /// ถ้าอยากได้แบบเดิม (ล็อก 8 ทิศ) เปลี่ยน Aim Mode เป็น EightDirections
    /// อาวุธ, กองของบนหลัง และการทิ้งของ อ่านทิศจากที่นี่ที่เดียว
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerAim : MonoBehaviour
    {
        public enum AimMode
        {
            /// <summary>หันแค่ซ้าย/ขวาตามฝั่งของเมาส์ (FacingDirection เป็น (1,0) หรือ (-1,0) เสมอ)</summary>
            LeftRight,
            /// <summary>หันตามเมาส์แล้วล็อกเป็น 8 ทิศ (ทุก 45 องศา) แบบเดิม</summary>
            EightDirections,
            /// <summary>หันซ้าย/ขวาตามทิศที่เดินล่าสุด (ไม่ใช้เมาส์) อาวุธตีไปทางที่เดิน</summary>
            Movement
        }

        [Header("Aim Settings (หันตามเมาส์)")]
        [Tooltip("LeftRight = หันแค่ซ้าย/ขวา (ค่าเริ่มต้น) / EightDirections = ล็อก 8 ทิศแบบเดิม")]
        [SerializeField] private AimMode aimMode = AimMode.LeftRight;

        [Tooltip("โหมด LeftRight: เมาส์ต้องห่างจากกลางตัวในแนวนอนอย่างน้อยเท่านี้ถึงจะสลับฝั่ง (กันตัวละครพลิกไปมาตอนเมาส์อยู่เหนือหัวพอดี)")]
        [SerializeField] private float sideDeadZone = 0.15f;

        [Tooltip("โหมด EightDirections: ถ้าเมาส์อยู่ใกล้ตัวผู้เล่นกว่าระยะนี้ จะคงทิศเดิมไว้ (กันตัวละครหมุนไปมา)")]
        [SerializeField] private float deadZone = 0.3f;

        [Tooltip("กลับภาพตัวละครซ้าย-ขวาตามทิศที่หัน")]
        [SerializeField] private bool flipSpriteToAim = true;

        [Tooltip("SpriteRenderer ของตัวละคร (ปล่อยว่างจะหาให้อัตโนมัติ)")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("กล้องที่ใช้แปลงตำแหน่งเมาส์ (ปล่อยว่างจะใช้ Camera.main)")]
        [SerializeField] private Camera aimCamera;

        private Vector2 facingDirection = Vector2.right;
        private int facingIndex;
        private Vector2 mouseDirection = Vector2.right;
        private int lockCount;
        private PlayerHealth playerHealth;
        private PlayerMovement movement;

        public AimMode Mode => aimMode;

        /// <summary>ทิศที่หัน (โหมด LeftRight = (±1, 0) / โหมด EightDirections = ล็อก 8 ทิศ ความยาว 1)</summary>
        public Vector2 FacingDirection => facingDirection;

        /// <summary>+1 = หันขวา, -1 = หันซ้าย</summary>
        public float FacingSign => facingDirection.x < -0.01f ? -1f : 1f;

        /// <summary>0 = ขวา, 1 = ขวาบน, 2 = บน, 3 = ซ้ายบน, 4 = ซ้าย, 5 = ซ้ายล่าง, 6 = ล่าง, 7 = ขวาล่าง (โหมด LeftRight มีแค่ 0 กับ 4)</summary>
        public int FacingIndex => facingIndex;

        /// <summary>มุมของทิศที่หัน (องศา)</summary>
        public float FacingAngle => facingIndex * 45f;

        /// <summary>หันขึ้นบน (หันหลังให้กล้อง) เช่น บน, ซ้ายบน, ขวาบน (โหมด LeftRight เป็น false เสมอ)</summary>
        public bool IsFacingAway => aimMode == AimMode.EightDirections && facingDirection.y > 0.1f;

        /// <summary>ทิศจากตัวไปยังเมาส์ล่าสุด (ไม่ล็อกทิศ ความยาว 1) โหมด Movement = ทิศที่เดินล่าสุด</summary>
        public Vector2 MouseDirection => mouseDirection;

        /// <summary>กำลังล็อกทิศอยู่ (เช่น ระหว่างเหวี่ยงอาวุธ) ทิศจะไม่เปลี่ยนตามเมาส์</summary>
        public bool IsFacingLocked => lockCount > 0;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            movement = GetComponent<PlayerMovement>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (aimMode == AimMode.EightDirections)
            {
                // โหมดเดิมเริ่มหันลง
                facingIndex = 6;
                facingDirection = Vector2.down;
            }
        }

        private void OnDisable()
        {
            lockCount = 0;
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead) return;
            RefreshFacing();
        }

        /// <summary>
        /// อ่านเมาส์แล้วอัปเดตทิศเดี๋ยวนี้ (ถ้าไม่ได้ล็อกอยู่) อาวุธเรียกตอนเริ่มตี
        /// เพื่อให้ตีรัวๆ แล้วสลับฝั่งเมาส์ได้ทันที ไม่ต้องรอ Update เฟรมหน้า
        /// </summary>
        public void RefreshFacing()
        {
            if (aimMode == AimMode.Movement)
            {
                // ทิศที่เดินล่าสุด (เดินขึ้น/ลงตรงๆ ยังหันซ้าย/ขวาฝั่งเดิม)
                Vector2 walk = movement != null ? movement.LastFacingDirection : Vector2.zero;
                if (walk.sqrMagnitude > 0.0001f) mouseDirection = walk.normalized;
                if (!IsFacingLocked && Mathf.Abs(walk.x) > 0.01f) SetFacingFromVector(new Vector2(walk.x, 0f));
            }
            else if (TryGetMouseWorld(out Vector2 mouseWorld))
            {
                Vector2 toMouse = mouseWorld - (Vector2)transform.position;
                if (toMouse.sqrMagnitude > 0.0001f) mouseDirection = toMouse.normalized;

                if (!IsFacingLocked)
                {
                    if (aimMode == AimMode.LeftRight)
                    {
                        if (Mathf.Abs(toMouse.x) >= sideDeadZone) SetFacingFromVector(toMouse);
                    }
                    else if (toMouse.magnitude >= deadZone)
                    {
                        SetFacingFromVector(toMouse);
                    }
                }
            }

            if (flipSpriteToAim && spriteRenderer != null)
            {
                if (facingDirection.x > 0.1f) spriteRenderer.flipX = false;
                else if (facingDirection.x < -0.1f) spriteRenderer.flipX = true;
            }
        }

        /// <summary>ล็อกทิศที่หัน (ซ้อนกันได้ ต้อง UnlockFacing ให้ครบจำนวน)</summary>
        public void LockFacing()
        {
            lockCount++;
        }

        public void UnlockFacing()
        {
            lockCount = Mathf.Max(0, lockCount - 1);
        }

        /// <summary>
        /// ตั้งทิศจากเวกเตอร์ใดก็ได้ โหมด LeftRight ใช้แค่ฝั่งซ้าย/ขวา โหมด EightDirections ปัดเป็น 8 ทิศที่ใกล้ที่สุด
        /// </summary>
        public void SetFacingFromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

            if (aimMode == AimMode.LeftRight || aimMode == AimMode.Movement)
            {
                if (Mathf.Abs(direction.x) < 0.0001f) return;
                bool right = direction.x > 0f;
                facingIndex = right ? 0 : 4;
                facingDirection = right ? Vector2.right : Vector2.left;
                return;
            }

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            facingIndex = ((Mathf.RoundToInt(angle / 45f) % 8) + 8) % 8;

            float rad = facingIndex * 45f * Mathf.Deg2Rad;
            facingDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
        }

        private bool TryGetMouseWorld(out Vector2 world)
        {
            world = Vector2.zero;
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera == null) return false;

            Vector3 screen;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            Vector2 p = Mouse.current.position.ReadValue();
            screen = new Vector3(p.x, p.y, 0f);
#elif ENABLE_LEGACY_INPUT_MANAGER
            screen = Input.mousePosition;
#else
            return false;
#endif
            // ระยะ z จากกล้องถึงตัวละคร (จำเป็นสำหรับกล้อง Perspective)
            screen.z = Mathf.Abs(aimCamera.transform.position.z - transform.position.z);
            world = aimCamera.ScreenToWorldPoint(screen);
            return true;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)(facingDirection * 1.2f));
            Gizmos.DrawWireSphere(transform.position, aimMode == AimMode.LeftRight ? sideDeadZone : deadZone);
        }
    }
}
