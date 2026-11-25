using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ท่ายืนของรูปตัวละครบน UI (เช่น หน้าเมนูหลัก): ค้างเฟรม 0 (ลืมตา) + กะพริบตาเป็นระยะ
    /// กะพริบ = เล่นเฟรม 0 -> 1 (ตาปรือ) -> 2 (หลับตา) แล้วกลับเฟรม 0 (เหมือน PlayerSpriteAnimator)
    /// ใช้เวลาจริง เมนูหยุดเกมก็ยังขยับ
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UiIdleAnimator : MonoBehaviour
    {
        [Tooltip("เฟรมท่ายืน: 0 = ลืมตา (ค้างไว้), 1 = ตาปรือ, 2 = หลับตา (เล่น 0 -> 1 -> 2 ตอนกะพริบ) ถ้ามีไม่ถึง 3 เฟรมจะโชว์แค่เฟรม 0")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("กะพริบตาทุกกี่วินาที (สุ่มในช่วงนี้)")]
        [SerializeField] private Vector2 blinkEvery = new Vector2(2.5f, 4.5f);

        [Tooltip("ตอนกะพริบ โชว์เฟรมตาปรือ / หลับตา เฟรมละกี่วินาที")]
        [SerializeField] private float blinkFrameTime = 0.07f;

        private Image image;
        private float nextBlink;
        private float blinkStart = -999f;

        private void Awake()
        {
            image = GetComponent<Image>();
            nextBlink = Time.unscaledTime + Random.Range(blinkEvery.x, blinkEvery.y);
        }

        private void Update()
        {
            if (image == null || frames == null || frames.Length == 0) return;

            Sprite s = frames[0];
            if (frames.Length >= 3)
            {
                // ค้างเฟรม 0 (ลืมตา) ถึงเวลากะพริบแล้วเล่นเฟรม 1 -> 2 เฟรมละ blinkFrameTime แล้วกลับเฟรม 0
                float now = Time.unscaledTime;
                float frameTime = Mathf.Max(0.01f, blinkFrameTime);
                if (now >= nextBlink)
                {
                    blinkStart = now;
                    nextBlink = now + frameTime * (frames.Length - 1) + Random.Range(blinkEvery.x, blinkEvery.y);
                }
                float step = (now - blinkStart) / frameTime;
                if (step < frames.Length - 1) s = frames[(int)step + 1];
            }
            if (image.sprite != s) image.sprite = s;
        }
    }
}
