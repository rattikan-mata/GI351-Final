using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    public class PlayerCarry : MonoBehaviour
    {
        [Header("Carry Capacity Settings")]
        [Tooltip("จำนวนชิ้นสูงสุดที่ Player สามารถแบกได้ (ปรับได้ใน Inspector)")]
        [SerializeField] private int maxCarryCapacity = 3;

        [Header("Visual Stack Settings")]
        [Tooltip("ตำแหน่งจุดเริ่มต้นที่ของจะวางซ้อนบนหลัง/หัว Player")]
        [SerializeField] private Transform carryPoint;

        [Tooltip("ระยะห่างของการซ้อนของแต่ละชิ้น (แกน X, Y, Z)")]
        [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.45f, -0.02f);

        [Header("Bag Mode (เก็บเข้ากระเป๋าแบบ Juice King)")]
        [Tooltip("เปิด = ของที่เก็บวูบหดเข้ากระเป๋าแล้วหายไป (ดูจำนวนจากตัวเลข 1/5 เหนือหัว) / ปิด = กองซ้อนบนหลังแบบเดิม")]
        [SerializeField] private bool bagMode = true;

        [Tooltip("เวลาที่ของวูบหดเข้ากระเป๋า (วินาที)")]
        [SerializeField] private float absorbTime = 0.15f;

        [Tooltip("ท่าวูบเข้ากระเป๋า InBack = ขยายนิดนึงก่อนแล้วหดหายเร็ว (Juice King ใช้ shrink InBack)")]
        [SerializeField] private Ease absorbEase = Ease.InBack;

        [Header("Carry On Back (กองของอยู่บนหลัง = ฝั่งตรงข้ามกับทิศที่หัน)")]
        [Tooltip("ย้ายกองของไปฝั่งหลังของตัวละครตามทิศที่หัน (ต้องมี PlayerAim)")]
        [SerializeField] private bool carryOnBack = true;

        [Tooltip("ระยะจากกลางตัวไปทางด้านหลัง (เมตร) ค่าน้อยจะดูติดตัวมากกว่า")]
        [SerializeField] private float backDistance = 0.15f;

        [Tooltip("ความเร็วที่กองของเลื่อนตามตอนหันตัว (0 = ย้ายทันที ติดตัวแน่น ไม่ลอยตาม)")]
        [SerializeField] private float backFollowSpeed = 0f;

        [Header("Controls")]
        [Tooltip("ปุ่มสำหรับกดทิ้งของชิ้นบนสุด (เช่น ปุ่ม Q)")]
        [SerializeField] private bool enableDropKey = true;

        [Header("Events")]
        public UnityEvent<int, int> onCarryCountChanged; // (currentCount, maxCapacity)

        private readonly List<Item> carriedItems = new List<Item>();
        private SpriteRenderer playerRenderer;
        private PlayerAim playerAim;
        private Vector3 carryPointBaseLocal;
        private bool stackInFront;

        public int MaxCarryCapacity => maxCarryCapacity;
        public int CurrentCarriedCount => carriedItems.Count;
        public IReadOnlyList<Item> CarriedItems => carriedItems;

        /// <summary>ผู้เล่นทุกคนในฉาก (ให้ของที่ดูดเข้าตัวหาผู้เล่นที่ใกล้ที่สุด)</summary>
        public static readonly List<PlayerCarry> All = new List<PlayerCarry>();

        // ของที่กำลังลอยเข้าตัว (จองช่องไว้แล้ว กันแบกเกินความจุ)
        private int incomingCount;
        private Coroutine stackPunch;
        private Vector3 carryPointBaseScale = Vector3.one;

        // โหมดกระเป๋า: สเกลเดิมของแต่ละชิ้น / renderer ที่ซ่อนไว้ / coroutine ที่กำลังวูบเข้า
        private readonly Dictionary<Item, Vector3> bagBaseScale = new Dictionary<Item, Vector3>();
        private readonly Dictionary<Item, List<Renderer>> bagHiddenRenderers = new Dictionary<Item, List<Renderer>>();
        private readonly Dictionary<Item, Coroutine> bagAbsorbing = new Dictionary<Item, Coroutine>();

        /// <summary>ของเข้ากระเป๋าแล้วหายไป (ไม่ได้กองบนหลัง)</summary>
        public bool BagMode => bagMode;

        /// <summary>ตำแหน่งในโลกที่ของชิ้นใหม่จะไปวาง (โหมดกระเป๋า = ปากกระเป๋า / โหมดกอง = ช่องว่างถัดไปบนกอง)</summary>
        public Vector3 NextStackWorldPosition =>
            carryPoint == null ? transform.position
            : bagMode ? carryPoint.position
            : carryPoint.TransformPoint(stackOffset * carriedItems.Count);

        private void OnEnable()
        {
            if (!All.Contains(this)) All.Add(this);
        }

        private void OnDisable()
        {
            All.Remove(this);
            incomingCount = 0;
            if (carryPoint != null) carryPoint.localScale = carryPointBaseScale;
        }

        /// <summary>จองช่องให้ของที่กำลังลอยเข้าตัว (false = เต็มแล้ว)</summary>
        public bool TryReserveIncoming()
        {
            if (!CanCarry()) return false;
            incomingCount++;
            return true;
        }

        public void ReleaseIncoming()
        {
            incomingCount = Mathf.Max(0, incomingCount - 1);
        }

        /// <summary>กองของบนหลังเด้งขยายแล้วกลับ (ตอนของลอยเข้าตัว)</summary>
        public void PunchStack(float scale, float duration)
        {
            if (bagMode) return; // โหมดกระเป๋าไม่มีกองให้เห็น ตัวเลขเหนือหัวเด้งแทน
            if (carryPoint == null || duration <= 0f || scale <= 1f || !isActiveAndEnabled) return;
            if (stackPunch != null) StopCoroutine(stackPunch);
            stackPunch = StartCoroutine(PunchStackRoutine(scale, duration));
        }

        private System.Collections.IEnumerator PunchStackRoutine(float scale, float duration)
        {
            yield return GI351.KitchenGame.Tween.Run(duration, k =>
            {
                float s = Mathf.Lerp(1f, scale, Mathf.Sin(k * Mathf.PI));
                carryPoint.localScale = carryPointBaseScale * s;
            });
            carryPoint.localScale = carryPointBaseScale;
            stackPunch = null;
        }

        /// <summary>
        /// เปลี่ยนความจุที่แบกได้ (เช่น ซื้อกระเป๋าอัปเกรด) แล้วแจ้ง UI ที่ผูก onCarryCountChanged ให้อัปเดตเป็น จำนวนที่แบก/ความจุใหม่
        /// </summary>
        public void SetMaxCarryCapacity(int newCapacity)
        {
            maxCarryCapacity = Mathf.Max(1, newCapacity);
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
        }

        /// <summary>
        /// ระยะซ้อนของกองในพิกัดโลก (ให้หม้อ/โต๊ะกองของด้วยระยะเดียวกับที่แบก)
        /// </summary>
        public Vector3 WorldStackOffset => carryPoint != null ? carryPoint.TransformVector(stackOffset) : stackOffset;

        private void Awake()
        {
            playerRenderer = GetComponentInChildren<SpriteRenderer>();

            // ถ้าไม่ได้กำหนด carryPoint ใน Inspector ให้สร้างจุด CarryPoint ให้อัตโนมัติ
            if (carryPoint == null)
            {
                var point = new GameObject("CarryPoint");
                point.transform.SetParent(transform);
                point.transform.localPosition = new Vector3(0f, 0.6f, -0.01f);
                carryPoint = point.transform;
            }

            playerAim = GetComponent<PlayerAim>();
            carryPointBaseLocal = carryPoint.localPosition;
            carryPointBaseScale = carryPoint.localScale;
        }

        private void LateUpdate()
        {
            if (!carryOnBack || playerAim == null || carryPoint == null) return;

            // ด้านหลัง = ตรงข้ามกับทิศที่หัน (แปลงระยะในโลกเป็นพิกัดของตัวละคร)
            Vector3 backWorld = (Vector3)(-playerAim.FacingDirection * backDistance);
            Vector3 targetLocal = carryPointBaseLocal + transform.InverseTransformVector(backWorld);
            carryPoint.localPosition = backFollowSpeed > 0f
                ? Vector3.Lerp(carryPoint.localPosition, targetLocal, Time.deltaTime * backFollowSpeed)
                : targetLocal;

            // หันขึ้นบน = หลังหันเข้ากล้อง กองของอยู่หน้าตัว / ทิศอื่นกองของอยู่หลังตัว
            bool inFront = playerAim.IsFacingAway;
            if (inFront != stackInFront)
            {
                stackInFront = inFront;
                UpdateStackPositions();
            }
        }

        private void Start()
        {
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
        }

        private void Update()
        {
            if (enableDropKey)
            {
                CheckDropInput();
            }
        }

        private void CheckDropInput()
        {
            bool dropPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                dropPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!dropPressed && Input.GetKeyDown(KeyCode.Q))
            {
                dropPressed = true;
            }
#endif

            if (dropPressed && carriedItems.Count > 0)
            {
                DropTopItem();
            }
        }

        /// <summary>
        /// เช็คว่ายังสามารถแบกของเพิ่มได้หรือไม่
        /// </summary>
        public bool CanCarry()
        {
            return carriedItems.Count + incomingCount < maxCarryCapacity;
        }

        /// <summary>
        /// รับของที่จองช่องไว้แล้ว (ของที่ลอยเข้าตัวมาถึง) ปลดการจองแล้วเก็บ
        /// absorbStartScale = ตอนมาถึงของหดเหลือกี่เท่าแล้ว (โหมดกระเป๋าจะวูบต่อจากขนาดนั้น ไม่เด้งกลับใหญ่ก่อน)
        /// </summary>
        public bool PickUpReservedItem(Item item, float absorbStartScale = 1f)
        {
            ReleaseIncoming();
            return PickUpItem(item, absorbStartScale);
        }

        /// <summary>
        /// เก็บไอเทม: โหมดกระเป๋า = วูบหดเข้ากระเป๋าแล้วซ่อน / โหมดกอง = ซ้อนบนหลัง
        /// </summary>
        public bool PickUpItem(Item item)
        {
            return PickUpItem(item, 1f);
        }

        private bool PickUpItem(Item item, float absorbStartScale)
        {
            if (item == null || !CanCarry()) return false;

            carriedItems.Add(item);
            item.SetCarriedState(true);
            AudioManager.Play(SoundId.ItemPickup);

            // กองกำลังเด้งอยู่: คืนสเกลก่อน ไม่งั้นชิ้นใหม่จะถูกจำขนาดผิด (เล็กลงถาวร)
            if (stackPunch != null)
            {
                StopCoroutine(stackPunch);
                stackPunch = null;
                carryPoint.localScale = carryPointBaseScale;
            }

            // เซ็ต Parent มาที่ CarryPoint ของตัว Player
            item.transform.SetParent(carryPoint);
            item.transform.localRotation = Quaternion.identity;

            if (bagMode) StartAbsorb(item, absorbStartScale);

            UpdateStackPositions();
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
            return true;
        }

        /// <summary>
        /// นำไอเทมชิ้นบนสุดออกจากหลัง Player (ส่งให้หม้อต้ม หรือ วางบนโต๊ะขาย)
        /// </summary>
        public Item PopTopItem()
        {
            if (carriedItems.Count == 0) return null;
            return RemoveCarriedAt(carriedItems.Count - 1);
        }

        /// <summary>เอาชิ้นที่ index ออก: คืนหน้าตาเดิม (ถ้าอยู่ในกระเป๋า) แล้ววางกลับสู่โลก</summary>
        private Item RemoveCarriedAt(int index)
        {
            Item item = carriedItems[index];
            carriedItems.RemoveAt(index);

            RestoreFromBag(item);
            if (item != null)
            {
                item.transform.SetParent(null);
                item.SetCarriedState(false);
            }

            UpdateStackPositions();
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
            return item;
        }

        #region Bag mode (วูบเข้ากระเป๋า)

        private void StartAbsorb(Item item, float startScale)
        {
            Vector3 baseScale = item.transform.localScale;
            bagBaseScale[item] = baseScale;
            if (bagAbsorbing.TryGetValue(item, out Coroutine running) && running != null) StopCoroutine(running);
            bagAbsorbing[item] = StartCoroutine(AbsorbRoutine(item, baseScale, Mathf.Max(0f, startScale)));
        }

        private System.Collections.IEnumerator AbsorbRoutine(Item item, Vector3 baseScale, float startScale)
        {
            Transform t = item.transform;
            Vector3 startPos = t.localPosition;

            if (absorbTime > 0f)
            {
                yield return GI351.KitchenGame.Tween.Run(absorbTime, k =>
                {
                    if (t == null) return;
                    t.localPosition = Vector3.LerpUnclamped(startPos, Vector3.zero, Mathf.Clamp01(k));
                    t.localScale = baseScale * Mathf.Max(0f, Mathf.LerpUnclamped(startScale, 0f, k));
                }, absorbEase);
            }

            bagAbsorbing.Remove(item);
            if (item == null) yield break;

            // ซ่อนไว้ในกระเป๋า (จำเฉพาะ renderer ที่เปิดอยู่ ตอนเอาออกจะเปิดคืนเฉพาะตัวนั้น)
            t.localPosition = Vector3.zero;
            t.localScale = baseScale;
            var hidden = new List<Renderer>();
            foreach (var r in item.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.enabled) continue;
                r.enabled = false;
                hidden.Add(r);
            }
            bagHiddenRenderers[item] = hidden;
        }

        private void RestoreFromBag(Item item)
        {
            if (item == null) return;

            if (bagAbsorbing.TryGetValue(item, out Coroutine running))
            {
                if (running != null) StopCoroutine(running);
                bagAbsorbing.Remove(item);
            }

            if (bagHiddenRenderers.TryGetValue(item, out List<Renderer> hidden))
            {
                foreach (var r in hidden)
                {
                    if (r != null) r.enabled = true;
                }
                bagHiddenRenderers.Remove(item);
            }

            if (bagBaseScale.TryGetValue(item, out Vector3 scale))
            {
                item.transform.localScale = scale;
                bagBaseScale.Remove(item);
            }
        }

        #endregion

        /// <summary>
        /// หาและนำไอเทมประเภทที่ต้องการออกจากหลัง Player (เช่น หาเนื้อไก่เพื่อใส่หม้อต้ม)
        /// </summary>
        public Item PopItemOfType(ItemType type)
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                if (carriedItems[i].Type == type)
                {
                    return RemoveCarriedAt(i);
                }
            }
            return null;
        }

        /// <summary>
        /// ตรวจสอบว่าในหลังมีไอเทมประเภทนี้หรือไม่
        /// </summary>
        public bool HasItemOfType(ItemType type)
        {
            foreach (var item in carriedItems)
            {
                if (item.Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าไอเทมนี้เป็นอาหารปรุงสุกหรือไม่ (ไม่ใช่เนื้อดิบ และไม่ใช่โจรที่สลบ)
        /// </summary>
        public static bool IsCookedFood(Item item)
        {
            if (item == null) return false;
            if (item.Type == ItemType.UnconsciousThief) return false;
            if (FoodTypes.IsRawMeat(item.Type)) return false;
            if (item is CookedDish) return true;
            return FoodTypes.IsCookedDish(item.Type);
        }

        /// <summary>
        /// ตรวจสอบว่าในหลังมีอาหารปรุงสุกหรือไม่
        /// </summary>
        public bool HasAnyCookedFood()
        {
            foreach (var item in carriedItems)
            {
                if (IsCookedFood(item)) return true;
            }
            return false;
        }

        /// <summary>
        /// ดึงอาหารปรุงสุกชิ้นใดก็ได้ออกจากหลังผู้เล่น (สำหรับส่งให้จุดขาย)
        /// </summary>
        public Item PopAnyCookedFood()
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                Item item = carriedItems[i];
                if (IsCookedFood(item))
                {
                    return RemoveCarriedAt(i);
                }
            }
            return null;
        }

        /// <summary>
        /// ดูข้อมูลไอเทมชิ้นบนสุดโดยไม่ต้องหยิบออก
        /// </summary>
        public Item PeekTopItem()
        {
            if (carriedItems.Count == 0) return null;
            return carriedItems[carriedItems.Count - 1];
        }

        /// <summary>
        /// ทิ้งของชิ้นบนสุดลงพื้นข้างหน้า Player
        /// </summary>
        public void DropTopItem()
        {
            Item item = PopTopItem();
            if (item != null)
            {
                Vector3 dropPos = transform.position + (Vector3)GetFacingDirection() * 0.8f;
                item.transform.position = dropPos;
                item.BlockPickupUntilExit(this);
            }
        }

        /// <summary>
        /// ล้างและทำลายไอเทมทั้งหมดที่ผู้เล่นกำลังแบกอยู่ (เช่น ตอนผู้เล่นตาย)
        /// ทำให้ของที่แบกอยู่หายไปทั้งหมด โดยไม่กระทบเงินหรือของอย่างอื่นในเกม
        /// </summary>
        public void ClearCarriedItems()
        {
            for (int i = 0; i < carriedItems.Count; i++)
            {
                if (carriedItems[i] != null)
                {
                    Destroy(carriedItems[i].gameObject);
                }
            }
            carriedItems.Clear();
            foreach (var running in bagAbsorbing.Values)
            {
                if (running != null) StopCoroutine(running);
            }
            bagAbsorbing.Clear();
            bagHiddenRenderers.Clear();
            bagBaseScale.Clear();
            onCarryCountChanged?.Invoke(0, maxCarryCapacity);
        }

        private Vector2 GetFacingDirection()
        {
            if (playerAim != null)
            {
                return playerAim.FacingDirection;
            }
            if (TryGetComponent<PlayerMovement>(out var movement))
            {
                return movement.LastFacingDirection;
            }
            return Vector2.down;
        }

        /// <summary>
        /// จัดเรียงตำแหน่งและความสูง (Sorting Order) ของไอเทมทั้งหมดที่อยู่บนหลัง
        /// </summary>
        private void UpdateStackPositions()
        {
            // โหมดกระเป๋า: ของถูกซ่อนในกระเป๋า ไม่ต้องจัดกอง (ตำแหน่งระหว่างวูบเข้า AbsorbRoutine คุมเอง)
            if (bagMode) return;

            int baseSortingOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;

            for (int i = 0; i < carriedItems.Count; i++)
            {
                Item item = carriedItems[i];
                if (item == null) continue;

                // ตำแหน่งซ้อนขึ้นไปทีละชั้น
                item.transform.localPosition = stackOffset * i;

                // ปรับ Sorting Order ให้ชิ้นที่อยู่สูงกว่า วาดทับชิ้นที่อยู่ต่ำกว่า
                // กองอยู่หลังตัว (ไม่ได้หันขึ้นบน) ให้วาดใต้ตัวละคร
                if (item.SpriteRenderer != null)
                {
                    bool behind = carryOnBack && playerAim != null && !stackInFront;
                    item.SpriteRenderer.sortingOrder = behind
                        ? baseSortingOrder - carriedItems.Count + i
                        : baseSortingOrder + 1 + i;
                }
            }
        }
    }
}
