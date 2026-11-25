using UnityEngine;

namespace GI351.KitchenGame
{
    public class UiAudioManager : MonoBehaviour
    {
        public const string BgmPrefKey = "ui.bgm";
        public const string SfxPrefKey = "ui.sfx";

        public static UiAudioManager Instance { get; private set; }
        /// <summary>ความดังเริ่มต้นตอนเปิดเกมครั้งแรกในเครื่อง (ยังไม่เคยเลื่อนแถบเสียง)</summary>
        public const float DefaultVolume = 0.35f;

        public static float BgmVolume { get; private set; } = DefaultVolume;
        public static float SfxVolume { get; private set; } = DefaultVolume;

        [Header("Audio Source")]
        [SerializeField] private AudioSource sfxSource;

        [Header("UI Clips")]
        public AudioClip buttonClick;
        public AudioClip panelToggle;
        public AudioClip upgradeSuccess;
        [Tooltip("ใช้ร่วมกันได้ทั้ง: ซื้อหม้อใหม่ / ปลดโซนใหม่ / จ้างพนักงาน")]
        public AudioClip buyUnlockItem;
        public AudioClip cantAfford;
        public AudioClip cashCollect;
        public AudioClip thiefAlert;
        [Tooltip("เสียงคิดเงิน (เครื่องคิดเงิน) ใช้ตอนจ่ายซื้อ/อัปเกรด: หม้อ แผงอาหาร แคชเชียร์ ของในร้าน จ้างพนักงาน")]
        public AudioClip purchase;

        [Header("UI Volumes (ความดังของแต่ละเสียง 0-1)")]
        [Tooltip("ตั้งจากการวัดความดังของไฟล์ ให้เสียงที่ดังบ่อยเบากว่าเสียงรางวัล/เตือน แล้วคูณกับแถบ SFX อีกที")]
        [Range(0f, 1f)] public float buttonClickVolume = 0.43f;
        [Range(0f, 1f)] public float panelToggleVolume = 0.15f;
        [Range(0f, 1f)] public float upgradeSuccessVolume = 0.76f;
        [Range(0f, 1f)] public float buyUnlockItemVolume = 0.71f;
        [Tooltip("ซื้อลูกจ้าง (ใช้เสียงเดียวกับซื้อของ แต่เบากว่า)")]
        [Range(0f, 1f)] public float buyStaffVolume = 0.53f;
        [Range(0f, 1f)] public float cantAffordVolume = 0.15f;
        [Range(0f, 1f)] public float cashCollectVolume = 0.37f;
        [Tooltip("เสียงคิดเงินตอนซื้อ/อัปเกรด (ตั้งให้ดังพอๆ กับเสียงอัปเกรดเดิม)")]
        [Range(0f, 1f)] public float purchaseVolume = 0.37f;
        [Range(0f, 1f)] public float thiefAlertVolume = 0.26f;   // 0.66 -> 0.4 (-40%) -> 0.26 (-35%)

        [Header("BGM (เพลงพื้นหลัง เล่นวนตลอด ทั้งหน้าเมนูและในเกม)")]
        [Tooltip("เพลงพื้นหลัง (ว่าง = ไม่มีเพลง)")]
        public AudioClip bgmClip;

        [Tooltip("ความดังของเพลงเทียบกับแถบ BGM (เพลงดังกว่าเสียงเอฟเฟกต์มาก จึงลดไว้ให้อยู่ใต้เสียงเกม) ความดังจริง = ค่านี้ x แถบ BGM")]
        [Range(0f, 1f)] public float bgmClipVolume = 0.18f;

        private AudioSource musicSource;

        [Header("Pitch Settings")]
        [SerializeField] private float pitchVariance = 0.06f;
        private float coinComboPitch = 1f;
        private float lastCoinTime;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
            }
            sfxSource.playOnAwake = false;
            DontDestroyOnLoad(gameObject);

