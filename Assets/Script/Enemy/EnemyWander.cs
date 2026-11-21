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

        [Header("Avoid Others (หลบสัตว์ตัวอื่น กันเดินชนแล้วติดกัน)")]
        [Tooltip("ระยะที่เริ่มเว้นห่างจากสัตว์ตัวอื่น (เมตร)")]
        [SerializeField] private float avoidRadius = 1f;

        [Tooltip("แรงผลักออกจากตัวที่อยู่ใกล้ (0 = ไม่หลบ)")]
        [SerializeField] private float separationStrength = 1.2f;

        [Tooltip("มองไปข้างหน้ากี่เมตร ถ้ามีตัวขวางทางจะเบี่ยงหลบด้านข้าง")]
        [SerializeField] private float lookAhead = 1.4f;

        [Tooltip("แรงเบี่ยงหลบตัวที่ขวางข้างหน้า")]
        [SerializeField] private float sidestepStrength = 1.4f;

        [Tooltip("ความไวในการเลี้ยว (สูง = เลี้ยวทันที, ต่ำ = เลี้ยวนุ่ม)")]
        [SerializeField] private float turnSharpness = 10f;

        [Tooltip("เช็กว่าติดอยู่ทุกกี่วินาที")]
        [SerializeField] private float stuckCheckInterval = 0.4f;

        [Tooltip("ถ้าเข้าใกล้เป้าหมายได้น้อยกว่าสัดส่วนนี้ของระยะที่ควรเดินได้ ถือว่าติด")]
        [Range(0f, 1f)]
        [SerializeField] private float stuckProgressRatio = 0.3f;

        [Tooltip("ติดซ้ำกี่ครั้งติดกันถึงจะเปลี่ยนเป้าหมายใหม่")]
        [SerializeField] private int stuckChecksBeforeRepath = 2;

        [Tooltip("ไม่สุ่มจุดหมายที่อยู่ใกล้สัตว์ตัวอื่นกว่านี้ (เมตร)")]
        [SerializeField] private float targetClearance = 1.2f;

        [Tooltip("ปิด = สัตว์ที่เดินสุ่ม/ยืนพักเดินผ่านกันได้ ไม่ดันกันจนติด (ตัวที่กำลังวิ่งหนียังชนตัวอื่นได้) / เปิด = ชนกันตลอดแบบเดิม")]
        [SerializeField] private bool collideWhileWandering = false;

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

        // หลบตัวอื่น / เช็กติด
        private static readonly System.Collections.Generic.List<EnemyWander> All = new System.Collections.Generic.List<EnemyWander>();
        private Vector2 steerDir;
        private float stuckTimer;
        private float lastTargetDistance;
        private int stuckCount;
        private Collider2D[] bodyColliders;

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
            CacheBodyColliders();

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

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
            RefreshAnimalCollisions();
        }

        private void OnDisable()
        {
            All.Remove(this);
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

            Vector2 desired = dir / distance;
            Vector2 steered = ApplyAvoidance(currentPos, desired, 1f);
            Vector2 moveVelocity = steered * moveSpeed;
            rb.linearVelocity = moveVelocity;
            FaceDirection(moveVelocity);

            CheckStuck(distance);
        }

        /// <summary>
        /// บวกแรงหลบสัตว์ตัวอื่นเข้ากับทิศที่อยากไป:
        /// 1) เว้นระยะ: ตัวที่ใกล้เกิน avoidRadius ผลักออก
        /// 2) เบี่ยงหลบ: ตัวที่ขวางทางข้างหน้า เบี่ยงไปด้านที่ว่าง (ประจันหน้าตรงๆ ทั้งคู่เบี่ยงไปทางขวาของตัวเอง จะเดินสวนกันได้)
        /// แล้วเลี้ยวนุ่มๆ ไม่ให้สั่นกลับหน้าไปมา
        /// </summary>
        private Vector2 ApplyAvoidance(Vector2 pos, Vector2 desired, float weight)
        {
            Vector2 separation = Vector2.zero;
            Vector2 sidestep = Vector2.zero;
            float nearestAhead = float.PositiveInfinity;

            for (int i = 0; i < All.Count; i++)
            {
                EnemyWander other = All[i];
                if (other == this || other == null || other.rb == null) continue;
                if (other.health != null && other.health.IsDead) continue;

                Vector2 toOther = other.rb.position - pos;
                float d = toOther.magnitude;
                if (d < 0.0001f)
                {
                    // ทับกันพอดี ดันออกตามลำดับในลิสต์
                    separation += (i % 2 == 0 ? Vector2.right : Vector2.left);
                    continue;
                }

                if (d < avoidRadius)
                {
                    separation -= toOther / d * (1f - d / avoidRadius);
                }

                // ขวางทางข้างหน้า?
                float forward = Vector2.Dot(toOther, desired);
                if (forward > 0f && forward < lookAhead && forward < nearestAhead)
                {
                    float cross = desired.x * toOther.y - desired.y * toOther.x; // + = อีกตัวอยู่ทางซ้าย
                    if (Mathf.Abs(cross) < avoidRadius)
                    {
                        nearestAhead = forward;
                        Vector2 right = new Vector2(desired.y, -desired.x);
                        // อีกตัวอยู่ซ้าย -> เบี่ยงขวา, อยู่ขวา -> เบี่ยงซ้าย, ตรงกลางพอดี -> ขวา
                        float side = cross > 0.05f ? 1f : cross < -0.05f ? -1f : 1f;
                        float urgency = (1f - forward / lookAhead) * (1f - Mathf.Abs(cross) / avoidRadius);
                        sidestep = right * side * urgency;
                    }
                }
            }

            Vector2 target = desired + (separation * separationStrength + sidestep * sidestepStrength) * weight;
            if (target.sqrMagnitude < 0.0001f) target = new Vector2(desired.y, -desired.x); // แรงหักล้างกันหมด เดินไปด้านข้าง
            target.Normalize();

            if (steerDir.sqrMagnitude < 0.0001f) steerDir = target;
            float k = 1f - Mathf.Exp(-turnSharpness * Time.fixedDeltaTime);
            steerDir = Vector2.Lerp(steerDir, target, k);
            if (steerDir.sqrMagnitude < 0.0001f) steerDir = target;
            return steerDir.normalized;
        }

        /// <summary>
        /// ทุก stuckCheckInterval ดูว่าเข้าใกล้เป้าหมายได้ตามที่ควรไหม ถ้าไม่ (โดนตัวอื่นดัน/ขวางอยู่) ติดกันหลายรอบ
        /// ให้เปลี่ยนเป้าหมายใหม่ไปทางที่ว่าง หรือบางทีก็หยุดพักแป๊บนึงให้อีกตัวเดินผ่านไปก่อน
        /// </summary>
        private void CheckStuck(float distance)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer < stuckCheckInterval) return;

            float expected = moveSpeed * stuckTimer;
            float progress = lastTargetDistance - distance;
            stuckTimer = 0f;
            lastTargetDistance = distance;

            if (expected <= 0f || progress >= expected * stuckProgressRatio)
            {
                stuckCount = 0;
                return;
            }

            stuckCount++;
            if (stuckCount < stuckChecksBeforeRepath) return;

            if (Random.value < 0.35f)
            {
                // หยุดพักสั้นๆ ให้ตัวอื่นไปก่อน
                rb.linearVelocity = Vector2.zero;
                isIdle = true;
                idleTimer = Random.Range(0.3f, 0.8f);
                ResetStuck(0f);
            }
            else
            {
                PickNewTargetPoint();
            }
        }

        #region Animal Collisions (สัตว์ไม่ชนกัน ยกเว้นตัวที่วิ่งหนี)

        private void CacheBodyColliders()
        {
            var list = new System.Collections.Generic.List<Collider2D>();
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true))
            {
                if (c != null && !c.isTrigger) list.Add(c);
            }
            bodyColliders = list.ToArray();
        }

        /// <summary>
        /// ตั้งการชนระหว่างตัวนี้กับสัตว์ทุกตัว: ชนกันเฉพาะคู่ที่มีตัวใดตัวหนึ่งกำลังวิ่งหนีอยู่
        /// ตัวที่เดินสุ่ม/ยืนพักจะเดินผ่านกันได้ ไม่มีทางดันกันจนติด (ส่วนการหลบด้านบนช่วยให้ไม่เดินทับกันให้เห็น)
        /// </summary>
        private void RefreshAnimalCollisions()
        {
            if (bodyColliders == null) CacheBodyColliders();
            for (int i = 0; i < All.Count; i++)
            {
                EnemyWander other = All[i];
                if (other == this || other == null) continue;
                bool collide = collideWhileWandering || isFleeing || other.isFleeing;
                SetCollision(other, collide);
            }
        }

        private void SetCollision(EnemyWander other, bool collide)
        {
            if (bodyColliders == null || other.bodyColliders == null) return;
            foreach (Collider2D a in bodyColliders)
            {
                if (a == null) continue;
                foreach (Collider2D b in other.bodyColliders)
                {
                    if (b != null) Physics2D.IgnoreCollision(a, b, !collide);
                }
            }
        }

        #endregion

        private void ResetStuck(float distance)
        {
            stuckTimer = 0f;
            stuckCount = 0;
            lastTargetDistance = distance;
        }

        private bool IsNearOtherAnimal(Vector2 point, float radius)
        {
            float r2 = radius * radius;
            for (int i = 0; i < All.Count; i++)
            {
                EnemyWander other = All[i];
                if (other == this || other == null || other.rb == null) continue;
                if ((other.rb.position - point).sqrMagnitude < r2) return true;
                // อย่าไปจุดเดียวกับที่อีกตัวกำลังเดินไป
                if (!other.isIdle && !other.isFleeing && (other.currentTargetPoint - point).sqrMagnitude < r2) return true;
            }
            return false;
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

            bool wasFleeing = isFleeing;
            isFleeing = true;
            isIdle = false;
            fleeTimer = fleeDuration;
            slideTimer = 0f;
            if (!wasFleeing) RefreshAnimalCollisions();
        }

        private void EndFlee()
        {
            isFleeing = false;
            slideTimer = 0f;
            RefreshAnimalCollisions();
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

            // หนีแล้วก็ยังเว้นระยะกับตัวอื่น ไม่วิ่งไปกองรวมกันที่มุม
            away = ApplyAvoidance(pos, away, 0.6f);

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
            // ลองสุ่มหลายจุด เอาจุดที่ไม่มีสัตว์ตัวอื่นอยู่/กำลังเดินไป ถ้าติดอยู่ให้ไปคนละทางกับเดิม
            Vector2 pos = rb != null ? rb.position : (Vector2)transform.position;
            Vector2 oldDir = currentTargetPoint - pos;
            bool wasStuck = stuckCount >= stuckChecksBeforeRepath && oldDir.sqrMagnitude > 0.0001f;
            if (wasStuck) oldDir.Normalize();

            Vector2 best = RandomPointInZone();
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 cand = attempt == 0 ? best : RandomPointInZone();
                bool clear = !IsNearOtherAnimal(cand, targetClearance);
                bool newWay = !wasStuck || Vector2.Dot((cand - pos).normalized, oldDir) < 0.3f;
                if (clear && newWay)
                {
                    best = cand;
                    break;
                }
                if (attempt == 0 || clear) best = cand;
            }

            currentTargetPoint = best;
            steerDir = Vector2.zero;
            ResetStuck((currentTargetPoint - pos).magnitude);
        }

        private Vector2 RandomPointInZone()
        {
            if (zoneShape == ZoneShape.Circle)
            {
                return centerPoint + Random.insideUnitCircle * circleRadius;
            }

            // ถ้ามีกรอบ เว้นระยะจากขอบนิดหน่อยไม่ให้เดินไปเบียดขอบ
            float pad = hasBoundary ? BoundaryEdgePadding : 0f;
            float halfW = Mathf.Max(0f, rectangleSize.x * 0.5f - pad);
            float halfH = Mathf.Max(0f, rectangleSize.y * 0.5f - pad);
            return centerPoint + new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));
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
