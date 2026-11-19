using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// หลอดเลือดเล็กลอยเหนือหัวสัตว์ (world-space ใช้ SpriteRenderer 2 ชิ้น ไม่ใช้ Canvas / TMP)
    /// ซ่อนตอนเลือดเต็ม โผล่เมื่อโดนตีครั้งแรก อัปเดตจาก EnemyHealth.onHealthChanged และหายไปตอนตาย
    /// ตั้งค่าความสูง / ขนาด / สี ได้ที่ Prefab ของสัตว์แต่ละตัว (Chicken / Pig)
    /// </summary>
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyHpBar : MonoBehaviour
    {
        [Header("Position & Size (ตำแหน่งและขนาด)")]
        [Tooltip("ความสูงของหลอดเหนือจุดกึ่งกลางตัวสัตว์ (เมตร)")]
        [SerializeField] private float heightOffset = 0.85f;

        [Tooltip("ขนาดหลอดทั้งอัน กว้าง x สูง (เมตร)")]
        [SerializeField] private Vector2 barSize = new Vector2(0.9f, 0.14f);

        [Tooltip("ความหนาขอบหลอด (เมตร) ส่วนที่เหลือจากขอบคือหลอดเลือดจริง")]
        [SerializeField] private float borderThickness = 0.03f;

        [Header("Colors (สี)")]
        [Tooltip("สีพื้นหลัง/ขอบหลอด")]
        [SerializeField] private Color backColor = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        [Tooltip("สีหลอดเลือดปกติ")]
        [SerializeField] private Color fillColor = new Color(0.35f, 0.85f, 0.3f, 1f);

        [Tooltip("สีหลอดเมื่อเลือดเหลือน้อย")]
        [SerializeField] private Color lowHealthColor = new Color(0.9f, 0.25f, 0.2f, 1f);

        [Tooltip("เลือดเหลือไม่เกินสัดส่วนนี้ (0-1) จะเปลี่ยนเป็นสีเลือดน้อย")]
        [Range(0f, 1f)]
        [SerializeField] private float lowHealthThreshold = 0.35f;

        [Header("Rendering")]
        [Tooltip("ลำดับการวาด ต้องมากกว่าตัวสัตว์ หลอดจะได้ไม่โดนบัง")]
        [SerializeField] private int sortingOrder = 50;

        [Header("Behaviour (พฤติกรรม)")]
        [Tooltip("เปิด = ซ่อนหลอดตอนเลือดเต็ม (โผล่เมื่อโดนตี) / ปิด = โชว์ตลอด")]
        [SerializeField] private bool hideWhenFull = true;

        private EnemyHealth health;
        private GameObject barRoot;
        private SpriteRenderer backRenderer;
        private SpriteRenderer fillRenderer;
        private float fillMaxWidth;

        private static Sprite leftPivotPixel;

        public bool IsVisible => barRoot != null && barRoot.activeSelf;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            BuildBar();

            if (health != null)
            {
                health.onHealthChanged.AddListener(OnHealthChanged);
                health.onDied.AddListener(OnDied);
            }
        }

        private void Start()
        {
            // รอให้ EnemyHealth.Awake ทำงานเสร็จก่อนค่อยอ่านเลือดเริ่มต้น
            if (health != null) Apply(health.CurrentHealth, health.MaxHealth);
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.onHealthChanged.RemoveListener(OnHealthChanged);
                health.onDied.RemoveListener(OnDied);
            }
        }

        private void OnHealthChanged(int current, int max)
        {
            Apply(current, max);
        }

        private void OnDied()
        {
            if (barRoot != null) barRoot.SetActive(false);
        }

        private void Apply(int current, int max)
        {
            if (barRoot == null) return;

            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            bool show = current > 0 && (!hideWhenFull || current < max);
            barRoot.SetActive(show);
            if (!show) return;

            Vector3 s = fillRenderer.transform.localScale;
            s.x = fillMaxWidth * ratio;
            fillRenderer.transform.localScale = s;
            fillRenderer.color = ratio <= lowHealthThreshold ? lowHealthColor : fillColor;
        }

        private void BuildBar()
        {
            Sprite pixel = GetPixelSprite();

            barRoot = new GameObject("HP Bar");
            barRoot.transform.SetParent(transform, false);
            barRoot.transform.localPosition = new Vector3(0f, heightOffset, 0f);

            // ใช้ Sorting Layer เดียวกับตัวสัตว์ เปลี่ยนแค่ลำดับ
            SpriteRenderer body = GetComponentInChildren<SpriteRenderer>();
            int layerId = body != null ? body.sortingLayerID : 0;

            float half = barSize.x * 0.5f;
            float border = Mathf.Clamp(borderThickness, 0f, Mathf.Min(barSize.x, barSize.y) * 0.45f);
            fillMaxWidth = Mathf.Max(0.01f, barSize.x - border * 2f);

            backRenderer = CreatePart("Back", pixel, backColor, layerId, sortingOrder);
            backRenderer.transform.localPosition = new Vector3(-half, 0f, 0f);
            backRenderer.transform.localScale = new Vector3(barSize.x, barSize.y, 1f);

            fillRenderer = CreatePart("Fill", pixel, fillColor, layerId, sortingOrder + 1);
            fillRenderer.transform.localPosition = new Vector3(-half + border, 0f, 0f);
            fillRenderer.transform.localScale = new Vector3(fillMaxWidth, Mathf.Max(0.01f, barSize.y - border * 2f), 1f);

            barRoot.SetActive(false);
        }

        private SpriteRenderer CreatePart(string partName, Sprite sprite, Color color, int layerId, int order)
        {
            GameObject go = new GameObject(partName);
            go.transform.SetParent(barRoot.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingLayerID = layerId;
            sr.sortingOrder = order;
            return sr;
        }

        /// <summary>สปไรต์สี่เหลี่ยม 1x1 เมตร จุดหมุนอยู่ซ้ายกลาง (ยืดแกน X ได้เป็นหลอด) ใช้ร่วมกันทุกตัว</summary>
        private static Sprite GetPixelSprite()
        {
            if (leftPivotPixel != null) return leftPivotPixel;

            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            tex.hideFlags = HideFlags.HideAndDontSave;

            leftPivotPixel = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0f, 0.5f), 1f);
            leftPivotPixel.hideFlags = HideFlags.HideAndDontSave;
            return leftPivotPixel;
        }

        private void OnDrawGizmosSelected()
        {
            // แสดงตำแหน่งและขนาดหลอดตอนเลือกสัตว์ใน Scene / Prefab
            Vector3 c = transform.position + Vector3.up * heightOffset;
            Gizmos.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0.9f);
            Gizmos.DrawWireCube(c, new Vector3(barSize.x, barSize.y, 0.05f));
        }
    }
}
