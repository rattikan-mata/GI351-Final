using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    /// <summary>
    /// ทิศที่ผู้เล่นหัน: หันตามเมาส์ แล้วล็อกเป็น 8 ทิศ (ทุก 45 องศา)
    /// อาวุธ, กองของบนหลัง และการทิ้งของ อ่านทิศจากที่นี่ที่เดียว
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class PlayerAim : MonoBehaviour
    {
        [Header("Aim Settings (หันตามเมาส์ 8 ทิศ)")]
        [Tooltip("ถ้าเมาส์อยู่ใกล้ตัวผู้เล่นกว่าระยะนี้ จะคงทิศเดิมไว้ (กันตัวละครหมุนไปมา)")]
        [SerializeField] private float deadZone = 0.3f;

        [Tooltip("กลับภาพตัวละครซ้าย-ขวาตามทิศที่หัน")]
        [SerializeField] private bool flipSpriteToAim = true;

        [Tooltip("SpriteRenderer ของตัวละคร (ปล่อยว่างจะหาให้อัตโนมัติ)")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Tooltip("กล้องที่ใช้แปลงตำแหน่งเมาส์ (ปล่อยว่างจะใช้ Camera.main)")]
        [SerializeField] private Camera aimCamera;

        private Vector2 facingDirection = Vector2.down;
        private int facingIndex = 6;
        private PlayerHealth playerHealth;

        /// <summary>ทิศที่หัน (ล็อก 8 ทิศ ความยาว 1)</summary>
        public Vector2 FacingDirection => facingDirection;

        /// <summary>0 = ขวา, 1 = ขวาบน, 2 = บน, 3 = ซ้ายบน, 4 = ซ้าย, 5 = ซ้ายล่าง, 6 = ล่าง, 7 = ขวาล่าง</summary>
        public int FacingIndex => facingIndex;

        /// <summary>มุมของทิศที่หัน (องศา)</summary>
        public float FacingAngle => facingIndex * 45f;

        /// <summary>หันขึ้นบน (หันหลังให้กล้อง) เช่น บน, ซ้ายบน, ขวาบน</summary>
        public bool IsFacingAway => facingDirection.y > 0.1f;

        private void Awake()
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            if (TryGetMouseWorld(out Vector2 mouseWorld))
            {
                Vector2 toMouse = mouseWorld - (Vector2)transform.position;
                if (toMouse.magnitude >= deadZone)
                {
                    SetFacingFromVector(toMouse);
                }
            }

            if (flipSpriteToAim && spriteRenderer != null)
            {
                if (facingDirection.x > 0.1f) spriteRenderer.flipX = false;
                else if (facingDirection.x < -0.1f) spriteRenderer.flipX = true;
            }
        }

        /// <summary>
        /// ตั้งทิศจากเวกเตอร์ใดก็ได้ แล้วปัดเป็น 8 ทิศที่ใกล้ที่สุด
        /// </summary>
        public void SetFacingFromVector(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f) return;

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
            Gizmos.DrawWireSphere(transform.position, deadZone);
        }
    }
}
