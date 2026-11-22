using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวช่วยสร้าง UI ด้วยโค้ด ใช้สร้างหน้าตาเริ่มต้นของ prefab หน้าต่าง (เมนู Tools > UI)
    /// และใช้สำรองตอนยังไม่ได้ใส่ prefab ให้ร้าน/หม้อ
    /// </summary>
    public static class UiBuild
    {
        private static Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            font = null;
        }

        /// <summary>ฟอนต์ในตัวของ Unity (ไม่มีภาษาไทย)</summary>
        public static Font DefaultFont
        {
            get
            {
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        /// <summary>ปุ่มบน UI กดได้ต้องมี EventSystem ในฉาก สร้างให้ถ้ายังไม่มี</summary>
        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Object.FindAnyObjectByType<EventSystem>() != null) return;

            GameObject es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            InputSystemUIInputModule module = es.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }

        /// <summary>Canvas แบบทับจอ ขนาดอ้างอิง 1920x1080</summary>
        public static RectTransform CreateCanvas(Transform parent, string name, int sortingOrder)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            if (parent != null) go.transform.SetParent(parent, false);

            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return go.GetComponent<RectTransform>();
        }

        public static RectTransform CreateRect(Transform parent, string name, params System.Type[] components)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            for (int i = 0; i < components.Length; i++) go.AddComponent(components[i]);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2 pivot)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static Text CreateText(Transform parent, string name, string content, int size, FontStyle style, Color color, TextAnchor align,
            HorizontalWrapMode wrap = HorizontalWrapMode.Wrap)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.GetComponent<RectTransform>().SetParent(parent, false);

            Text t = go.GetComponent<Text>();
            t.font = DefaultFont;
            t.text = content;
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            Outline outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        /// <summary>ปุ่มสี่เหลี่ยมสีเดียว + ข้อความเต็มปุ่ม (ผูก onClick ในโค้ดที่ใช้)</summary>
        public static Button CreateButton(Transform parent, string name, string text, Vector2 size, Color color, int fontSize = 36)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = color;

            Button btn = go.GetComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.None;

            Text t = CreateText(rt, "Text", text, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform);
            return btn;
        }

        /// <summary>ให้ข้อความย่อขนาดเองให้พอดีกรอบ (ขนาดใหญ่สุด = maxSize) Best Fit ทำงานเฉพาะตอน Vertical Overflow = Truncate</summary>
        public static void BestFit(Text t, int minSize, int maxSize)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = minSize;
            t.resizeTextMaxSize = maxSize;
            t.fontSize = maxSize;
        }

        public static void SetText(Text t, string value)
        {
            if (t != null) t.text = value;
        }
    }
}
