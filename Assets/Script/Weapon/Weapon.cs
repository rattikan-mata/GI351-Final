using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    /// <summary>
    /// กระบองของผู้เล่น: หันแค่ซ้าย/ขวา (ทิศจาก PlayerAim) ท่าถือลอยข้างตัวด้านหน้า และท่าตี 3 จังหวะ
    ///   1) ง้าง (Windup)        หมุนกระบองขึ้นเหนือหัวเยื้องไปด้านหลัง ยังไม่ทำดาเมจ
    ///   2) ฟาด (Strike)          กวาดลงด้านหน้าเร็วๆ ดาเมจ + knockback + juice เกิดเฉพาะช่วงนี้ ศัตรูโดนได้ครั้งเดียวต่อการฟาด
    ///   3) ตามแรง (FollowThrough) ค่อยๆ กลับเข้าท่าถือ (กดตีระหว่างช่วงนี้ = จำไว้ตีต่อทันทีที่จบ)
    /// ความช้าของการตีมาจากท่าเหวี่ยง ไม่ใช่คูลดาวน์ (attackCooldown ตั้งเป็น 0)
    /// มุมทั้งหมดในหน้านี้วัดแบบ "หันขวา": 0° = ชี้ไปข้างหน้า, + = ชี้ขึ้น, - = ชี้ลง (ตอนหันซ้ายสะท้อนให้เอง)
    /// ระยะเป็นหน่วยโลก (world units) วัดจากกลางตัวผู้เล่น
    /// </summary>
    public class Weapon : MonoBehaviour
    {
        public enum SwingPhase
        {
            Idle,
            Windup,
            Strike,
            FollowThrough
        }

        [Header("Facing (หันซ้าย/ขวา ทิศจาก PlayerAim)")]
        [Tooltip("ล็อกทิศไม่ให้พลิกซ้าย/ขวาระหว่างง้างและฟาด")]
        [SerializeField] private bool lockFacingDuringSwing = true;

        [Tooltip("ล็อกทิศต่อไปจนจบช่วงตามแรงด้วย (ปิด = หันกลับได้ตั้งแต่ฟาดเสร็จ)")]
        [SerializeField] private bool lockFacingDuringFollowThrough = true;

        [Header("Weapon Visual (ภาพกระบอง)")]
        [Tooltip("Prefab ของอาวุธที่จะโชว์ (ลาก Prefab กระบองมาใส่)")]
        [SerializeField] private GameObject weaponPrefab;

        [Tooltip("GameObject แสดงตัวอาวุธในฉาก (ถ้าว่างจะดึงจาก weaponPrefab หรือค้นหาให้อัตโนมัติ)")]
        [SerializeField] private GameObject weaponVisualObject;

        [Tooltip("ปรับองศาชดเชยของรูปกระบอง (ถ้าภาพวาดเฉียง ให้ใส่ -45 หรือ 45) รูปต้องวาดให้ปลายกระบองชี้ไปทางแกน X")]
        [SerializeField] private float spriteRotationOffset = 0f;

        [Tooltip("ลำดับการวาดของกระบอง = ลำดับของตัวละคร + ค่านี้ (บวก = อยู่หน้าตัวละคร)")]
        [SerializeField] private int weaponSortingOrderOffset = 10;

        [Tooltip("Pixels Per Unit ของเกม (pixel art ใช้ 16) ใช้ปัดตำแหน่งกระบองให้ลงตารางพิกเซล")]
        [SerializeField] private float pixelsPerUnit = 16f;

        [Tooltip("ปัดตำแหน่งกระบองลงตารางพิกเซล")]
        [SerializeField] private bool snapToPixel = true;

        [Header("Hold Pose (ท่าถือ: ลอยข้างตัวด้านหน้า)")]
        [Tooltip("ตำแหน่งกลางกระบองจากกลางตัว (x = ไปข้างหน้า ตอนหันซ้ายสะท้อนให้เอง, y = สูงขึ้น)")]
        [SerializeField] private Vector2 holdOffset = new Vector2(0.45f, -0.1f);

        [Tooltip("มุมกระบองตอนถือ (องศา) 90 = ตั้งตรง, 60 = เอียงไปข้างหน้า 30° หัวกระบองชี้ขึ้น")]
        [SerializeField] private float holdAngle = 60f;

        [Tooltip("ระยะที่ลอยขึ้นลง (idle bob) 0.0625 = 1 พิกเซล")]
        [SerializeField] private float holdBobHeight = 0.0625f;

        [Tooltip("เวลาที่ลอยขึ้นลงครบ 1 รอบ (วินาที)")]
        [SerializeField] private float holdBobPeriod = 0.8f;

        [Tooltip("ความหน่วงตอนตามตัวผู้เล่นที่กำลังเดิน (วินาที) ยิ่งมากยิ่งช้า 0 = ติดตัวเป๊ะ")]
        [SerializeField] private float holdFollowSmoothTime = 0.06f;

        [Tooltip("เวลาที่กระบองเลื่อนข้ามไปอีกฝั่งตอนหันกลับ (วินาที)")]
        [SerializeField] private float sideSlideTime = 0.08f;

        [Header("Swing (ท่าตี 3 จังหวะ)")]
        [Tooltip("จุดหมุนของท่าเหวี่ยงจากกลางตัว (x = ไปข้างหน้า, y = สูงขึ้น) ครึ่งวงกลมของ hitbox ก็ใช้จุดนี้เป็นศูนย์กลาง")]
        [SerializeField] private Vector2 swingPivotOffset = new Vector2(0.1f, 0.15f);

        [Tooltip("ระยะจากจุดหมุนถึงกลางกระบองตอนเหวี่ยง")]
        [SerializeField] private float swingRadius = 0.55f;

        [Tooltip("1) ง้าง: เวลา (วินาที)")]
        [SerializeField] private float windupTime = 0.30f;

        [Tooltip("1) ง้าง: มุมปลายกระบอง (องศา) 110 = เหนือหัวเยื้องไปด้านหลังนิดหน่อย")]
        [SerializeField] private float windupAngle = 110f;

        [SerializeField] private Ease windupEase = Ease.OutQuad;

        [Tooltip("2) ฟาด: เวลา (วินาที)")]
        [SerializeField] private float strikeTime = 0.10f;

        [Tooltip("2) ฟาด: มุมสุดท้าย (องศา) -60 = กวาดลงด้านหน้าต่ำกว่าแนวนอน")]
        [SerializeField] private float strikeEndAngle = -60f;

        [SerializeField] private Ease strikeEase = Ease.InCubic;

        [Tooltip("3) ตามแรง: เวลา (วินาที) ที่ค่อยๆ กลับเข้าท่าถือ")]
        [SerializeField] private float followThroughTime = 0.40f;

        [SerializeField] private Ease followThroughEase = Ease.InOutSine;

        [Header("Input Buffer (จำปุ่มตี)")]
        [Tooltip("กดตีระหว่างท่ากำลังจะจบ จะจำไว้ตีต่อทันทีที่จบ ค่านี้ = กี่วินาทีสุดท้ายของท่าที่รับปุ่ม (0.4 = ตลอดช่วงตามแรง, 0.15 = แค่ 0.15 วินาทีสุดท้าย)")]
        [SerializeField] private float inputBufferTime = 0.4f;

        [Header("Weapon Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเสียหายต่อการโจมตี 1 ครั้ง")]
        [SerializeField] private int damage = 25;

        [Tooltip("รัศมีของ hitbox ครึ่งวงกลมฝั่งที่หัน (หน่วยโลก) วัดจากจุดหมุน")]
        [SerializeField] private float attackRange = 1.0f;

        [Tooltip("คูลดาวน์ก่อนตีครั้งถัดไป (วินาที นับจากเริ่มตี) ตั้ง 0 เพราะความช้าอยู่ในท่าเหวี่ยงแล้ว")]
        [SerializeField] private float attackCooldown = 0f;

        [Tooltip("แรงผลักเมื่อศัตรูโดนโจมตี")]
        [SerializeField] private float knockbackForce = 3f;

        [Tooltip("ศัตรูที่ล้ำไปด้านหลังเส้นกลางตัวเกินระยะนี้จะไม่โดน (เผื่อตัวใหญ่ที่ทับตัวผู้เล่นอยู่)")]
        [SerializeField] private float hitBackTolerance = 0.05f;

        [Tooltip("กระบองกวาดผ่านศัตรูแล้วถึงจะโดน: โดนเมื่อมุมกระบองลงมาถึงมุมของศัตรู + ค่านี้ (องศา) ตั้งสูงๆ = โดนเร็วขึ้น")]
        [SerializeField] private float hitSweepLeadAngle = 10f;

        [Header("Targeting Settings")]
        [Tooltip("เลเยอร์ของเป้าหมายที่จะโดนโจมตี (เช่น Enemy) ว่าง = ทุก Layer")]
        [SerializeField] private LayerMask targetLayer;

        [Header("Swoosh (แสงกวาด)")]
        [SerializeField] private bool swooshEnabled = true;

        [Tooltip("สีและความทึบของแสงกวาด (ขาวจางๆ)")]
        [SerializeField] private Color swooshColor = new Color(1f, 1f, 1f, 0.55f);

        [Tooltip("ความกว้างของแสงที่ลากตามหลังกระบอง (องศา)")]
        [SerializeField] private float swooshArcAngle = 70f;

        [Tooltip("รัศมีด้านในของแสงกวาด (หน่วยโลก)")]
        [SerializeField] private float swooshInnerRadius = 0.55f;

        [Tooltip("รัศมีด้านนอกของแสงกวาด (หน่วยโลก)")]
        [SerializeField] private float swooshOuterRadius = 1.1f;

        [Tooltip("เวลาที่แสงจางหายหลังฟาดเสร็จ (วินาที)")]
        [SerializeField] private float swooshFadeTime = 0.1f;

        [Header("Events")]
        public UnityEvent onAttack;

        private Transform owner;
        private PlayerMovement playerMovement;
        private PlayerHealth playerHealth;
        private PlayerAim playerAim;
        private SpriteRenderer playerSpriteRenderer;
        private Transform weaponPivot;
        private SpriteRenderer weaponSpriteRenderer;
        private SpriteRenderer[] weaponRenderers;

        private SwingPhase phase = SwingPhase.Idle;
        private float phaseTimer;
        private float swingSide = 1f;
        private readonly HashSet<IDamageable> swingHits = new HashSet<IDamageable>();
        private bool facingLockedByMe;
        private bool bufferedAttack;
        private float bufferedUntil;
        private float nextAttackTime;

        // ท่าถือ / ท่าปัจจุบัน
        private float sideBlend = 1f;
        private Vector3 smoothedAnchor;
        private bool anchorInit;
        private float bobTime;
        private Vector2 lastPoseOffset;
        private float lastPoseAngle;
        private bool poseInit;
        private Vector2 windupStartOffset;
        private float windupStartAngle;
        private Vector2 strikeEndOffset;
        private float strikeEndWorldAngle;

        // แสงกวาด
        private GameObject swooshObject;
        private SpriteRenderer swooshRenderer;
        private float swooshAlpha;
        private bool swooshFading;
        private float swooshFacingAngle;
        private float swooshSide = 1f;
        private static Sprite swooshSprite;
        private static string swooshSpriteKey;

        public int Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public bool IsSwinging => phase != SwingPhase.Idle;
        public SwingPhase Phase => phase;
        public float SwingSide => swingSide;

        // ถ้าลืมตั้ง Target Layer (เป็น Nothing) ให้ชนทุก Layer แทน เพื่อไม่ให้ฟันแล้วไม่โดนอะไรเลย
        private LayerMask EffectiveTargetLayer => targetLayer.value == 0 ? (LayerMask)(~0) : targetLayer;

        private Vector3 OwnerPosition => owner != null ? owner.position : transform.position;

        private void OnEnable()
        {
            // อาวุธใหม่ / สลับอาวุธ ให้รับดาเมจตามเลเวลอัปเกรดที่ซื้อไว้ (ถ้ามีร้านอัปเกรดในฉาก)
            HuntUpgrades.ApplyToWeapon(this);
        }

        private void Awake()
        {
            playerMovement = GetComponentInParent<PlayerMovement>();
            if (playerMovement == null)
            {
                playerMovement = GetComponent<PlayerMovement>();
            }

            playerHealth = GetComponentInParent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = GetComponent<PlayerHealth>();
            }

            owner = playerMovement != null ? playerMovement.transform : (transform.parent != null ? transform.parent : transform);

            if (playerMovement != null)
            {
                playerSpriteRenderer = playerMovement.GetComponentInChildren<SpriteRenderer>();
            }

            playerAim = GetComponentInParent<PlayerAim>();

            SetupWeaponVisual();
            sideBlend = CurrentFacingSign();
        }

        private void OnDisable()
        {
            CancelSwing();
        }

        private void OnDestroy()
        {
            if (swooshObject != null) Destroy(swooshObject);
        }

        #region Setup

        private void SetupWeaponVisual()
        {
            // จุดอ้างอิง (WeaponPivot) ไว้เป็นพ่อของตัวกระบอง
            Transform existingPivot = transform.Find("WeaponPivot");
            if (existingPivot != null)
            {
                weaponPivot = existingPivot;
            }
            else
            {
                var pivotObj = new GameObject("WeaponPivot");
                pivotObj.transform.SetParent(transform);
                pivotObj.transform.localPosition = Vector3.zero;
                pivotObj.transform.localRotation = Quaternion.identity;
                weaponPivot = pivotObj.transform;
            }

            // ถ้ามี weaponPrefab (ไฟล์ใน Project) ให้ Instantiate ออกมา
            if (weaponVisualObject == null && weaponPrefab != null)
            {
                weaponVisualObject = Instantiate(weaponPrefab, weaponPivot);
                weaponVisualObject.name = weaponPrefab.name + "_Visual";

                // ถ้าเป็นวัตถุในฉาก ให้ปิดต้นฉบับทิ้ง ไม่ให้ค้างอยู่
                if (weaponPrefab.scene.IsValid())
                {
                    weaponPrefab.SetActive(false);
                }
            }
            else if (weaponVisualObject != null)
            {
                // ถ้าลาก Prefab จาก Project มาใส่ช่อง Visual Object ให้สร้างสำเนาในฉาก (ไม่แก้ไฟล์ Prefab จริง)
                if (!weaponVisualObject.scene.IsValid())
                {
                    weaponVisualObject = Instantiate(weaponVisualObject, weaponPivot);
                }
                else
                {
                    weaponVisualObject.transform.SetParent(weaponPivot);
                }
            }
            else
            {
                // ถ้าไม่มี prefab ให้ลองค้นหา SpriteRenderer ที่เป็นลูก
                var childRenderer = weaponPivot.GetComponentInChildren<SpriteRenderer>(true);
                if (childRenderer != null)
                {
                    weaponVisualObject = childRenderer.gameObject;
                }
            }

            // ถ้ายังไม่มีอาวุธเลย สร้างกระบองสำรอง (placeholder) ให้เห็นแน่ๆ
            if (weaponVisualObject == null)
            {
                weaponVisualObject = CreatePlaceholderSword();
                Debug.LogWarning("[Weapon] ยังไม่ได้ใส่ Weapon Prefab จึงใช้กระบองสำรองสีเทาแทน ลาก Prefab มาใส่ช่อง Weapon Prefab ได้เลย", this);
            }

            weaponVisualObject.SetActive(true);
            weaponSpriteRenderer = weaponVisualObject.GetComponentInChildren<SpriteRenderer>(true);
            weaponRenderers = weaponVisualObject.GetComponentsInChildren<SpriteRenderer>(true);

            // วาดกระบองให้อยู่หน้าตัวละคร (ซ้าย/ขวาอย่างเดียว ไม่มีท่าหันหลังแล้ว)
            int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
            foreach (var sr in weaponRenderers)
            {
                if (playerSpriteRenderer != null) sr.sortingLayerID = playerSpriteRenderer.sortingLayerID;
                sr.sortingOrder = baseOrder + weaponSortingOrderOffset;
            }
        }

        private GameObject CreatePlaceholderSword()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.85f, 0.85f, 0.9f));
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            // 1 pixel = 1 unit แล้วใช้ scale กำหนดขนาด (ยาว 1.0 x กว้าง 0.18)
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("PlaceholderSword");
            go.transform.SetParent(weaponPivot);
            go.transform.localScale = new Vector3(1f, 0.18f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return go;
        }

        #endregion

        #region Input / state machine

        private void Update()
        {
            if (playerHealth != null && playerHealth.IsDead)
            {
                if (phase != SwingPhase.Idle) CancelSwing();
                bufferedAttack = false;
                return;
            }

            CheckAttackInput();

            if (phase != SwingPhase.Idle)
            {
                AdvanceSwing(Time.deltaTime);
            }

            if (phase == SwingPhase.Idle && bufferedAttack)
            {
                TryStartBuffered();
            }
        }

        private void CheckAttackInput()
        {
            // กดปุ่ม/หน้าจออัปเกรดอยู่ ห้ามฟันกระบอง
            if (UpgradeTreeUI.IsBlockingGameInput) return;
            bool attackPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                attackPressed = true;
            }
            if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.jKey.wasPressedThisFrame))
            {
                attackPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!attackPressed && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J)))
            {
                attackPressed = true;
            }
