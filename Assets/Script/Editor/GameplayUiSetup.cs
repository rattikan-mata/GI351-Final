using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GI351.KitchenGame.EditorTools
{
    public static class GameplayUiSetup
    {
        private const string ScenePath = "Assets/Scenes/Map Layout.unity";
        private const string HudPrefabPath = "Assets/Prefab/UI/HUD Extras Canvas.prefab";
        private const string PausePrefabPath = "Assets/Prefab/UI/Pause Menu Panel.prefab";

        public static void Build()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            HudExtrasView[] extras = Object.FindObjectsByType<HudExtrasView>(FindObjectsSortMode.None);
            for (int i = 0; i < extras.Length; i++)
                Object.DestroyImmediate(extras[i].gameObject);

            GameMenuPanelsView[] menus = Object.FindObjectsByType<GameMenuPanelsView>(FindObjectsSortMode.None);
            for (int i = 0; i < menus.Length; i++)
            {
                if (menus[i] != null && menus[i].mode == GameMenuPanelsView.MenuMode.PauseMenu)
                    Object.DestroyImmediate(menus[i].gameObject);
            }

            Transform uiRoot = GameObject.Find("UI") != null ? GameObject.Find("UI").transform : null;

            HudExtrasView hud = HudExtrasView.CreateDefault(uiRoot);
            PrefabUtility.SaveAsPrefabAssetAndConnect(hud.gameObject, HudPrefabPath, InteractionMode.AutomatedAction);

            GameMenuPanelsView pause = GameMenuPanelsView.CreateDefault(uiRoot, GameMenuPanelsView.MenuMode.PauseMenu);
            PrefabUtility.SaveAsPrefabAssetAndConnect(pause.gameObject, PausePrefabPath, InteractionMode.AutomatedAction);

            UiBuild.EnsureEventSystem();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] HUD Extras Canvas and Pause Menu Panel saved and placed in " + ScenePath);
        }
    }
}
