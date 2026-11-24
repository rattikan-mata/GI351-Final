using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// หางตอนวิ่ง ตามเลเวลรองเท้า (Boots) ที่ซื้อจากร้าน: แต่ละเลเวลคนละสี เลเวลแรก (ยังไม่อัป) ไม่มีหาง
    /// HuntUpgrades เรียก SetLevel ทุกครั้งที่ซื้อรองเท้า / ตอนเริ่มเกม
    /// หางจะลากเฉพาะตอนกำลังเคลื่อนที่เร็วกว่า minSpeed หยุดเดินแล้วหางค่อยๆ จางหาย
    /// </summary>
    public class RunTrail : MonoBehaviour
    {
        [Header("Colors per Level (สีหางแต่ละเลเวล)")]
        [Tooltip("ช่องที่ 0 = เลเวล 1, ช่องที่ 1 = เลเวล 2 ... สีที่ alpha = 0 คือไม่มีหาง (เลเวลเกินจำนวนช่องใช้สีช่องสุดท้าย)")]
        [SerializeField] private Color[] levelColors =
        {
            new Color(1f, 1f, 1f, 0f),          // Lv1 ยังไม่อัป: ไม่มีหาง
            new Color(0.45f, 1f, 0.5f, 0.85f),  // Lv2 เขียว
            new Color(0.35f, 0.75f, 1f, 0.85f), // Lv3 ฟ้า
            new Color(0.8f, 0.45f, 1f, 0.9f),   // Lv4 ม่วง
            new Color(1f, 0.82f, 0.25f, 0.95f)  // Lv5 ทอง
        };

        [Header("Shape (รูปหาง)")]
        [Tooltip("ความยาวหาง (วินาทีที่หางค้างอยู่)")]
        [SerializeField] private float trailTime = 0.22f;

        [Tooltip("ความกว้างหางที่โคน (หน่วยในโลก)")]
        [SerializeField] private float startWidth = 0.45f;

        [Tooltip("เลเวลสูงขึ้นหางกว้างขึ้นอีกเท่านี้ต่อเลเวล")]
        [SerializeField] private float widthPerLevel = 0.05f;

        [Tooltip("ตำแหน่งที่หางออก เทียบกับกลางตัว (หน่วยในโลก) y ติดลบ = ต่ำลงไปทางเท้า")]
        [SerializeField] private Vector2 offset = new Vector2(0f, -0.6f);

        [Tooltip("ต้องวิ่งเร็วกว่านี้ (หน่วย/วินาที) ถึงจะมีหาง")]
        [SerializeField] private float minSpeed = 1f;

        [Header("Rendering")]
        [Tooltip("Material ของหาง (ปล่อยว่างจะสร้างจาก Sprite-Unlit-Default ให้)")]
        [SerializeField] private Material trailMaterial;

        [Tooltip("ลำดับการวาดเทียบกับตัวผู้เล่น (ลบ = อยู่หลังตัว)")]
        [SerializeField] private int sortingOrderOffset = -1;

        private TrailRenderer trail;
        private SpriteRenderer playerRenderer;
        private int level = 1;
        private Vector3 lastPos;
        private bool hasColor;

        public int Level => level;

        private void Awake()
        {
            playerRenderer = GetComponent<SpriteRenderer>();
            Build();
            lastPos = transform.position;
            SetLevel(level);
        }

        private void Build()
        {
            var go = new GameObject("Run Trail");
            go.transform.SetParent(transform, false);
            trail = go.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.minVertexDistance = 0.05f;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.emitting = false;

            if (trailMaterial == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (s == null) s = Shader.Find("Sprites/Default");
                if (s != null) trailMaterial = new Material(s) { name = "Run Trail (runtime)" };
            }
            trail.sharedMaterial = trailMaterial;

            if (playerRenderer != null)
            {
                trail.sortingLayerID = playerRenderer.sortingLayerID;
                trail.sortingOrder = playerRenderer.sortingOrder + sortingOrderOffset;
            }
        }

        /// <summary>ตั้งเลเวลรองเท้า (1 = ยังไม่อัป) เปลี่ยนสี/ความกว้างหาง</summary>
        public void SetLevel(int newLevel)
        {
            level = Mathf.Max(1, newLevel);
            if (trail == null || levelColors == null || levelColors.Length == 0) return;

            Color c = levelColors[Mathf.Clamp(level - 1, 0, levelColors.Length - 1)];
            hasColor = c.a > 0.001f;

            var g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(c.a * 0.5f, 0.4f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = g;

            float w = startWidth + widthPerLevel * Mathf.Max(0, level - 2);
            trail.widthCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0f));
            trail.widthMultiplier = w;
            trail.time = trailTime;

            if (!hasColor)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        private void LateUpdate()
        {
            if (trail == null) return;

            trail.transform.position = transform.position + (Vector3)offset;

            float dt = Time.deltaTime;
            float moved = (transform.position - lastPos).magnitude;
            float speed = dt > 0f ? moved / dt : 0f;
            lastPos = transform.position;

            // วาร์ป (เกิดใหม่ / ย้ายตำแหน่ง) ไม่ให้หางลากยาวข้ามแมพ
            if (moved > 2f) trail.Clear();

            trail.emitting = hasColor && speed >= minSpeed;
        }

        private void OnDisable()
        {
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }
    }
}
