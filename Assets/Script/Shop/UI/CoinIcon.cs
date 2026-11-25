using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// รูปเหรียญอีแปะที่ใช้แทนหน่วย $ ทุกที่ที่แสดงเงิน/ราคา
    /// ไม่ได้ตั้งเอง = หยิบรูปจาก Coin Prefab ของแคชเชียร์ในฉาก (รูปเดียวกับเหรียญที่เด้งตอนจ่ายเงิน)
    /// หาไม่เจอ = คืน null (ข้อความจะแสดงแค่ตัวเลข ไม่มีรูปเหรียญ)
    /// </summary>
    public static class CoinIcon
    {
        private const float LookupInterval = 1f; // หาแคชเชียร์ซ้ำได้อย่างมากวินาทีละครั้ง (กันหาทุกเฟรมตอนไม่มีแคชเชียร์)

        private static Sprite sprite;
        private static float lastLookupTime = float.NegativeInfinity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            sprite = null;
            lastLookupTime = float.NegativeInfinity;
        }

        /// <summary>รูปเหรียญ (จำไว้หลังหาเจอครั้งแรก) ตั้งเองได้ถ้าอยากใช้รูปอื่น</summary>
        public static Sprite Sprite
        {
            get
            {
                if (sprite != null) return sprite;

                float now = Time.realtimeSinceStartup;
                if (Mathf.Abs(now - lastLookupTime) < LookupInterval) return null;
                lastLookupTime = now;

                CashierCounter cashier = Object.FindAnyObjectByType<CashierCounter>(FindObjectsInactive.Include);
                if (cashier == null || cashier.CoinPrefab == null) return null;
                SpriteRenderer sr = cashier.CoinPrefab.GetComponentInChildren<SpriteRenderer>(true);
                if (sr != null) sprite = sr.sprite;
                return sprite;
            }
            set
            {
                sprite = value;
                lastLookupTime = float.NegativeInfinity;
            }
        }
    }

    /// <summary>
    /// วางรูปเหรียญอีแปะไว้หน้าตัวเลขของ Text (แทนเครื่องหมาย $)
    /// เลื่อนตัวอักษรไปทางขวาให้เหรียญ+ตัวเลขยังชิดตามการจัดตำแหน่งเดิม (ชิดซ้าย/กลาง/ขวา)
    /// ใส่จากโค้ดด้วย CoinTextIcon.Attach(text) หรือใส่เองใน Inspector ก็ได้
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Text))]
    public class CoinTextIcon : BaseMeshEffect
    {
        public const string IconName = "Coin Icon";

        [Tooltip("ขนาดรูปเหรียญ เทียบกับขนาดตัวอักษร (1 = สูงเท่าขนาดฟอนต์)")]
        [SerializeField] private float iconScale = 1f;

        [Tooltip("ช่องว่างระหว่างเหรียญกับตัวเลข เทียบกับขนาดตัวอักษร")]
        [SerializeField] private float gap = 0.15f;

        [Tooltip("เลื่อนเหรียญขึ้น/ลง เทียบกับขนาดตัวอักษร (บวก = ขึ้น)")]
        [SerializeField] private float verticalOffset = 0f;

        [Tooltip("แสดงรูปเหรียญ (ปิด = ไม่มีเหรียญ และตัวอักษรไม่ถูกเลื่อน)")]
        [SerializeField] private bool show = true;

        private Text text;
        private Image icon;

        // ค่าที่ได้ตอนสร้าง mesh ของตัวอักษร (ModifyMesh) เอาไปวางเหรียญหลัง rebuild เสร็จ (OnWillRenderCanvases)
        private bool meshHasText;
        private bool meshWithIcon;   // mesh ล่าสุดเลื่อนตัวอักษรเผื่อที่ให้เหรียญแล้ว
        private float meshMinX;      // ขอบซ้ายของตัวอักษรก่อนเลื่อน (พิกัด local ของ Text)
        private float meshFontSize;  // ขนาดฟอนต์ที่ใช้จริง (รวม Best Fit)
        private float appliedShift;  // ระยะที่เลื่อนตัวอักษรไปทางขวา

        private bool placementDirty = true;
        private Vector2 lastRectSize;
        private float lastAlpha = -1f;
        private bool lastVisible;

        /// <summary>แสดงรูปเหรียญหรือไม่ (ปิดตอนข้อความไม่ใช่ราคา เช่น MAX / SOLD OUT)</summary>
        public bool Show
        {
            get => show;
            set
            {
                if (show == value) return;
                show = value;
                placementDirty = true;
                if (!show) SetIconVisible(false);
                if (graphic != null) graphic.SetVerticesDirty();
            }
        }

        /// <summary>ใส่เหรียญให้ Text (ใส่ครั้งเดียว เรียกซ้ำได้ คืนตัวเดิม)</summary>
        public static CoinTextIcon Attach(Text text)
        {
            if (text == null) return null;
            CoinTextIcon c = text.GetComponent<CoinTextIcon>();
            if (c == null) c = text.gameObject.AddComponent<CoinTextIcon>();
            c.EnsureIcon();
            return c;
        }

        /// <summary>เปิด/ปิดเหรียญของ Text: เปิด = ใส่ให้ถ้ายังไม่มี / ปิด = ปิดเฉพาะตัวที่มีอยู่แล้ว</summary>
        public static void SetVisible(Text text, bool visible)
        {
            if (text == null) return;
            CoinTextIcon c = visible ? Attach(text) : text.GetComponent<CoinTextIcon>();
            if (c != null) c.Show = visible;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnsureIcon();
            placementDirty = true;
            // วางเหรียญหลัง UI rebuild ของเฟรมนี้เสร็จ (CanvasUpdateRegistry สมัครไว้ก่อนแล้ว = ทำงานก่อนเรา)
            // เหรียญเลยขยับ/ซ่อนตามตัวเลขในเฟรมเดียวกัน ไม่ช้าไป 1 เฟรม
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            Canvas.willRenderCanvases += OnWillRenderCanvases;
        }

        protected override void OnDisable()
        {
            Canvas.willRenderCanvases -= OnWillRenderCanvases;
            base.OnDisable();
            // ตอนทั้ง GameObject ถูกปิด เหรียญ (ลูก) ก็หายไปด้วยอยู่แล้ว
            if (icon != null && gameObject.activeInHierarchy) SetIconVisible(false);
        }

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();
            placementDirty = true;
        }
