using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ประตูปลดล็อกพื้นที่: มีสิ่งกีดขวาง (ต้นไม้/รั้ว) ปิดทางเดินอยู่ ผู้เล่นยืนในวงแหวนทองหน้าประตู
    /// เงินจะไหลออกจากกระเป๋าเข้าประตูทีละน้อย (ตัวเลขราคากลางวงนับถอยหลัง) ครบราคาแล้วสิ่งกีดขวางจางหาย
    /// เปิดทางให้เดินผ่านได้ พร้อมเปิดของที่ต้องโผล่ (เช่น จุดเกิดสัตว์ หม้อ) และปิดของที่ต้องซ่อน
    /// ถ้าเงินในกระเป๋าไม่ถึงราคาประตู จะไม่เก็บเงินเลย (เปิดตัวเลือก Require Full Price ไว้)
    /// </summary>
    public class AreaGate : MonoBehaviour
    {
        [Header("Price (ราคาและการจ่าย)")]
        [Tooltip("ราคาที่ต้องจ่ายเพื่อเปิดประตู (เหรียญ)")]
        [SerializeField] private int price = 150;

        [Tooltip("ความเร็วที่เงินไหลออกจากกระเป๋าตอนยืนในวง (เหรียญต่อวินาที)")]
        [SerializeField] private float drainPerSecond = 60f;

        [Tooltip("ต้องมีเงินในกระเป๋าถึงราคาประตูก่อน ถึงจะเริ่มเก็บเงิน (ไม่ถึง = ไม่เก็บเลย ไม่จ่ายแบบผ่อน) ปิดตัวเลือกนี้ = เก็บเท่าที่มีทีละน้อยจนครบ")]
        [SerializeField] private bool requireFullPrice = true;

        [Tooltip("ต้องเปิดประตูนี้ก่อน ประตูนี้ถึงจะจ่ายเงินได้ (ว่าง = จ่ายได้เลย) ระหว่างที่ยังไม่เปิด วงจ่ายเงินจะซ่อนอยู่แต่สิ่งกีดขวางยังปิดทางอยู่")]
        [SerializeField] private AreaGate requiredGate;

        [Tooltip("ยังไม่เปิดขาย (โซนยังไม่มีของ): สิ่งกีดขวางปิดทางไว้ วงขึ้นสีเทาพร้อมข้อความด้านล่าง จ่ายเงินไม่ได้ ราคาเก็บไว้ตามเดิม เอาติ๊กออกเมื่อโซนพร้อม")]
        [SerializeField] private bool comingSoon = false;

        [Tooltip("ข้อความกลางวงตอนยังไม่เปิดขาย")]
        [SerializeField] private string comingSoonText = "SOON";

        [SerializeField] private Color ringComingSoonColor = new Color(0.6f, 0.6f, 0.65f, 0.7f);

        [Header("Barrier (สิ่งกีดขวาง)")]
        [Tooltip("ตัวแม่ของสิ่งกีดขวางทั้งหมด (ต้นไม้/รั้ว + Collider) เมื่อเปิดประตู Collider ทั้งหมดจะถูกปิดและภาพจางหาย")]
        [SerializeField] private Transform barrierRoot;

        [Tooltip("เวลาที่สิ่งกีดขวางจางหายตอนเปิด (วินาที)")]
        [SerializeField] private float fadeDuration = 0.8f;

        [Header("Pay Ring (วงแหวนจ่ายเงิน)")]
        [Tooltip("จุดศูนย์กลางวงแหวนจ่ายเงิน (ลาก child Ring มาใส่)")]
        [SerializeField] private Transform payRing;

        [Tooltip("รัศมีวงจ่ายเงิน (เมตร) ผู้เล่นต้องยืนในวงนี้ถึงจะจ่าย")]
        [SerializeField] private float payRadius = 0.9f;

        [SerializeField] private SpriteRenderer ringRenderer;

        [Tooltip("ข้อความราคาที่เหลือ ($150) กลางวง")]
        [SerializeField] private TextMesh priceText;

        [SerializeField] private Color ringColor = new Color(1f, 0.85f, 0.2f, 0.95f);
        [SerializeField] private Color ringPayingColor = new Color(0.4f, 1f, 0.6f, 0.95f);
        [SerializeField] private Color ringBrokeColor = new Color(1f, 0.4f, 0.4f, 0.9f);

        [Header("On Unlock (เมื่อเปิดประตูสำเร็จ)")]
        [Tooltip("ของที่ซ่อนอยู่ แล้วจะถูกเปิดเมื่อประตูเปิด (เช่น จุดเกิดสัตว์ หม้อ ของตกแต่งโซนใหม่)")]
        [SerializeField] private GameObject[] revealOnUnlock;

        [Tooltip("ของที่จะถูกปิดเมื่อประตูเปิด")]
        [SerializeField] private GameObject[] hideOnUnlock;

        [Tooltip("เลื่อนกล้องไปดูโซนใหม่แป๊บหนึ่งตอนเปิดประตู")]
        [SerializeField] private bool panCameraOnUnlock = true;

        [Tooltip("จุดที่กล้องเลื่อนไปดู (ว่าง = ใช้ของชิ้นแรกในรายการที่เปิด)")]
        [SerializeField] private Transform panTarget;

        [Tooltip("เวลากล้องเลื่อนไป/กลับ (วินาที)")]
        [SerializeField] private float panTravelTime = 0.7f;

        [Tooltip("เวลาที่กล้องค้างดูโซนใหม่ (วินาที)")]
        [SerializeField] private float panHoldTime = 1.2f;

        [Header("Events")]
        [Tooltip("ยิงเมื่อจ่ายครบและประตูเปิด")]
        public UnityEvent onUnlocked;

        private int paid;
        private float drainBuffer;
        private bool isUnlocked;
        private bool isBuyerInside;
        private Transform cachedBuyer;
        private float nextBuyerSearchTime;
        private float baseRingScale = 1f;

        public bool IsUnlocked => isUnlocked;
        public int Price => price;
        public int Paid => paid;
        public int Remaining => Mathf.Max(0, price - paid);
        public float Progress => price > 0 ? Mathf.Clamp01((float)paid / price) : 1f;
        public bool IsAvailable => !comingSoon && (requiredGate == null || requiredGate.IsUnlocked);
        public bool IsComingSoon => comingSoon;
        /// <summary>ประตูที่ต้องเปิดก่อนประตูนี้ (ว่าง = เปิดได้เลย) ใช้เรียงลำดับปลดล็อกโซน</summary>
        public AreaGate RequiredGate => requiredGate;

        // ช่องทางเดินของประตู (กรอบรวมของ Collider สิ่งกีดขวาง) เก็บไว้ตั้งแต่ Awake เพราะเปิดประตูแล้ว Collider จะถูกปิด
        private bool hasPassage;
        private Rect passageRect;

        /// <summary>ช่องทางเดินระหว่างโซนที่ประตูนี้กั้นอยู่ (ใช้ให้สัตว์วิ่งหนีข้ามโซนที่เปิดแล้ว)</summary>
        public bool TryGetPassage(out Rect rect)
        {
            rect = passageRect;
            return hasPassage;
        }

        private void CachePassage()
        {
            if (barrierRoot == null) return;
            foreach (Collider2D c in barrierRoot.GetComponentsInChildren<Collider2D>(true))
            {
                if (c == null || c.isTrigger) continue;
                Rect r;
                if (c is BoxCollider2D box)
                {
                    Vector3 center = box.transform.TransformPoint(box.offset);
                    Vector3 s = box.transform.lossyScale;
                    Vector2 size = new Vector2(Mathf.Abs(box.size.x * s.x), Mathf.Abs(box.size.y * s.y));
                    r = new Rect((Vector2)center - size * 0.5f, size);
                }
                else
                {
                    Bounds b = c.bounds;
                    r = new Rect(b.min, b.size);
                }
                if (!hasPassage) passageRect = r;
                else passageRect = Rect.MinMaxRect(Mathf.Min(passageRect.xMin, r.xMin), Mathf.Min(passageRect.yMin, r.yMin),
                    Mathf.Max(passageRect.xMax, r.xMax), Mathf.Max(passageRect.yMax, r.yMax));
                hasPassage = true;
            }
        }

        private void Awake()
        {
            CachePassage();

            if (ringRenderer != null && ringRenderer.sprite != null)
            {
                float w = ringRenderer.sprite.bounds.size.x;
                baseRingScale = (payRadius * 2f) / Mathf.Max(0.0001f, w);
            }
        }

        private void Start()
        {
            RefreshVisuals();
        }

        private void OnValidate()
        {
            if (price < 0) price = 0;
            if (drainPerSecond < 1f) drainPerSecond = 1f;
            if (payRadius < 0.2f) payRadius = 0.2f;
        }

        private void Update()
        {
            if (isUnlocked) return;

            bool available = IsAvailable;
            // ยังไม่เปิดขาย: โชว์วงเทา + SOON (เฉพาะเมื่อประตูก่อนหน้าเปิดแล้ว ไม่งั้นซ่อนเหมือนเดิม)
            bool showSoon = comingSoon && (requiredGate == null || requiredGate.IsUnlocked);
            bool ringVisible = available || showSoon;
            if (payRing != null && payRing.gameObject.activeSelf != ringVisible) payRing.gameObject.SetActive(ringVisible);
            if (showSoon)
            {
                drainBuffer = 0f;
                isBuyerInside = false;
                if (ringRenderer != null)
                {
                    ringRenderer.color = Color.Lerp(ringRenderer.color, ringComingSoonColor, Time.deltaTime * 10f);
                    ringRenderer.transform.localScale = Vector3.one * baseRingScale;
                }
                RefreshVisuals();
                return;
            }

            isBuyerInside = false;
            if (available && payRing != null)
            {
                Transform buyer = GetBuyer();
                isBuyerInside = buyer != null && Vector2.Distance(buyer.position, payRing.position) <= payRadius;
            }

            int money = MoneyManager.Instance != null ? MoneyManager.Instance.CurrentMoney : 0;

            bool canPay = money > 0 && (!requireFullPrice || money >= Remaining);

            if (available && isBuyerInside && Remaining > 0 && canPay)
            {
                drainBuffer += drainPerSecond * Time.deltaTime;
                int units = Mathf.FloorToInt(drainBuffer);
                if (units > 0)
                {
                    int spent = PayAmount(units);
                    // เงินไม่พอจ่ายครบหน่วย: ไม่สะสมค้างเกิน
                    drainBuffer = spent < units ? 0f : drainBuffer - units;
                }
            }
            else
            {
                drainBuffer = 0f;
            }

            UpdateRing(money);
        }

        private void LateUpdate()
        {
            // ป้ายราคาให้ตั้งตรงเสมอ แม้หมุนตัวประตูไปด้านอื่น
            if (priceText != null) priceText.transform.rotation = Quaternion.identity;
        }

        // ---------------- Public API ----------------

        /// <summary>จ่ายเงินเข้าประตู (สูงสุด amount เหรียญ ไม่เกินที่ยังขาด/ที่มี) คืนจำนวนที่จ่ายจริง เปิดประตูเมื่อครบ ถ้าเปิด Require Full Price แล้วเงินไม่ถึงราคา จะไม่จ่ายเลย</summary>
        public int PayAmount(int amount)
        {
            if (isUnlocked || amount <= 0 || MoneyManager.Instance == null || !IsAvailable) return 0;
            // เงินไม่ถึงราคาประตู = ไม่เก็บเงินเลย
            if (requireFullPrice && MoneyManager.Instance.CurrentMoney < Remaining) return 0;

            int spend = Mathf.Min(amount, Remaining, MoneyManager.Instance.CurrentMoney);
            if (spend <= 0) return 0;
            if (!MoneyManager.Instance.SpendMoney(spend)) return 0;

            paid += spend;
            RefreshVisuals();

            if (paid >= price) Unlock();
            return spend;
        }

        /// <summary>เปิดประตูทันทีโดยไม่เก็บเงิน (ใช้ทดสอบ/สคริปต์เนื้อเรื่อง)</summary>
        public void Unlock()
        {
            if (isUnlocked) return;
            isUnlocked = true;
            paid = price;

            // เปิดทางทันที: ปิด Collider ของสิ่งกีดขวาง
            if (barrierRoot != null)
            {
                foreach (Collider2D c in barrierRoot.GetComponentsInChildren<Collider2D>(true)) c.enabled = false;
            }
            if (payRing != null) payRing.gameObject.SetActive(false);

            if (revealOnUnlock != null)
                foreach (GameObject go in revealOnUnlock) if (go != null) go.SetActive(true);
            if (hideOnUnlock != null)
                foreach (GameObject go in hideOnUnlock) if (go != null) go.SetActive(false);

            Debug.Log("[AreaGate] " + name + " unlocked");
            onUnlocked?.Invoke();

            StartCoroutine(FadeBarrier());
            if (panCameraOnUnlock) StartCoroutine(PanCamera());
        }

        // ---------------- Internals ----------------

        private Transform GetBuyer()
        {
            if (cachedBuyer != null) return cachedBuyer;
            if (Time.unscaledTime < nextBuyerSearchTime) return null;
            nextBuyerSearchTime = Time.unscaledTime + 0.5f;
            PlayerCarry carry = FindAnyObjectByType<PlayerCarry>();
            cachedBuyer = carry != null ? carry.transform : null;
            return cachedBuyer;
        }

        private void RefreshVisuals()
        {
            if (priceText != null) priceText.text = comingSoon ? comingSoonText : "$" + Remaining;
        }

        private void UpdateRing(int money)
        {
            if (ringRenderer == null) return;

            Color target = ringColor;
            if (isBuyerInside)
            {
                bool canPay = money > 0 && (!requireFullPrice || money >= Remaining);
                target = canPay ? ringPayingColor : ringBrokeColor;
            }
            ringRenderer.color = Color.Lerp(ringRenderer.color, target, Time.deltaTime * 10f);

            float pulse = isBuyerInside ? 1f + Mathf.Sin(Time.time * 8f) * 0.04f : 1f + Mathf.Sin(Time.time * 3f) * 0.03f;
            ringRenderer.transform.localScale = Vector3.one * (baseRingScale * pulse);

            RefreshVisuals();
        }

        private IEnumerator FadeBarrier()
        {
            if (barrierRoot == null) yield break;

            SpriteRenderer[] renderers = barrierRoot.GetComponentsInChildren<SpriteRenderer>(true);
            Color[] start = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) start[i] = renderers[i].color;
            Vector3 startScale = barrierRoot.localScale;

            float t = 0f;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(fadeDuration > 0f ? t / fadeDuration : 1f);
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] == null) continue;
                    Color c = start[i]; c.a = start[i].a * (1f - k);
                    renderers[i].color = c;
                }
                barrierRoot.localScale = Vector3.Lerp(startScale, startScale * 0.85f, k);
                yield return null;
            }

            barrierRoot.gameObject.SetActive(false);
        }

        private IEnumerator PanCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) yield break;

            Vector3? focus = null;
            if (panTarget != null) focus = panTarget.position;
            else if (revealOnUnlock != null)
            {
                foreach (GameObject go in revealOnUnlock) if (go != null) { focus = go.transform.position; break; }
            }
            if (!focus.HasValue) yield break;

            Transform ct = cam.transform;
            Transform oldParent = ct.parent;
            Vector3 oldLocalPos = ct.localPosition;
            float z = ct.position.z;

            // แยกกล้องออกจากตัวผู้เล่นชั่วคราวเพื่อเลื่อนไปดูโซนใหม่
            ct.SetParent(null, true);
            Vector3 from = ct.position;
            Vector3 to = new Vector3(focus.Value.x, focus.Value.y, z);

            yield return Move(ct, from, to, panTravelTime);
            yield return new WaitForSeconds(panHoldTime);

            // กลับมาที่ตัวผู้เล่น (ตามตำแหน่งปัจจุบันของผู้เล่น)
            Vector3 back = oldParent != null ? oldParent.TransformPoint(oldLocalPos) : from;
            yield return Move(ct, ct.position, back, panTravelTime);

            if (oldParent != null)
            {
                ct.SetParent(oldParent, false);
                ct.localPosition = oldLocalPos;
            }
        }

        private static IEnumerator Move(Transform t, Vector3 from, Vector3 to, float duration)
        {
            float e = 0f;
            while (e < duration)
            {
                e += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(duration > 0f ? e / duration : 1f));
                t.position = Vector3.Lerp(from, to, k);
                yield return null;
            }
            t.position = to;
        }

        private void OnDrawGizmos()
        {
            if (payRing == null) return;
            Gizmos.color = new Color(ringColor.r, ringColor.g, ringColor.b, 0.3f);
            Gizmos.DrawSphere(payRing.position, payRadius);
            Gizmos.color = ringColor;
            Gizmos.DrawWireSphere(payRing.position, payRadius);
        }
    }
}
