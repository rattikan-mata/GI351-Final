using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    public class Weapon : MonoBehaviour
    {
        [Header("Aiming Settings (การเล็งทิศทาง)")]
        [Tooltip("เล็งทิศทางการโจมตีตามตำแหน่งเมาส์")]
        [SerializeField] private bool aimTowardsMouse = true;

        [Tooltip("หมุนภาพเอฟเฟกต์หรืออาวุธให้ชี้ไปตามมุมเมาส์")]
        [SerializeField] private bool rotateVisualTowardsTarget = true;

        [Tooltip("หันหน้าตัวละครตามเมาส์เวลาโจมตี")]
        [SerializeField] private bool flipPlayerToMouseOnAttack = true;

        [Tooltip("กล้องหลักสำหรับแปลงพิกัดเมาส์ (ถ้าว่างจะค้นหา Camera.main ให้อัตโนมัติ)")]
        [SerializeField] private Camera mainCamera;

        [Tooltip("แสดงองศาที่เล็งใน Console ตอนฟัน (ไว้เช็คว่าเมาส์ถูกอ่านได้ไหม)")]
        [SerializeField] private bool debugAim = true;

        [Header("Sword Swing Settings (การฟันดาบแบบกวาดโค้ง)")]
        [Tooltip("เปิดใช้งานแอนิเมชันการฟันดาบกวาดโค้งจากบนลงล่าง")]
        [SerializeField] private bool enableArcSwing = true;

        [Tooltip("Prefab ของอาวุธที่จะโชว์ตอนฟัน (ลาก Prefab ดาบมาใส่)")]
        [SerializeField] private GameObject weaponPrefab;

        [Tooltip("GameObject แสดงตัวอาวุธในฉาก (ถ้าว่างจะดึงจาก weaponPrefab หรือค้นหาให้อัตโนมัติ)")]
        [SerializeField] private GameObject weaponVisualObject;

        [Tooltip("ซ่อนอาวุธเวลาไม่ได้ฟัน (จะแสดงขึ้นมาเฉพาะตอนกดฟัน)")]
        [SerializeField] private bool hideWeaponWhenIdle = true;

        [Tooltip("มุมกว้างของการฟัน (องศา) เช่น 140 องศา")]
        [SerializeField] private float swingArcAngle = 140f;

        [Tooltip("ความเร็วในการฟันแต่ละครั้ง (วินาที) ยิ่งน้อยยิ่งฟันไว")]
        [SerializeField] private float swingDuration = 0.22f;

        [Tooltip("ระยะห่างของตัวดาบจากจุดหมุนตัวผู้เล่น")]
        [SerializeField] private float swordDistance = 0.7f;

        [Tooltip("ปรับองศาชดเชยของรูปดาบ (ถ้าภาพ Sprite วาดเฉียงแนวทแยง 45 องศา ให้ใส่ -45 หรือ 45)")]
        [SerializeField] private float spriteRotationOffset = 0f;

        [Header("Hold In Front (ถืออาวุธไว้ด้านหน้าตลอด)")]
        [Tooltip("ถืออาวุธไว้ด้านหน้าตามทิศที่ผู้เล่นหัน (ทับค่า Hide Weapon When Idle)")]
        [SerializeField] private bool holdWeaponInFront = true;

        [Tooltip("ระยะจากกลางตัวผู้เล่นถึงอาวุธตอนถือ (เมตร)")]
        [SerializeField] private float holdDistance = 0.55f;

        [Header("Weapon Stats (ปรับได้ใน Inspector)")]
        [Tooltip("ความเสียหายต่อการโจมตี 1 ครั้ง")]
        [SerializeField] private int damage = 25;

        [Tooltip("ระยะการโจมตี (รัศมีวงกลม Hitbox)")]
        [SerializeField] private float attackRange = 1.0f;

        [Tooltip("ระยะห่างระหว่างจุดโจมตีกับตัวผู้เล่น")]
        [SerializeField] private float attackOffsetDistance = 0.8f;

        [Tooltip("คูลดาวน์การโจมตี (วินาที)")]
        [SerializeField] private float attackCooldown = 0.35f;

        [Tooltip("แรงผลักเมื่อศัตรูโดนโจมตี")]
        [SerializeField] private float knockbackForce = 3f;

        [Header("Targeting Settings")]
        [Tooltip("เลเยอร์ของเป้าหมายที่จะโดนโจมตี (เช่น Enemy)")]
        [SerializeField] private LayerMask targetLayer;

        [Tooltip("จุดศูนย์กลางการฟันแบบกำหนดเอง (ถ้าว่างจะใช้ทิศทางเมาส์หรือการเดิน)")]
        [SerializeField] private Transform customAttackPoint;

        [Header("Weapon Visual & FX")]
        [Tooltip("GameObject เอฟเฟกต์การฟันเพิ่มเติม (เปิดชั่วคราวตอนฟัน)")]
        [SerializeField] private GameObject attackVisualEffect;

        [Header("Events")]
        public UnityEvent onAttack;

        private float nextAttackTime;
        private PlayerMovement playerMovement;
        private SpriteRenderer playerSpriteRenderer;
        private Transform weaponPivot;
        private SpriteRenderer weaponSpriteRenderer;
        private Coroutine activeSwingCoroutine;
        private bool isSwinging;
        private PlayerHealth playerHealth;
        private PlayerAim playerAim;
        private SpriteRenderer[] weaponRenderers;

        public int Damage => damage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public bool AimTowardsMouse => aimTowardsMouse;
        public bool IsSwinging => isSwinging;

        // ถ้าลืมตั้ง Target Layer (เป็น Nothing) ให้ชนทุก Layer แทน เพื่อไม่ให้ฟันแล้วไม่โดนอะไรเลย
        private LayerMask EffectiveTargetLayer => targetLayer.value == 0 ? (LayerMask)(~0) : targetLayer;

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

            if (playerMovement != null)
            {
                playerSpriteRenderer = playerMovement.GetComponentInChildren<SpriteRenderer>();
            }

            playerAim = GetComponentInParent<PlayerAim>();

            ResolveCamera();

            SetupWeaponVisual();

            if (attackVisualEffect != null)
            {
                attackVisualEffect.SetActive(false);
            }
        }

        private void SetupWeaponVisual()
        {
            // สร้างจุดหมุน (Weapon Pivot) ที่ศูนย์กลางของตัวผู้เล่น/อาวุธ
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

            // ถ้าไม่ได้ใส่ Weapon Prefab แต่ไปใส่ Prefab ดาบไว้ในช่อง Attack Visual Effect ให้ใช้อันนั้นเป็นตัวดาบแทน
            if (weaponVisualObject == null && weaponPrefab == null && attackVisualEffect != null)
            {
                weaponPrefab = attackVisualEffect;
                attackVisualEffect = null;
                Debug.Log("[Weapon] ใช้ Prefab ในช่อง Attack Visual Effect เป็นตัวดาบแทน (แนะนำให้ไปใส่ที่ช่อง Weapon Prefab)", this);
            }

            // ถ้ามี weaponPrefab (ไฟล์ใน Project) ให้ Instantiate ออกมา
            if (weaponVisualObject == null && weaponPrefab != null)
            {
                weaponVisualObject = Instantiate(weaponPrefab, weaponPivot);
                weaponVisualObject.name = weaponPrefab.name + "_Visual";

                // ถ้าเป็นวัตถุในฉาก ให้ปิดต้นฉบับทิ้ง ไม่ให้ค้างอยู่ทางขวา
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

            // ถ้ายังไม่มีอาวุธเลย สร้างดาบสำรอง (placeholder) ให้เห็นตอนฟันแน่ๆ
            if (weaponVisualObject == null)
            {
                weaponVisualObject = CreatePlaceholderSword();
                Debug.LogWarning("[Weapon] ยังไม่ได้ใส่ Weapon Prefab จึงใช้ดาบสำรองสีเทาแทน ลาก Prefab ดาบมาใส่ช่อง Weapon Prefab ได้เลย", this);
            }

            weaponVisualObject.SetActive(true);
            weaponSpriteRenderer = weaponVisualObject.GetComponentInChildren<SpriteRenderer>(true);
            weaponRenderers = weaponVisualObject.GetComponentsInChildren<SpriteRenderer>(true);

            // วาดดาบให้อยู่เหนือตัวผู้เล่น (ตอนหันหลังจะสลับไปอยู่หลังตัวใน UpdateWeaponSorting)
            int baseOrder = playerSpriteRenderer != null ? playerSpriteRenderer.sortingOrder : 0;
            foreach (var sr in weaponRenderers)
            {
                if (playerSpriteRenderer != null) sr.sortingLayerID = playerSpriteRenderer.sortingLayerID;
                sr.sortingOrder = baseOrder + 10;
            }

            // จัดตำแหน่งดาบให้ยื่นออกจากจุดหมุน
            weaponVisualObject.transform.localPosition = new Vector3(swordDistance, 0f, 0f);
            weaponVisualObject.transform.localRotation = Quaternion.Euler(0f, 0f, spriteRotationOffset);

            if (hideWeaponWhenIdle && !holdWeaponInFront)
            {
                weaponVisualObject.SetActive(false);
            }
        }

        private GameObject CreatePlaceholderSword()
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.85f, 0.85f, 0.9f));
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            // 1 pixel = 1 unit แล้วใช้ scale กำหนดขนาดดาบ (ยาว 1.0 x กว้าง 0.18)
            var sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

            var go = new GameObject("PlaceholderSword");
            go.transform.SetParent(weaponPivot);
            go.transform.localScale = new Vector3(1f, 0.18f, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return go;
        }

        private void Update()
        {
            CheckAttackInput();
        }

        private void LateUpdate()
        {
            if (isSwinging)
            {
                UpdateSwing();
            }
            else if (holdWeaponInFront)
            {
                UpdateHoldPose();
            }
            else if (hideWeaponWhenIdle && weaponVisualObject != null && weaponVisualObject.activeSelf)
            {
                // ไม่ได้กำลังฟัน ให้ซ่อนดาบเสมอ
                weaponVisualObject.SetActive(false);
            }
        }

        private void CheckAttackInput()
        {
            if (playerHealth != null && playerHealth.IsDead) return;
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

            if (attackPressed && Time.time >= nextAttackTime && !isSwinging)
            {
                PerformAttack();
            }
        }

        public void PerformAttack()
        {
            nextAttackTime = Time.time + attackCooldown;
            onAttack?.Invoke();

            Vector2 aimDirection = GetAimDirection();

            // หันหน้าตัวละครตามทิศทางเมาส์เมื่อกดโจมตี
            if (flipPlayerToMouseOnAttack && playerSpriteRenderer != null)
            {
                if (aimDirection.x > 0.05f) playerSpriteRenderer.flipX = false;
                else if (aimDirection.x < -0.05f) playerSpriteRenderer.flipX = true;
            }

            if (enableArcSwing && weaponPivot != null && weaponVisualObject != null)
            {
                BeginSwing(aimDirection);
            }
            else
            {
                // Fallback: โจมตีแบบเฟรมเดียวปกติถ้าไม่ได้เปิดใช้ Arc Swing
                InstantAttack(aimDirection);
            }
        }

        private float swingTimer;
        private float swingSign = 1f;
        private HashSet<IDamageable> swingHits;

        /// <summary>
        /// เริ่มการกวาดดาบจากบนลงล่าง (อัปเดตทุกเฟรมใน UpdateSwing)
        /// </summary>
        private void BeginSwing(Vector2 aimDirection)
        {
            isSwinging = true;
            swingTimer = 0f;
            swingHits = new HashSet<IDamageable>();

            // กำหนดด้านที่ฟัน (ซ้าย/ขวา) ตอนกด แล้วล็อกไว้จนจบการฟัน
            swingSign = aimDirection.x >= 0f ? 1f : -1f;

            weaponVisualObject.SetActive(true);
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipY = swingSign < 0f;
            }

            if (debugAim)
            {
                float a = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                Debug.Log($"[Weapon] Swing start | aim angle = {a:F0} deg | camera = {(mainCamera != null ? mainCamera.name : "NONE")} | aimTowardsMouse = {aimTowardsMouse}", this);
            }

            if (attackVisualEffect != null)
            {
                attackVisualEffect.transform.position = GetPivotOrigin() + (Vector3)(aimDirection * attackOffsetDistance);
                if (rotateVisualTowardsTarget)
                {
                    float baseAngle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                    attackVisualEffect.transform.rotation = Quaternion.Euler(0f, 0f, baseAngle);
                }
                attackVisualEffect.SetActive(true);
            }

            UpdateSwing(0f);
        }

        private void UpdateSwing()
        {
            UpdateSwing(Time.deltaTime);
        }

        private void UpdateSwing(float deltaTime)
        {
            swingTimer += deltaTime;
            float duration = Mathf.Max(0.01f, swingDuration);
            float t = Mathf.Clamp01(swingTimer / duration);

            // ใช้ SmoothStep ให้ดาบเร่งความเร็วตอนฟัน
            float easedT = Mathf.SmoothStep(0f, 1f, t);

            // อ่านทิศเมาส์ทุกเฟรม ดาบจะชี้ไปทางเมาส์เสมอ
            Vector2 liveAim = GetAimDirection();
            float liveBaseAngle = Mathf.Atan2(liveAim.y, liveAim.x) * Mathf.Rad2Deg;

            float halfArc = swingArcAngle * 0.5f;
            float arcOffset = Mathf.Lerp(halfArc, -halfArc, easedT) * swingSign;
            float currentAngle = liveBaseAngle + arcOffset;
            Quaternion swingRotation = Quaternion.Euler(0f, 0f, currentAngle);
            weaponPivot.rotation = swingRotation;

            // วางดาบด้วยตำแหน่งโลกโดยตรง (ศูนย์กลาง + ทิศที่หมุน * ระยะ) ไม่พึ่งลำดับชั้นของ Pivot
            Vector3 origin = GetPivotOrigin();
            UpdateWeaponSorting(liveAim);
            Vector3 bladeDirection = swingRotation * Vector3.right;
            weaponVisualObject.transform.SetPositionAndRotation(
                origin + bladeDirection * swordDistance,
                Quaternion.Euler(0f, 0f, currentAngle + spriteRotationOffset));

            // ตรวจจับศัตรูตลอดแนวดาบ (จากตัวผู้เล่นไปถึงปลายดาบ)
            Vector3 pivotPos = origin;
            Vector3 bladeDir = bladeDirection;
            float reach = swordDistance + attackRange;
            for (int i = 1; i <= 3; i++)
            {
                CheckHitsAtPosition(pivotPos + bladeDir * (reach * i / 3f), swingHits);
            }

            if (t >= 1f)
            {
                EndSwing();
            }
        }

        private void EndSwing()
        {
            isSwinging = false;

            if (!holdWeaponInFront && hideWeaponWhenIdle && weaponVisualObject != null)
            {
                weaponVisualObject.SetActive(false);
            }

            if (attackVisualEffect != null)
            {
                attackVisualEffect.SetActive(false);
            }
        }

        private void CheckHitsAtPosition(Vector3 position, HashSet<IDamageable> hitTargets)
        {
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(position, attackRange, EffectiveTargetLayer);

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    // โจมตีศัตรูตัวละ 1 ครั้งต่อการกวาดดาบ 1 รอบ
                    if (hitTargets.Add(damageable))
                    {
                        damageable.TakeDamage(damage);
                        ApplyKnockbackTo(col);
                    }
                }
            }
        }

        private void InstantAttack(Vector2 aimDirection)
        {
            Vector3 attackOrigin = GetAttackOrigin(aimDirection);
            Collider2D[] hitColliders = Physics2D.OverlapCircleAll(attackOrigin, attackRange, EffectiveTargetLayer);

            foreach (var col in hitColliders)
            {
                if (col.TryGetComponent<IDamageable>(out var damageable))
                {
                    damageable.TakeDamage(damage);
                    ApplyKnockbackTo(col);
                }
            }

            if (attackVisualEffect != null)
            {
                attackVisualEffect.transform.position = attackOrigin;
                if (rotateVisualTowardsTarget)
                {
                    float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
                    attackVisualEffect.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                attackVisualEffect.SetActive(true);
                CancelInvoke(nameof(HideVisualEffect));
                Invoke(nameof(HideVisualEffect), 0.15f);
            }
        }

        /// <summary>
        /// จุดหมุนของอาวุธ = กลางตัวผู้เล่น (ตัว Weapon อาจถูกวางเยื้องไว้ใน Prefab)
        /// </summary>
        private Vector3 GetPivotOrigin()
        {
            return playerMovement != null ? playerMovement.transform.position : transform.position;
        }

        /// <summary>
        /// ท่าถืออาวุธ: ยื่นไปด้านหน้าตามทิศที่ผู้เล่นหัน
        /// </summary>
        private void UpdateHoldPose()
        {
            if (weaponVisualObject == null) return;
            if (!weaponVisualObject.activeSelf) weaponVisualObject.SetActive(true);

            Vector2 dir = GetAimDirection();
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
            weaponVisualObject.transform.SetPositionAndRotation(
                GetPivotOrigin() + (Vector3)(dir * holdDistance),
                Quaternion.Euler(0f, 0f, angle + spriteRotationOffset));

            // หันซ้ายแล้วกลับภาพดาบ ไม่ให้ดาบกลับหัว
            if (weaponSpriteRenderer != null)
            {
                weaponSpriteRenderer.flipY = dir.x < -0.01f;
            }

            UpdateWeaponSorting(dir);
        }

        /// <summary>
        /// หันขึ้นบน (หันหลังให้กล้อง) อาวุธจะอยู่หลังตัว ทิศอื่นอาวุธอยู่หน้าตัว
        /// </summary>
        private void UpdateWeaponSorting(Vector2 dir)
        {
            if (weaponRenderers == null || playerSpriteRenderer == null) return;

            int order = dir.y > 0.1f ? playerSpriteRenderer.sortingOrder - 1 : playerSpriteRenderer.sortingOrder + 10;
            foreach (var sr in weaponRenderers)
            {
                if (sr != null) sr.sortingOrder = order;
            }
        }

        /// <summary>
        /// แรงผลัก (Knockback) พุ่งออกจากตัวผู้เล่น
        /// </summary>
        private void ApplyKnockbackTo(Collider2D col)
        {
            if (knockbackForce <= 0f || col == null) return;

            Vector2 pushDir = (col.transform.position - GetPivotOrigin()).normalized;

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

        private void HideVisualEffect()
        {
            if (attackVisualEffect != null)
            {
                attackVisualEffect.SetActive(false);
            }
        }

        /// <summary>
        /// ดึงเวกเตอร์ทิศทางการเล็ง (ถ้าเปิด aimTowardsMouse จะหาทิศทางจากตัวละครไปยังตำแหน่งเมาส์)
        /// </summary>
        private void ResolveCamera()
        {
            if (mainCamera != null) return;

            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                // กล้องอาจไม่ได้ตั้ง Tag เป็น MainCamera ให้หากล้องตัวแรกในฉากแทน
                mainCamera = FindFirstObjectByType<Camera>();
                if (mainCamera == null)
                {
                    Debug.LogWarning("[Weapon] หา Camera ในฉากไม่เจอ เล่นแล้วดาบจะชี้ตามทิศเดินแทนเมาส์", this);
                }
            }
        }

        public Vector2 GetAimDirection()
        {
            // ใช้ทิศที่ผู้เล่นหัน (ล็อก 8 ทิศ) ให้ตรงกับภาพตัวละคร
            if (playerAim != null)
            {
                return playerAim.FacingDirection;
            }

            if (aimTowardsMouse)
            {
                ResolveCamera();

                if (mainCamera != null)
                {
                    Vector3 mouseScreenPos = Vector3.zero;
                    bool gotMouse = false;

#if ENABLE_INPUT_SYSTEM
                    if (Mouse.current != null)
                    {
                        Vector2 p = Mouse.current.position.ReadValue();
                        mouseScreenPos = new Vector3(p.x, p.y, 0f);
                        gotMouse = true;
                    }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
                    if (!gotMouse)
                    {
                        mouseScreenPos = Input.mousePosition;
                    }
#endif

                    // ต้องกำหนดระยะ z จากกล้องถึงตัวละคร ไม่งั้นกล้อง Perspective จะได้ตำแหน่งเมาส์เพี้ยน
                    mouseScreenPos.z = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
                    Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(mouseScreenPos);
                    mouseWorldPos.z = transform.position.z;

                    Vector2 aimDir = (mouseWorldPos - transform.position);
                    if (aimDir.sqrMagnitude > 0.001f)
                    {
                        return aimDir.normalized;
                    }
                }
            }

            // ถ้าไม่ได้เล็งตามเมาส์ หรือไม่พบเมาส์ ให้ใช้ทิศทางการเดินล่าสุดของผู้เล่น
            if (playerMovement != null)
            {
                return playerMovement.LastFacingDirection;
            }

            return Vector2.down;
        }

        public Vector3 GetAttackOrigin()
        {
            return GetAttackOrigin(GetAimDirection());
        }

        public Vector3 GetAttackOrigin(Vector2 direction)
        {
            if (customAttackPoint != null)
            {
                return customAttackPoint.position;
            }

            return GetPivotOrigin() + (Vector3)(direction.normalized * attackOffsetDistance);
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
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.5f);
            Vector3 origin = Application.isPlaying ? GetAttackOrigin() : (transform.position + Vector3.right * attackOffsetDistance);
            Gizmos.DrawWireSphere(origin, attackRange);

            // วาดตัวอย่างแนวรัศมีการกวาดดาบ (Arc Swing)
            if (enableArcSwing)
            {
                Vector3 center = transform.position;
                Vector2 aimDir = Application.isPlaying ? GetAimDirection() : Vector2.right;
                float baseAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg;
                float halfArc = swingArcAngle * 0.5f;

                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
                Vector3 p1 = center + (Quaternion.Euler(0, 0, baseAngle + halfArc) * Vector3.right) * (swordDistance + attackRange);
                Vector3 p2 = center + (Quaternion.Euler(0, 0, baseAngle - halfArc) * Vector3.right) * (swordDistance + attackRange);

                Gizmos.DrawLine(center, p1);
                Gizmos.DrawLine(center, p2);
            }
        }
    }
}
