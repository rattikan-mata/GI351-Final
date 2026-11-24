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

        [Tooltip("ตี 4 ทิศ: ทิศที่หัน (PlayerAim โหมด Movement = ทิศที่เดิน, โหมดเมาส์ = ทิศเมาส์) ขึ้น/ลงมากกว่าด้านข้าง = ตีขึ้น/ลง (ปิด = ตีซ้าย/ขวาอย่างเดียว)")]
        [SerializeField] private bool fourDirectionAttack = true;

        [Header("Weapon Visual (ภาพกระบอง)")]
        [Tooltip("Prefab ของอาวุธที่จะโชว์ (ลาก Prefab กระบองมาใส่)")]
        [SerializeField] private GameObject weaponPrefab;

        [Tooltip("GameObject แสดงตัวอาวุธในฉาก (ถ้าว่างจะดึงจาก weaponPrefab หรือค้นหาให้อัตโนมัติ)")]
        [SerializeField] private GameObject weaponVisualObject;

        [Tooltip("ปรับองศาชดเชยของรูปกระบอง (ถ้าภาพวาดเฉียง ให้ใส่ -45 หรือ 45) รูปต้องวาดให้ปลายกระบองชี้ไปทางแกน X")]
        [SerializeField] private float spriteRotationOffset = 0f;

        [Tooltip("ขนาดกระบอง คูณจากขนาดใน Prefab กระบอง (1 = เท่าเดิม, 0.7 = เล็กลง 30%)")]
        [SerializeField] private float weaponScale = 0.91f;

        [Tooltip("ลำดับการวาดของกระบอง = ลำดับของตัวละคร + ค่านี้ (บวก = อยู่หน้าตัวละคร)")]
        [SerializeField] private int weaponSortingOrderOffset = 10;

        [Tooltip("Pixels Per Unit ของเกม (pixel art ใช้ 16) ใช้ปัดตำแหน่งกระบองให้ลงตารางพิกเซล")]
        [SerializeField] private float pixelsPerUnit = 16f;

        [Tooltip("ปัดตำแหน่งกระบองลงตารางพิกเซล")]
        [SerializeField] private bool snapToPixel = true;

        [Header("Hold Pose (ท่าถือ: ลอยข้างตัวด้านหน้า)")]
        [Tooltip("หันซ้าย/ขวา: ตำแหน่งกลางกระบองจากกลางตัว (x บวก = ไปข้างหน้า, ลบ = ด้านหลังตัว ตอนหันซ้ายสะท้อนให้เอง, y = สูงขึ้น)")]
        [SerializeField] private Vector2 holdOffset = new Vector2(0.38f, 0.22f);

        [Tooltip("มุมกระบองตอนถือ (องศา) 90 = ตั้งตรง, น้อยกว่า 90 = หัวเอียงไปข้างหน้า, มากกว่า 90 = หัวเอียงไปด้านหลัง")]
        [SerializeField] private float holdAngle = 65f;

        [Tooltip("ระยะที่ลอยขึ้นลง (idle bob) 0.0625 = 1 พิกเซล")]
        [SerializeField] private float holdBobHeight = 0.0625f;

        [Tooltip("เวลาที่ลอยขึ้นลงครบ 1 รอบ (วินาที)")]
        [SerializeField] private float holdBobPeriod = 0.8f;

        [Tooltip("ความหน่วงตอนตามตัวผู้เล่นที่กำลังเดิน (วินาที) ยิ่งมากยิ่งช้า 0 = ติดตัวเป๊ะ")]
        [SerializeField] private float holdFollowSmoothTime = 0.06f;

        [Tooltip("เวลาที่กระบองเลื่อนข้ามไปอีกฝั่งตอนหันกลับ (วินาที)")]
        [SerializeField] private float sideSlideTime = 0.08f;

        [Header("4 Directions (ท่าถือตามทิศที่ตัวละครหัน)")]
        [Tooltip("หันหน้า (เดินลง): ตำแหน่งกลางกระบองจากกลางตัว (x บวก = ข้างขวาของจอ) ถือมือขวา = มือที่อยู่ฝั่งซ้ายของจอ")]
        [SerializeField] private Vector2 holdOffsetFront = new Vector2(-0.51f, 0.42f);

        [Tooltip("หันหน้า: มุมกระบอง (90 = ตั้งตรง, มากกว่า 90 = เอียงออกซ้าย)")]
        [SerializeField] private float holdAngleFront = 102f;

        [Tooltip("หันหลัง (เดินขึ้น): ตำแหน่งกลางกระบองจากกลางตัว ถือมือขวา = ฝั่งขวาของจอ")]
        [SerializeField] private Vector2 holdOffsetBack = new Vector2(0.49f, 0.43f);

        [Tooltip("หันหลัง: มุมกระบอง")]
        [SerializeField] private float holdAngleBack = 80f;

        [Tooltip("หันหลัง / หันซ้าย / หันขวา วาดกระบองไว้หลังตัวละคร (ตัวละครทับ), หันหน้า วาดไว้หน้าตัว")]
        [SerializeField] private bool drawBehindWhenBack = true;

        [Tooltip("เวลาที่กระบองไหลไปท่าใหม่ตอนเปลี่ยนทิศ (วินาที) มากขึ้น = นุ่มขึ้น")]
        [SerializeField] private float facingBlendTime = 0.1f;

        [Header("Swing (ท่าตี 3 จังหวะ)")]
        [Tooltip("จุดหมุนของท่าเหวี่ยงจากกลางตัว (x = ไปข้างหน้า, y = สูงขึ้น) ครึ่งวงกลมของ hitbox ก็ใช้จุดนี้เป็นศูนย์กลาง")]
        [SerializeField] private Vector2 swingPivotOffset = new Vector2(0.4f, 0.16f);

        [Tooltip("ระยะจากจุดหมุนถึงกลางกระบองตอนเหวี่ยง")]
        [SerializeField] private float swingRadius = 0.72f;

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
        [SerializeField] private float attackRange = 1.3f;

        [Tooltip("คูลดาวน์ก่อนตีครั้งถัดไป (วินาที นับจากเริ่มตี) ตั้ง 0 เพราะความช้าอยู่ในท่าเหวี่ยงแล้ว")]
        [SerializeField] private float attackCooldown = 0f;

        [Tooltip("ความเร็วโจมตี (ตัวคูณ) 1 = ท่าเหวี่ยงตามเวลาที่ตั้งไว้, 1.5 = เร็วขึ้น 1.5 เท่า (ทุกจังหวะสั้นลง) ร้าน Gloves ตั้งค่านี้ตอนเล่น")]
        [SerializeField] private float attackSpeed = 1f;

        [Tooltip("แรงผลักเมื่อศัตรูโดนโจมตี")]
        [SerializeField] private float knockbackForce = 3f;

        [Tooltip("ศัตรูที่ล้ำไปด้านหลังเส้นกลางตัวเกินระยะนี้จะไม่โดน (เผื่อตัวใหญ่ที่ทับตัวผู้เล่นอยู่)")]
        [SerializeField] private float hitBackTolerance = 0.05f;

        [Tooltip("ระยะประชิดรอบตัวผู้เล่น: สัตว์ที่ชิดตัวมากๆ ฝั่งที่หัน (ไม่ใช่ข้างหลัง) โดนทันทีตอนเริ่มฟาด แม้อยู่หลังจุดหมุนของกระบอง")]
        [SerializeField] private float closeHitRadius = 1.2f;

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
        [SerializeField] private float swooshInnerRadius = 0.72f;

        [Tooltip("รัศมีด้านนอกของแสงกวาด (หน่วยโลก)")]
        [SerializeField] private float swooshOuterRadius = 1.43f;

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
        private float swingRot;     // หมุนท่าเหวี่ยง: 0 = ซ้าย/ขวา, 90 = ตีขึ้น, -90 = ตีลง
        private float swooshRot;
        private UnityEngine.Rendering.SortingGroup ownerSortingGroup;
        private readonly HashSet<IDamageable> swingHits = new HashSet<IDamageable>();
        private bool facingLockedByMe;
        private bool bufferedAttack;
        private float bufferedUntil;
        private float nextAttackTime;

        // ท่าถือ / ท่าปัจจุบัน
        private PlayerSpriteAnimator spriteAnimator;
        private Vector2 holdPoseOffset;
        private float holdPoseAngle;
        private bool holdPoseInit;
        private bool drawnBehind;
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
            spriteAnimator = owner != null ? owner.GetComponentInChildren<PlayerSpriteAnimator>() : null;

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
            weaponVisualObject.transform.localScale *= weaponScale;
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
            // เมาส์อยู่บน UI (กำลังกดปุ่มร้าน/อัปเกรด) ห้ามฟันกระบอง
            if (UiBuild.IsPointerOverUi) return;
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
            attackDirectionOverride = null;
            TryAttack();
        }

        /// <summary>สั่งตีไปทางที่กำหนด (ไม่อ่านเมาส์) เช่น ปุ่มบนจอมือถือ หรือทดสอบ</summary>
        public void PerformAttack(Vector2 direction)
        {
            attackDirectionOverride = direction;
            TryAttack();
        }

        private Vector2? attackDirectionOverride;

        private void TryAttack()
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
                float remaining = Mathf.Max(0f, PhaseDuration(SwingPhase.FollowThrough) - phaseTimer);
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
                case SwingPhase.Windup: return Mathf.Max(0.0001f, windupTime / SpeedMul);
                case SwingPhase.Strike: return Mathf.Max(0.0001f, strikeTime / SpeedMul);
                case SwingPhase.FollowThrough: return Mathf.Max(0.0001f, followThroughTime / SpeedMul);
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
            // ท่าก่อนหน้าเพิ่งปลดล็อกในเฟรมนี้ (PlayerAim อ่านเมาส์ไปก่อนแล้วตอนยังล็อก) ให้อ่านเมาส์ใหม่ ไม่งั้นตีรัวๆ จะติดฝั่งเดิม
            UnlockFacing();
            AudioManager.Play(SoundId.Attack);
            if (playerAim != null) playerAim.RefreshFacing();
            swingSide = CurrentFacingSign();
            swingRot = 0f;
            if (fourDirectionAttack && !attackDirectionOverride.HasValue && spriteAnimator != null
                && playerAim != null && playerAim.Mode == PlayerAim.AimMode.Movement)
            {
                // ตีตามทิศที่เดินล่าสุด (อ่านจากปุ่มเดิน ไม่ใช่ท่าที่ภาพค้างอยู่) ตีรัวๆ แล้วเปลี่ยนทิศเดินก็หันตามทันที
                PlayerSpriteAnimator.Facing f = spriteAnimator.CurrentFacing;
                if (playerMovement != null && playerMovement.LastFacingDirection.sqrMagnitude > 0.0001f)
                {
                    Vector2 d = playerMovement.LastFacingDirection;
                    f = Mathf.Abs(d.x) > Mathf.Abs(d.y) * 0.9f ? PlayerSpriteAnimator.Facing.Side
                        : (d.y > 0f ? PlayerSpriteAnimator.Facing.Back : PlayerSpriteAnimator.Facing.Front);
                    if (f == PlayerSpriteAnimator.Facing.Side) swingSide = d.x > 0f ? 1f : -1f;
                }
                if (f == PlayerSpriteAnimator.Facing.Back) swingRot = 90f;
                else if (f == PlayerSpriteAnimator.Facing.Front) swingRot = -90f;
            }
            else if (fourDirectionAttack && (playerAim != null || attackDirectionOverride.HasValue))
            {
                Vector2 m = attackDirectionOverride ?? playerAim.MouseDirection;
                if (attackDirectionOverride.HasValue && Mathf.Abs(m.x) >= Mathf.Abs(m.y) && Mathf.Abs(m.x) > 0.01f) swingSide = m.x > 0f ? 1f : -1f;
                if (Mathf.Abs(m.y) > Mathf.Abs(m.x)) swingRot = m.y > 0f ? 90f : -90f;
            }
            if (swingRot != 0f) swingSide = 1f;   // ตีขึ้น/ลง: ใช้กรอบเดียว (กวาดผ่านทิศที่ตีพอดี)
            if (spriteAnimator != null)
            {
                spriteAnimator.SetAttackFacing(swingRot > 0f ? PlayerSpriteAnimator.Facing.Back
                    : swingRot < 0f ? PlayerSpriteAnimator.Facing.Front : PlayerSpriteAnimator.Facing.Side);
            }
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

            if (swingRot == 0f) sideBlend = swingSide;
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
            if (spriteAnimator != null) spriteAnimator.ClearAttackFacing();
            phase = SwingPhase.Idle;
            phaseTimer = 0f;
            UnlockFacing();
        }

        /// <summary>ยกเลิกท่าเหวี่ยงทันที (ตาย / ปิดสคริปต์) ปลดล็อกทิศและซ่อนแสงกวาด</summary>
        private void CancelSwing()
        {
            if (spriteAnimator != null) spriteAnimator.ClearAttackFacing();
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

        private Vector2 PivotWorld(float side, float rot)
        {
            Vector3 p = OwnerPosition;
            return (Vector2)p + SwingToWorld(swingPivotOffset, side, rot);
        }

        /// <summary>
        /// ครึ่งวงกลมฝั่งที่หัน รัศมี attackRange รอบจุดหมุน ศัตรูโดนเมื่อกระบองกวาดลงมาถึงมุมของมัน (ช่วงท้ายฟาดเก็บที่เหลือทั้งหมด)
        /// ศัตรูแต่ละตัวโดนได้ครั้งเดียวต่อการฟาด ศัตรูด้านหลังไม่โดน
        /// </summary>
        private void ProcessStrikeHits(bool final)
        {
            Vector2 pivot = PivotWorld(swingSide, swingRot);
            float clubAngle = Mathf.Lerp(windupAngle, strikeEndAngle, Tween.Evaluate(strikeEase, PhaseProgress()));

            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(pivot, attackRange, EffectiveTargetLayer);
            foreach (var col in hitColliders)
            {
                if (col == null) continue;
                if (col.GetComponentInParent<PlayerMovement>() != null) continue;
                if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (swingHits.Contains(damageable)) continue;

                // แปลงเป็นกรอบของท่าเหวี่ยง (เหมือนหันขวา) ตีขึ้น/ลงก็ใช้กฎเดียวกัน
                Vector2 rel = WorldToSwing(col.ClosestPoint(pivot) - pivot, swingSide, swingRot);
                float forward = rel.x;
                if (forward < -hitBackTolerance) continue;

                // มุมของศัตรูจากจุดหมุน (กรอบหันขวา: 0 = ตรงหน้า, + = สูงกว่า, - = ต่ำกว่า)
                float bearing = Mathf.Atan2(rel.y, Mathf.Max(forward, 0f)) * Mathf.Rad2Deg;
                if (!final && clubAngle > bearing + hitSweepLeadAngle) continue;

                swingHits.Add(damageable);
                damageable.TakeDamage(damage);
                AudioManager.Play(SoundId.WeaponHit);
                ApplyKnockbackTo(col, pivot);
            }

            // ประชิดตัว: ตัวที่ติดกับผู้เล่นมากๆ อยู่หลังจุดหมุนกระบอง วงกลมข้างบนเลยไม่นับ ให้เช็กจากกลางตัวแทน (เฉพาะฝั่งที่หัน)
            if (closeHitRadius <= 0f) return;
            Vector2 center = OwnerPosition;
            Collider2D[] close = Physics2D.OverlapCircleAll(center, closeHitRadius, EffectiveTargetLayer);
            foreach (var col in close)
            {
                if (col == null) continue;
                if (col.GetComponentInParent<PlayerMovement>() != null) continue;
                if (!col.TryGetComponent<IDamageable>(out var damageable)) continue;
                if (swingHits.Contains(damageable)) continue;

                Vector2 rel = WorldToSwing(col.ClosestPoint(center) - center, swingSide, swingRot);
                if (rel.x < -hitBackTolerance) continue;   // อยู่ข้างหลังตัว ไม่โดน

                swingHits.Add(damageable);
                damageable.TakeDamage(damage);
                AudioManager.Play(SoundId.WeaponHit);
                ApplyKnockbackTo(col, center);
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

        /// <summary>ทิศที่ตัวละครหันอยู่ (ไม่มีตัวสลับภาพ 4 ทิศ = ถือแบบหันข้างอย่างเดียว)</summary>
        private PlayerSpriteAnimator.Facing CurrentView =>
            spriteAnimator != null ? spriteAnimator.CurrentFacing : PlayerSpriteAnimator.Facing.Side;

        /// <summary>ท่าถือเป้าหมายตามทิศที่หัน: ข้าง = ข้างหน้าตัว, หน้า = ต่ำข้างลำตัว, หลัง = ข้างลำตัวด้านหลัง (ฝั่งซ้าย/ขวาตามที่หันล่าสุด)</summary>
        private void GetHoldTarget(out Vector2 offset, out float angle)
        {
            float s = sideBlend;   // -1..1 เลื่อนข้ามฝั่งแบบนุ่มอยู่แล้ว
            switch (CurrentView)
            {
                // หันหน้า/หันหลัง: ถือข้างลำตัวฝั่งเดียวเสมอ (ไม่สลับตามเมาส์ ไม่งั้นกระบองเลื่อนผ่านหน้าตัวละคร)
                case PlayerSpriteAnimator.Facing.Front:
                    offset = holdOffsetFront;
                    angle = holdAngleFront;
                    break;
                case PlayerSpriteAnimator.Facing.Back:
                    offset = holdOffsetBack;
                    angle = holdAngleBack;
                    break;
                default:
                    offset = new Vector2(holdOffset.x * s, holdOffset.y);
                    angle = Mathf.Lerp(holdAngle, 180f - holdAngle, (1f - s) * 0.5f);
                    break;
            }
        }

        /// <summary>ไหลท่าถือเข้าหาท่าของทิศปัจจุบัน (เรียกเฟรมละครั้ง) เปลี่ยนทิศแล้วกระบองค่อยๆ เลื่อน/หมุนไป ไม่วาร์ป</summary>
        private void UpdateHoldPose(float dt)
        {
            GetHoldTarget(out Vector2 targetOffset, out float targetAngle);
            if (!holdPoseInit)
            {
                holdPoseOffset = targetOffset;
                holdPoseAngle = targetAngle;
                holdPoseInit = true;
                return;
            }
            float k = facingBlendTime <= 0.0001f ? 1f : 1f - Mathf.Exp(-dt / facingBlendTime);
            holdPoseOffset = Vector2.Lerp(holdPoseOffset, targetOffset, k);
            holdPoseAngle = Mathf.LerpAngle(holdPoseAngle, targetAngle, k);
        }

        /// <summary>ท่าถือ: ตำแหน่งกลางกระบองสัมพัทธ์กับกลางตัว + มุมโลก</summary>
        private void GetHoldPose(out Vector2 offset, out float worldAngle)
        {
            if (!holdPoseInit) UpdateHoldPose(0f);
            Vector3 ownerPos = OwnerPosition;
            Vector3 basePos = anchorInit ? smoothedAnchor : ownerPos;
            Vector3 center = basePos + new Vector3(holdPoseOffset.x, holdPoseOffset.y + (anchorInit ? BobOffset() : 0f), 0f);
            offset = new Vector2(center.x - ownerPos.x, center.y - ownerPos.y);
            worldAngle = holdPoseAngle;
        }

        /// <summary>
        /// หันหลัง / หันซ้าย / หันขวา: กระบองอยู่หลังตัวละคร (ตัวละครทับ) หันหน้า: กระบองอยู่หน้าตัว
        /// ตอนเหวี่ยงใช้กฎเดียวกัน (ตีขึ้น/ตีข้าง = หลังตัว, ตีลง = หน้าตัว) เลเยอร์ไม่สลับกลางท่า
        /// </summary>
        private void UpdateWeaponSorting()
        {
            // ใช้กฎเดียวกันทั้งตอนถือและตอนตี เลเยอร์จึงไม่สลับกลางท่าเหวี่ยง
            bool behind = false;
            if (drawBehindWhenBack)
            {
                if (phase == SwingPhase.Idle)
                {
                    PlayerSpriteAnimator.Facing view = CurrentView;
                    behind = view == PlayerSpriteAnimator.Facing.Back || view == PlayerSpriteAnimator.Facing.Side;
                }
                else
                {
                    // ตีขึ้น/ตีข้าง = หลังตัว (ตัวละครทับกระบอง), ตีลง = หน้าตัว
                    behind = swingRot >= 0f;
                }
            }
            if (behind == drawnBehind || weaponRenderers == null) return;
            drawnBehind = behind;
            int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
            foreach (var sr in weaponRenderers)
            {
                if (sr != null) sr.sortingOrder = behind ? baseOrder - 1 : baseOrder + weaponSortingOrderOffset;
            }
            // แสงกวาดอยู่ชั้นเดียวกับกระบอง (หลังกระบองนิดหนึ่ง)
            if (swooshRenderer != null) swooshRenderer.sortingOrder = behind ? baseOrder - 2 : baseOrder + weaponSortingOrderOffset - 1;
        }

        /// <summary>ท่าเหวี่ยงที่มุม a (องศาแบบหันขวา) ของฝั่ง swingSide: กระบองวนรอบจุดหมุนที่รัศมี swingRadius</summary>
        private void GetSwingPose(float a, out Vector2 offset, out float worldAngle)
        {
            float rad = a * Mathf.Deg2Rad;
            Vector2 local = new Vector2(
                swingPivotOffset.x + Mathf.Cos(rad) * swingRadius,
                swingPivotOffset.y + Mathf.Sin(rad) * swingRadius);
            offset = SwingToWorld(local, swingSide, swingRot);
            worldAngle = SwingAngleToWorld(a, swingSide, swingRot);
        }

        private static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), sn = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * sn, v.x * sn + v.y * c);
        }

        /// <summary>กรอบท่าเหวี่ยง (หันขวา) -> โลก: สะท้อนซ้าย/ขวาก่อน แล้วหมุนตามทิศที่ตี</summary>
        private static Vector2 SwingToWorld(Vector2 local, float side, float rot)
        {
            return Rotate(new Vector2(local.x * side, local.y), rot);
        }

        private static Vector2 WorldToSwing(Vector2 world, float side, float rot)
        {
            Vector2 v = Rotate(world, -rot);
            return new Vector2(v.x * side, v.y);
        }

        private static float SwingAngleToWorld(float a, float side, float rot)
        {
            return (side > 0f ? a : 180f - a) + rot;
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
            UpdateHoldPose(dt);
            UpdateWeaponSorting();

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

            // กลับด้านภาพกระบองตามฝั่ง (ไม่ให้กลับหัว) ตอนถือใช้มุมจริง: กระบองเอียงไปทางซ้าย (มุมเกิน 90) = พลิก
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipY = phase == SwingPhase.Idle ? Mathf.Cos(angle * Mathf.Deg2Rad) < 0f : side < 0f;
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
            swooshRot = swingRot;
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

            Vector2 pivot = PivotWorld(swooshSide, swooshRot);
            swooshObject.transform.SetPositionAndRotation(
                new Vector3(pivot.x, pivot.y, ownerPos.z),
                Quaternion.Euler(0f, 0f, SwingAngleToWorld(swooshFacingAngle, swooshSide, swooshRot)));

            swooshRenderer.flipY = swooshSide < 0f;

            // แสงกวาดไม่ได้อยู่ในตัวผู้เล่น: ตามลำดับชั้นของตัวผู้เล่น (เรียงตาม Y) หน้า/หลังตามกระบอง
            if (ownerSortingGroup == null && playerSpriteRenderer != null)
                ownerSortingGroup = playerSpriteRenderer.GetComponentInParent<UnityEngine.Rendering.SortingGroup>();
            if (ownerSortingGroup != null)
                swooshRenderer.sortingOrder = ownerSortingGroup.sortingOrder + (drawnBehind ? -1 : 1);
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
            swooshRenderer.sortingOrder = drawnBehind ? baseOrder - 2 : baseOrder + weaponSortingOrderOffset - 1;
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

        private float SpeedMul => Mathf.Max(0.1f, attackSpeed);

        /// <summary>ความเร็วโจมตีปัจจุบัน (ตัวคูณ)</summary>
        public float AttackSpeed => attackSpeed;

        /// <summary>
        /// ตั้งความเร็วโจมตี (ตัวคูณ) ใช้โดยร้านอัปเกรดการล่า HuntUpgrades: ท่าง้าง/ฟาด/ตามแรงสั้นลงตามตัวคูณ
        /// ถ้ากำลังเหวี่ยงอยู่ ท่านั้นจะเร็วขึ้นต่อจากจุดเดิม (ไม่กระตุก)
        /// </summary>
        public void SetAttackSpeed(float multiplier)
        {
            float newSpeed = Mathf.Max(0.1f, multiplier);
            if (phase != SwingPhase.Idle) phaseTimer *= SpeedMul / newSpeed;
            attackSpeed = newSpeed;
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