#endif

        /// <summary>สร้าง (หรือหาตัวเดิม) รูปเหรียญลูก "Coin Icon"</summary>
        private void EnsureIcon()
        {
            if (text == null) text = GetComponent<Text>();
            if (icon != null) return;
            if (!gameObject.scene.IsValid()) return; // ตัว prefab asset เอง ไม่สร้างลูก

            Transform existing = transform.Find(IconName);
            if (existing != null) icon = existing.GetComponent<Image>();
            if (icon == null)
            {
                GameObject go = new GameObject(IconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                if (!Application.isPlaying) go.hideFlags = HideFlags.DontSaveInEditor | HideFlags.NotEditable;
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                icon = go.GetComponent<Image>();
            }

            icon.raycastTarget = false;
            icon.preserveAspect = true;
            LayoutElement le = icon.GetComponent<LayoutElement>();
            if (le == null) le = icon.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true; // Layout Group ของตัวแม่ไม่ต้องจัดเหรียญ
            icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            if (icon.sprite == null) icon.sprite = CoinIcon.Sprite;
            icon.enabled = false; // ซ่อนไว้ก่อนจนกว่าจะวางตำแหน่งครั้งแรก
            lastVisible = false;
            lastAlpha = -1f;
            placementDirty = true;
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            if (text == null) text = GetComponent<Text>();

            int count = vh.currentVertCount;
            meshHasText = text != null && count > 0 && !string.IsNullOrEmpty(text.text);
            meshWithIcon = false;
            appliedShift = 0f;
            placementDirty = true;
            if (!meshHasText) return;

            meshFontSize = UsedFontSize();

            UIVertex v = default;
            float minX = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                if (v.position.x < minX) minX = v.position.x;
            }
            meshMinX = minX;

            // ห้ามแตะรูปเหรียญตรงนี้ (อยู่ระหว่าง rebuild ของ UI) แค่เช็กว่ามีรูปให้ใช้ไหม
            Sprite s = icon != null && icon.sprite != null ? icon.sprite : CoinIcon.Sprite;
            if (!show || icon == null || s == null) return;
            meshWithIcon = true;

            float total = (iconScale + gap) * meshFontSize;
            int h = (int)text.alignment % 3; // 0 ซ้าย 1 กลาง 2 ขวา
            appliedShift = h == 0 ? total : (h == 1 ? total * 0.5f : 0f);
            if (appliedShift == 0f) return;

            for (int i = 0; i < count; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.x += appliedShift;
                vh.SetUIVertex(v, i);
            }
        }

        /// <summary>ขนาดฟอนต์ที่ใช้จริง (Best Fit = ขนาดที่ย่อ/ขยายแล้ว)</summary>
        private float UsedFontSize()
        {
            float size = text.fontSize;
            if (text.resizeTextForBestFit && text.font != null && text.font.dynamic)
            {
                int used = text.cachedTextGenerator.fontSizeUsedForBestFit;
                float ppu = text.pixelsPerUnit;
                if (used > 0 && ppu > 0f)
                {
                    size = Mathf.Clamp(used / ppu, text.resizeTextMinSize, Mathf.Max(text.resizeTextMinSize, text.resizeTextMaxSize));
                }
            }
            return Mathf.Max(1f, size);
        }

        /// <summary>เช็กทุกเฟรมแบบถูกๆ หลัง UI rebuild แล้ววางเหรียญใหม่เฉพาะตอนมีอะไรเปลี่ยน</summary>
        private void OnWillRenderCanvases()
        {
            if (this == null) return;
            if (text == null)
            {
                text = GetComponent<Text>();
                if (text == null) return;
            }
            if (icon == null)
            {
                EnsureIcon();
                if (icon == null) return;
            }

            if (icon.sprite == null)
            {
                Sprite s = CoinIcon.Sprite;
                if (s != null)
                {
                    icon.sprite = s;
                    placementDirty = true;
                    if (graphic != null) graphic.SetVerticesDirty(); // สร้าง mesh ใหม่ให้เลื่อนตัวอักษรเผื่อที่เหรียญ
                }
            }

            Vector2 size = text.rectTransform.rect.size;
            if (size != lastRectSize)
            {
                lastRectSize = size;
                placementDirty = true;
            }

            float a = text.color.a;
            if (a != lastAlpha)
            {
                lastAlpha = a;
                icon.color = new Color(1f, 1f, 1f, a);
            }

            bool visible = show && text.enabled && meshHasText && meshWithIcon && icon.sprite != null;
            if (visible != lastVisible) placementDirty = true;
            if (!placementDirty) return;
            placementDirty = false;

            SetIconVisible(visible);
            if (visible) PlaceIcon();
        }

        /// <summary>วางเหรียญชิดซ้ายของตัวอักษร (ที่เลื่อนแล้ว) สูงต่ำตามการจัดแนวตั้งของ Text</summary>
        private void PlaceIcon()
        {
            RectTransform textRect = text.rectTransform;
            RectTransform rt = icon.rectTransform;
            Rect r = textRect.rect;

            float iconSize = meshFontSize * iconScale;
            float gapSize = meshFontSize * gap;
            float x = meshMinX + appliedShift - gapSize - iconSize * 0.5f;

            int vAlign = (int)text.alignment / 3; // 0 บน 1 กลาง 2 ล่าง
            float y;
            if (vAlign == 0) y = r.yMax - meshFontSize * 0.5f;
            else if (vAlign == 2) y = r.yMin + meshFontSize * 0.5f;
            else y = r.center.y;
            y += verticalOffset * meshFontSize;

            // anchor ที่ pivot ของ Text = anchoredPosition ตรงกับพิกัด local ของ mesh ตัวอักษร
            rt.anchorMin = textRect.pivot;
            rt.anchorMax = textRect.pivot;
            rt.sizeDelta = new Vector2(iconSize, iconSize);
            rt.anchoredPosition = new Vector2(x, y);
        }

        private void SetIconVisible(bool visible)
        {
            lastVisible = visible;
            if (icon != null && icon.enabled != visible) icon.enabled = visible;
        }
    }
}