#endif

            if (attackPressed)
            {
                PerformAttack();
            }
        }

        /// <summary>
        /// สั่งตี: ว่างอยู่ = เริ่มท่าเหวี่ยง / กำลังเหวี่ยงอยู่แต่ท่ากำลังจะจบ (ดู Input Buffer Time) = จำไว้ตีต่อทันทีที่จบ
        /// </summary>
        public void PerformAttack()
        {
            if (playerHealth != null && playerHealth.IsDead) return;

            if (phase == SwingPhase.Idle)
            {
                if (Time.time >= nextAttackTime)
                {
                    BeginSwing();
                }
                else
                {
                    bufferedAttack = true;
                    bufferedUntil = Time.time + Mathf.Max(0f, inputBufferTime);
                }
                return;
            }

            if (phase == SwingPhase.FollowThrough)
            {
                float remaining = Mathf.Max(0f, followThroughTime - phaseTimer);
                if (remaining <= inputBufferTime)
                {
                    bufferedAttack = true;
                    bufferedUntil = float.PositiveInfinity;
                }
            }
        }

        private void TryStartBuffered()
        {
            if (Time.time > bufferedUntil)
            {
                bufferedAttack = false;
                return;
            }

            if (Time.time >= nextAttackTime)
            {
                BeginSwing();
            }
        }

        private float PhaseDuration(SwingPhase p)
        {
            switch (p)
            {
                case SwingPhase.Windup: return Mathf.Max(0.0001f, windupTime);
                case SwingPhase.Strike: return Mathf.Max(0.0001f, strikeTime);
                case SwingPhase.FollowThrough: return Mathf.Max(0.0001f, followThroughTime);
                default: return 0f;
            }
        }

        private float PhaseProgress()
        {
            float d = PhaseDuration(phase);
            return d > 0f ? Mathf.Clamp01(phaseTimer / d) : 0f;
        }

        private void BeginSwing()
        {
            swingSide = CurrentFacingSign();
            phase = SwingPhase.Windup;
            phaseTimer = 0f;
            swingHits.Clear();
            bufferedAttack = false;
            nextAttackTime = Time.time + Mathf.Max(0f, attackCooldown);

            // เริ่มง้างจากท่าที่เห็นอยู่ตอนนี้ (ไม่กระโดด)
            if (poseInit)
            {
                windupStartOffset = lastPoseOffset;
                windupStartAngle = lastPoseAngle;
            }
            else
            {
                GetHoldPose(out windupStartOffset, out windupStartAngle);
            }

            sideBlend = swingSide;
            if (lockFacingDuringSwing) LockFacing();
            StopSwoosh();

            onAttack?.Invoke();
        }

        private void AdvanceSwing(float dt)
        {
            float remaining = dt;
            int guard = 0;
            while (phase != SwingPhase.Idle && guard++ < 8)
            {
                float duration = PhaseDuration(phase);
                float step = Mathf.Min(remaining, duration - phaseTimer);
                phaseTimer += step;
                remaining -= step;

                bool finished = phaseTimer >= duration - 0.000001f;
                if (phase == SwingPhase.Strike) ProcessStrikeHits(finished);
                if (!finished) break;

                AdvancePhase();
            }
        }

        private void AdvancePhase()
        {
            phaseTimer = 0f;
            switch (phase)
            {
                case SwingPhase.Windup:
                    phase = SwingPhase.Strike;
                    swingHits.Clear();
                    StartSwoosh();
                    break;

                case SwingPhase.Strike:
                    phase = SwingPhase.FollowThrough;
                    GetSwingPose(strikeEndAngle, out strikeEndOffset, out strikeEndWorldAngle);
                    if (!lockFacingDuringFollowThrough) UnlockFacing();
                    if (swooshObject != null && swooshObject.activeSelf) swooshFading = true;
                    break;

                case SwingPhase.FollowThrough:
                    EndSwing();
                    break;
            }
        }

        private void EndSwing()
        {
            phase = SwingPhase.Idle;
            phaseTimer = 0f;
            UnlockFacing();
        }

        /// <summary>ยกเลิกท่าเหวี่ยงทันที (ตาย / ปิดสคริปต์) ปลดล็อกทิศและซ่อนแสงกวาด</summary>
        private void CancelSwing()
        {
            phase = SwingPhase.Idle;
            phaseTimer = 0f;
            swingHits.Clear();
            UnlockFacing();
            StopSwoosh();
        }

        private void LockFacing()
        {
            if (playerAim == null || facingLockedByMe) return;
            playerAim.LockFacing();
            facingLockedByMe = true;
        }

        private void UnlockFacing()
        {
            if (!facingLockedByMe) return;
            facingLockedByMe = false;
            if (playerAim != null) playerAim.UnlockFacing();
        }

        #endregion

        #region Hit detection (เฉพาะช่วงฟาด)

        private Vector2 PivotWorld(float side)
        {
            Vector3 p = OwnerPosition;
            return new Vector2(p.x + swingPivotOffset.x * side, p.y + swingPivotOffset.y);
        }

        /// <summary>
        /// ครึ่งวงกลมฝั่งที่หัน รัศมี attackRange รอบจุดหมุน ศัตรูโดนเมื่อกระบองกวาดลงมาถึงมุมของมัน (ช่วงท้ายฟาดเก็บที่เหลือทั้งหมด)
        /// ศัตรูแต่ละตัวโดนได้ครั้งเดียวต่อการฟาด ศัตรูด้านหลังไม่โดน
        /// </summary>
        private void ProcessStrikeHits(bool final)
        {
            Vector2 pivot = PivotWorld(swingSide);
            float clubAngle = Mathf.Lerp(windupAngle, strikeEndAngle, Tween.Evaluate(strikeEase, PhaseProgress()));

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pivot, attackRange, EffectiveTargetLayer);
            foreach (var col in hitColliders)
            {
                if (col == null) continue;
                if (col.GetComponentInParent<PlayerMovement>() != null) continue;
                if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (swingHits.Contains(damageable)) continue;

                Vector2 rel = col.ClosestPoint(pivot) - pivot;
                float forward = rel.x * swingSide;
                if (forward < -hitBackTolerance) continue;

                // มุมของศัตรูจากจุดหมุน (หันขวา: 0 = ตรงหน้า, + = สูงกว่า, - = ต่ำกว่า)
                float bearing = Mathf.Atan2(rel.y, Mathf.Max(forward, 0f)) * Mathf.Rad2Deg;
                if (!final && clubAngle > bearing + hitSweepLeadAngle) continue;

                swingHits.Add(damageable);
                damageable.TakeDamage(damage);
                ApplyKnockbackTo(col, pivot);
            }
        }

        /// <summary>
        /// แรงผลัก (Knockback) พุ่งออกจากตัวผู้เล่น
        /// </summary>
        private void ApplyKnockbackTo(Collider2D col, Vector2 origin)
        {
            if (knockbackForce <= 0f || col == null) return;

            Vector2 pushDir = ((Vector2)col.transform.position - origin).normalized;

            // ศัตรูตั้ง linearVelocity เองทุกเฟรม ต้องผ่าน EnemyHealth เพื่อพักระบบเดินระหว่างกระเด็น
            if (col.TryGetComponent<EnemyHealth>(out var enemyHealth))
            {
                enemyHealth.ApplyKnockback(pushDir * knockbackForce);
            }
            else if (col.TryGetComponent<Rigidbody2D>(out var targetRb))
            {
                targetRb.AddForce(pushDir * knockbackForce, ForceMode2D.Impulse);
            }
        }

        #endregion

        #region Pose (ท่าถือ + ท่าเหวี่ยง)

        private float CurrentFacingSign()
        {
            if (playerAim != null) return playerAim.FacingSign;
            return GetAimDirection().x >= 0f ? 1f : -1f;
        }

        /// <summary>
        /// ทิศที่หัน: โหมดซ้าย/ขวาจะเป็น (1,0) หรือ (-1,0) เสมอ (ไม่มี PlayerAim ใช้ทิศเดินล่าสุด)
        /// </summary>
        public Vector2 GetAimDirection()
        {
            if (playerAim != null)
            {
                return playerAim.FacingDirection;
            }

            if (playerMovement != null)
            {
                return playerMovement.LastFacingDirection;
            }

            return Vector2.right;
        }

        private float BobOffset()
        {
            if (holdBobHeight <= 0f || holdBobPeriod <= 0f) return 0f;
            float wave = 0.5f * (1f + Mathf.Sin(bobTime / holdBobPeriod * Mathf.PI * 2f));
            return Snap(holdBobHeight * wave);
        }

        private float Snap(float v)
        {
            return snapToPixel ? Tween.Snap(v, pixelsPerUnit) : v;
        }

        /// <summary>ท่าถือ: ตำแหน่งกลางกระบองสัมพัทธ์กับกลางตัว + มุมโลก (ตอนเลื่อนข้ามฝั่งมุมจะค่อยๆ กลับด้าน)</summary>
        private void GetHoldPose(out Vector2 offset, out float worldAngle)
        {
            Vector3 center = smoothedAnchor + new Vector3(holdOffset.x * sideBlend, holdOffset.y + BobOffset(), 0f);
            Vector3 ownerPos = OwnerPosition;
            if (!anchorInit) center = ownerPos + new Vector3(holdOffset.x * sideBlend, holdOffset.y, 0f);
            offset = new Vector2(center.x - ownerPos.x, center.y - ownerPos.y);
            worldAngle = Mathf.Lerp(holdAngle, 180f - holdAngle, (1f - sideBlend) * 0.5f);
        }

        /// <summary>ท่าเหวี่ยงที่มุม a (องศาแบบหันขวา) ของฝั่ง swingSide: กระบองวนรอบจุดหมุนที่รัศมี swingRadius</summary>
        private void GetSwingPose(float a, out Vector2 offset, out float worldAngle)
        {
            float rad = a * Mathf.Deg2Rad;
            offset = new Vector2(
                swingPivotOffset.x * swingSide + Mathf.Cos(rad) * swingSide * swingRadius,
                swingPivotOffset.y + Mathf.Sin(rad) * swingRadius);
            worldAngle = swingSide > 0f ? a : 180f - a;
        }

        private void LateUpdate()
        {
            if (weaponVisualObject == null) return;

            float dt = Time.deltaTime;
            Vector3 ownerPos = OwnerPosition;

            // ฝั่งที่กระบองอยู่: เลื่อนข้ามตามเวลา sideSlideTime ไม่วาร์ป
            float targetSide = phase != SwingPhase.Idle ? swingSide : CurrentFacingSign();
            sideBlend = Mathf.MoveTowards(sideBlend, targetSide, dt * 2f / Mathf.Max(0.001f, sideSlideTime));

            // จุดยึดของท่าถือตามตัวผู้เล่นแบบหน่วงนิดหน่อย (ช่วงเหวี่ยงติดตัวเป๊ะ)
            if (phase == SwingPhase.Idle)
            {
                if (!anchorInit || (smoothedAnchor - ownerPos).sqrMagnitude > 9f)
                {
                    smoothedAnchor = ownerPos;
                }
                else
                {
                    float k = holdFollowSmoothTime <= 0.0001f ? 1f : 1f - Mathf.Exp(-dt / holdFollowSmoothTime);
                    smoothedAnchor = Vector3.Lerp(smoothedAnchor, ownerPos, k);
                }
                bobTime += dt;
            }
            else
            {
                smoothedAnchor = ownerPos;
            }
            anchorInit = true;

            Vector2 offset;
            float angle;
            float side = sideBlend;

            switch (phase)
            {
                case SwingPhase.Windup:
                {
                    float t = Tween.Evaluate(windupEase, PhaseProgress());
                    GetSwingPose(windupAngle, out Vector2 target, out float targetAngle);
                    offset = Vector2.Lerp(windupStartOffset, target, t);
                    angle = Mathf.LerpAngle(windupStartAngle, targetAngle, t);
                    side = swingSide;
                    break;
                }

                case SwingPhase.Strike:
                {
                    float t = Tween.Evaluate(strikeEase, PhaseProgress());
                    float a = Mathf.Lerp(windupAngle, strikeEndAngle, t);
                    GetSwingPose(a, out offset, out angle);
                    side = swingSide;
                    swooshFacingAngle = a;
                    break;
                }

                case SwingPhase.FollowThrough:
                {
                    float t = Tween.Evaluate(followThroughEase, PhaseProgress());
                    GetHoldPose(out Vector2 holdOff, out float holdAng);
                    offset = Vector2.Lerp(strikeEndOffset, holdOff, t);
                    angle = Mathf.LerpAngle(strikeEndWorldAngle, holdAng, t);
                    side = swingSide;
                    break;
                }

                default:
                    GetHoldPose(out offset, out angle);
                    break;
            }

            lastPoseOffset = offset;
            lastPoseAngle = angle;
            poseInit = true;

            float x = Snap(ownerPos.x + offset.x);
            float y = Snap(ownerPos.y + offset.y);
            weaponVisualObject.transform.SetPositionAndRotation(
                new Vector3(x, y, ownerPos.z),
                Quaternion.Euler(0f, 0f, angle + spriteRotationOffset));

            // กลับด้านภาพกระบองตามฝั่ง (ไม่ให้กลับหัว)
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipY = side < 0f;
            }

            UpdateSwoosh(dt, ownerPos);
        }

        #endregion

        #region Swoosh (แสงกวาด)

        private void StartSwoosh()
        {
            if (!swooshEnabled) return;

            EnsureSwooshObject();
            if (swooshObject == null) return;

            swooshSide = swingSide;
            swooshFading = false;
            swooshAlpha = 0f;
            swooshFacingAngle = windupAngle;
            swooshObject.SetActive(true);
        }

        private void StopSwoosh()
        {
            swooshFading = false;
            swooshAlpha = 0f;
            if (swooshObject != null) swooshObject.SetActive(false);
        }

        private void UpdateSwoosh(float dt, Vector3 ownerPos)
        {
            if (swooshObject == null || !swooshObject.activeSelf) return;

            if (phase == SwingPhase.Strike)
            {
                // โผล่เร็วตอนเริ่มฟาด
                swooshAlpha = swooshColor.a * Mathf.Clamp01(PhaseProgress() * 6f);
            }
            else if (swooshFading)
            {
                swooshAlpha -= dt * swooshColor.a / Mathf.Max(0.001f, swooshFadeTime);
                if (swooshAlpha <= 0f)
                {
                    StopSwoosh();
                    return;
                }
            }
            else if (phase != SwingPhase.FollowThrough)
            {
                StopSwoosh();
                return;
            }

            Vector2 pivot = PivotWorld(swooshSide);
            swooshObject.transform.SetPositionAndRotation(
                new Vector3(pivot.x, pivot.y, ownerPos.z),
                Quaternion.Euler(0f, 0f, swooshSide > 0f ? swooshFacingAngle : 180f - swooshFacingAngle));

            swooshRenderer.flipY = swooshSide < 0f;
            Color c = swooshColor;
            c.a = Mathf.Max(0f, swooshAlpha);
            swooshRenderer.color = c;
        }

        private void EnsureSwooshObject()
        {
            Sprite sprite = GetSwooshSprite();
            if (swooshObject == null)
            {
                swooshObject = new GameObject("Weapon Swoosh");
                swooshRenderer = swooshObject.AddComponent<SpriteRenderer>();
                swooshObject.SetActive(false);
            }

            swooshRenderer.sprite = sprite;
            if (playerSpriteRenderer != null) swooshRenderer.sortingLayerID = playerSpriteRenderer.sortingLayerID;
            int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
            // อยู่หน้าตัวละคร หลังตัวกระบอง
            swooshRenderer.sortingOrder = baseOrder + weaponSortingOrderOffset - 1;
        }

        /// <summary>
        /// สร้าง sprite แสงกวาดเอง (ไม่ใช้ asset ภายนอก): วงแหวนบางๆ ช่วง 0..swooshArcAngle องศา ขอบนำ (มุม 0) ทึบสุด จางลงด้านหลัง
        /// จุดกึ่งกลางรูป = จุดหมุน หมุนตามมุมกระบองได้เลย
        /// </summary>
        private Sprite GetSwooshSprite()
        {
            float ppu = Mathf.Max(1f, pixelsPerUnit);
            string key = swooshInnerRadius + "|" + swooshOuterRadius + "|" + swooshArcAngle + "|" + ppu;
            if (swooshSprite != null && swooshSpriteKey == key) return swooshSprite;

            int half = Mathf.CeilToInt(swooshOuterRadius * ppu) + 1;
            int size = half * 2;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.name = "Weapon Swoosh";
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color32[] pixels = new Color32[size * size];
            float arc = Mathf.Max(1f, swooshArcAngle);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - half) / ppu;
                    float dy = (y + 0.5f - half) / ppu;
                    float r = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = 0f;

                    if (r >= swooshInnerRadius && r <= swooshOuterRadius)
                    {
                        float deg = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                        if (deg >= 0f && deg <= arc)
                        {
                            float along = Mathf.Pow(1f - deg / arc, 1.5f);
                            float radial = (r - swooshInnerRadius) / Mathf.Max(0.0001f, swooshOuterRadius - swooshInnerRadius);
                            float body = 1f - Mathf.Pow(Mathf.Abs(radial * 2f - 1f), 3f);
                            alpha = Mathf.Round(along * body * 4f) / 4f;
                        }
                    }

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha) * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply();

            swooshSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), ppu);
            swooshSprite.name = "Weapon Swoosh";
            swooshSpriteKey = key;
            return swooshSprite;
        }

        #endregion

        /// <summary>
        /// ตั้งค่าดาเมจโดยตรง (ใช้โดยร้านอัปเกรดการล่า HuntUpgrades)
        /// </summary>
        public void SetDamage(int newDamage)
        {
            damage = Mathf.Max(0, newDamage);
        }

        /// <summary>
        /// ปรับค่า Stat ของอาวุธผ่านโค้ด (เช่น การอัปเกรดอาวุธ)
        /// </summary>
        public void UpgradeStats(int addDamage, float rangeMultiplier = 1f, float cooldownMultiplier = 1f)
        {
            damage += addDamage;
            attackRange *= rangeMultiplier;
            attackCooldown *= cooldownMultiplier;
        }

        private void OnDrawGizmosSelected()
        {
            Transform o = owner != null ? owner : (transform.parent != null ? transform.parent : transform);
            float s = Application.isPlaying ? (phase != SwingPhase.Idle ? swingSide : CurrentFacingSign()) : 1f;
            Vector3 center = o.position;
            Vector3 pivot = center + new Vector3(swingPivotOffset.x * s, swingPivotOffset.y, 0f);

            // hitbox ครึ่งวงกลมฝั่งที่หัน
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
            const int steps = 24;
            Vector3 prev = pivot + new Vector3(0f, -attackRange, 0f);
            Gizmos.DrawLine(pivot, prev);
            for (int i = 1; i <= steps; i++)
            {
                float a = Mathf.Lerp(-90f, 90f, i / (float)steps) * Mathf.Deg2Rad;
                Vector3 p = pivot + new Vector3(Mathf.Cos(a) * s * attackRange, Mathf.Sin(a) * attackRange, 0f);
                Gizmos.DrawLine(prev, p);
                prev = p;
            }
            Gizmos.DrawLine(prev, pivot);

            // ท่าง้าง / ท่าฟาดสุดทาง (เส้นฟ้า = แกนกระบอง จากจุดหมุนไปปลาย)
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            DrawClubLine(pivot, windupAngle, s);
            DrawClubLine(pivot, strikeEndAngle, s);

            // ท่าถือ (เหลือง)
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            Vector3 hold = center + new Vector3(holdOffset.x * s, holdOffset.y, 0f);
            Gizmos.DrawWireSphere(hold, 0.06f);
            float ha = holdAngle * Mathf.Deg2Rad;
            Vector3 hd = new Vector3(Mathf.Cos(ha) * s, Mathf.Sin(ha), 0f) * 0.5f;
            Gizmos.DrawLine(hold - hd, hold + hd);
        }

        private void DrawClubLine(Vector3 pivot, float angle, float s)
        {
            float a = angle * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(a) * s, Mathf.Sin(a), 0f);
            Gizmos.DrawLine(pivot, pivot + dir * (swingRadius + 0.56f));
        }
    }
}
