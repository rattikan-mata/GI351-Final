using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// จุ้ยง่ายๆ ตอนซื้อ/อัปเกรด (ใช้เวลาจริง เกมหยุดก็ยังเด้ง):
    ///   Punch  = เด้งขยายแล้วหดกลับแบบสปริง (ปุ่ม / เลข LV / ค่า)
    ///   Shake  = สั่นซ้ายขวา (ปุ่มตอนเงินไม่พอ)
    ///   Squash = ยืดหดดึ๋ง (ตัวเครื่องในฉากตอนอัปเกรดเสร็จ)
    /// เรียกซ้ำระหว่างเล่นอยู่ได้: เริ่มใหม่จากขนาด/ตำแหน่งเดิม ไม่สะสมจนเพี้ยน
    /// </summary>
    public static class UiJuice
    {
        private class Runner : MonoBehaviour { }

        /// <summary>ความแรงรวมของจุ้ยทุกแบบ (0.5 = เด้ง/สั่น/ยืดหด ครึ่งหนึ่งของค่าที่ส่งมา)</summary>
        public static float Strength = 0.5f;

        private static Runner runner;
        private static readonly Dictionary<Transform, Coroutine> running = new Dictionary<Transform, Coroutine>();
        private static readonly Dictionary<Transform, Vector3> baseScale = new Dictionary<Transform, Vector3>();
        private static readonly Dictionary<Transform, Vector3> basePos = new Dictionary<Transform, Vector3>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runner = null;
            running.Clear();
            baseScale.Clear();
            basePos.Clear();
        }

        /// <summary>เด้งขยาย amount (0.18 = ใหญ่ขึ้น 18%) แล้วสปริงกลับขนาดเดิมใน duration วินาที</summary>
        public static void Punch(Transform t, float amount = 0.18f, float duration = 0.3f)
        {
            if (!Begin(t, out Vector3 s, out _)) return;
            running[t] = runner.StartCoroutine(PunchRoutine(t, s, amount * Strength, duration));
        }

        /// <summary>สั่นซ้ายขวา pixels พิกเซล (UI) ใน duration วินาที</summary>
        public static void Shake(Transform t, float pixels = 12f, float duration = 0.3f)
        {
            if (!Begin(t, out _, out Vector3 p)) return;
            running[t] = runner.StartCoroutine(ShakeRoutine(t, p, pixels * Strength, duration));
        }

        /// <summary>ยืดหดดึ๋ง: แบนกว้างก่อน แล้วยืดสูง แล้วกลับขนาดเดิม (ตัวเครื่องในฉาก)
        /// onDone = เรียกตอนเด้งจบ (เช่น ให้แผงจัดจานใหม่ ถ้ามีจานถูกจัดวางระหว่างที่ตัวเครื่องยืดหดอยู่)</summary>
        public static void Squash(Transform t, float amount = 0.16f, float duration = 0.35f, System.Action onDone = null)
        {
            if (!Begin(t, out Vector3 s, out _)) return;
            running[t] = runner.StartCoroutine(SquashRoutine(t, s, amount * Strength, duration, onDone));
        }

        // หยุดอันเก่าของ transform เดียวกัน แล้วคืนขนาด/ตำแหน่งเดิมก่อนเริ่มอันใหม่
        private static bool Begin(Transform t, out Vector3 scale, out Vector3 pos)
        {
            scale = Vector3.one;
            pos = Vector3.zero;
            if (t == null || !t.gameObject.activeInHierarchy) return false;
            if (runner == null)
            {
                var go = new GameObject("UiJuice");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            if (running.TryGetValue(t, out Coroutine c) && c != null) runner.StopCoroutine(c);
            if (!baseScale.TryGetValue(t, out scale)) { scale = t.localScale; baseScale[t] = scale; }
            if (!basePos.TryGetValue(t, out pos)) { pos = t.localPosition; basePos[t] = pos; }
            t.localScale = scale;
            t.localPosition = pos;
            return true;
        }

        private static void End(Transform t, Vector3 scale, Vector3 pos)
        {
            if (t != null)
            {
                t.localScale = scale;
                t.localPosition = pos;
            }
            running.Remove(t);
            baseScale.Remove(t);
            basePos.Remove(t);
        }

        private static IEnumerator PunchRoutine(Transform t, Vector3 s, float amount, float duration)
        {
            Vector3 p = t.localPosition;
            for (float e = 0f; e < duration && t != null; e += Time.unscaledDeltaTime)
            {
                float k = e / duration;
                // สปริงหน่วง: ขึ้นเร็ว เด้งเกินนิดหนึ่ง แล้วค่อยๆ นิ่ง
                float wave = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k);
                t.localScale = s * (1f + amount * wave);
                yield return null;
            }
            End(t, s, p);
        }

        private static IEnumerator ShakeRoutine(Transform t, Vector3 p, float pixels, float duration)
        {
            Vector3 s = t.localScale;
            for (float e = 0f; e < duration && t != null; e += Time.unscaledDeltaTime)
            {
                float k = e / duration;
                t.localPosition = p + new Vector3(Mathf.Sin(k * Mathf.PI * 8f) * pixels * (1f - k), 0f, 0f);
                yield return null;
            }
            End(t, s, p);
        }

        private static IEnumerator SquashRoutine(Transform t, Vector3 s, float amount, float duration, System.Action onDone)
        {
            Vector3 p = t.localPosition;
            for (float e = 0f; e < duration && t != null; e += Time.unscaledDeltaTime)
            {
                float k = e / duration;
                float wave = Mathf.Sin(k * Mathf.PI * 2f) * (1f - k);   // + = แบนกว้าง, - = ยืดสูง
                t.localScale = new Vector3(s.x * (1f + amount * wave), s.y * (1f - amount * wave), s.z);
                yield return null;
            }
            End(t, s, p);
            if (t != null) onDone?.Invoke();
        }
    }
}
