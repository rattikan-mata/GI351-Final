using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ทางเข้าเดียวของ juice ตอนตี/ฆ่าสัตว์: hit stop และ camera shake (ตัวที่ต้องอยู่รอดแม้สัตว์ถูก destroy)
    /// สร้างตัวถือ (JuiceRunner) กับ CameraShake บนกล้องหลักให้อัตโนมัติ ไม่ต้องวางอะไรในซีน
    /// </summary>
    public static class JuiceDirector
    {
        private static JuiceRunner runner;
        private static JuiceConfig defaultConfig;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            runner = null;
            defaultConfig = null;
            particles = null;
        }

        /// <summary>ค่าเริ่มต้น (ตามค่าในโค้ดของ JuiceConfig) ใช้เมื่อสัตว์ไม่ได้ผูก asset ไว้</summary>
        public static JuiceConfig DefaultConfig
        {
            get
            {
                if (defaultConfig == null)
                {
                    defaultConfig = ScriptableObject.CreateInstance<JuiceConfig>();
                    defaultConfig.name = "JuiceConfig (default)";
                }
                return defaultConfig;
            }
        }

        public static JuiceConfig Resolve(JuiceConfig assigned)
        {
            return assigned != null ? assigned : DefaultConfig;
        }

        /// <summary>กำลังหยุดเกมจาก hit stop อยู่หรือไม่</summary>
        public static bool IsHitStopActive => runner != null && runner.IsHolding;

        /// <summary>ยกเลิก hit stop ที่ค้างอยู่ (ไม่คืน timeScale) ใช้ตอนเมนู pause จะตั้ง timeScale เอง</summary>
        public static void CancelHitStop()
        {
            if (runner != null) runner.Cancel();
        }

        /// <summary>หยุดเกม seconds วินาทีตามเวลาจริง (ซ้อนกันใช้ค่าที่นานกว่า ไม่บวกสะสม)</summary>
        public static void HitStop(float seconds)
        {
            if (!Application.isPlaying || seconds <= 0f) return;
            JuiceRunner r = GetRunner();
            if (r != null) r.RequestHitStop(seconds);
        }

        /// <summary>สั่นกล้องหลัก pixels พิกเซล (ตามขนาดพิกเซลของเกม) นาน seconds วินาที (เวลาจริง)</summary>
        public static void Shake(float pixels, float seconds, float pixelsPerUnit)
        {
            if (!Application.isPlaying || pixels <= 0f || seconds <= 0f) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            if (!cam.TryGetComponent(out CameraShake shake))
            {
                shake = cam.gameObject.AddComponent<CameraShake>();
            }
            shake.Shake(pixels, seconds, pixelsPerUnit);
        }

        /// <summary>
        /// เศษกระเด็นตอนตีโดน/ฆ่า ที่ตำแหน่ง position (ตอนตีพุ่งไปทาง awayFrom->position, ตอนฆ่าพุ่งรอบตัว + ควัน)
        /// colors = สีเม็ด (ว่าง = ขาว) sorting ใช้ของ sprite สัตว์แล้ววางเม็ดไว้ด้านหน้า
        /// </summary>
        public static void Particles(JuiceConfig cfg, Vector2 position, Vector2 awayFrom, bool kill, Color[] colors,
            int sortingLayerId, int sortingOrder)
        {
            if (!Application.isPlaying || cfg == null || !cfg.particlesEnabled) return;
            JuiceParticles fx = GetParticles();

            Vector2Int range = kill ? cfg.killParticleCount : cfg.hitParticleCount;
            int count = Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);

            fx.Burst(cfg, new JuiceParticles.BurstSettings
            {
                position = position,
                direction = kill ? Vector2.zero : (position - awayFrom).normalized,
                spread = kill ? 360f : cfg.hitParticleSpread,
                count = count,
                extraPixels = kill ? cfg.killParticleExtraPixels : 0,
                colors = colors,
                sortingLayerId = sortingLayerId,
                sortingOrder = sortingOrder + 6
            });

            if (kill && cfg.killSmokeEnabled)
            {
                Vector2Int sc = cfg.killSmokeCount;
                int smoke = Random.Range(Mathf.Min(sc.x, sc.y), Mathf.Max(sc.x, sc.y) + 1);
                fx.Smoke(cfg, position, smoke, sortingLayerId, sortingOrder + 5);
            }
        }

        private static JuiceParticles particles;

        private static JuiceParticles GetParticles()
        {
            if (particles != null) return particles;
            GameObject go = new GameObject("JuiceParticles");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            particles = go.AddComponent<JuiceParticles>();
            return particles;
        }

        internal static void ForgetRunner(JuiceRunner r)
        {
            if (runner == r) runner = null;
        }

        private static JuiceRunner GetRunner()
        {
            if (runner != null) return runner;

            GameObject go = new GameObject("JuiceRunner");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<JuiceRunner>();
            return runner;
        }
    }
}
