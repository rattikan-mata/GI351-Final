using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ซูมกล้องเบาๆ ตอนเปิดแผงอัปเกรด (หม้อ / แผงอาหาร / แคชเชียร์) แบบ Juice King:
    /// เดินเข้าวงแล้วแผงอัปเกรดเด้งขึ้นด้านล่างจอ กล้องค่อยๆ ซูมเข้า 5% ที่ตัวละคร (ไม่ดึงจอไปที่เครื่อง)
    /// เดินออกจากวง กล้องค่อยๆ กลับขนาดเดิม  (ติ๊ก Shift To Station = เลื่อนกล้องให้เครื่องอยู่ครึ่งบนของจอแบบเดิม)
    /// สร้างตัวเองบนกล้องหลักตอนมีเครื่องเรียก Focus ครั้งแรก ไม่ต้องวางในฉาก  ระหว่างกล้องดึงจอ (CameraPan) จะไม่ยุ่ง
    /// </summary>
    [DisallowMultipleComponent]
    public class StationCameraZoom : MonoBehaviour
    {
        [Tooltip("ขนาดกล้องตอนซูมเทียบกับปกติ (0.95 = ซูมเข้าเบาๆ 5% ที่ตัวละคร)")]
        [Range(0.4f, 1f)] [SerializeField] private float zoom = 0.95f;

        [Tooltip("ปิด = ซูมอยู่กับที่ที่ตัวละคร ไม่เลื่อนกล้อง / เปิด = เลื่อนกล้องให้เครื่องไปอยู่ครึ่งบนของจอ (ค่าข้างล่างใช้เฉพาะตอนเปิด)")]
        [SerializeField] private bool shiftToStation = false;

        [Tooltip("ให้เครื่องไปอยู่ตรงไหนของจอแนวตั้ง (0 = ล่าง, 0.5 = กลาง, 1 = บน) แผงอัปเกรดกินครึ่งล่างของจอ จึงดันเครื่องขึ้นไปครึ่งบน")]
        [Range(0.3f, 0.85f)] [SerializeField] private float focusViewportY = 0.66f;

        [Tooltip("จุดโฟกัสอยู่ระหว่างเครื่องกับผู้เล่น (0 = ตรงเครื่อง, 1 = ตรงผู้เล่น) ให้เห็นทั้งเครื่องและตัวละคร")]
        [Range(0f, 1f)] [SerializeField] private float playerWeight = 0.5f;

        [Tooltip("ความเร็วซูมเข้า/ออก (ต่อวินาที) 3 = ราว 0.33 วินาที")]
        [Min(0.1f)] [SerializeField] private float blendSpeed = 3f;

        private static StationCameraZoom instance;

        private Camera cam;
        private Object owner;
        private Transform focusTarget;
        private bool wanted;
        private float blend;
        private bool captured;
        private float baseSize;
        private Vector3 baseLocal;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        /// <summary>ขอให้กล้องซูมไปที่เครื่อง target (เครื่องที่เปิดแผงอัปเกรดอยู่)</summary>
        public static void Focus(Object requester, Transform target)
        {
            StationCameraZoom z = Ensure();
            if (z == null || target == null) return;
            z.owner = requester;
            z.focusTarget = target;
            z.wanted = true;
        }

        /// <summary>เลิกซูม (เฉพาะเครื่องที่ขอไว้ล่าสุด เดินจากเครื่องหนึ่งไปอีกเครื่องติดกันจะไม่กระตุก)</summary>
        public static void Release(Object requester)
        {
            if (instance != null && instance.owner == requester) instance.wanted = false;
        }

        private static StationCameraZoom Ensure()
        {
            if (instance != null) return instance;
            Camera main = Camera.main;
            if (main == null) return null;
            if (!main.TryGetComponent(out instance)) instance = main.gameObject.AddComponent<StationCameraZoom>();
            return instance;
        }

        private void Awake()
        {
            cam = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            // กล้องดึงจอไปโชว์จุดอื่นอยู่ (CameraPan คุมกล้องเอง) ไม่ยุ่ง แล้วเก็บค่าใหม่ตอนว่าง
            if (CameraPan.Busy) return;

            if (!captured)
            {
                if (!wanted) return;
                baseSize = cam.orthographicSize;
                baseLocal = transform.localPosition;
                captured = true;
                blend = 0f;
            }

            blend = Mathf.MoveTowards(blend, wanted ? 1f : 0f, blendSpeed * Time.unscaledDeltaTime);
            float e = blend * blend * (3f - 2f * blend);   // smoothstep: นุ่มตอนเริ่มและตอนถึง

            float size = baseSize * Mathf.Lerp(1f, zoom, e);
            cam.orthographicSize = size;

            Transform parent = transform.parent;
            if (shiftToStation && parent != null && focusTarget != null)
            {
                // โฟกัสกึ่งกลางระหว่างเครื่องกับผู้เล่น (ผู้เล่นยืนในวงข้างเครื่อง) ให้เห็นทั้งคู่
                Vector3 focus = Vector3.Lerp(focusTarget.position, parent.position, playerWeight);
                // ตำแหน่งกล้องที่ทำให้เครื่องไปอยู่ที่ focusViewportY ของจอ (คิดจากขนาดกล้องตอนซูมแล้ว)
                Vector3 desired = new Vector3(focus.x, focus.y - (focusViewportY - 0.5f) * 2f * size, 0f);
                Vector3 local = parent.InverseTransformPoint(desired);
                local.z = baseLocal.z;
                transform.localPosition = Vector3.Lerp(baseLocal, local, e);
            }

            // ซูมออกจนสุดแล้ว: คืนค่าเดิมเป๊ะ แล้วรอคำขอรอบใหม่
            if (!wanted && blend <= 0f)
            {
                cam.orthographicSize = baseSize;
                transform.localPosition = baseLocal;
                captured = false;
            }
        }
    }
}