            BgmVolume = PlayerPrefs.GetFloat(BgmPrefKey, DefaultVolume);
            SfxVolume = PlayerPrefs.GetFloat(SfxPrefKey, DefaultVolume);
            sfxSource.volume = SfxVolume;
            StartMusic();
        }

        /// <summary>เริ่มเพลงพื้นหลังแบบวนลูป (ตัวนี้อยู่ข้ามฉาก เพลงจึงเล่นต่อเนื่องตอนเปลี่ยนจากเมนูเข้าเกม)</summary>
        private void StartMusic()
        {
            if (bgmClip == null) return;
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
                musicSource.spatialBlend = 0f;
                musicSource.priority = 0;   // เพลงไม่ถูกตัดตอนเสียงเอฟเฟกต์เยอะ
            }
            musicSource.clip = bgmClip;
            ApplyMusicVolume();
            if (!musicSource.isPlaying) musicSource.Play();
        }

        private void ApplyMusicVolume()
        {
            if (musicSource != null) musicSource.volume = BgmVolume * bgmClipVolume;
        }

        public static void SetBgmVolume(float value)
        {
            BgmVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(BgmPrefKey, BgmVolume);
            if (Instance != null) Instance.ApplyMusicVolume();
        }

        public static void SetSfxVolume(float value)
        {
            SfxVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(SfxPrefKey, SfxVolume);
            if (Instance != null && Instance.sfxSource != null)
                Instance.sfxSource.volume = SfxVolume;
        }

        public void PlayClick() => PlayClip(buttonClick, 1f, true, buttonClickVolume);
        public void PlayPanelToggle() => PlayClip(panelToggle, 1f, false, panelToggleVolume);
        public void PlayUpgrade() => PlayClip(upgradeSuccess, 1f, true, upgradeSuccessVolume);
        public void PlayBuyOrUnlockZone() => PlayClip(buyUnlockItem, 1f, false, buyUnlockItemVolume);
        public void PlayBuyStaff() => PlayClip(buyUnlockItem, 1f, false, buyStaffVolume);
        public void PlayCantAfford() => PlayClip(cantAfford, 0.95f, false, cantAffordVolume);
        /// <summary>เสียงคิดเงินตอนจ่ายซื้อ/อัปเกรด (ไม่มีไฟล์ = ใช้เสียงอัปเกรดเดิม)</summary>
        public void PlayPurchase()
        {
            if (purchase != null) PlayClip(purchase, 1f, true, purchaseVolume);
            else PlayUpgrade();
        }

        // เสียงเตือนโจรยาว ~5 วินาที: เล่นใน AudioSource แยก จะได้หยุดได้ทันทีตอนตีโจรโดน
        private AudioSource alertSource;

        public void PlayThiefAlert()
        {
            if (thiefAlert == null) return;
            if (alertSource == null)
            {
                alertSource = gameObject.AddComponent<AudioSource>();
                alertSource.playOnAwake = false;
                alertSource.loop = false;
                alertSource.spatialBlend = 0f;
            }
            alertSource.clip = thiefAlert;
            alertSource.volume = SfxVolume * thiefAlertVolume;
            alertSource.pitch = 1f;
            alertSource.Play();
        }

        /// <summary>หยุดเสียงเตือนโจร (ตีโจรโดนแล้ว / ไม่มีโจรถือของขโมยเหลือ)</summary>
        public void StopThiefAlert()
        {
            if (alertSource != null && alertSource.isPlaying) alertSource.Stop();
        }

        public void PlayCashCollect()
        {
            if (Time.unscaledTime - lastCoinTime < 0.45f)
                coinComboPitch = Mathf.Min(1.45f, coinComboPitch + 0.05f);
            else
                coinComboPitch = 1f;

            lastCoinTime = Time.unscaledTime;

            // ห้ามเสียงเหรียญซ้อนกัน: เสียงก่อนหน้ายังไม่จบ = ข้าม (เสียงถัดไปยังไล่โน้ตสูงขึ้นตามคอมโบ)
            if (cashCollect == null || Time.unscaledTime < coinSoundEndTime) return;
            coinSoundEndTime = Time.unscaledTime + cashCollect.length / Mathf.Max(0.1f, coinComboPitch);
            PlayClip(cashCollect, coinComboPitch, false, cashCollectVolume);
        }

        private float coinSoundEndTime;

        private void PlayClip(AudioClip clip, float basePitch, bool randomizePitch, float volume = 1f)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = randomizePitch
                ? basePitch + Random.Range(-pitchVariance, pitchVariance)
                : basePitch;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}