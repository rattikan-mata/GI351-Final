using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ท่ายืนของรูปตัวละครบน UI (เช่น หน้าเมนูหลัก): สลับเฟรมยืนไปมา + กะพริบตาเป็นระยะ
    /// เฟรมสุดท้าย = หลับตา (เหมือน PlayerSpriteAnimator) ใช้เวลาจริง เมนูหยุดเกมก็ยังขยับ
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class UiIdleAnimator : MonoBehaviour
    {
        [Tooltip("เฟรมท่ายืน: 0-1 สลับไปมา เฟรมสุดท้าย = หลับตา")]
        [SerializeField] private Sprite[] frames;

        [Tooltip("สลับเฟรมยืนกี่เฟรมต่อวินาที")]
        [SerializeField] private float fps = 2f;

        [Tooltip("กะพริบตาทุกกี่วินาที (สุ่มในช่วงนี้)")]
        [SerializeField] private Vector2 blinkEvery = new Vector2(2.5f, 4.5f);

        [Tooltip("หลับตานานกี่วินาที")]
        [SerializeField] private float blinkTime = 0.14f;

        private Image image;
        private float timer;
        private float nextBlink;
        private float blinkUntil;

        private void Awake()
        {
            image = GetComponent<Image>();
            nextBlink = Time.unscaledTime + Random.Range(blinkEvery.x, blinkEvery.y);
        }

        private void Update()
        {
            if (image == null || frames == null || frames.Length == 0) return;

            float now = Time.unscaledTime;
            if (frames.Length >= 3 && now >= nextBlink)
            {
                blinkUntil = now + blinkTime;
                nextBlink = now + Random.Range(blinkEvery.x, blinkEvery.y);
            }

            Sprite s;
            if (frames.Length >= 3 && now < blinkUntil)
            {
                s = frames[frames.Length - 1];
            }
            else
            {
                timer += Time.unscaledDeltaTime * fps;
                int loop = Mathf.Max(1, frames.Length >= 3 ? frames.Length - 1 : frames.Length);
                s = frames[(int)timer % loop];
            }
            if (image.sprite != s) image.sprite = s;
        }
    }
}
