using UnityEngine;

namespace GI351.KitchenGame
{
    public class UiAudioManager : MonoBehaviour
    {
        public static UiAudioManager Instance { get; private set; }

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
                sfxSource.playOnAwake = false;
            }
        }

        public void PlayClick() => PlayClip(buttonClick, 1f, true);
        public void PlayPanelToggle() => PlayClip(panelToggle, 1f, false);
        public void PlayUpgrade() => PlayClip(upgradeSuccess, 1f, true);
        public void PlayBuyOrUnlockZone() => PlayClip(buyUnlockItem, 1f, false);
        public void PlayCantAfford() => PlayClip(cantAfford, 0.95f, false);
        public void PlayThiefAlert() => PlayClip(thiefAlert, 1f, false);

        public void PlayCashCollect()
        {
            if (Time.unscaledTime - lastCoinTime < 0.45f)
                coinComboPitch = Mathf.Min(1.45f, coinComboPitch + 0.05f);
            else
                coinComboPitch = 1f;

            lastCoinTime = Time.unscaledTime;
            PlayClip(cashCollect, coinComboPitch, false);
        }

        private void PlayClip(AudioClip clip, float basePitch, bool randomizePitch)
        {
            if (clip == null || sfxSource == null) return;
            sfxSource.pitch = randomizePitch
                ? basePitch + Random.Range(-pitchVariance, pitchVariance)
                : basePitch;
            sfxSource.PlayOneShot(clip);
        }
    }
}