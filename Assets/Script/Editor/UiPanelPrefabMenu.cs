using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GI351.KitchenGame.EditorTools
{
    /// <summary>
    /// เมนู Tools > UI: สร้าง prefab หน้าต่างแบบหน้าตาเริ่มต้นใหม่ (ไม่ทับไฟล์เดิม ถ้ามีชื่อซ้ำจะเติมเลขต่อท้าย)
    /// ใช้ตอนอยากได้หน้าตาเริ่มต้นกลับมา หรือทำหน้าต่างชุดใหม่ให้ร้าน/หม้อตัวอื่น
    /// </summary>
    public static class UiPanelPrefabMenu
    {
        private const string Folder = "Assets/Prefab/UI";

        [MenuItem("Tools/UI/Create Default Shop Panel Prefab")]
        private static void CreateShopPanel()
        {
            Select(CreatePrefab("Shop Panel", parent => ShopPanelView.CreateDefault(parent, true).gameObject));
        }

        [MenuItem("Tools/UI/Create Default Pot Upgrade Panel Prefab")]
        private static void CreatePotUpgradePanel()
        {
            Select(CreatePrefab("Pot Upgrade Panel", parent => PotUpgradePanelView.CreateDefault(parent).gameObject));
        }

        /// <summary>สร้าง UI ในฉากชั่วคราว บันทึกเป็น prefab แล้วปิดฉากนั้น (ฉากที่เปิดอยู่ไม่ถูกแก้) คืน path ของ prefab</summary>
        public static string CreatePrefab(string prefabName, Func<Transform, GameObject> build)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[UI] หยุด Play ก่อนแล้วค่อยสร้าง prefab");
                return null;
            }

            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning("[UI] ไม่เจอโฟลเดอร์ " + Folder);
                return null;
            }

            // ฉากชั่วคราวเปิดใน Prefab Mode ไม่ได้ ออกไปฉากหลักก่อน (ถ้ามีของยังไม่เซฟ Unity จะถามเอง)
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) StageUtility.GoToMainStage();
            if (PrefabStageUtility.GetCurrentPrefabStage() != null) return null;

            // Unity สร้างฉากชั่วคราวแบบ Additive ไม่ได้ ถ้ามีฉาก Untitled (ยังไม่เคยเซฟ) เปิดอยู่
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                {
                    Debug.LogWarning("[UI] เซฟฉาก Untitled ก่อน แล้วค่อยสร้าง prefab");
                    return null;
                }
            }

            Scene previous = SceneManager.GetActiveScene();
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(temp);
                GameObject root = build(null);
                root.name = prefabName;
                string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + prefabName + ".prefab");
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log("[UI] สร้าง " + path);
                return path;
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temp, true);
            }
        }

        private static void Select(string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
