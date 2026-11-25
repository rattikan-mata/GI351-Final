using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ป้ายราคาในโลก (TextMesh) แบบมีรูปเหรียญอีแปะหน้าตัวเลข แทนการเขียน "$"
    /// ใส่ช่องว่างนำหน้าตัวเลขไว้เป็นที่ของรูปเหรียญ (ป้าย MiddleCenter จึงยังอยู่กลางทั้งคู่)
    /// แล้ววางรูปเหรียญ (SpriteRenderer ลูกชื่อ "Coin Icon") ชิดซ้ายของตัวเลข สูงเท่าตัวเลข
    /// ใช้: CoinMeshIcon.Attach(textMesh).SetAmount(50) / SetPlainText("SOON") (ไม่มีเหรียญ)
    /// </summary>
    [RequireComponent(typeof(TextMesh))]
    public class CoinMeshIcon : MonoBehaviour
    {
        [Tooltip("ความสูงรูปเหรียญ เทียบกับความสูงตัวเลข (1 = เท่ากัน)")]
        [SerializeField] private float iconHeightScale = 1.15f;

        [Tooltip("ระยะห่างระหว่างเหรียญกับตัวเลข เทียบกับความสูงตัวเลข")]
        [SerializeField] private float gapScale = 0.12f;

        [Tooltip("จำนวนช่องว่างที่เว้นไว้หน้าตัวเลข เมื่อวัดขนาดฟอนต์ไม่ได้ (ปกติคำนวณเองจากขนาดเหรียญ)")]
        [SerializeField] private int fallbackPadSpaces = 3;

        private TextMesh textMesh;
        private MeshRenderer textRenderer;
        private SpriteRenderer icon;
        private Sprite coinSprite;
        private float nextSpriteTry;

        private bool showAmount;
        private int amount;
        private string plainText;

        // ค่าที่วัดจากฟอนต์ (เทียบกับความสูงตัวเลข) -1 = วัดไม่ได้
        private float digitsWidthRatio = -1f;
        private float spaceWidthRatio = -1f;
        private Font measuredFont;
        private int measuredFontSize = -1;
        private FontStyle measuredStyle;
        private string appliedText;

        /// <summary>หา/ใส่ CoinMeshIcon ให้ TextMesh นี้ (ว่าง = คืน null)</summary>
        public static CoinMeshIcon Attach(TextMesh tm)
        {
            if (tm == null) return null;
            var c = tm.GetComponent<CoinMeshIcon>();
            if (c == null) c = tm.gameObject.AddComponent<CoinMeshIcon>();
            c.Init();
            return c;
        }

        /// <summary>แสดงราคาเป็น [เหรียญ] + ตัวเลข</summary>
        public void SetAmount(int value)
        {
            Init();
            // ไม่อ่าน textMesh.text (สร้าง string ใหม่ทุกครั้ง) เพราะ AreaGate เรียกทุกเฟรม
            if (showAmount && amount == value && appliedText != null) return;
            showAmount = true;
            amount = value;
            plainText = null;
            ApplyText();
        }

        /// <summary>แสดงข้อความธรรมดา ไม่มีรูปเหรียญ (เช่น "SOON")</summary>
        public void SetPlainText(string s)
        {
            Init();
            if (s == null) s = "";
            if (!showAmount && plainText == s) return;
            showAmount = false;
            plainText = s;
            appliedText = s;
            textMesh.text = s;
            SetIconVisible(false);
        }

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (textMesh == null) textMesh = GetComponent<TextMesh>();
            if (textRenderer == null) textRenderer = GetComponent<MeshRenderer>();
        }

        private void EnsureIcon()
        {
            if (icon != null) return;
            Transform existing = transform.Find("Coin Icon");
            GameObject go = existing != null ? existing.gameObject : new GameObject("Coin Icon");
            go.transform.SetParent(transform, false);
            icon = go.GetComponent<SpriteRenderer>();
            if (icon == null) icon = go.AddComponent<SpriteRenderer>();
            icon.enabled = false;
        }

        private Sprite GetCoinSprite()
        {
            if (coinSprite != null) return coinSprite;
            // CoinIcon.Sprite ค้นหาในฉากถ้ายังไม่มี จึงลองใหม่แค่วินาทีละครั้ง
            if (Time.unscaledTime < nextSpriteTry) return null;
            nextSpriteTry = Time.unscaledTime + 1f;
            coinSprite = CoinIcon.Sprite;
            return coinSprite;
        }

        /// <summary>ใส่ข้อความตัวเลข + ช่องว่างนำหน้าพอดีกับรูปเหรียญ</summary>
        private void ApplyText()
        {
            if (textMesh == null) return;
            // จำฟอนต์/ขนาดที่ใช้จัดไว้เสมอ (แม้ยังไม่มีรูปเหรียญ) ไม่งั้น LateUpdate จะจัดใหม่ทุกเฟรม
            measuredFont = textMesh.font;
            measuredFontSize = textMesh.fontSize;
            measuredStyle = textMesh.fontStyle;
            string digits = amount.ToString();
            Sprite sprite = GetCoinSprite();
            if (sprite == null)
            {
                // ยังไม่มีรูปเหรียญ: โชว์ตัวเลขเฉยๆ (ไม่มี "$")
                appliedText = digits;
                textMesh.text = digits;
                SetIconVisible(false);
                return;
            }

            Measure(digits);
            int pad = fallbackPadSpaces;
            if (spaceWidthRatio > 0f)
            {
                Vector2 size = sprite.bounds.size;
                float aspect = size.y > 0.0001f ? size.x / size.y : 1f;
                float need = iconHeightScale * aspect + gapScale;
                pad = Mathf.CeilToInt(need / spaceWidthRatio);
            }
            pad = Mathf.Clamp(pad, 1, 12);
            appliedText = new string(' ', pad) + digits;
            textMesh.text = appliedText;
        }

        /// <summary>วัดความกว้างตัวเลข/ช่องว่างจากฟอนต์ เทียบกับความสูงตัวเลข</summary>
        private void Measure(string digits)
        {
            digitsWidthRatio = -1f;
            spaceWidthRatio = -1f;
            Font f = textMesh.font != null ? textMesh.font : GameFont.Get();
            if (f == null) return;

            int size = f.dynamic ? (textMesh.fontSize > 0 ? textMesh.fontSize : f.fontSize) : 0;
            FontStyle style = textMesh.fontStyle;
            f.RequestCharactersInTexture(digits + " ", size, style);

            int advance = 0;
            int minY = int.MaxValue, maxY = int.MinValue;
            int firstMinX = 0, lastAdvance = 0, lastMaxX = 0;
            bool first = true;
            foreach (char ch in digits)
            {
                if (!f.GetCharacterInfo(ch, out CharacterInfo ci, size, style)) return;
                if (first) { firstMinX = ci.minX; first = false; }
                advance += ci.advance;
                lastAdvance = ci.advance;
                lastMaxX = ci.maxX;
                minY = Mathf.Min(minY, ci.minY);
                maxY = Mathf.Max(maxY, ci.maxY);
            }
            float height = maxY - minY;
            if (height <= 0f) return;
            // ความกว้างที่เห็นจริง: จากขอบหมึกซ้ายตัวแรกถึงขอบหมึกขวาตัวสุดท้าย (ไม่รวมระยะเว้นข้างตัวอักษร)
            float visible = advance - lastAdvance + lastMaxX - firstMinX;
            digitsWidthRatio = (visible > 0f ? visible : advance) / height;
            if (f.GetCharacterInfo(' ', out CharacterInfo space, size, style) && space.advance > 0)
            {
                spaceWidthRatio = space.advance / height;
            }
        }

        private void SetIconVisible(bool visible)
        {
            if (icon != null && icon.enabled != visible) icon.enabled = visible;
        }

        private void LateUpdate()
        {
            if (textMesh == null) return;

            // เปลี่ยนฟอนต์/ขนาดจากที่อื่น: จัดใหม่ (ไม่อ่าน textMesh.text ทุกเฟรม เพราะสร้าง string ใหม่ทุกครั้ง
            // ข้อความต้องตั้งผ่าน SetAmount/SetPlainText เท่านั้น)
            if (showAmount && (textMesh.fontSize != measuredFontSize
                || textMesh.font != measuredFont || textMesh.fontStyle != measuredStyle))
            {
                ApplyText();
            }

            if (!showAmount) { SetIconVisible(false); return; }

            if (coinSprite == null)
            {
                if (GetCoinSprite() == null) { SetIconVisible(false); return; }
                ApplyText();   // ได้รูปเหรียญแล้ว: ใส่ช่องว่างเว้นที่ให้
            }

            // ซ่อนตามตัวหนังสือ (ปิด MeshRenderer / จางจนใส)
            Color tc = textMesh.color;
            if (textRenderer == null || !textRenderer.enabled || tc.a <= 0.001f) { SetIconVisible(false); return; }

            // ขอบเขตของตัวเลขที่เห็นจริง (ช่องว่างไม่มีรูป) อาจช้าไป 1 เฟรมหลังเปลี่ยนข้อความ จึงคำนวณใหม่ทุกเฟรม
            Bounds b = textRenderer.bounds;
            float h = b.size.y;
            if (h < 0.00001f) { SetIconVisible(false); return; }

            EnsureIcon();
            if (icon.sprite != coinSprite) icon.sprite = coinSprite;
            if (icon.sortingLayerID != textRenderer.sortingLayerID) icon.sortingLayerID = textRenderer.sortingLayerID;
            if (icon.sortingOrder != textRenderer.sortingOrder) icon.sortingOrder = textRenderer.sortingOrder;
            Color ic = new Color(1f, 1f, 1f, tc.a);
            if (icon.color != ic) icon.color = ic;

            Vector2 spriteSize = coinSprite.bounds.size;
            float spriteH = Mathf.Max(0.0001f, spriteSize.y);
            float iconH = h * iconHeightScale;
            float iconW = iconH * (spriteSize.x / spriteH);

            // ความกว้างตัวเลข: จากฟอนต์ (ไม่เกินกรอบที่เห็น) หรือใช้กรอบเลยถ้าวัดไม่ได้
            float digitsW = digitsWidthRatio > 0f ? Mathf.Min(digitsWidthRatio * h, b.size.x) : b.size.x;
            float digitsLeft = b.max.x - digitsW;
            float x = digitsLeft - gapScale * h - iconW * 0.5f;

            Transform it = icon.transform;
            it.position = new Vector3(x, b.center.y, transform.position.z);
            it.rotation = Quaternion.identity;   // ตั้งตรงเสมอ
            Vector3 lossy = transform.lossyScale;
            float s = iconH / spriteH;
            it.localScale = new Vector3(
                s / Mathf.Max(0.0001f, Mathf.Abs(lossy.x)),
                s / Mathf.Max(0.0001f, Mathf.Abs(lossy.y)),
                1f);

            SetIconVisible(true);
        }
    }
}
