#if UNITY_EDITOR || DEVELOPMENT_BUILD || GAME_CHEATS
using UnityEngine;
using UnityEngine.InputSystem;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวโกงเงินไว้ทดสอบ (มีเฉพาะใน Editor, Development Build และบิ้ลด์ที่ใส่ define GAME_CHEATS ตอนบิ้ลด์ เกมที่ build ปกติจะไม่มีสคริปต์นี้)
    /// สร้างตัวเองอัตโนมัติตอนเริ่มเกม ไม่ต้องวางในฉาก
    ///   F1 = +100   F2 = +1,000   F3 = +10,000   F4 = เงินเป็น 0
    /// ปิดได้ที่ MoneyCheat.Enabled = false หรือลบไฟล์นี้ทิ้ง
    /// </summary>
    public class MoneyCheat : MonoBehaviour
    {
        public static bool Enabled = true;

        private const float ToastTime = 1.2f;

        private string toast;
        private float toastTimer;
        private GUIStyle style;

        private static MoneyCheat instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null) return;
            var go = new GameObject("[Money Cheat]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<MoneyCheat>();
        }

        private void Update()
        {
            if (!Enabled) return;
            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.f1Key.wasPressedThisFrame) Add(100);
            else if (kb.f2Key.wasPressedThisFrame) Add(1000);
            else if (kb.f3Key.wasPressedThisFrame) Add(10000);
            else if (kb.f4Key.wasPressedThisFrame) ResetMoney();

            if (toastTimer > 0f) toastTimer -= Time.unscaledDeltaTime;
        }

        private void Add(int amount)
        {
            MoneyManager mm = MoneyManager.Instance;
            if (mm == null)
            {
                Show("CHEAT: no MoneyManager in scene");
                return;
            }
            mm.AddMoney(amount);
            Show("CHEAT +" + amount.ToString("N0") + "  (" + mm.CurrentMoney.ToString("N0") + ")");
        }

        private void ResetMoney()
        {
            MoneyManager mm = MoneyManager.Instance;
            if (mm == null) return;
            mm.SetMoney(0);
            Show("CHEAT money = 0");
        }

        private void Show(string msg)
        {
            toast = msg;
            toastTimer = ToastTime;
        }

        private void OnGUI()
        {
            if (!Enabled || toastTimer <= 0f || string.IsNullOrEmpty(toast)) return;

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            }

            float a = Mathf.Clamp01(toastTimer / 0.3f);
            var rect = new Rect(0f, 12f, Screen.width, 32f);
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * a);
            GUI.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), toast, style);
            style.normal.textColor = new Color(1f, 0.85f, 0.25f, a);
            GUI.Label(rect, toast, style);
        }
    }
}
#endif
