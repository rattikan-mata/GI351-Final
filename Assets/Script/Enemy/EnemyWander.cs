using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyWander : MonoBehaviour
    {
        public enum ZoneShape { Circle, Rectangle }

        [Header("Movement Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเร็วในการเดินสุ่ม")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("เวลาหยุดพักขั้นต่ำเมื่อเดินถึงจุดหมาย (วินาที)")]
        [SerializeField] private float minIdleDuration = 1.5f;

        [Tooltip("เวลาหยุดพักสูงสุดเมื่อเดินถึงจุดหมาย (วินาที)")]
        [SerializeField] private float maxIdleDuration = 3.5f;

        [Header("Wander Zone (ขอบเขตการเดิน ปรับได้ใน Inspector)")]
        [Tooltip("รูปแบบโซน (วงกลม หรือ สี่เหลี่ยม)")]
        [SerializeField] private ZoneShape zoneShape = ZoneShape.Circle;

        [Tooltip("ใช้จุดเกิดเป็นจุดศูนย์กลางของโซนอัตโนมัติ")]
        [SerializeField] private bool useSpawnPositionAsCenter = true;

        [Tooltip("จุดศูนย์กลางของโซน (ถ้าไม่ติ๊ก useSpawnPositionAsCenter)")]
        [SerializeField] private Vector2 customCenter;

        [Tooltip("รัศมีของโซนเมื่อเลือกเป็น Circle")]
        [SerializeField] private float circleRadius = 4f;

        [Tooltip("ขนาดกว้าง x ยาวของโซนเมื่อเลือกเป็น Rectangle")]
        [SerializeField] private Vector2 rectangleSize = new Vector2(6f, 4f);

        [Header("Gizmo Settings (แสดงผลโซนใน Scene View)")]
        [Tooltip("สีของโซนในหน้าต่าง Scene View")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.9f, 0.4f, 0.25f);

        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private EnemyHealth health;
        private Vector2 centerPoint;
        private Vector2 currentTargetPoint;
        private bool isIdle;
        private float idleTimer;

        // ขอบเขตที่ห้ามออก (ตั้งจาก EnemySpawner)
        private bool hasBoundary;
        private Vector2 boundaryMin;
        private Vector2 boundaryMax;
        private const float BoundaryEdgePadding = 0.3f;

        public float MoveSpeed
        {
            get => moveSpeed;
            set => moveSpeed = value;
        }

        public Vector2 CenterPoint => centerPoint;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            health = GetComponent<EnemyHealth>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;

            centerPoint = useSpawnPositionAsCenter ? (Vector2)transform.position : customCenter;
        }

        private void Start()
        {
            PickNewTargetPoint();
        }

        private void Update()
        {
            if (isIdle)
            {
                idleTimer -= Time.deltaTime;
                if (idleTimer <= 0f)
                {
                    isIdle = false;
                    PickNewTargetPoint();
                }
            }
        }

        /// <summary>
        /// ล็อกให้เดินอยู่แค่ในกรอบสี่เหลี่ยมนี้ (ใช้กรอบเป็นโซนเดินสุ่มด้วย)
        /// </summary>
        public void SetBoundary(Vector2 center, Vector2 size)
        {
            hasBoundary = true;
            Vector2 half = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y)) * 0.5f;
            boundaryMin = center - half;
            boundaryMax = center + half;

            zoneShape = ZoneShape.Rectangle;
            centerPoint = center;
            rectangleSize = new Vector2(Mathf.Abs(size.x), Mathf.Abs(size.y));
        }

        /// <summary>
        /// ดึงตัวกลับเข้ากรอบถ้าหลุดออกไป (เช่น โดนตีกระเด็น) และตัดความเร็วที่พุ่งออกนอกกรอบ
        /// </summary>
        private void KeepInsideBoundary()
        {
            Vector2 pos = rb.position;
            Vector2 clamped = new Vector2(
                Mathf.Clamp(pos.x, boundaryMin.x, boundaryMax.x),
                Mathf.Clamp(pos.y, boundaryMin.y, boundaryMax.y));

            if (clamped == pos) return;

            rb.position = clamped;
            Vector2 vel = rb.linearVelocity;
            if ((pos.x < boundaryMin.x && vel.x < 0f) || (pos.x > boundaryMax.x && vel.x > 0f)) vel.x = 0f;
            if ((pos.y < boundaryMin.y && vel.y < 0f) || (pos.y > boundaryMax.y && vel.y > 0f)) vel.y = 0f;
            rb.linearVelocity = vel;
        }

        private void FixedUpdate()
        {
            if (hasBoundary) KeepInsideBoundary();

            // ระหว่างกระเด็น ปล่อยให้ EnemyHealth ควบคุมความเร็ว
            if (health != null && health.IsKnockedBack) return;

            if (!isIdle)
            {
                MoveTowardsTarget();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        private void MoveTowardsTarget()
        {
            Vector2 currentPos = rb.position;
            Vector2 dir = currentTargetPoint - currentPos;
            float distance = dir.magnitude;

            if (distance <= 0.15f)
            {
                // ถึงจุดหมายแล้ว เริ่มหยุดพัก
                rb.linearVelocity = Vector2.zero;
                isIdle = true;
                idleTimer = Random.Range(minIdleDuration, maxIdleDuration);
                return;
            }

            Vector2 moveVelocity = dir.normalized * moveSpeed;
            rb.linearVelocity = moveVelocity;

            // กลับหน้าซ้าย-ขวา
            if (spriteRenderer != null)
            {
                if (moveVelocity.x > 0.05f)
                {
                    spriteRenderer.flipX = false;
                }
                else if (moveVelocity.x < -0.05f)
                {
                    spriteRenderer.flipX = true;
                }
            }
        }

        public void PickNewTargetPoint()
        {
            if (zoneShape == ZoneShape.Circle)
            {
                Vector2 randomOffset = Random.insideUnitCircle * circleRadius;
                currentTargetPoint = centerPoint + randomOffset;
            }
            else
            {
                // ถ้ามีกรอบ เว้นระยะจากขอบนิดหน่อยไม่ให้เดินไปเบียดขอบ
                float pad = hasBoundary ? BoundaryEdgePadding : 0f;
                float halfW = Mathf.Max(0f, rectangleSize.x * 0.5f - pad);
                float halfH = Mathf.Max(0f, rectangleSize.y * 0.5f - pad);
                float rx = Random.Range(-halfW, halfW);
                float ry = Random.Range(-halfH, halfH);
                currentTargetPoint = centerPoint + new Vector2(rx, ry);
            }
        }

        private void OnDrawGizmos()
        {
            Vector2 c = useSpawnPositionAsCenter && !Application.isPlaying ? (Vector2)transform.position : (Application.isPlaying ? centerPoint : customCenter);

            Gizmos.color = gizmoColor;
            if (zoneShape == ZoneShape.Circle)
            {
                Gizmos.DrawSphere(new Vector3(c.x, c.y, 0f), circleRadius);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
                Gizmos.DrawWireSphere(new Vector3(c.x, c.y, 0f), circleRadius);
            }
            else
            {
                Gizmos.DrawCube(new Vector3(c.x, c.y, 0f), new Vector3(rectangleSize.x, rectangleSize.y, 0.1f));
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
                Gizmos.DrawWireCube(new Vector3(c.x, c.y, 0f), new Vector3(rectangleSize.x, rectangleSize.y, 0.1f));
            }

            // ถ้ากำลังเล่นอยู่ วาดเป้าหมายที่กำลังเดินไป
            if (Application.isPlaying && !isIdle)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetPoint);
                Gizmos.DrawWireSphere(currentTargetPoint, 0.15f);
            }
        }
    }
}
