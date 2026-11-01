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
        private Vector2 centerPoint;
        private Vector2 currentTargetPoint;
        private bool isIdle;
        private float idleTimer;

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

        private void FixedUpdate()
        {
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
                float halfW = rectangleSize.x * 0.5f;
                float halfH = rectangleSize.y * 0.5f;
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
