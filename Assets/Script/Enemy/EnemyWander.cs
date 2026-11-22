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

        [Tooltip("มองทางข้างหน้ากี่เมตรตอนวิ่งหนี ถ้าเจอกำแพง/น้ำ/ขอบโซน จะเลี้ยวไปทางที่โล่งก่อนถึง ไม่วิ่งไปติดกำแพง")]
        [Min(0.3f)]
        [SerializeField] private float fleeWallLookAhead = 2.2f;

        [Header("Flee Between Areas (วิ่งหนีข้ามโซนล่า)")]
        [Tooltip("เปิด = ตอนวิ่งหนี วิ่งผ่านช่องประตูที่เปิดแล้วไปโซนล่าอื่นได้ (หนีไปถึงแล้วจะอยู่โซนใหม่ต่อ)")]
        [SerializeField] private bool fleeToOtherAreas = true;

        [Tooltip("โอกาสที่ตัวที่วิ่งหนีจะมุ่งไปช่องประตู (ถ้าช่องไม่ได้อยู่ทางผู้เล่น)")]
        [Range(0f, 1f)]
        [SerializeField] private float crossAreaChance = 0.6f;

        [Header("Panic (ตีตัวหนึ่ง ตัวรอบๆ แตกตื่นหนีตาม)")]
        [Tooltip("เปิด = ตัวนี้โดนตีแล้ว สัตว์ตัวอื่นในรัศมีด้านล่างวิ่งหนีไปด้วย (ตัวที่ปิด Flee When Hit จะไม่หนีตาม)")]
        [SerializeField] private bool panicNearbyOnHit = true;

        [Tooltip("รัศมีที่สัตว์ตัวอื่นจะตกใจตาม (เมตร)")]
        [Min(0f)]
        [SerializeField] private float panicRadius = 3.5f;

        [Tooltip("ตัวที่อยู่ไกลสุดในรัศมีจะเริ่มหนีช้ากว่าตัวที่อยู่ใกล้เท่านี้ (วินาที) ให้ดูเป็นคลื่นตกใจกระจายออก")]
        [Min(0f)]
        [SerializeField] private float panicSpreadDelay = 0.25f;

        [Tooltip("ตัวที่ตกใจตามวิ่งหนีนานเท่านี้ของ fleeDuration (1 = นานเท่าตัวที่โดนตี)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float panicDurationScale = 0.75f;

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
        private Vector2 lastFleeDir;
        private float fleeStuckTimer;

        // วิ่งหนีข้ามโซน
        private HuntArea homeArea;
        private bool isCrossing;
        private HuntArea.Link crossLink;
        private bool crossPassedLink;
        private HuntArea crossFrom;
        private static readonly RaycastHit2D[] wallHits = new RaycastHit2D[8];
        private const int FleeDirections = 16;
        private static readonly ContactFilter2D WallFilter = new ContactFilter2D { useTriggers = false };

        // ตกใจตามตัวอื่น (รอดีเลย์ก่อนเริ่มหนี)
        private float pendingPanicTimer = -1f;

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
            // อยู่ในระบบโซนล่า: ใช้กรอบของโซน/ทางเชื่อมที่ตัวอยู่ตอนนี้ (หนีข้ามโซนไปแล้ว เนื้อจะตกในโซนนั้น)
            if (UsesAreaRegion && HuntArea.TryGetRegionRectAt(Position, out Rect r))
            {
                min = r.min;
                max = r.max;
                return true;
            }
            min = boundaryMin;
            max = boundaryMax;
            return hasBoundary;
        }

        private Vector2 Position => rb != null ? rb.position : (Vector2)transform.position;

        /// <summary>เดินอยู่ในโซนล่า (HuntArea) และวิ่งข้ามไปโซนอื่นที่เปิดแล้วได้</summary>
        private bool UsesAreaRegion => fleeToOtherAreas && homeArea != null;

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
            if (pendingPanicTimer >= 0f)
            {
                pendingPanicTimer -= Time.deltaTime;
                if (pendingPanicTimer < 0f) StartFlee(fleeDuration * panicDurationScale);
            }

            if (isFleeing)
            {
                // ให้กระเด็นเสร็จก่อนค่อยเริ่มนับเวลาหนี
                if (health == null || !health.IsKnockedBack)
                {
                    fleeTimer -= Time.deltaTime;
                    // หมดเวลาตอนอยู่กลางช่องประตู: วิ่งต่อให้พ้นช่องก่อน ไม่ยืนขวางทาง
                    if (fleeTimer <= 0f && UsesAreaRegion && HuntArea.FindOpenAreaAt(Position) == null) fleeTimer = 0.2f;
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

            homeArea = HuntArea.FindAreaAt(center);
        }

        /// <summary>
        /// ดึงตัวกลับเข้ากรอบถ้าหลุดออกไป (เช่น โดนตีกระเด็น) และตัดความเร็วที่พุ่งออกนอกกรอบ
        /// อยู่ในระบบโซนล่า: กรอบ = ทุกโซนที่เปิดแล้ว + ช่องประตูที่เชื่อมกัน
        /// </summary>
        private void KeepInsideBoundary()
        {
            Vector2 pos = rb.position;
            Vector2 clamped;
            if (UsesAreaRegion)
            {
                if (HuntArea.IsInsideRoamRegion(pos)) return;
                clamped = HuntArea.ClampToRoamRegion(pos);
            }
            else
            {
                clamped = new Vector2(
                    Mathf.Clamp(pos.x, boundaryMin.x, boundaryMax.x),
                    Mathf.Clamp(pos.y, boundaryMin.y, boundaryMax.y));
            }

            if (clamped == pos) return;

            rb.position = clamped;
            Vector2 vel = rb.linearVelocity;
            Vector2 push = pos - clamped; // ทิศที่หลุดออกไป
            if ((push.x < 0f && vel.x < 0f) || (push.x > 0f && vel.x > 0f)) vel.x = 0f;
            if ((push.y < 0f && vel.y < 0f) || (push.y > 0f && vel.y > 0f)) vel.y = 0f;
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
            if (!enabled) return;
            if (fleeWhenHit) StartFlee();
            // ตัวที่ตายในทีเดียวก็ยังทำให้ตัวรอบๆ ตกใจ
            if (panicNearbyOnHit) PanicNearby();
        }

        /// <summary>สัตว์ตัวอื่นในรัศมี panicRadius วิ่งหนีตาม (ตัวใกล้หนีก่อน ตัวไกลหนีทีหลัง)</summary>
        public void PanicNearby()
        {
            if (panicRadius <= 0f) return;
            Vector2 pos = transform.position;
            float r2 = panicRadius * panicRadius;

            for (int i = 0; i < All.Count; i++)
            {
                EnemyWander other = All[i];
                if (other == null || other == this || !other.enabled || !other.fleeWhenHit) continue;
                if (other.health != null && other.health.IsDead) continue;

                float d2 = ((Vector2)other.transform.position - pos).sqrMagnitude;
                if (d2 > r2) continue;

                float delay = panicSpreadDelay * Mathf.Sqrt(d2) / panicRadius;
                other.QueuePanic(delay);
            }
        }

        /// <summary>ตกใจตามตัวอื่น: เริ่มหนีหลังดีเลย์ (ถ้ากำลังหนีอยู่แล้วจะต่อเวลาให้)</summary>
        public void QueuePanic(float delay)
        {
            if (isFleeing)
            {
                fleeTimer = Mathf.Max(fleeTimer, fleeDuration * panicDurationScale);
                return;
            }
            if (pendingPanicTimer >= 0f) pendingPanicTimer = Mathf.Min(pendingPanicTimer, delay);
            else pendingPanicTimer = Mathf.Max(0f, delay);
        }

        /// <summary>เริ่ม (หรือเริ่มนับใหม่) การวิ่งหนีผู้เล่นตาม fleeSpeed / fleeDuration</summary>
        public void StartFlee()
        {
            StartFlee(fleeDuration);
        }

        /// <summary>เริ่มวิ่งหนีนาน duration วินาที (ถ้ากำลังหนีอยู่และเหลือเวลานานกว่าจะไม่ลดเวลาลง)</summary>
        public void StartFlee(float duration)
        {
            pendingPanicTimer = -1f;
            if (duration <= 0f || fleeSpeed <= 0f) return;
            if (health != null && health.IsDead) return;

            if (playerTarget == null)
            {
                PlayerMovement player = FindAnyObjectByType<PlayerMovement>();
                if (player != null) playerTarget = player.transform;
            }

            bool wasFleeing = isFleeing;
            isFleeing = true;
            isIdle = false;
            fleeTimer = wasFleeing ? Mathf.Max(fleeTimer, duration) : duration;
            if (!wasFleeing)
            {
                lastFleeDir = Vector2.zero;
                fleeStuckTimer = 0f;
                ChooseCrossing();
                RefreshAnimalCollisions();
            }
        }

        private void EndFlee()
        {
            isFleeing = false;
            isCrossing = false;

            // หนีไปถึงโซนอื่นแล้ว อยู่โซนใหม่ต่อเลย
            if (UsesAreaRegion)
            {
                HuntArea now = HuntArea.FindOpenAreaAt(Position);
                if (now != null && now != homeArea)
                {
                    Rect r = now.InnerRect;
                    SetBoundary(r.center, r.size);
                    homeArea = now;
                }
            }

            RefreshAnimalCollisions();
            PickNewTargetPoint();
        }

        /// <summary>
        /// ตอนเริ่มหนี สุ่มว่าจะมุ่งไปช่องประตูที่เปิดแล้วไหม (เลือกช่องที่ไม่ได้อยู่ทางผู้เล่น)
        /// </summary>
        private void ChooseCrossing()
        {
            isCrossing = false;
            crossPassedLink = false;
            if (!UsesAreaRegion || Random.value > crossAreaChance) return;

            Vector2 pos = Position;
            HuntArea here = HuntArea.FindOpenAreaAt(pos);
            if (here == null) return;

            Vector2 away = AwayFromPlayer(pos);
            float best = -0.15f; // ช่องที่อยู่ทางผู้เล่นมากๆ ไม่ไป
            var links = HuntArea.Links;
            for (int i = 0; i < links.Count; i++)
            {
                HuntArea.Link l = links[i];
                if (l.a != here && l.b != here) continue;
                Vector2 to = l.Center - pos;
                if (to.sqrMagnitude < 0.0001f) continue;
                float score = Vector2.Dot(to.normalized, away);
                if (score > best)
                {
                    best = score;
                    crossLink = l;
                    crossFrom = here;
                    isCrossing = true;
                }
            }

            // ตัวที่มุ่งไปช่องประตู หนีนานขึ้นให้พ้นไปถึงอีกโซน
            if (isCrossing) fleeTimer = Mathf.Max(fleeTimer, fleeDuration * 1.6f);
        }

        private Vector2 AwayFromPlayer(Vector2 pos)
        {
            Vector2 away = playerTarget != null ? pos - (Vector2)playerTarget.position : Vector2.zero;
            if (away.sqrMagnitude < 0.0001f)
            {
                // ไม่รู้ว่าผู้เล่นอยู่ไหน (หรือทับกันพอดี) วิ่งออกจากกลางโซนแทน
                away = pos - centerPoint;
                if (away.sqrMagnitude < 0.0001f) away = Vector2.right;
            }
            return away.normalized;
        }

        /// <summary>วิ่งออกห่างจากผู้เล่นด้วย fleeSpeed มองทางข้างหน้า ถ้าจะชนกำแพง/น้ำ/ขอบโซนเลี้ยวไปทางที่โล่งก่อน</summary>
        private void FleeStep()
        {
            Vector2 pos = rb.position;
            Vector2 away = AwayFromPlayer(pos);
            Vector2 goal = away;

            if (isCrossing)
            {
                // มุ่งไปกลางช่องประตู ผ่านช่องแล้วมุ่งเข้ากลางโซนอีกฝั่ง
                HuntArea dest = crossLink.Other(crossFrom);
                if (dest == null || !dest.IsOpen) isCrossing = false;
                else
                {
                    if (!crossPassedLink && (crossLink.Center - pos).sqrMagnitude < 0.6f * 0.6f) crossPassedLink = true;
                    Vector2 target = crossPassedLink ? (Vector2)dest.transform.position : crossLink.Center;
                    Vector2 to = target - pos;
                    if (crossPassedLink && dest.InnerRect.Contains(pos) && to.magnitude < 2f) isCrossing = false;
                    else if (to.sqrMagnitude > 0.0001f) goal = (to.normalized * 1.4f + away * 0.4f).normalized;
                }
            }

            // ยังติดอยู่ (ความเร็วจริงต่ำมากสักพัก): เลิกมุ่งไปช่องประตู และไม่ยึดทิศเดิม ให้เลือกทางใหม่
            if (rb.linearVelocity.magnitude < fleeSpeed * 0.3f) fleeStuckTimer += Time.fixedDeltaTime;
            else fleeStuckTimer = 0f;
            if (fleeStuckTimer > 0.4f)
            {
                fleeStuckTimer = 0f;
                isCrossing = false;
                lastFleeDir = -lastFleeDir;
            }

            // หนีแล้วก็ยังเว้นระยะกับตัวอื่น ไม่วิ่งไปกองรวมกันที่มุม
            Vector2 clear = ChooseClearDirection(pos, goal);
            Vector2 dir = ApplyAvoidance(pos, clear, 0.6f);
            lastFleeDir = dir;
            rb.linearVelocity = dir * fleeSpeed;
            FaceDirection(dir);
        }

        /// <summary>
        /// ลองหลายทิศรอบตัว ให้คะแนน = ตรงกับทิศที่อยากไป + ทางข้างหน้าโล่งแค่ไหน + ต่อเนื่องจากทิศเดิม
        /// ทิศที่อีกไม่กี่ก้าวจะชนกำแพง/น้ำ/ขอบโซนโดนหักคะแนนหนัก เลยเลี้ยวเลียบกำแพงออกไปแทนการวิ่งเข้าไปติด
        /// </summary>
        private Vector2 ChooseClearDirection(Vector2 pos, Vector2 goal)
        {
            Vector2 best = goal;
            float bestScore = float.NegativeInfinity;
            for (int i = 0; i <= FleeDirections; i++)
            {
                Vector2 d;
                if (i == FleeDirections) d = goal;
                else
                {
                    float a = i * Mathf.PI * 2f / FleeDirections;
                    d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                }

                // อยากไปทางนี้แค่ไหน (0..1) x ทางโล่งแค่ไหน (ยกกำลังสอง: ทางที่อีกไม่ไกลจะชนแทบไม่ได้คะแนน)
                // ติดมุมโดยมีผู้เล่นไล่หลัง ทางที่ชนะคือทางเลียบกำแพงออกไป ไม่ใช่ซอกเล็กๆ เข้ามุม
                float clear = Clearance(pos, d) / fleeWallLookAhead; // 0..1
                float interest = (Vector2.Dot(d, goal) + 1f) * 0.5f;
                float score = interest * clear * clear;
                if (lastFleeDir.sqrMagnitude > 0.0001f) score += Mathf.Max(0f, Vector2.Dot(d, lastFleeDir)) * clear * 0.15f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = d;
                }
            }
            return best;
        }

        /// <summary>วิ่งไปทิศนี้ได้อีกกี่เมตรก่อนชนกำแพง/น้ำ (Collider ที่ไม่ขยับ) หรือหลุดขอบพื้นที่ (สูงสุด fleeWallLookAhead)</summary>
        private float Clearance(Vector2 pos, Vector2 d)
        {
            float max = fleeWallLookAhead;

            // ขอบพื้นที่ที่วิ่งได้
            const float step = 0.3f;
            for (float t = step; t <= max; t += step)
            {
                Vector2 q = pos + d * t;
                bool inside = UsesAreaRegion
                    ? HuntArea.IsInsideRoamRegion(q)
                    : !hasBoundary || (q.x >= boundaryMin.x + fleeEdgeMargin && q.x <= boundaryMax.x - fleeEdgeMargin
                                       && q.y >= boundaryMin.y + fleeEdgeMargin && q.y <= boundaryMax.y - fleeEdgeMargin);
                if (!inside) { max = t - step; break; }
            }

            // กำแพง/น้ำ/ประตูที่ยังไม่เปิด ใช้รูปร่างตัวจริงของสัตว์กวาดไป (ไม่นับสัตว์ ผู้เล่น และของที่ขยับได้)
            int n = rb.Cast(d, WallFilter, wallHits, max);
            for (int i = 0; i < n; i++)
            {
                Collider2D c = wallHits[i].collider;
                if (c == null || c.isTrigger) continue;
                if (c.attachedRigidbody != null && (c.attachedRigidbody == rb || c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic)) continue;
                if (wallHits[i].distance <= 0.001f)
                {
                    // ชิดกำแพงอยู่แล้ว: ทิศที่ดันเข้ากำแพงไปไม่ได้เลย ทิศที่ไถเลียบ/ออกจากกำแพงไม่นับ
                    if (Vector2.Dot(wallHits[i].normal, d) < -0.1f) return 0f;
                    continue;
                }
                max = Mathf.Min(max, wallHits[i].distance);
            }
            return Mathf.Max(0f, max);
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
