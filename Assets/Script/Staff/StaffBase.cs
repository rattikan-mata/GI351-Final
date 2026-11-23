using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ส่วนที่พนักงานทุกแบบใช้ร่วมกัน: ภาพ (แอนิเมชันยืน) และการเด้งโผล่ตอนถูกจ้าง
    /// พนักงานแต่ละแบบ (CashierStaff / WaiterStaff) สืบทอดจากตัวนี้ แล้วเรียก UpdateVisual() ใน Update
    /// </summary>
    public abstract class StaffBase : MonoBehaviour
    {
        [Header("Visual (ภาพ)")]
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected SpriteRenderer hairRenderer;

        [Tooltip("เฟรมแอนิเมชันยืน (ตัว)")]
        [SerializeField] private Sprite[] bodyFrames;

        [Tooltip("เฟรมแอนิเมชันยืน (ผม) ต้องจำนวนเท่ากับตัว")]
        [SerializeField] private Sprite[] hairFrames;

        [Tooltip("ความเร็วแอนิเมชัน (เฟรมต่อวินาที)")]
        [SerializeField] private float frameRate = 8f;

        [Tooltip("เวลาเด้งตอนเกิด (วินาที) ขยายเกินแล้วหดกลับ")]
        [SerializeField] private float popInDuration = 0.35f;

        private float animTimer;
        private float popTimer;
        private Vector3 baseScale;

        protected virtual void Awake()
        {
            baseScale = transform.localScale;
            transform.localScale = Vector3.zero;
        }

        /// <summary>เล่นแอนิเมชันและการเด้งโผล่ (เรียกทุกเฟรม)</summary>
        protected void UpdateVisual()
        {
            UpdatePopIn();
            UpdateAnimation();
        }

        /// <summary>หันหน้าซ้าย/ขวาตามทิศที่เดิน</summary>
        protected void FaceDirection(float x)
        {
            if (Mathf.Abs(x) < 0.01f) return;
            bool flip = x < 0f;
            if (bodyRenderer != null) bodyRenderer.flipX = flip;
            if (hairRenderer != null) hairRenderer.flipX = flip;
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
