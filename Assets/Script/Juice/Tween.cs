using System.Collections;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>ชนิด ease ที่เลือกได้ใน Inspector (JuiceConfig)</summary>
    public enum Ease
    {
        Linear,
        InQuad,
        OutQuad,
        InOutQuad,
        OutCubic,
        InSine,
        OutSine,
        InOutSine,
        InBack,
        OutBack
    }

    /// <summary>
    /// tween helper เล็กๆ แบบ coroutine (ไม่ใช้ DOTween) ใช้กับ StartCoroutine ของ MonoBehaviour ตัวไหนก็ได้
    /// เลือกได้ว่าจะนับเวลาแบบ unscaled (ไม่หยุดตอน hit stop / pause) หรือแบบปกติ
    /// ทุกตัวจะเรียก onUpdate ด้วยค่า 0 ตอนเริ่ม และ 1 ตอนจบเสมอ (ค่าที่ส่งเป็นค่าที่ผ่าน ease แล้ว InBack/OutBack อาจเกิน 0-1 ระหว่างทาง)
    /// </summary>
    public static class Tween
    {
        private const float BackC1 = 1.70158f;
        private const float BackC3 = BackC1 + 1f;

        /// <summary>แปลงเวลา 0-1 เป็นค่า 0-1 ตามชนิด ease</summary>
        public static float Evaluate(Ease ease, float t)
        {
            t = Mathf.Clamp01(t);
            switch (ease)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.InSine: return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
                case Ease.OutSine: return Mathf.Sin(t * Mathf.PI * 0.5f);
                case Ease.InOutSine: return -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f;
                case Ease.InBack: return BackC3 * t * t * t - BackC1 * t * t;
                case Ease.OutBack:
                    float u = t - 1f;
                    return 1f + BackC3 * u * u * u + BackC1 * u * u;
                default: return t;
            }
        }

        /// <summary>แปลงเวลา 0-1 ด้วย AnimationCurve (ถ้า curve ว่างใช้ linear)</summary>
        public static float Evaluate(AnimationCurve curve, float t)
        {
            t = Mathf.Clamp01(t);
            return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
        }

        /// <summary>รัน onUpdate(eased) ตลอด duration วินาที (เรียกที่ 0 ตอนเริ่ม และที่ 1 ตอนจบ)</summary>
        public static IEnumerator Run(float duration, System.Action<float> onUpdate, Ease ease = Ease.Linear, bool unscaled = false)
        {
            return RunCore(duration, onUpdate, t => Evaluate(ease, t), unscaled);
        }

        /// <summary>เหมือนกันแต่ใช้ AnimationCurve เป็น ease</summary>
        public static IEnumerator Run(float duration, System.Action<float> onUpdate, AnimationCurve curve, bool unscaled = false)
        {
            return RunCore(duration, onUpdate, t => Evaluate(curve, t), unscaled);
        }

        /// <summary>รอ seconds วินาที (unscaled = ไม่หยุดตอน hit stop / pause)</summary>
        public static IEnumerator Wait(float seconds, bool unscaled = false)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                yield return null;
            }
        }

        private static IEnumerator RunCore(float duration, System.Action<float> onUpdate, System.Func<float, float> easing, bool unscaled)
        {
            if (onUpdate == null) yield break;

            if (duration <= 0f)
            {
                onUpdate(easing(1f));
                yield break;
            }

            float elapsed = 0f;
            onUpdate(easing(0f));
            while (elapsed < duration)
            {
                yield return null;
                elapsed += unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                onUpdate(easing(t));
            }
        }

        /// <summary>ปัดค่าให้ลงตารางพิกเซลเต็ม (1 พิกเซล = 1 / pixelsPerUnit หน่วย)</summary>
        public static float Snap(float value, float pixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f) return value;
            return Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;
        }

        public static Vector2 Snap(Vector2 value, float pixelsPerUnit)
        {
            return new Vector2(Snap(value.x, pixelsPerUnit), Snap(value.y, pixelsPerUnit));
        }
    }
}
