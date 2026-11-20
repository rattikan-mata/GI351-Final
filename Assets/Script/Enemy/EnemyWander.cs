using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyWander : MonoBehaviour
    {
        public enum ZoneShape { Circle, Rectangle }

        [Header("Movement Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเร็วในการเดินสุ่ม (ตั้งที่ Prefab ของสัตว์แต่ละตัว ไม่มีสคริปต์อื่นแก้ทับ)")]
        [SerializeField] private float moveSpeed = 2f;

        [Tooltip("เวลาหยุดพักขั้นต่ำเมื่อเดินถึงจุดหมาย (วินาที)")]
        [SerializeField] private float minIdleDuration = 1.5f;

        [Tooltip("เวลาหยุดพักสูงสุดเมื่อเดินถึงจุดหมาย (วินาที)")]
        [SerializeField] private float maxIdleDuration = 3.5f;

        [Header("Flee (วิ่งหนีเมื่อโดนตี)")]
        [Tooltip("เปิด = โดนตีแล้ววิ่งหนีผู้เล่น (หมู: ให้ตั้ง PigEnemy.onHitReaction แทน ตัวนั้นจะเป็นตัวกำหนดว่าหนีหรือพุ่งชน)")]
        [SerializeField] private bool fleeWhenHit = true;

        [Tooltip("ความเร็วตอนวิ่งหนี (แยกจาก moveSpeed ที่เป็นความเร็วเดินสุ่ม)")]
        [SerializeField] private float fleeSpeed = 3.5f;

        [Tooltip("วิ่งหนีนานกี่วินาที (เริ่มนับหลังกระเด็นเสร็จ) แล้วกลับไปเดินสุ่ม โดนตีซ้ำระหว่างหนีจะนับใหม่")]
        [SerializeField] private float fleeDuration = 2f;

        [Tooltip("ระยะที่ถือว่าชิดขอบเขต (เมตร) ถ้าวิ่งหนีมาถึงจะเลี้ยวไปตามขอบ ไม่วิ่งทะลุและไม่ยืนติดขอบ")]
        [SerializeField] private float fleeEdgeMargin = 0.35f;

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

        // วิ่งหนี
        private bool isFleeing;
        private float fleeTimer;
        private Transform playerTarget;
        private Vector2 slideDir;
        private float slideTimer;
        private const float SlideCommitTime = 0.6f;

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

        public float FleeSpeed
        {
            get => fleeSpeed;
            set => fleeSpeed = value;
        }

        public float FleeDuration => fleeDuration;

        /// <summary>เปิด/ปิดการวิ่งหนีเมื่อโดนตี (PigEnemy ใช้สลับระหว่างหนี/พุ่งชน)</summary>
        public bool FleeWhenHit
        {
            get => fleeWhenHit;
            set => fleeWhenHit = value;
        }

        public bool IsFleeing => isFleeing;

        public Vector2 CenterPoint => centerPoint;

        /// <summary>กรอบที่สัตว์ถูกขังไว้ (ตั้งจาก EnemySpawner) คืน false ถ้าไม่มีกรอบ</summary>
        public bool TryGetBoundary(out Vector2 min, out Vector2 max)
        {
            min = boundaryMin;
            max = boundaryMax;
            return hasBoundary;
        }

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

            // โดนตีเมื่อไหร่ก็เริ่มวิ่งหนี (ถ้าเปิดไว้)
            if (health != null)
            {
                health.onDamaged.AddListener(HandleDamaged);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.onDamaged.RemoveListener(HandleDamaged);
            }
        }

        private void Update()
        {
            if (isFleeing)
            {
                // ให้กระเด็นเสร็จก่อนค่อยเริ่มนับเวลาหนี
                if (health == null || !health.IsKnockedBack)
                {
                    fleeTimer -= Time.deltaTime;
                    if (fleeTimer <= 0f) EndFlee();
                }
                return;
            }

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

            if (isFleeing)
            {
                FleeStep();
                return;
            }

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
            FaceDirection(moveVelocity);
        }

        /// <summary>กลับหน้าซ้าย-ขวาตามทิศที่เคลื่อนที่</summary>
        private void FaceDirection(Vector2 velocity)
        {
            if (spriteRenderer == null) return;

            if (velocity.x > 0.05f)
            {
                spriteRenderer.flipX = false;
            }
            else if (velocity.x < -0.05f)
            {
                spriteRenderer.flipX = true;
            }
        }

        #region Flee (วิ่งหนี)

        private void HandleDamaged()
        {
            if (!fleeWhenHit || !enabled) return;
            StartFlee();
        }

        /// <summary>เริ่ม (หรือเริ่มนับใหม่) การวิ่งหนีผู้เล่นตาม fleeSpeed / fleeDuration</summary>
        public void StartFlee()
        {
            if (fleeDuration <= 0f || fleeSpeed <= 0f) return;
            if (health != null && health.IsDead) return;

            if (playerTarget == null)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null) playerTarget = player.transform;
            }

            isFleeing = true;
            isIdle = false;
            fleeTimer = fleeDuration;
            slideTimer = 0f;
        }

        private void EndFlee()
        {
            isFleeing = false;
            slideTimer = 0f;
            PickNewTargetPoint();
        }

        /// <summary>วิ่งออกห่างจากผู้เล่นด้วย fleeSpeed ถ้าชนขอบเขตให้เลี้ยวไปตามขอบ</summary>
        private void FleeStep()
        {
            Vector2 pos = rb.position;

            Vector2 away = playerTarget != null ? pos - (Vector2)playerTarget.position : Vector2.zero;
            if (away.sqrMagnitude < 0.0001f)
            {
                // ไม่รู้ว่าผู้เล่นอยู่ไหน (หรือทับกันพอดี) วิ่งออกจากกลางโซนแทน
                away = pos - centerPoint;
                if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
            }
            away.Normalize();

            Vector2 dir = hasBoundary ? SteerAlongEdges(pos, away) : away;
            rb.linearVelocity = dir * fleeSpeed;
            FaceDirection(dir);
        }

        /// <summary>
        /// ถ้าทิศหนีพุ่งเข้าหาขอบเขต ตัดส่วนที่พุ่งออกนอกแล้วไถไปตามขอบแทน
        /// ถ้าเกือบตรงเข้าหาขอบ/มุม จะเลือกทิศไถไปตามขอบที่ห่างผู้เล่นที่สุด แล้วยึดทิศนั้นสักพัก (กันสั่นไปมาติดมุม)
        /// </summary>
        private Vector2 SteerAlongEdges(Vector2 pos, Vector2 away)
        {
            float m = fleeEdgeMargin;
            bool wallMinX = pos.x <= boundaryMin.x + m;
            bool wallMaxX = pos.x >= boundaryMax.x - m;
            bool wallMinY = pos.y <= boundaryMin.y + m;
            bool wallMaxY = pos.y >= boundaryMax.y - m;

            // กำลังไถตามทิศที่ยึดไว้ ใช้ต่อจนหมดเวลาหรือจะชนกำแพงใหม่
            if (slideTimer > 0f)
            {
                slideTimer -= Time.fixedDeltaTime;
                if (!PushesIntoWall(slideDir, wallMinX, wallMaxX, wallMinY, wallMaxY)) return slideDir;
                slideTimer = 0f;
            }

            bool blockX = (wallMinX && away.x < 0f) || (wallMaxX && away.x > 0f);
            bool blockY = (wallMinY && away.y < 0f) || (wallMaxY && away.y > 0f);
            if (!blockX && !blockY) return away;

            Vector2 dir = away;
            if (blockX) dir.x = 0f;
            if (blockY) dir.y = 0f;

            // ยังเหลือแรงไถไปตามขอบมากพอ ใช้ทิศนั้นได้เลย
            if (dir.magnitude >= 0.5f) return dir.normalized;

            // ตรงเข้าหาขอบ/มุม: เลือกทิศที่ไม่ชนกำแพงและไกลผู้เล่นที่สุด (เสมอกันเลือกเข้าหากลางโซน)
            Vector2 toCenter = ((boundaryMin + boundaryMax) * 0.5f - pos).normalized;
            Vector2 best = Vector2.zero;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i < 4; i++)
            {
                Vector2 cand = i == 0 ? Vector2.up : i == 1 ? Vector2.down : i == 2 ? Vector2.left : Vector2.right;
                if (PushesIntoWall(cand, wallMinX, wallMaxX, wallMinY, wallMaxY)) continue;

                float score = Vector2.Dot(cand, away) + 0.01f * Vector2.Dot(cand, toCenter);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = cand;
                }
            }

            // หาทิศไม่ได้เลย (โซนแคบมาก) วิ่งเข้ากลางโซน
            if (best == Vector2.zero) best = toCenter;

            slideDir = best;
            slideTimer = SlideCommitTime;
            return slideDir;
        }

        private bool PushesIntoWall(Vector2 d, bool wallMinX, bool wallMaxX, bool wallMinY, bool wallMaxY)
        {
            return (wallMinX && d.x < 0f) || (wallMaxX && d.x > 0f) || (wallMinY && d.y < 0f) || (wallMaxY && d.y > 0f);
        }

        #endregion

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
            if (Application.isPlaying && !isIdle && !isFleeing)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetPoint);
                Gizmos.DrawWireSphere(currentTargetPoint, 0.15f);
            }
        }
    }
}
