using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// สอนเล่นด้วยมือชี้ + เลื่อนกล้อง: มือสีเด้งอยู่เหนือสิ่งที่ต้องทำต่อ ถ้าอยู่นอกจอ มือไปเกาะขอบจอแล้วชี้ไปทางนั้น
    /// ทุกครั้งที่ขึ้นขั้นใหม่และเป้าหมายอยู่นอกจอ กล้องจะเลื่อนไปโชว์แป๊บหนึ่งแล้วกลับมาที่ผู้เล่น
    ///
    /// ขั้นสอน: ตีไก่เก็บเนื้อ -> ใส่หม้อ (วงขาว) -> เก็บอาหาร (วงเหลือง) -> วางชั้น -> รับเงินที่โต๊ะคิดเงิน -> ซื้ออัปเกรดที่ร้าน
    /// ถ้าผู้เล่นทำล่วงหน้าไปแล้ว จะข้ามขั้นให้เอง  จบสอนแล้ว มือจะโผล่อีกเฉพาะตอนเงินพอเปิดประตูโซนใหม่
    /// วางไว้ในฉากที่เดียว หาของที่ต้องชี้เองทั้งหมด (หม้อไก่ ชั้นไก่ โต๊ะคิดเงิน ร้านที่ขายกระบอง ประตูโซน)
    /// </summary>
    public class TutorialGuide : MonoBehaviour
    {
        private enum Step { Hunt, Cook, Collect, Shelf, Cashier, Upgrade, Done }

        [Header("Tutorial (สอนเล่น)")]
        [Tooltip("เปิดสอนเล่นตอนเริ่มเกม (ปิด = ข้ามไปช่วงชี้ประตูเลย)")]
        [SerializeField] private bool runTutorial = true;

        [Tooltip("ต้องเก็บเนื้อไก่กี่ชิ้นก่อนไปใส่หม้อ (ไม่เกินความจุกระเป๋า)")]
        [Min(1)] [SerializeField] private int meatToCollect = 3;

        [Tooltip("จบสอนแล้ว ยังชี้ประตูโซนที่เงินพอเปิดแล้วไหม")]
        [SerializeField] private bool hintGates = true;

        [Tooltip("จ้างแคชเชียร์แล้วและเงินพออัปเกรด: ชี้มือไปที่แคชเชียร์ให้มาดูแผงอัปเกรด (ลูกค้า / Big Orders) จนกว่าจะเปิดแผงครั้งแรก")]
        [SerializeField] private bool hintCashierUpgrade = true;

        [Tooltip("id สินค้าที่ใช้หาร้านอัปเกรด (ร้านที่ขายของ id นี้)")]
        [SerializeField] private string upgradeShopItemId = "club";

        [Tooltip("ใส่ไก่ลงหม้อแล้ว ให้ลูกค้าคนแรกเดินมารอที่แผงเลย 1 คน (รอได้ไม่จำกัด ไม่เดินหนี)")]
        [SerializeField] private bool spawnFirstCustomer = true;

        [Header("Hand (มือชี้)")]
        [Tooltip("รูปมือชี้ (นิ้วชี้ลง จุดหมุน Pivot อยู่ที่ปลายนิ้ว)")]
        [SerializeField] private Sprite handSprite;

        [Tooltip("สีคูณกับรูป (ขาว = สีตามรูปเดิม)")]
        [SerializeField] private Color handColor = Color.white;

        [Tooltip("ความสูงของมือ (เมตร)")]
        [SerializeField] private float handSize = 1.2f;

        [Tooltip("ปลายนิ้วลอยเหนือเป้าหมายเท่านี้ (เมตร)")]
        [SerializeField] private float hoverHeight = 0.45f;

        [Tooltip("ระยะเด้งขึ้นลง (เมตร) และความเร็ว")]
        [SerializeField] private float bobHeight = 0.28f;
        [SerializeField] private float bobSpeed = 4f;

        [Tooltip("ตอนเป้าหมายอยู่นอกจอ มือเกาะขอบจอห่างจากขอบเท่านี้ (สัดส่วนของจอ)")]
        [Range(0.02f, 0.3f)] [SerializeField] private float edgeMargin = 0.09f;

        [SerializeField] private int sortingOrder = 500;

        [Header("Camera (เลื่อนกล้องไปโชว์)")]
        [Tooltip("ขึ้นขั้นใหม่แล้วเป้าหมายอยู่นอกจอ ให้กล้องเลื่อนไปโชว์ก่อน")]
        [SerializeField] private bool panToNewTarget = true;
        [Tooltip("เวลาที่กล้องไหลไป/กลับ (วินาที)")]
        [SerializeField] private float panTravelTime = 1.1f;

        [Tooltip("เวลาที่กล้องค้างโชว์เป้าหมาย (วินาที)")]
        [SerializeField] private float panHoldTime = 1.4f;

        [Tooltip("ขนาดกล้องตอนโชว์เทียบกับปกติ (0.82 = ซูมเข้า 18%)")]
        [Range(0.5f, 1f)] [SerializeField] private float panZoom = 0.82f;

        [Tooltip("เริ่มเกมแล้วรอกี่วินาทีก่อนเริ่มสอน (ให้ผู้เล่นเห็นตัวเองก่อน)")]
        [SerializeField] private float introDelay = 1.2f;

        [Header("Smooth (ความนุ่มของมือ)")]
        [Tooltip("มือไหลตามเป้าหมายเร็วแค่ไหน (มากขึ้น = ตามติดขึ้น)")]
        [SerializeField] private float followSharpness = 10f;

        [Tooltip("เวลาเด้งโผล่ตอนมือขึ้นมาใหม่ (วินาที)")]
        [SerializeField] private float popInTime = 0.35f;

        private Step step = Step.Hunt;
        private bool stepStarted;
        private PlayerCarry player;
        private HuntArea chickenArea;
        private CookingPot chickenPot;
        private FoodSellCounter chickenShelf;
        private CashierCounter cashier;
        private StationUpgrade cashierUpgrade;
        private bool cashierHintDone;
        private bool cashierHintPanned;
        private bool showingCashierHint;
        private ShopStall upgradeShop;
        private readonly List<AreaGate> gates = new List<AreaGate>();
        private readonly HashSet<AreaGate> pannedGates = new HashSet<AreaGate>();
        private bool purchased;
        private int lastMoney;
        private bool earned;   // ได้เงินเพิ่มครั้งแรกแล้ว (นับจากเงินที่เพิ่มขึ้น ไม่ใช่ยอดเงิน เพราะผู้เล่นอาจใช้เงินตั้งต้นไปก่อน)
        private bool firstCustomerSpawned;

        private Transform hand;
        private SpriteRenderer handRenderer;
        private Vector3 handBaseScale = Vector3.one;
        private bool handVisible;
        private float popTimer = 999f;
        private bool waitingForPan;
        private Step shownStep = (Step)(-1);
        private Vector3 smoothPos;
        private Quaternion smoothRot = Quaternion.identity;

        public bool IsTutorialDone => step == Step.Done;

        /// <summary>ชื่อขั้นที่กำลังสอนอยู่ (ไว้ดูตอนทดสอบ)</summary>
        public string CurrentStep => step.ToString();

        private void Start()
        {
            player = FindAnyObjectByType<PlayerCarry>();
            foreach (HuntArea a in HuntArea.All)
                if (a != null && a.Gate == null && a.DropsMeat(ItemType.ChickenMeat)) chickenArea = a;
            foreach (CookingPot p in CookingPot.AllPots)
                if (p != null && p.RequiredIngredient == ItemType.ChickenMeat) chickenPot = p;
            foreach (FoodSellCounter c in FindObjectsByType<FoodSellCounter>())
                if (c.Accepts(ItemType.CookedChicken)) chickenShelf = c;
            cashier = FindAnyObjectByType<CashierCounter>();
            if (cashier != null) cashierUpgrade = cashier.GetComponent<StationUpgrade>();
            foreach (ShopStall s in FindObjectsByType<ShopStall>())
            {
                if (s.FindItem(upgradeShopItemId) == null) continue;
                upgradeShop = s;
                s.ItemPurchased += OnPurchased;
            }
            gates.AddRange(FindObjectsByType<AreaGate>());
            lastMoney = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

            BuildHand();
            step = runTutorial ? Step.Hunt : Step.Done;
            stepStarted = false;
        }

        private void OnDestroy()
        {
            if (upgradeShop != null) upgradeShop.ItemPurchased -= OnPurchased;
        }

        private void OnPurchased(ShopItemData item) => purchased = true;

        private void LateUpdate()
        {
            if (player == null) return;

            // เริ่มเกมแล้วให้ผู้เล่นเห็นตัวเองแป๊บหนึ่งก่อน
            if (Time.timeSinceLevelLoad < introDelay) { SetHandVisible(false); return; }

            if (step != Step.Done) AdvanceTutorial();

            bool show = GetTarget(out Vector3 target, out Vector3 panFocus, out AreaGate gate);
            if (show && !stepStarted)
            {
                stepStarted = true;
                StartPanIfOffScreen(target, panFocus);
            }
            if (showingCashierHint && !cashierHintPanned)
            {
                // ชี้แคชเชียร์ครั้งแรก: ถ้าอยู่นอกจอ ดึงกล้องไปโชว์ครั้งเดียว
                cashierHintPanned = true;
                StartPanIfOffScreen(target, panFocus);
            }
            if (gate != null && !pannedGates.Contains(gate))
            {
                // ประตูที่เพิ่งพอเงิน: โชว์ครั้งเดียว
                pannedGates.Add(gate);
                StartPanIfOffScreen(target, panFocus);
            }

            // ระหว่างกล้องกำลังไหลไป ซ่อนมือไว้ แล้วเด้งโผล่ตอนกล้องถึงเป้าหมาย
            if (waitingForPan && (CameraPan.Holding || !CameraPan.Busy)) waitingForPan = false;
            bool visible = show && !waitingForPan;

            // ขึ้นขั้นใหม่ = มือเด้งโผล่ใหม่ที่เป้าหมายใหม่
            if (visible && step != shownStep) { shownStep = step; SetHandVisible(false); }

            SetHandVisible(visible);
            if (visible) PlaceHand(target);
        }

        private void StartPanIfOffScreen(Vector3 target, Vector3 panFocus)
        {
            if (!panToNewTarget || CameraPan.IsOnScreen(target) || CameraPan.Busy) return;
            waitingForPan = true;
            StartCoroutine(CameraPan.Run(panFocus, panTravelTime, panHoldTime, panZoom));
        }

        private void SetHandVisible(bool visible)
        {
            if (hand == null || visible == handVisible) return;
            handVisible = visible;
            hand.gameObject.SetActive(visible);
            if (visible) popTimer = 0f;   // เริ่มเด้งโผล่ และวางมือที่เป้าหมายทันที (ไม่ไหลมาจากที่เก่า)
        }

        // ---------------- ขั้นสอน ----------------

        private void AdvanceTutorial()
        {
            int raw = CountCarried(ItemType.ChickenMeat);
            bool carriesFood = player.HasAnyCookedFood();
            bool potBusy = chickenPot != null && (chickenPot.CookingItemsCount > 0 || chickenPot.TrayCount > 0);
            bool shelfHasFood = chickenShelf != null && chickenShelf.DishesCount > 0;
            // วางจานลงแผงแล้วลูกค้าหยิบไปทันที (แผงว่างอีกรอบ) ก็นับว่าวางแล้ว: ไปขั้นคิดเงินเลย
            if (!shelfHasFood && !carriesFood && step <= Step.Shelf && step >= Step.Collect) shelfHasFood = CustomerHoldsFood();
            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;
            if (money > lastMoney) earned = true;
            lastMoney = money;

            Step next = step;
            switch (step)
            {
                case Step.Hunt:
                    if (raw >= Mathf.Min(meatToCollect, player.MaxCarryCapacity) || potBusy) next = Step.Cook;
                    break;
                case Step.Cook:
                    if (potBusy && raw == 0) next = Step.Collect;
                    else if (raw == 0 && !potBusy) next = Step.Hunt;   // ทำเนื้อหายระหว่างทาง: กลับไปล่าใหม่
                    break;
                case Step.Collect:
                    if (carriesFood) next = Step.Shelf;
                    break;
                case Step.Shelf:
                    if (shelfHasFood && !carriesFood) next = Step.Cashier;
                    break;
                case Step.Cashier:
                    if (earned) next = Step.Upgrade;
                    break;
                case Step.Upgrade:
                    if (purchased) next = Step.Done;
                    break;
            }
            // ข้ามขั้นถ้าผู้เล่นทำล่วงหน้าแล้ว
            if (next < Step.Shelf && carriesFood) next = Step.Shelf;
            if (next < Step.Cashier && shelfHasFood && !carriesFood && !potBusy) next = Step.Cashier;
            if (next < Step.Upgrade && earned) next = Step.Upgrade;

            if (next != step)
            {
                step = next;
                stepStarted = false;
                // นับเฉพาะการซื้อตอนถึงขั้นซื้ออัปเกรดแล้ว (มีเงินตั้งต้น ผู้เล่นอาจซื้อก่อน ซึ่งไม่ควรข้ามบทเรียนทำอาหาร)
                if (step == Step.Upgrade) purchased = false;
            }

            // ไก่ลงหม้อแล้ว (กำลังรอเก็บจาน/วางแผง): ลูกค้าคนแรกเดินมารอที่แผง ให้ผู้เล่นเห็นว่าทำอาหารไปให้ใคร
            if (spawnFirstCustomer && !firstCustomerSpawned && (step == Step.Collect || step == Step.Shelf))
            {
                firstCustomerSpawned = true;
                CustomerSpawner spawner = CustomerSpawner.All.Count > 0 ? CustomerSpawner.All[0] : null;
                if (spawner != null) spawner.SpawnCustomerNow(true);
            }
        }

        /// <summary>จุดที่มือต้องชี้ตอนนี้ (false = ไม่ต้องโชว์มือ)</summary>
        private bool GetTarget(out Vector3 target, out Vector3 panFocus, out AreaGate gateHint)
        {
            target = panFocus = Vector3.zero;
            gateHint = null;
            showingCashierHint = false;
            switch (step)
            {
                case Step.Hunt:
                    if (chickenArea == null) return false;
                    panFocus = chickenArea.transform.position;
                    Transform chicken = NearestChicken();
                    target = chicken != null ? chicken.position : panFocus;
                    return true;
                case Step.Cook:
                    if (chickenPot == null) return false;
                    target = panFocus = chickenPot.GetStandZonePosition();
                    return true;
                case Step.Collect:
                    if (chickenPot == null) return false;
                    target = panFocus = chickenPot.GetOutputPosition();
                    return true;
                case Step.Shelf:
                    if (chickenShelf == null) return false;
                    target = panFocus = chickenShelf.GetDepositPosition();
                    return true;
                case Step.Cashier:
                    if (cashier == null || cashier.PlayerRing == null) return false;
                    target = panFocus = cashier.PlayerRing.position;
                    return true;
                case Step.Upgrade:
                    if (upgradeShop == null) return false;
                    target = panFocus = upgradeShop.BuyRingPosition;
                    return true;
            }

            if (MoneyManager.Instance == null) return false;
            int money = MoneyManager.Instance.CurrentMoney;

            // จบสอนแล้ว + จ้างแคชเชียร์แล้ว + เงินพออัปเกรด: ชี้ให้มาดูแผงอัปเกรดที่แคชเชียร์ (จนกว่าจะเปิดแผงครั้งแรก)
            if (hintCashierUpgrade && !cashierHintDone && cashierUpgrade != null && cashier.PlayerRing != null && cashierUpgrade.IsAvailable)
            {
                if (cashierUpgrade.EverOpened) cashierHintDone = true;
                else
                {
                    int cost = cashierUpgrade.CheapestNextCost;
                    if (cost >= 0 && money >= cost)
                    {
                        target = panFocus = cashier.PlayerRing.position;
                        showingCashierHint = true;
                        return true;
                    }
                }
            }

            // จบสอนแล้ว: ชี้ประตูที่เปิดได้และเงินพอ (ประตูที่ใกล้ที่สุดก่อน)
            if (!hintGates) return false;
            float best = float.MaxValue;
            foreach (AreaGate g in gates)
            {
                if (g == null || g.IsUnlocked || !g.IsAvailable || money < g.Remaining) continue;
                float d = (g.PayRingPosition - player.transform.position).sqrMagnitude;
                if (d < best) { best = d; gateHint = g; }
            }
            if (gateHint == null) return false;
            target = panFocus = gateHint.PayRingPosition;
            return true;
        }

        /// <summary>มีลูกค้าปกติถืออาหารอยู่ (หยิบจากแผงแล้ว กำลังเดินไป/รอจ่ายเงิน)</summary>
        private static bool CustomerHoldsFood()
        {
            foreach (CustomerAI c in FindObjectsByType<CustomerAI>())
            {
                if (c != null && !c.IsThief && c.CarriedDishCount > 0) return true;
            }
            return false;
        }

        private int CountCarried(ItemType type)
        {
            int n = 0;
            foreach (Item it in player.CarriedItems) if (it != null && it.Type == type) n++;
            return n;
        }

        private Transform cachedChicken;
        private float nextChickenSearch;

        // หาไก่ใหม่ทุก 0.25 วินาที (ค้นหาวัตถุทั้งฉากทุกเฟรมแล้วเปลือง)
        private Transform NearestChicken()
        {
            bool stale = cachedChicken == null || !cachedChicken.gameObject.activeInHierarchy
                || (cachedChicken.TryGetComponent(out EnemyHealth h) && h.IsDead);
            if (!stale && Time.time < nextChickenSearch) return cachedChicken;
            nextChickenSearch = Time.time + 0.25f;
            cachedChicken = SearchNearestChicken();
            return cachedChicken;
        }

        private Transform SearchNearestChicken()
        {
            Transform best = null;
            float bestD = float.MaxValue;
            Vector3 from = player.transform.position;
            foreach (EnemyHealth e in FindObjectsByType<EnemyHealth>())
            {
                if (e == null || e.IsDead || e.MeatDropPrefab == null) continue;
                Item drop = e.MeatDropPrefab.GetComponentInChildren<Item>(true);
                if (drop == null || drop.Type != ItemType.ChickenMeat) continue;
                float d = (e.transform.position - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = e.transform; }
            }
            return best;
        }

        // ---------------- มือชี้ ----------------

        private void BuildHand()
        {
            var go = new GameObject("Tutorial Hand");
            hand = go.transform;
            hand.SetParent(transform, false);
            handRenderer = go.AddComponent<SpriteRenderer>();
            handRenderer.sprite = handSprite != null ? handSprite : WorldUi.Arrow;
            handRenderer.color = handColor;
            handRenderer.sortingOrder = YSort.Overlay + sortingOrder;
            float h = handRenderer.sprite.bounds.size.y;
            float s = h > 0.0001f ? handSize / h : 1f;
            handBaseScale = new Vector3(s, s, 1f);
            hand.localScale = handBaseScale;
            go.SetActive(false);
            handVisible = false;
        }

        private void PlaceHand(Vector3 target)
        {
            ComputeHandPose(target, out Vector3 pos, out Quaternion rot);

            bool justAppeared = popTimer == 0f;
            float follow = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            smoothPos = justAppeared ? pos : Vector3.Lerp(smoothPos, pos, follow);
            smoothRot = justAppeared ? rot : Quaternion.Slerp(smoothRot, rot, follow);
            hand.position = smoothPos;
            hand.rotation = smoothRot;

            // เด้งโผล่: เล็ก -> ใหญ่เกินนิดหนึ่ง -> ขนาดจริง
            popTimer += Time.deltaTime;
            float p = popInTime > 0f ? Mathf.Clamp01(popTimer / popInTime) : 1f;
            float scale = p < 0.65f ? Mathf.Lerp(0.2f, 1.18f, p / 0.65f) : Mathf.Lerp(1.18f, 1f, (p - 0.65f) / 0.35f);
            hand.localScale = handBaseScale * scale;
        }

        /// <summary>ตำแหน่งและมุมที่มือควรอยู่ (ก่อนทำให้นุ่ม)</summary>
        private void ComputeHandPose(Vector3 target, out Vector3 pos, out Quaternion rot)
        {
            float bob = (Mathf.Sin(Time.time * bobSpeed) * 0.5f + 0.5f) * bobHeight;
            Camera cam = Camera.main;

            if (cam == null || CameraPan.IsOnScreen(target, edgeMargin))
            {
                // ในจอ: ปลายนิ้วลอยเหนือเป้าหมาย ชี้ลง
                pos = new Vector3(target.x, target.y + hoverHeight + bob, target.z - 0.5f);
                rot = Quaternion.identity;
                return;
            }

            // นอกจอ: เกาะขอบจอในทิศของเป้าหมาย แล้วหมุนให้นิ้วชี้ไปทางนั้น
            Vector3 v = cam.WorldToViewportPoint(target);
            Vector2 dir = new Vector2(v.x - 0.5f, v.y - 0.5f);
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            float half = 0.5f - edgeMargin;
            float k = half / Mathf.Max(Mathf.Abs(dir.x), Mathf.Abs(dir.y));
            Vector2 edge = new Vector2(0.5f, 0.5f) + dir * k;
            Vector3 world = cam.ViewportToWorldPoint(new Vector3(edge.x, edge.y, Mathf.Abs(cam.transform.position.z)));
            Vector2 worldDir = ((Vector2)(target - world)).normalized;
            pos = new Vector3(world.x, world.y, 0f) - (Vector3)(worldDir * bob);
            // รูปมือชี้ลง (-Y): หมุนให้ -Y หันไปทางเป้าหมาย
            float angle = Mathf.Atan2(worldDir.y, worldDir.x) * Mathf.Rad2Deg + 90f;
            rot = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
