using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("ความเร็วในการเดินของผู้เล่น")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Visual Settings")]
        [Tooltip("SpriteRenderer ของ Player สำหรับกลับด้านซ้าย-ขวา")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Rigidbody2D rb;
        private Vector2 movementInput;
        private Vector2 lastFacingDirection = Vector2.down;
        private PlayerHealth playerHealth;
        private PlayerAim playerAim;

        public Vector2 MovementInput => movementInput;
        public Vector2 LastFacingDirection => lastFacingDirection;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerHealth = GetComponent<PlayerHealth>();
            playerAim = GetComponent<PlayerAim>();

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            // ตั้งค่า Rigidbody2D ให้เหมาะกับเกม Top-Down 2D
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                movementInput = Vector2.zero;
                return;
            }

            ReadInput();

            // ถ้ามี PlayerAim ให้หันตามเมาส์แทนทิศที่เดิน
            if (playerAim == null)
            {
                HandleFacingDirection();
            }
        }

        private void FixedUpdate()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }

            Move();
        }

        private void ReadInput()
        {
            float h = 0f;
            float v = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) h -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) h += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) v -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) v += 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (h == 0f && v == 0f)
            {
                h = Input.GetAxisRaw("Horizontal");
                v = Input.GetAxisRaw("Vertical");
            }
#endif

            // เดินได้ 8 ทิศทาง (แนวนอน, แนวตั้ง, แนวทแยง 4 ทิศ)
            movementInput = new Vector2(h, v);

            // Normalize เพื่อให้เดินแนวทแยงความเร็วไม่เร็วกว่าเดินทิศตรง
            if (movementInput.sqrMagnitude > 1f)
            {
                movementInput.Normalize();
            }

            if (movementInput != Vector2.zero)
            {
                lastFacingDirection = movementInput;
            }
        }

        private Vector2 knockbackVelocity;
        private float knockbackTimer;

        private void Move()
        {
            if (knockbackTimer > 0f)
            {
                knockbackTimer -= Time.fixedDeltaTime;
                // ในช่วงโดนผลัก (Knockback) ให้ตัวละครเคลื่อนตามแรงผลักและชะลอลงอย่างนุ่มนวล
                rb.linearVelocity = knockbackVelocity + (movementInput * (moveSpeed * 0.2f));
                knockbackVelocity = Vector2.Lerp(knockbackVelocity, Vector2.zero, Time.fixedDeltaTime * 12f);
            }
            else
            {
                // เคลื่อนที่ตาม Rigidbody2D ปกติ
                rb.linearVelocity = movementInput * moveSpeed;
            }
        }

        /// <summary>
        /// ผลักตัวละครให้กระเด็น (Knockback) ตามทิศทางและแรงที่กำหนด
        /// </summary>
        public void ApplyKnockback(Vector2 direction, float force, float duration = 0.2f)
        {
            if (direction == Vector2.zero)
            {
                direction = -lastFacingDirection;
            }
            knockbackVelocity = direction.normalized * force;
            knockbackTimer = duration;
        }

        private void HandleFacingDirection()
        {
            // หันหน้าซ้าย-ขวาตามทิศที่เดิน
            if (spriteRenderer != null)
            {
                if (movementInput.x > 0.05f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (movementInput.x < -0.05f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }

        /// <summary>
        /// ปรับความเร็วเดินจากโค้ดหรือบัฟไอเทม
        /// </summary>
        public void SetMoveSpeed(float newSpeed)
        {
            moveSpeed = newSpeed;
        }
    }
}
