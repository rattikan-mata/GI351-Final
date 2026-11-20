using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวถือสถานะ hit stop ที่อยู่ข้ามฉาก (สร้างอัตโนมัติโดย JuiceDirector ไม่ต้องวางในซีน)
    /// ไม่ผูกกับสัตว์ตัวไหน เพราะสัตว์ที่โดนตีอาจถูก destroy ระหว่างหยุด แต่ timeScale ต้องคืนเสมอ
    /// </summary>
    internal sealed class JuiceRunner : MonoBehaviour
    {
        private bool holding;
        private float savedScale = 1f;
        private float releaseAt;

        public bool IsHolding => holding;

        /// <summary>
        /// ขอหยุดเกม duration วินาที (เวลาจริง) ถ้าหยุดอยู่แล้วใช้ค่าที่ "เหลือนานกว่า" ไม่บวกสะสม
        /// ถ้าเกมถูกหยุดด้วยอย่างอื่นอยู่แล้ว (timeScale = 0 เช่นเมนู pause) จะไม่ไปทับ
        /// </summary>
        public void RequestHitStop(float duration)
        {
            if (duration <= 0f) return;

            float now = Time.unscaledTime;
            if (holding)
            {
                releaseAt = Mathf.Max(releaseAt, now + duration);
                return;
            }

            if (Time.timeScale <= 0f) return;

            savedScale = Time.timeScale;
            Time.timeScale = 0f;
            holding = true;
            releaseAt = now + duration;
        }

        private void Update()
        {
            if (holding && Time.unscaledTime >= releaseAt) Release();
        }

        private void Release()
        {
            if (!holding) return;
            holding = false;

            // timeScale ยังเป็น 0 จากเรา -> คืนค่าเดิม / ถ้าถูกระบบอื่นเปลี่ยนไปแล้ว (ไม่ใช่ 0) ไม่ไปทับ
            if (Mathf.Approximately(Time.timeScale, 0f)) Time.timeScale = savedScale;
        }

        private void OnDisable()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
            JuiceDirector.ForgetRunner(this);
        }
    }
}
