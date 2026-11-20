using UnityEngine;
using UnityEngine.Rendering;

namespace GI351.KitchenGame
{
    /// <summary>
    /// กล้องสั่นแบบสุ่ม offset แล้วลดลงจนหยุด ปัดเป็นพิกเซลเต็มเสมอ นับเวลาแบบ unscaled (สั่นต่อแม้ช่วง hit stop)
    /// ไม่แย่งตำแหน่งกล้อง: ใส่ offset เฉพาะตอนเรนเดอร์ (beginCameraRendering) แล้วถอนคืนทันที (endCameraRendering)
    /// ระบบกล้องตามผู้เล่น (กล้องเป็นลูกของ Player) / การแพนกล้อง (AreaGate) / การเล็งเมาส์ จึงไม่เห็นตำแหน่งที่สั่นเลย
    /// ถูกเพิ่มบนกล้องหลักอัตโนมัติโดย JuiceDirector.Shake (ใช้กับ URP)
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class CameraShake : MonoBehaviour
    {
        private Camera cam;
        private bool active;
        private float duration;
        private float elapsed;
        private float magnitudePixels;
        private float pixelsPerUnit = 16f;

        private Vector3 offset;
        private bool applied;
        private Vector3 savedLocalPosition;

        /// <summary>offset ที่จะใส่ตอนเรนเดอร์เฟรมนี้ (หน่วยโลก) ศูนย์ = ไม่สั่น</summary>
        public Vector3 CurrentOffset => offset;
        public bool IsShaking => active;

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void OnEnable()
        {
            if (cam == null) cam = GetComponent<Camera>();
            RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        private void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
            RestoreIfApplied();
            active = false;
            offset = Vector3.zero;
        }

        /// <summary>
        /// เริ่มสั่น ถ้าตอนนี้กำลังสั่นแรงกว่าอยู่ จะไม่ถูกทับด้วยอันที่เบากว่า (ไม่บวกสะสมกัน)
        /// </summary>
        public void Shake(float pixels, float seconds, float ppu)
        {
            if (pixels <= 0f || seconds <= 0f) return;

            if (active)
            {
                float currentStrength = magnitudePixels * (1f - Mathf.Clamp01(elapsed / duration));
                if (pixels < currentStrength) return;
            }

            magnitudePixels = pixels;
            duration = seconds;
            elapsed = 0f;
            pixelsPerUnit = Mathf.Max(1f, ppu);
            active = true;
        }

        private void Update()
        {
            if (!active)
            {
                offset = Vector3.zero;
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            if (elapsed >= duration)
            {
                active = false;
                offset = Vector3.zero;
                return;
            }

            // ลดความแรงลงเรื่อยๆ ตามเวลา สุ่มทิศใหม่ทุกเฟรม แล้วปัดเป็นพิกเซลเต็ม
            float strength = magnitudePixels * (1f - elapsed / duration);
            Vector2 dir = Random.insideUnitCircle;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;

            float px = Mathf.Round(dir.x * strength);
            float py = Mathf.Round(dir.y * strength);
            offset = new Vector3(px / pixelsPerUnit, py / pixelsPerUnit, 0f);
        }

        private void OnBeginCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam || applied || offset == Vector3.zero) return;

            Transform t = cam.transform;
            savedLocalPosition = t.localPosition;
            t.position += offset;
            applied = true;
        }

        private void OnEndCameraRendering(ScriptableRenderContext context, Camera rendering)
        {
            if (rendering != cam) return;
            RestoreIfApplied();
        }

        private void RestoreIfApplied()
        {
            if (!applied) return;
            applied = false;
            if (cam != null) cam.transform.localPosition = savedLocalPosition;
        }
    }
}
