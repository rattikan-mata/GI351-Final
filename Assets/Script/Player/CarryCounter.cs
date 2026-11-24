using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวเลขจำนวนของในกระเป๋าเหนือหัวผู้เล่นแบบ Juice King เช่น "1/5"
    /// ฟังจาก PlayerCarry.onCarryCountChanged: ทุกครั้งที่เลขเปลี่ยนจะเด้งขยาย, เต็มแล้วเปลี่ยนเป็นสีแดง
    /// world-space ใช้ TextMesh (แบบเดียวกับป้ายเหนือหัวลูกค้า) มีขอบดำ 1 พิกเซลให้อ่านง่ายบนพื้นหญ้า
    /// </summary>
    [RequireComponent(typeof(PlayerCarry))]
    public class CarryCounter : MonoBehaviour
    {
        [Header("Position (ตำแหน่ง)")]
        [Tooltip("ความสูงเหนือกลางตัวผู้เล่น (หน่วยในโลก)")]
        [SerializeField] private float heightOffset = 1.85f;

        [Header("Look (หน้าตา)")]
        [Tooltip("ขนาดตัวอักษร (หน่วยในโลก ต่อ 1 บรรทัดโดยประมาณ)")]
        [SerializeField] private float textHeight = 0.36f;

        [SerializeField] private Color normalColor = Color.white;

        [Tooltip("สีตอนกระเป๋าเต็ม")]
        [SerializeField] private Color fullColor = new Color(1f, 0.35f, 0.3f, 1f);

        [Tooltip("สีขอบตัวอักษร")]
        [SerializeField] private Color outlineColor = new Color(0.1f, 0.07f, 0.06f, 1f);

        [Tooltip("ความหนาขอบ (หน่วยในโลก)")]
        [SerializeField] private float outlineThickness = 0.025f;

        [Tooltip("ลำดับการวาด (ให้อยู่บนสุด)")]
        [SerializeField] private int sortingOrder = 60;

        [Header("Behaviour (พฤติกรรม)")]
        [Tooltip("ซ่อนตัวเลขตอนกระเป๋าว่าง")]
        [SerializeField] private bool hideWhenEmpty = false;

        [Tooltip("ตอนเลขเปลี่ยน เด้งขยายเป็นกี่เท่า")]
        [SerializeField] private float popScale = 1.35f;

        [Tooltip("เวลาเด้ง (วินาที)")]
        [SerializeField] private float popTime = 0.2f;

        [Tooltip("ตอนเต็ม ส่ายซ้ายขวากี่พิกเซล (0 = ไม่ส่าย)")]
        [SerializeField] private float fullShakePixels = 1f;

        private PlayerCarry carry;
        private Transform root;
        private TextMesh mainText;
        private TextMesh[] outlineTexts;
        private float popTimer = -1f;
        private int lastCount = -1;

        private static readonly Vector2[] OutlineDirs =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1)
        };

        private void Awake()
        {
            carry = GetComponent<PlayerCarry>();
            Build();
        }

        private void OnEnable()
        {
            if (carry == null) return;
            if (carry.onCarryCountChanged == null) carry.onCarryCountChanged = new UnityEngine.Events.UnityEvent<int, int>();
            carry.onCarryCountChanged.AddListener(OnCountChanged);
        }

        private void OnDisable()
        {
            if (carry != null && carry.onCarryCountChanged != null) carry.onCarryCountChanged.RemoveListener(OnCountChanged);
        }

        private void Start()
        {
            Refresh(carry.CurrentCarriedCount, carry.MaxCarryCapacity, false);
        }

        private void Build()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var rootObj = new GameObject("Carry Counter");
            root = rootObj.transform;
            root.SetParent(transform, false);
            YSort.PutOnTop(rootObj);   // ตัวเลขอยู่บนสุดเสมอ ไม่โดนโต๊ะ/ร้านบัง

            outlineTexts = new TextMesh[OutlineDirs.Length];
            for (int i = 0; i < OutlineDirs.Length; i++)
            {
                outlineTexts[i] = CreateText("Outline", font, outlineColor, sortingOrder);
                outlineTexts[i].transform.localPosition = (Vector3)(OutlineDirs[i] * outlineThickness);
            }
            mainText = CreateText("Text", font, normalColor, sortingOrder + 1);
        }

        private TextMesh CreateText(string name, Font font, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var tm = go.AddComponent<TextMesh>();
            tm.fontSize = 64;
            tm.characterSize = textHeight / 6.4f; // fontSize 64 -> สูงราว textHeight
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = color;
            if (font != null)
            {
                tm.font = font;
                var mr = go.GetComponent<MeshRenderer>();
                mr.sharedMaterial = font.material;
                mr.sortingOrder = order;
            }
            return tm;
        }

        private void OnCountChanged(int count, int max)
        {
            Refresh(count, max, true);
        }

        private void Refresh(int count, int max, bool pop)
        {
            lastCount = count;
            string s = count + "/" + max;
            bool full = count >= max;

            mainText.text = s;
            mainText.color = full ? fullColor : normalColor;
            foreach (var o in outlineTexts) o.text = s;

            root.gameObject.SetActive(!(hideWhenEmpty && count <= 0));
            if (pop && root.gameObject.activeSelf) popTimer = 0f;
        }

        private void LateUpdate()
        {
            if (root == null || !root.gameObject.activeSelf) return;

            // ตัวเลขไม่ย่อ/กลับด้านตามสเกลของตัวผู้เล่น
            Vector3 parentScale = transform.lossyScale;
            float inv = Mathf.Abs(parentScale.x) > 0.0001f ? 1f / Mathf.Abs(parentScale.x) : 1f;

            float s = 1f;
            float shakeX = 0f;
            if (popTimer >= 0f)
            {
                popTimer += Time.deltaTime;
                float k = popTime > 0f ? Mathf.Clamp01(popTimer / popTime) : 1f;
                s = Mathf.Lerp(popScale, 1f, Tween.Evaluate(Ease.OutBack, k));
                if (lastCount >= carry.MaxCarryCapacity && fullShakePixels > 0f)
                {
                    shakeX = Mathf.Sin(k * Mathf.PI * 4f) * (1f - k) * fullShakePixels / 16f;
                }
                if (k >= 1f) popTimer = -1f;
            }

            root.localScale = Vector3.one * (s * inv);
            root.position = transform.position + new Vector3(shakeX, heightOffset, -0.1f);
        }
    }
}
