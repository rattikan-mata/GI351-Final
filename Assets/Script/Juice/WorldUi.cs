using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// รูปทรง UI ในโลก (world-space) ที่วาดด้วยโค้ดเอง: วงรี / สี่เหลี่ยมมุมมน / ลูกศร ขอบนุ่ม
    /// ใช้ร่วมกันที่ป้ายบนหม้อ ป้ายจำนวนบนแผง หลอดคิดเงิน ฯลฯ (ไม่ต้องมีไฟล์ภาพ)
    /// </summary>
    public static class WorldUi
    {
        private static Sprite pill;
        private static Sprite roundRect;
        private static Sprite arrow;
        private static Sprite circle;
        private static Sprite pixel;
        private static Sprite pixelLeft;

        /// <summary>วงรีแบบแคปซูล (กว้าง 2 : สูง 1)</summary>
        public static Sprite Pill => pill != null ? pill : (pill = MakeRounded(128, 64, 32f));

        /// <summary>สี่เหลี่ยมมุมมน (กว้าง 2.5 : สูง 1)</summary>
        public static Sprite RoundRect => roundRect != null ? roundRect : (roundRect = MakeRounded(160, 64, 14f));

        /// <summary>วงกลม</summary>
        public static Sprite Circle => circle != null ? circle : (circle = MakeRounded(64, 64, 32f));

        /// <summary>สี่เหลี่ยมขาว 1x1 หน่วย pivot กลาง</summary>
        public static Sprite Pixel => pixel != null ? pixel : (pixel = MakePixel(new Vector2(0.5f, 0.5f)));

        /// <summary>สี่เหลี่ยมขาว 1x1 หน่วย pivot ซ้าย (ใช้ทำหลอดที่ยืดไปทางขวา)</summary>
        public static Sprite PixelLeft => pixelLeft != null ? pixelLeft : (pixelLeft = MakePixel(new Vector2(0f, 0.5f)));

        /// <summary>ลูกศรสามเหลี่ยมชี้ขวา</summary>
        public static Sprite Arrow
        {
            get
            {
                if (arrow != null) return arrow;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float px = (x + 0.5f) / n, py = (y + 0.5f) / n;
                        // สามเหลี่ยม: ฐานซ้าย (x=0.15) ปลายขวา (x=0.9, y=0.5)
                        float half = Mathf.Lerp(0.42f, 0f, Mathf.InverseLerp(0.15f, 0.9f, px));
                        float inside = Mathf.Min(px - 0.15f, half - Mathf.Abs(py - 0.5f));
                        float a = Mathf.Clamp01(inside * n + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, px > 0.9f ? 0f : a));
                    }
                }
                tex.Apply();
                tex.hideFlags = HideFlags.HideAndDontSave;
                arrow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), n);
                arrow.hideFlags = HideFlags.HideAndDontSave;
                return arrow;
            }
        }

        /// <summary>สร้าง SpriteRenderer ลูก ขนาด size หน่วย (ถ้า sprite ว่าง = ยังไม่ตั้งขนาด ใช้ FitIcon ทีหลัง)</summary>
        public static SpriteRenderer MakeShape(Transform parent, string name, Sprite sprite, Color color, Vector2 size,
            Vector3 localPos, int layer, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerID = layer;
            sr.sortingOrder = order;
            if (sprite != null)
            {
                Vector3 b = sprite.bounds.size;
                go.transform.localScale = new Vector3(size.x / Mathf.Max(0.0001f, b.x), size.y / Mathf.Max(0.0001f, b.y), 1f);
            }
            return sr;
        }

        /// <summary>ใส่ภาพไอคอนแล้วย่อ/ขยายให้ด้านยาวสุดเท่ากับ boxSize หน่วย</summary>
        public static void FitIcon(SpriteRenderer sr, Sprite sprite, float boxSize)
        {
            if (sr == null) return;
            if (sr.sprite != sprite) sr.sprite = sprite;
            if (sprite == null) return;
            Vector3 size = sprite.bounds.size;
            float s = boxSize / Mathf.Max(0.0001f, Mathf.Max(size.x, size.y));
            sr.transform.localScale = new Vector3(s, s, 1f);
        }

        /// <summary>ชดเชยสเกลของ parent ให้ root นี้มีหน่วยเป็นเมตรในโลก</summary>
        public static void KeepWorldScale(Transform root)
        {
            if (root == null || root.parent == null) return;
            Vector3 lossy = root.parent.lossyScale;
            root.localScale = new Vector3(
                1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                1f / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                1f);
        }

        private static Sprite MakeRounded(int w, int h, float radius)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float cx = Mathf.Clamp(px, radius, w - radius);
                    float cy = Mathf.Clamp(py, radius, h - radius);
                    float d = Mathf.Sqrt((px - cx) * (px - cx) + (py - cy) * (py - cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            var s = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), h);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }

        private static Sprite MakePixel(Vector2 pivot)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.hideFlags = HideFlags.HideAndDontSave;
            var s = Sprite.Create(tex, new Rect(0, 0, 1, 1), pivot, 1f);
            s.hideFlags = HideFlags.HideAndDontSave;
            return s;
        }
    }

    /// <summary>
    /// ป้ายวงรีบอกจำนวน: ไอคอนซ้าย + ตัวเลขขวา (แบบป้ายบนหม้อ / ใต้แผงขาย)
    /// สร้างด้วย Create แล้วเรียก Set ทุกครั้งที่ค่าเปลี่ยน และ Tick ทุกเฟรม (ให้เด้งตอน Punch)
    /// </summary>
    public class WorldCountBadge
    {
        public GameObject Root { get; private set; }

        private SpriteRenderer icon;
        private TextMesh text;
        private float iconBox;
        private int lastCount = int.MinValue;
        private string lastSuffix;
        private float punchStart = -10f;
        private const float PunchTime = 0.16f;

        public static WorldCountBadge Create(Transform parent, string name, Vector2 size, Color fill, Color outline, Color textColor,
            int layer, int order)
        {
            var b = new WorldCountBadge();
            b.Root = new GameObject(name);
            b.Root.transform.SetParent(parent, false);

            const float line = 0.06f;
            WorldUi.MakeShape(b.Root.transform, "Outline", WorldUi.Pill, outline, new Vector2(size.x + line * 2f, size.y + line * 2f), Vector3.zero, layer, order);
            WorldUi.MakeShape(b.Root.transform, "Fill", WorldUi.Pill, fill, size, Vector3.zero, layer, order + 1);
            b.icon = WorldUi.MakeShape(b.Root.transform, "Icon", null, Color.white, Vector2.one, new Vector3(-size.x * 0.23f, 0f, 0f), layer, order + 2);
            b.iconBox = size.y * 0.78f;

            var textGo = new GameObject("Count");
            textGo.transform.SetParent(b.Root.transform, false);
            textGo.transform.localPosition = new Vector3(size.x * 0.2f, 0.01f, 0f);
            b.text = textGo.AddComponent<TextMesh>();
            b.text.anchor = TextAnchor.MiddleCenter;
            b.text.alignment = TextAlignment.Center;
            b.text.fontStyle = FontStyle.Bold;
            b.text.fontSize = 64;
            b.text.characterSize = size.y * 0.11f;
            b.text.color = textColor;
            var mr = textGo.GetComponent<MeshRenderer>();
            mr.sortingLayerID = layer;
            mr.sortingOrder = order + 3;
            return b;
        }

        public bool Visible
        {
            get => Root != null && Root.activeSelf;
            set { if (Root != null && Root.activeSelf != value) Root.SetActive(value); }
        }

        /// <summary>ตั้งไอคอน (สีย้อม) และตัวเลข suffix ต่อท้ายได้ เช่น "/40"</summary>
        public void Set(Sprite iconSprite, Color iconColor, int count, string suffix = null)
        {
            if (Root == null) return;
            WorldUi.FitIcon(icon, iconSprite, iconBox);
            if (icon.color != iconColor) icon.color = iconColor;
            if (count != lastCount || suffix != lastSuffix)
            {
                text.text = string.IsNullOrEmpty(suffix) ? count.ToString() : count + suffix;
                lastCount = count;
                lastSuffix = suffix;
            }
        }

        /// <summary>เด้งขยายแล้วกลับ</summary>
        public void Punch()
        {
            punchStart = Time.unscaledTime;
        }

        public void Tick()
        {
            if (Root == null) return;
            float k = (Time.unscaledTime - punchStart) / PunchTime;
            float s = k >= 0f && k < 1f ? Mathf.Lerp(1f, 1.22f, Mathf.Sin(k * Mathf.PI)) : 1f;
            Root.transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
