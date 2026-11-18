using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พนักงานแคชเชียร์: ยืนในวงขาวของโต๊ะจ่ายเงิน แล้วเก็บเหรียญบนโต๊ะให้อัตโนมัติ (ผู้เล่นไม่ต้องเดินมาเก็บเอง)
    /// อ่อนกว่าผู้เล่น: เก็บได้ทีละเหรียญตามช่วงเวลา (ผู้เล่นเข้าวงดูดหมดทีเดียว) ไม่เดิน และสู้โจรไม่ได้
    /// </summary>
    public class CashierStaff : MonoBehaviour
    {
        [Header("Work (การทำงาน)")]
        [Tooltip("เก็บเหรียญ 1 เหรียญทุกกี่วินาที (ยิ่งมากยิ่งช้า)")]
        [SerializeField] private float collectInterval = 0.6f;

        [Tooltip("ยืนเยื้องจากกลางวงขาวเล็กน้อย ไม่บังผู้เล่นตอนเดินเข้าวง (เมตร)")]
        [SerializeField] private Vector2 standOffset = new Vector2(0.55f, 0f);

        [Header("Visual (ภาพ)")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer hairRenderer;

        [Tooltip("เฟรมแอนิเมชันยืน (ตัว)")]
        [SerializeField] private Sprite[] bodyFrames;

        [Tooltip("เฟรมแอนิเมชันยืน (ผม) ต้องจำนวนเท่ากับตัว")]
        [SerializeField] private Sprite[] hairFrames;

        [Tooltip("ความเร็วแอนิเมชัน (เฟรมต่อวินาที)")]
        [SerializeField] private float frameRate = 8f;

        [Tooltip("เวลาเด้งตอนเกิด (วินาที) ขยายเกินแล้วหดกลับ")]
        [SerializeField] private float popInDuration = 0.35f;

        private CashierCounter counter;
        private float collectTimer;
        private float animTimer;
        private float popTimer;
        private Vector3 baseScale;

        public CashierCounter Counter => counter;

        /// <summary>เริ่มงานที่โต๊ะจ่ายเงินนี้</summary>
        public void Init(CashierCounter cashier)
        {
            counter = cashier;
            if (counter != null && counter.PlayerRing != null)
            {
                transform.position = counter.PlayerRing.position + (Vector3)standOffset;
            }
        }

        private void Awake()
        {
            baseScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            UpdatePopIn();
            UpdateAnimation();

            if (counter == null) return;

            collectTimer += Time.deltaTime;
            if (collectTimer >= collectInterval)
            {
                collectTimer = 0f;
                counter.AttractOneCoinTo(transform);
            }
        }

        private void UpdatePopIn()
        {
            if (popTimer >= popInDuration) return;
            popTimer += Time.deltaTime;
            float t = Mathf.Clamp01(popInDuration > 0f ? popTimer / popInDuration : 1f);
            // 0 -> 1.15 (ขยายเกิน) -> 1
            float s = t < 0.7f
                ? Mathf.Lerp(0f, 1.15f, t / 0.7f)
                : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
            transform.localScale = baseScale * s;
        }

        private void UpdateAnimation()
        {
            if (bodyFrames == null || bodyFrames.Length == 0) return;
            animTimer += Time.deltaTime * frameRate;
            int frame = (int)animTimer % bodyFrames.Length;
            if (bodyRenderer != null) bodyRenderer.sprite = bodyFrames[frame];
            if (hairRenderer != null && hairFrames != null && hairFrames.Length > 0)
            {
                hairRenderer.sprite = hairFrames[frame % hairFrames.Length];
            }
        }
    }
}
