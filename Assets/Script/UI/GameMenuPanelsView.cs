using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    public class GameMenuPanelsView : MonoBehaviour
    {
        public enum MenuMode { MainMenu, PauseMenu }

        [Header("Mode")]
        public MenuMode mode = MenuMode.MainMenu;
        public string gameplaySceneName = "Map Layout";
        public string mainMenuSceneName = "MainMenu";

        [Header("Parts")]
        public GameObject panel;
        public Text titleText;
        public Button primaryButton;
        public Button settingsButton;
        public Button quitOrMenuButton;

        [Header("Settings Sub-Panel")]
        public GameObject settingsBox;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Button closeSettingsButton;

        private bool isPaused;

        private void Start()
        {
            if (primaryButton != null)
            {
                primaryButton.onClick.AddListener(() =>
                {
                    UiAudioManager.Instance?.PlayClick();
                    if (mode == MenuMode.MainMenu)
                        SceneManager.LoadScene(gameplaySceneName);
                    else
                        SetPaused(false);
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    UiAudioManager.Instance?.PlayClick();
                    if (settingsBox != null) settingsBox.SetActive(!settingsBox.activeSelf);
                });
            }

            if (closeSettingsButton != null)
            {
                closeSettingsButton.onClick.AddListener(() =>
                {
                    UiAudioManager.Instance?.PlayClick();
                    if (settingsBox != null) settingsBox.SetActive(false);
                });
            }

            if (quitOrMenuButton != null)
            {
                quitOrMenuButton.onClick.AddListener(() =>
                {
                    UiAudioManager.Instance?.PlayClick();
                    Time.timeScale = 1f;
                    if (mode == MenuMode.MainMenu)
                        Application.Quit();
                    else
                        SceneManager.LoadScene(mainMenuSceneName);
                });
            }

            BindVolumeSlider(bgmSlider, true);
            BindVolumeSlider(sfxSlider, false);

            if (settingsBox != null) settingsBox.SetActive(false);
            if (mode == MenuMode.PauseMenu && panel != null) panel.SetActive(false);
        }

        private static void BindVolumeSlider(Slider slider, bool bgm)
        {
            if (slider == null) return;
            float saved = PlayerPrefs.GetFloat(bgm ? UiAudioManager.BgmPrefKey : UiAudioManager.SfxPrefKey, UiAudioManager.DefaultVolume);
            slider.SetValueWithoutNotify(saved);
            if (bgm)
            {
                UiAudioManager.SetBgmVolume(saved);
                slider.onValueChanged.AddListener(UiAudioManager.SetBgmVolume);
            }
            else
            {
                UiAudioManager.SetSfxVolume(saved);
                slider.onValueChanged.AddListener(UiAudioManager.SetSfxVolume);
            }
        }

        // ใช้ LateUpdate: ให้ร้านค้าปิดตัวเองด้วย ESC ก่อน (Update) แล้วค่อยเช็คว่า ESC ถูกใช้ไปหรือยัง
        private void LateUpdate()
        {
            if (mode != MenuMode.PauseMenu) return;

            bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                escPressed = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape))
                escPressed = true;
