using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ซ่อน UI ชั่วคราวตอนกล้องดึงไปโชว์จุดอื่น (CameraPan: เปิดโซนใหม่ / สอนเล่น) แล้วค่อยๆ โผล่กลับมาเมื่อกล้องกลับถึงผู้เล่น
    /// ใช้ CanvasGroup (จางหาย + กดไม่ได้) ไม่ปิด GameObject สคริปต์ของ UI จึงยังทำงานต่อ
    /// ใส่ให้: HUD (เงิน + Inventory), แถบเตือนโจร, แผงอัปเกรดหม้อ/แผงอาหาร/แคชเชียร์, แผงร้านค้า  (เมนูหยุดเกมไม่ซ่อน)
    /// </summary>
    [DisallowMultipleComponent]
    public class HideDuringCameraPan : MonoBehaviour
    {
        [Tooltip("ความเร็วจาง (ต่อวินาที) 8 = หาย/โผล่ในราว 0.12 วินาที")]
        [Min(0.1f)] [SerializeField] private float fadeSpeed = 8f;

        private CanvasGroup group;
        private float visible = 1f;

        /// <summary>ใส่ให้ GameObject นี้ (ใส่ซ้ำได้ ไม่เพิ่มซ้ำ)</summary>
        public static HideDuringCameraPan Attach(GameObject go)
        {
            if (go == null) return null;
            if (!go.TryGetComponent(out HideDuringCameraPan h)) h = go.AddComponent<HideDuringCameraPan>();
            return h;
        }

        private void Awake()
        {
            if (!TryGetComponent(out group)) group = gameObject.AddComponent<CanvasGroup>();
        }

        private void LateUpdate()
        {
            float target = CameraPan.Busy ? 0f : 1f;
            if (Mathf.Approximately(visible, target) && Mathf.Approximately(group.alpha, target)) return;

            visible = Mathf.MoveTowards(visible, target, fadeSpeed * Time.unscaledDeltaTime);
            group.alpha = visible;
            bool usable = visible > 0.99f;
            group.interactable = usable;
            group.blocksRaycasts = usable;
        }
    }
}
