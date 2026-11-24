using UnityEditor;

namespace GI351.KitchenGame.EditorTools
{
    public static class ExtraUiPrefabMenu
    {

        [MenuItem("Tools/UI/Create Default Main Menu Prefab")]
        private static void CreateMainMenu()
        {
            string path = UiPanelPrefabMenu.CreatePrefab("Main Menu Panel",
                parent => GameMenuPanelsView.CreateDefault(parent, GameMenuPanelsView.MenuMode.MainMenu).gameObject);
            Select(path);
        }

        [MenuItem("Tools/UI/Create Default Pause Menu Prefab")]
        private static void CreatePauseMenu()
        {
            string path = UiPanelPrefabMenu.CreatePrefab("Pause Menu Panel",
                parent => GameMenuPanelsView.CreateDefault(parent, GameMenuPanelsView.MenuMode.PauseMenu).gameObject);
            Select(path);
        }

        [MenuItem("Tools/UI/Create Default HUD Extras Prefab")]
        private static void CreateHudExtras()
        {
            string path = UiPanelPrefabMenu.CreatePrefab("HUD Extras Canvas",
                parent => HudExtrasView.CreateDefault(parent).gameObject);
            Select(path);
        }

        private static void Select(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}