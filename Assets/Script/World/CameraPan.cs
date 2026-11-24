using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เลื่อนกล้องแบบฉากหนัง: แถบดำเลื่อนเข้าบน-ล่าง กล้องซูมเข้าเล็กน้อยแล้วค่อยๆ ไหลไปที่จุดเป้าหมาย ค้างไว้ แล้วไหลกลับมาที่ผู้เล่น
    /// ระหว่างนั้นผู้เล่นขยับไม่ได้ ใช้ร่วมกัน: เปิดประตูโซน (AreaGate) และสอนเล่น (TutorialGuide)  ทีละครั้ง ไม่ซ้อนกัน
    /// </summary>
    public static class CameraPan
    {
        /// <summary>true ระหว่างที่กล้องกำลังเลื่อนไปโชว์จุดอื่น</summary>
        public static bool Busy { get; private set; }

        /// <summary>true ตอนกล้องถึงเป้าหมายแล้วและกำลังค้างโชว์อยู่</summary>
        public static bool Holding { get; private set; }

        private const float BarHeight = 0.11f;   // ความสูงแถบดำ (สัดส่วนของจอ)
        private const float BarTime = 0.35f;     // เวลาที่แถบดำเลื่อนเข้า/ออก
        private static RectTransform topBar, bottomBar;

        /// <summary>
        /// เลื่อนไปที่ focus ค้างไว้ holdTime วินาที แล้วกลับ (เรียกผ่าน StartCoroutine)
        /// zoom = ขนาดกล้องตอนโชว์เทียบกับปกติ (0.8 = ซูมเข้า 20%)  cinematic = มีแถบดำและล็อกการเดินของผู้เล่น
        /// </summary>
        public static IEnumerator Run(Vector3 focus, float travelTime, float holdTime, float zoom = 0.82f, bool cinematic = true)
        {
            Camera cam = Camera.main;
            if (cam == null || Busy) yield break;
            Busy = true;
            Holding = false;

            Transform ct = cam.transform;
            Transform oldParent = ct.parent;
            Vector3 oldLocalPos = ct.localPosition;
            float z = ct.position.z;
            float baseSize = cam.orthographicSize;

            PlayerMovement mover = cinematic && oldParent != null ? oldParent.GetComponentInParent<PlayerMovement>() : null;
            Rigidbody2D body = mover != null ? mover.GetComponent<Rigidbody2D>() : null;
            if (mover != null) mover.enabled = false;
            if (body != null) body.linearVelocity = Vector2.zero;

            // แยกกล้องออกจากตัวผู้เล่นชั่วคราว
            ct.SetParent(null, true);
            Vector3 from = ct.position;
            Vector3 to = new Vector3(focus.x, focus.y, z);

            float t = 0f;
            while (t < travelTime)
            {
                t += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(travelTime > 0f ? t / travelTime : 1f));
                ct.position = Vector3.Lerp(from, to, k);
                cam.orthographicSize = Mathf.Lerp(baseSize, baseSize * zoom, k);
                if (cinematic) SetBars(Mathf.Clamp01(t / BarTime));
                yield return null;
            }
            ct.position = to;
            cam.orthographicSize = baseSize * zoom;
            if (cinematic) SetBars(1f);

            Holding = true;
            yield return new WaitForSeconds(holdTime);
            Holding = false;

            // กลับมาที่ตำแหน่งปัจจุบันของผู้เล่น
            Vector3 start = ct.position;
            t = 0f;
            while (t < travelTime)
            {
                t += Time.deltaTime;
                float k = EaseInOut(Mathf.Clamp01(travelTime > 0f ? t / travelTime : 1f));
                Vector3 back = oldParent != null ? oldParent.TransformPoint(oldLocalPos) : from;
                ct.position = Vector3.Lerp(start, back, k);
                cam.orthographicSize = Mathf.Lerp(baseSize * zoom, baseSize, k);
                if (cinematic) SetBars(1f - Mathf.Clamp01((t - (travelTime - BarTime)) / BarTime));
                yield return null;
            }

            cam.orthographicSize = baseSize;
            if (cinematic) SetBars(0f);
            if (oldParent != null)
            {
                ct.SetParent(oldParent, false);
                ct.localPosition = oldLocalPos;
            }
            if (mover != null) mover.enabled = true;
            Busy = false;
        }

        // เริ่มฉากใหม่ (เช่น กลับเมนูแล้วเข้าเกมใหม่กลางการเลื่อนกล้อง) ให้ล้างสถานะค้าง
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Busy = false;
            Holding = false;
            topBar = bottomBar = null;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            Busy = false;
            Holding = false;
        }

        /// <summary>จุดนี้อยู่ในจอตอนนี้ไหม (margin = เว้นขอบจอเป็นสัดส่วน 0-0.5)</summary>
        public static bool IsOnScreen(Vector3 world, float margin = 0.08f)
        {
            Camera cam = Camera.main;
            if (cam == null) return true;
            Vector3 v = cam.WorldToViewportPoint(world);
            return v.x > margin && v.x < 1f - margin && v.y > margin && v.y < 1f - margin;
        }

        /// <summary>ช้าตอนออกตัว เร็วกลางทาง ช้าตอนถึง (ease in-out cubic)</summary>
        private static float EaseInOut(float x) => x < 0.5f ? 4f * x * x * x : 1f - Mathf.Pow(-2f * x + 2f, 3f) / 2f;

        // ---------------- แถบดำบน-ล่าง ----------------

        private static void SetBars(float amount)
        {
            amount = Mathf.Clamp01(amount);
            if (amount <= 0f && topBar == null) return;
            if (topBar == null || bottomBar == null) BuildBars();
            float h = BarHeight * EaseInOut(amount);
            topBar.anchorMin = new Vector2(0f, 1f - h);
            bottomBar.anchorMax = new Vector2(1f, h);
            bool show = amount > 0f;
            if (topBar.gameObject.activeSelf != show) topBar.gameObject.SetActive(show);
            if (bottomBar.gameObject.activeSelf != show) bottomBar.gameObject.SetActive(show);
        }

        private static void BuildBars()
        {
            RectTransform canvas = UiBuild.CreateCanvas(null, "Cinematic Bars", 110);
            Object.Destroy(canvas.GetComponent<GraphicRaycaster>());   // ไม่บังการคลิก
            topBar = MakeBar(canvas, "Top Bar", new Vector2(0f, 1f), new Vector2(1f, 1f));
            bottomBar = MakeBar(canvas, "Bottom Bar", new Vector2(0f, 0f), new Vector2(1f, 0f));
        }

        private static RectTransform MakeBar(RectTransform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.GetComponent<Image>();
            img.color = new Color(0.05f, 0.05f, 0.06f, 1f);
            img.raycastTarget = false;
            return rt;
        }
    }
}
