using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เครื่องผสมเนื้อ: มีที่ใส่เนื้อดิบ 2 ด้าน (ซ้าย/ขวา) แต่ละด้านมีวงแหวนบนพื้นเหมือนหม้อ
    /// ผู้เล่นแบกเนื้อดิบไปยืนที่วงซ้ายและวงขวา ใส่ด้านละ 1 ชิ้น เมื่อครบทั้ง 2 ด้านเครื่องจะผสม
    /// แล้วได้ "อาหารผสม" (MixedDish) ราคา = ราคาเนื้อทั้งสองชิ้น + 20% นับเป็นอาหารปรุงสุก
    /// ต้องจ่ายเงินซื้อก่อนถึงจะใช้งานได้ (ก่อนซื้อ: โปร่งแสง เดินทะลุได้ ใช้งานไม่ได้)
    /// </summary>
    public class MeatMixer : MonoBehaviour
    {
        [Header("Unlock & Purchase (ต้องซื้อก่อนใช้งาน)")]
        [SerializeField] private bool requiresPurchase = true;
        [SerializeField] private int purchasePrice = 100;
        [Tooltip("ตำแหน่งวงซื้อ (เมตร เทียบกับตัวเครื่อง)")]
        [SerializeField] private Vector2 purchaseZoneOffset = new Vector2(0f, -1.4f);
        [SerializeField] private float purchaseZoneRadius = 0.65f;
        [SerializeField] private Color purchaseRingColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [Range(0.05f, 1f)]
        [SerializeField] private float lockedAlpha = 0.35f;
        [SerializeField] private bool requireKeyPressToBuy = false;

        [Header("Mixer Settings (ตั้งค่าเครื่องผสมเนื้อ)")]
        [Tooltip("ราคาที่นับเมื่อใส่ไก่ดิบ (เท่ากับราคาไก่ต้มที่ปรุงแล้ว)")]
        [SerializeField] private int chickenCookedPrice = 10;

        [Tooltip("ราคาที่นับเมื่อใส่หมูดิบ (เท่ากับราคาหมูต้มที่ปรุงแล้ว)")]
        [SerializeField] private int pigCookedPrice = 20;
        [Tooltip("เวลาที่ใช้ผสม (วินาที)")]
        [SerializeField] private float mixDuration = 3f;

        [Tooltip("โบนัสราคาที่บวกเพิ่มจากราคาเนื้อรวม (0.2 = +20%)")]
        [SerializeField] private float priceBonusPercent = 0.2f;

        [Tooltip("Prefab อาหารปรุงสุก (ต้องมี CookedDish) ที่จะเสกออกมาเป็นอาหารผสม")]
        [SerializeField] private GameObject cookedDishPrefab;

        [Header("Input Zones (วงใส่เนื้อ 2 ด้าน - วงสีขาว)")]
        [SerializeField] private Vector2 leftInputOffset = new Vector2(-1.8f, 0f);
        [SerializeField] private Vector2 rightInputOffset = new Vector2(1.8f, 0f);
        [SerializeField] private float inputZoneRadius = 0.65f;
        [SerializeField] private Color ringColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color ringActiveColor = new Color(0.4f, 1f, 0.6f, 0.95f);
        [SerializeField] private Color ringFilledColor = new Color(0.6f, 0.6f, 0.6f, 0.7f);

        [Header("Output Zone (วงอาหารออก - วงสีเหลือง)")]
        [SerializeField] private Vector2 outputOffset = new Vector2(0f, -1.4f);
        [SerializeField] private float outputZoneRadius = 0.5f;
        [SerializeField] private Color outputRingColor = new Color(1f, 0.85f, 0.2f, 0.9f);
        [SerializeField] private float foodPopForce = 2f;
        [SerializeField] private Color mixedDishColor = new Color(0.78f, 0.55f, 1f, 1f);

        [SerializeField] private int ringSortingOrder = 1;

        [Header("Events")]
        public UnityEvent onMixerPurchased;
        public UnityEvent onMixStarted;
        public UnityEvent onMixFinished;

        private const int SlotCount = 2;

        private bool isUnlocked = true;
        private bool isMixing;
        private float mixTimer;

        private readonly bool[] slotFilled = new bool[SlotCount];
        private readonly int[] slotPrice = new int[SlotCount];
        private readonly string[] slotName = new string[SlotCount];
        private int mixingPriceSum;

        private PlayerCarry cachedPlayer;
        private SpriteRenderer bodyRenderer;
        private Collider2D bodyCollider;
        private Color bodyDefaultColor = Color.white;

        private GameObject uiRoot;
        private SpriteRenderer[] inputRings = new SpriteRenderer[SlotCount];
        private TextMesh[] inputLabels = new TextMesh[SlotCount];
        private SpriteRenderer outputRing;
        private SpriteRenderer purchaseRing;
        private TextMesh purchaseText;
        private TextMesh statusLabel;
        private float outputPulseTimer;

        public bool IsUnlocked => isUnlocked;
        public bool IsMixing => isMixing;

        public static readonly System.Collections.Generic.List<MeatMixer> AllMixers = new System.Collections.Generic.List<MeatMixer>();

        private void Awake()
        {
            // กำหนดสถานะปลดล็อกตั้งแต่ Awake เพื่อให้ลูกค้าเช็คเมนูได้ถูกต้องตั้งแต่เฟรมแรก
            isUnlocked = !requiresPurchase;
        }

        private void OnEnable()
        {
            if (!AllMixers.Contains(this)) AllMixers.Add(this);
        }

        private void OnDisable()
        {
            AllMixers.Remove(this);
        }

        private void Start()
        {
            bodyRenderer = GetComponent<SpriteRenderer>();
            bodyCollider = GetComponent<Collider2D>();
            if (bodyRenderer != null) bodyDefaultColor = bodyRenderer.color;

            BuildUi();

            if (isUnlocked) ApplyUnlockedState(); else ApplyLockedState();
        }

        // ---------------------------------------------------------------- state

        private void ApplyLockedState()
        {
            if (bodyRenderer != null)
            {
                Color c = bodyDefaultColor;
                c.a = lockedAlpha;
                bodyRenderer.color = c;
            }
            if (bodyCollider != null) bodyCollider.isTrigger = true; // เดินทะลุได้

            SetActiveSafe(purchaseRing, true);
            SetActiveSafe(purchaseText, true);
            for (int i = 0; i < SlotCount; i++)
            {
                SetActiveSafe(inputRings[i], false);
                SetActiveSafe(inputLabels[i], false);
            }
            SetActiveSafe(outputRing, false);
            SetActiveSafe(statusLabel, false);
        }

        private void ApplyUnlockedState()
        {
            if (bodyRenderer != null) bodyRenderer.color = bodyDefaultColor;
            if (bodyCollider != null) bodyCollider.isTrigger = false; // ชนแล้วทะลุไม่ได้

            SetActiveSafe(purchaseRing, false);
            SetActiveSafe(purchaseText, false);
            for (int i = 0; i < SlotCount; i++)
            {
                SetActiveSafe(inputRings[i], true);
                SetActiveSafe(inputLabels[i], true);
            }
            SetActiveSafe(outputRing, true);
            SetActiveSafe(statusLabel, true);
            RefreshLabels();
        }

        private static void SetActiveSafe(Component c, bool active)
        {
            if (c != null) c.gameObject.SetActive(active);
        }

        public bool TryBuy()
        {
            if (isUnlocked) return true;
            if (MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(purchasePrice))
            {
                isUnlocked = true;
                ApplyUnlockedState();
                onMixerPurchased?.Invoke();
                Debug.Log($"[MeatMixer] ซื้อเครื่องผสมเนื้อสำเร็จ ราคา {purchasePrice} เหรียญ");
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- update

        private void Update()
        {
            if (cachedPlayer == null)
            {
                cachedPlayer = Object.FindAnyObjectByType<PlayerCarry>();
            }

            if (!isUnlocked)
            {
                UpdatePurchase();
                return;
            }

            if (isMixing)
            {
                mixTimer -= Time.deltaTime;
                if (mixTimer <= 0f) FinishMixing();
            }

            UpdateInputZones();
            UpdateOutputRing();
            RefreshLabels();
        }

        private void UpdatePurchase()
        {
            if (purchaseRing == null || cachedPlayer == null) return;

            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.05f;
            SetRingWorld(purchaseRing, GetWorldPos(purchaseZoneOffset), purchaseZoneRadius * 2f * pulse);

            bool inZone = Vector2.Distance(cachedPlayer.transform.position, GetWorldPos(purchaseZoneOffset)) <= purchaseZoneRadius;
            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            bool canAfford = money >= purchasePrice;

            purchaseRing.color = inZone ? (canAfford ? Color.green : new Color(1f, 0.4f, 0.4f, 0.9f)) : purchaseRingColor;
            if (purchaseText != null)
            {
                purchaseText.color = canAfford ? (inZone ? Color.green : Color.yellow) : Color.red;
            }

            if (inZone && (!requireKeyPressToBuy || InteractPressed()))
            {
                TryBuy();
            }
        }

        private void UpdateInputZones()
        {
            Vector2[] offsets = { leftInputOffset, rightInputOffset };
            for (int i = 0; i < SlotCount; i++)
            {
                Vector3 pos = GetWorldPos(offsets[i]);
                bool inZone = cachedPlayer != null &&
                              Vector2.Distance(cachedPlayer.transform.position, pos) <= inputZoneRadius;

                if (inZone && !isMixing && !slotFilled[i])
                {
                    TryFillSlot(i, cachedPlayer);
                }

                if (inputRings[i] != null)
                {
                    SetRingWorld(inputRings[i], pos, inputZoneRadius * 2f);
                    Color target = slotFilled[i] ? ringFilledColor : (inZone ? ringActiveColor : ringColor);
                    inputRings[i].color = Color.Lerp(inputRings[i].color, target, Time.deltaTime * 12f);
                }
            }
        }

        private void TryFillSlot(int index, PlayerCarry player)
        {
            Item meat = player.PopItemOfType(ItemType.ChickenMeat);
            if (meat == null) meat = player.PopItemOfType(ItemType.PigMeat);
            if (meat == null) return;

            slotFilled[index] = true;
            // ราคาที่นับ = ราคาของอาหารปรุงสุกของเนื้อชนิดนั้น (ไก่ดิบ -> ราคาไก่ต้ม, หมูดิบ -> ราคาหมูต้ม)
            bool isChicken = meat.Type == ItemType.ChickenMeat;
            slotPrice[index] = isChicken ? chickenCookedPrice : pigCookedPrice;
            slotName[index] = isChicken ? "Chicken" : "Pig";
            Destroy(meat.gameObject);

            if (slotFilled[0] && slotFilled[1])
            {
                StartMixing();
            }
        }

        private void StartMixing()
        {
            isMixing = true;
            mixTimer = mixDuration;
            mixingPriceSum = slotPrice[0] + slotPrice[1];
            onMixStarted?.Invoke();
        }

        private void FinishMixing()
        {
            isMixing = false;
            mixTimer = 0f;
            for (int i = 0; i < SlotCount; i++)
            {
                slotFilled[i] = false;
                slotPrice[i] = 0;
                slotName[i] = null;
            }

            int finalPrice = Mathf.Max(1, Mathf.RoundToInt(mixingPriceSum * (1f + priceBonusPercent)));
            mixingPriceSum = 0;
            SpawnMixedDish(finalPrice);
            onMixFinished?.Invoke();
        }

        private void SpawnMixedDish(int price)
        {
            if (cookedDishPrefab == null)
            {
                Debug.LogWarning("[MeatMixer] ยังไม่ได้กำหนด cookedDishPrefab");
                return;
            }

            outputPulseTimer = 0.35f;
            Vector3 spawnPos = GetWorldPos(outputOffset);
            Vector2 spread = Random.insideUnitCircle * 0.15f;
            spawnPos += new Vector3(spread.x, spread.y, 0f);

            GameObject dishObj = Instantiate(cookedDishPrefab, spawnPos, Quaternion.identity);

            if (dishObj.TryGetComponent<Item>(out var item))
            {
                item.SetItemType(ItemType.MixedDish);
                item.SetItemName("Mixed Meat Dish");
                item.SetSellPrice(price);
                item.SetItemColor(mixedDishColor);
            }

            if (dishObj.TryGetComponent<CookedDish>(out var cooked))
            {
                cooked.SetDishName("Mixed Meat Dish");
                cooked.SetDishQuality(3);
            }

            if (dishObj.TryGetComponent<SpriteRenderer>(out var sr))
            {
                sr.color = mixedDishColor;
            }

            if (foodPopForce > 0f && dishObj.TryGetComponent<Rigidbody2D>(out var dishRb))
            {
                Vector2 popDir = ((Vector2)(spawnPos - transform.position));
                popDir = popDir.sqrMagnitude > 0.001f ? popDir.normalized : Vector2.down;
                dishRb.AddForce(popDir * foodPopForce, ForceMode2D.Impulse);
            }

            Debug.Log($"[MeatMixer] ผสมเสร็จ! ได้ Mixed Meat Dish ราคา {price} เหรียญ");
        }

        private void UpdateOutputRing()
        {
            if (outputRing == null) return;

            outputPulseTimer = Mathf.Max(0f, outputPulseTimer - Time.deltaTime);
            float pop = outputPulseTimer > 0f ? 1f + outputPulseTimer * 0.6f : 1f;
            float pulse = (1f + Mathf.Sin(Time.time * 2.5f) * 0.03f) * pop;
            SetRingWorld(outputRing, GetWorldPos(outputOffset), outputZoneRadius * 2f * pulse);
        }

        // ---------------------------------------------------------------- ui

        private void BuildUi()
        {
            uiRoot = new GameObject("MixerUI");
            uiRoot.transform.SetParent(transform, false);
            Vector3 lossy = transform.lossyScale;
            uiRoot.transform.localScale = new Vector3(
                1f / (Mathf.Abs(lossy.x) > 0.0001f ? Mathf.Abs(lossy.x) : 1f),
                1f / (Mathf.Abs(lossy.y) > 0.0001f ? Mathf.Abs(lossy.y) : 1f), 1f);

            Vector2[] offsets = { leftInputOffset, rightInputOffset };
            for (int i = 0; i < SlotCount; i++)
            {
                inputRings[i] = CreateRing($"InputRing_{i}", offsets[i], ringColor);
                inputLabels[i] = CreateText($"InputLabel_{i}", offsets[i] + new Vector2(0f, 0.95f), 40, 0.06f, Color.white);
            }
            outputRing = CreateRing("OutputRing", outputOffset, outputRingColor);
            purchaseRing = CreateRing("PurchaseRing", purchaseZoneOffset, purchaseRingColor);
            purchaseText = CreateText("PriceText", purchaseZoneOffset, 44, 0.06f, Color.yellow);
            purchaseText.text = $"${purchasePrice}";
            statusLabel = CreateText("StatusLabel", new Vector2(0f, 1.2f), 48, 0.08f, Color.white);
        }

        private SpriteRenderer CreateRing(string ringName, Vector2 offset, Color color)
        {
            var go = new GameObject(ringName);
            go.transform.SetParent(uiRoot.transform, false);
            go.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CookingPot.GetSharedRingSprite();
            sr.color = color;
            sr.sortingOrder = ringSortingOrder;
            if (bodyRenderer != null)
            {
                if (bodyRenderer.sharedMaterial != null) sr.sharedMaterial = bodyRenderer.sharedMaterial;
                sr.sortingLayerID = bodyRenderer.sortingLayerID;
            }
            return sr;
        }

        private TextMesh CreateText(string textName, Vector2 offset, int fontSize, float charSize, Color color)
        {
            var go = new GameObject(textName);
            go.transform.SetParent(uiRoot.transform, false);
            go.transform.localPosition = new Vector3(offset.x, offset.y, -0.1f);
            var tm = go.AddComponent<TextMesh>();
            tm.fontSize = fontSize;
            tm.characterSize = charSize;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                tm.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    mr.sharedMaterial = font.material;
                    mr.sortingOrder = ringSortingOrder + 49;
                }
            }
            return tm;
        }

        private void SetRingWorld(SpriteRenderer ring, Vector3 worldPos, float worldDiameter)
        {
            if (ring == null) return;
            ring.transform.position = worldPos;
            // uiRoot ชดเชยสเกลของเครื่องไว้แล้ว (world scale = 1) ใช้ขนาดเมตรตรงๆ ได้เลย
            ring.transform.localScale = new Vector3(worldDiameter, worldDiameter, 1f);
        }

        private void RefreshLabels()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (inputLabels[i] != null)
                {
                    inputLabels[i].text = slotFilled[i] ? slotName[i] : "Meat";
                }
            }
            if (statusLabel != null)
            {
                statusLabel.text = isMixing ? $"Mixing {Mathf.Max(0f, mixTimer):0.0}s" : "";
            }
        }

        // ---------------------------------------------------------------- utils

        private Vector3 GetWorldPos(Vector2 offset)
        {
            return transform.position + new Vector3(offset.x, offset.y, 0f);
        }

        private static bool InteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var kb = UnityEngine.InputSystem.Keyboard.current;
            return kb != null && kb.eKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(GetWorldPos(leftInputOffset), inputZoneRadius);
            Gizmos.DrawWireSphere(GetWorldPos(rightInputOffset), inputZoneRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(GetWorldPos(outputOffset), outputZoneRadius);
            Gizmos.color = new Color(1f, 0.85f, 0.2f);
            Gizmos.DrawWireSphere(GetWorldPos(purchaseZoneOffset), purchaseZoneRadius);
        }
    }
}