#endif
            if (escPressed && !UiBuild.EscapeUsedThisFrame)
            {
                SetPaused(!isPaused);
            }
        }

        public void SetPaused(bool pause)
        {
            isPaused = pause;
            Time.timeScale = pause ? 0f : 1f;
            if (panel != null) panel.SetActive(pause);
            if (!pause && settingsBox != null) settingsBox.SetActive(false);
            UiAudioManager.Instance?.PlayPanelToggle();
        }

        public static GameMenuPanelsView CreateDefault(Transform parent, MenuMode menuMode)
        {
            bool isMain = menuMode == MenuMode.MainMenu;
            string objName = isMain ? "Main Menu Panel" : "Pause Menu Panel";
            RectTransform canvasRect = UiBuild.CreateCanvas(parent, objName, 120);
            UiBuild.Stretch(canvasRect);
            canvasRect.GetComponent<GraphicRaycaster>().ignoreReversedGraphics = true;
            GameMenuPanelsView view = canvasRect.gameObject.AddComponent<GameMenuPanelsView>();
            view.mode = menuMode;
            view.gameplaySceneName = "Map Layout";
            view.mainMenuSceneName = "MainMenu";

            RectTransform overlay = UiBuild.CreateRect(canvasRect, "Panel", typeof(Image));
            UiBuild.Stretch(overlay);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, isMain ? 0.75f : 0.88f);
            view.panel = overlay.gameObject;

            RectTransform box = UiBuild.CreateRect(overlay, "Window Box", typeof(Image), typeof(VerticalLayoutGroup));
            UiBuild.Place(box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520f, 540f), new Vector2(0.5f, 0.5f));
            box.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.2f, 0.96f);

            VerticalLayoutGroup vlg = box.GetComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(48, 48, 48, 48);
            vlg.spacing = 22f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;

            view.titleText = UiBuild.CreateText(box, "Title", isMain ? "KITCHEN HUNT" : "PAUSED", 52, FontStyle.Bold, new Color(1f, 0.85f, 0.25f, 1f), TextAnchor.MiddleCenter);
            view.titleText.rectTransform.sizeDelta = new Vector2(440f, 90f);

            view.primaryButton = UiBuild.CreateButton(box, "Primary Button", isMain ? "START GAME" : "RESUME", new Vector2(440f, 78f), new Color(0.25f, 0.75f, 0.35f, 1f), 36);
            view.settingsButton = UiBuild.CreateButton(box, "Settings Button", "SETTINGS", new Vector2(440f, 78f), new Color(0.45f, 0.47f, 0.52f, 1f), 36);
            view.quitOrMenuButton = UiBuild.CreateButton(box, "Quit Button", isMain ? "QUIT GAME" : "MAIN MENU", new Vector2(440f, 78f), new Color(0.95f, 0.25f, 0.42f, 1f), 36);

            RectTransform setBox = UiBuild.CreateRect(overlay, "Settings Box", typeof(Image));
            UiBuild.Place(setBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 380f), new Vector2(0.5f, 0.5f));
            setBox.GetComponent<Image>().color = new Color(0.14f, 0.16f, 0.26f, 0.98f);
            view.settingsBox = setBox.gameObject;

            Text setTitle = UiBuild.CreateText(setBox, "Settings Title", "AUDIO SETTINGS", 38, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            UiBuild.Place(setTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -45f), new Vector2(480f, 50f), new Vector2(0.5f, 0.5f));

            view.bgmSlider = CreateLabeledSlider(setBox, "BGM", new Vector2(0f, -125f));
            view.sfxSlider = CreateLabeledSlider(setBox, "SFX", new Vector2(0f, -206f));

            view.closeSettingsButton = UiBuild.CreateButton(setBox, "Close Settings", "BACK", new Vector2(220f, 64f), new Color(0.3f, 0.35f, 0.45f, 1f), 30);
            UiBuild.Place((RectTransform)view.closeSettingsButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(220f, 64f), new Vector2(0.5f, 0f));

            setBox.gameObject.SetActive(false);
            if (!isMain) overlay.gameObject.SetActive(false);
            return view;
        }

        private static Slider CreateLabeledSlider(RectTransform parent, string label, Vector2 pos)
        {
            RectTransform row = UiBuild.CreateRect(parent, label + " Row");
            UiBuild.Place(row, new Vector2(0.5f, 1f), pos, new Vector2(460f, 50f), new Vector2(0.5f, 0.5f));

            Text txt = UiBuild.CreateText(row, "Label", label, 30, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
            UiBuild.Place(txt.rectTransform, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(120f, 50f), new Vector2(0f, 0.5f));

            RectTransform sliderRt = UiBuild.CreateRect(row, "Slider", typeof(Slider));
            UiBuild.Place(sliderRt, new Vector2(1f, 0.5f), Vector2.zero, new Vector2(320f, 28f), new Vector2(1f, 0.5f));

            RectTransform bg = UiBuild.CreateRect(sliderRt, "Background", typeof(Image));
            UiBuild.Stretch(bg);
            bg.GetComponent<Image>().color = new Color(0.08f, 0.09f, 0.14f, 1f);

            RectTransform fill = UiBuild.CreateRect(sliderRt, "Fill", typeof(Image));
            UiBuild.Stretch(fill);
            fill.GetComponent<Image>().color = new Color(0.2f, 0.75f, 1f, 1f);

            Image bgImage = bg.GetComponent<Image>();
            Slider slider = sliderRt.GetComponent<Slider>();
            slider.targetGraphic = bgImage;
            slider.fillRect = fill;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            return slider;
        }
    }
}