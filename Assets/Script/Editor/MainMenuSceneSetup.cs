using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GI351.KitchenGame.EditorTools
{
    public static class MainMenuSceneSetup
    {
        private const string PrefabPath = "Assets/Prefab/UI/Main Menu Panel.prefab";
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        public static void Build()
        {
            Scene menuScene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameMenuPanelsView[] existing = Object.FindObjectsByType<GameMenuPanelsView>(FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
                Object.DestroyImmediate(existing[i].gameObject);

            GameMenuPanelsView view = GameMenuPanelsView.CreateDefault(null, GameMenuPanelsView.MenuMode.MainMenu);
            GameObject root = view.gameObject;
            root.name = "Main Menu Panel";
            PrefabUtility.SaveAsPrefabAssetAndConnect(root, PrefabPath, InteractionMode.AutomatedAction);

            UiBuild.EnsureEventSystem();
            EnsureBuildScene(ScenePath);

            EditorSceneManager.MarkSceneDirty(menuScene);
            EditorSceneManager.SaveScene(menuScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[UI] Main Menu Panel saved to " + PrefabPath + " and placed in " + ScenePath);
        }

        private static void EnsureBuildScene(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Exists(scene => scene.path == scenePath)) return;
            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
