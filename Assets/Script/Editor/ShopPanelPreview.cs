using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame.EditorTools
{
    /// <summary>
    /// ตอนเปิด prefab หน้าซื้อ (Shop Panel / Staff Panel) ใน Prefab Mode: โชว์แถวสินค้าและแท็บจริงจากร้านในช่อง Preview Shop
    /// ตัวอย่างก๊อปจากต้นแบบในกล่อง Templates และอัปเดตเองเมื่อแก้ต้นแบบ / สี / ข้อมูลร้าน
    /// ตัวอย่างแก้ไม่ได้ และไม่ถูกบันทึกลง prefab (ชื่อขึ้นต้นด้วย "Preview")
    /// </summary>
    [InitializeOnLoad]
    internal static class ShopPanelPreview
    {
        private const HideFlags PreviewFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild | HideFlags.NotEditable;
        private const double CheckInterval = 0.25;

        private static double nextCheck;
        private static string lastSignature;
        private static ShopPanelView lastView;

        static ShopPanelPreview()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + CheckInterval;

            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            ShopPanelView view = stage != null && stage.prefabContentsRoot != null ? stage.prefabContentsRoot.GetComponent<ShopPanelView>() : null;
            if (view == null)
            {
                lastView = null;
                lastSignature = null;
                return;
            }

            string signature = Signature(view);
            if (view == lastView && signature == lastSignature) return;

            Rebuild(view);
            lastView = view;
            lastSignature = Signature(view);
        }

        /// <summary>สร้างตัวอย่างใหม่ทั้งหมดจากข้อมูลร้าน</summary>
        private static void Rebuild(ShopPanelView view)
        {
            Clear(view.transform);

            ShopStall shop = view.previewShop;
            if (shop == null || view.listContent == null || view.rowTemplate == null) return;
            if (view.rowTemplate.nameText == null || view.rowTemplate.priceText == null || view.rowTemplate.buyButton == null) return;

            int tabCount = shop.Tabs != null ? shop.Tabs.Count : 0;
            int tabIndex = tabCount > 0 ? Mathf.Clamp(view.previewTab, 0, tabCount - 1) : 0;

            if (tabCount > 0 && view.tabBar != null && view.tabTemplate != null)
            {
                for (int i = 0; i < tabCount; i++)
                {
                    Button tab = ShopStall.SpawnTab(view, shop.Tabs[i]);
                    ShopStall.ApplyTabState(view, tab, i == tabIndex);
                    Mark(tab.gameObject);
                }
            }

            for (int i = 0; i < shop.ItemCount; i++)
            {
                ShopItemData item = shop.GetItem(i);
                if (item == null || !shop.IsItemInTab(item, tabIndex)) continue;

                ShopRowView row = ShopStall.SpawnRow(view, item, i);
                ShopStall.ApplyRowState(view, row, item, view.previewMoney);
                Mark(row.gameObject);
            }
        }

        /// <summary>ทำให้เป็นตัวอย่าง: แก้ไม่ได้ ไม่ถูกบันทึก</summary>
        private static void Mark(GameObject root)
        {
            root.name = "Preview " + root.name;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                t.gameObject.hideFlags = PreviewFlags;
                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c != null) c.hideFlags = PreviewFlags;
                }
            }
        }

        private static bool IsPreview(GameObject go)
        {
            return (go.hideFlags & HideFlags.DontSaveInEditor) != 0;
        }

        /// <summary>ลบตัวอย่างทั้งหมดใต้ root (รวมที่ค้างอยู่ใน List Content / Tab Bar เดิม ถ้าช่องพวกนั้นถูกเปลี่ยน)</summary>
        private static void Clear(Transform root)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || !IsPreview(t.gameObject)) continue;
                if (t.parent != null && IsPreview(t.parent.gameObject)) continue; // ลบที่ตัวบนสุดของตัวอย่างพอ
                Object.DestroyImmediate(t.gameObject);
            }
        }

        /// <summary>ข้อมูลทุกอย่างที่มีผลกับหน้าตาตัวอย่าง (เปลี่ยนเมื่อไหร่ = สร้างตัวอย่างใหม่)</summary>
        private static string Signature(ShopPanelView view)
        {
            StringBuilder sb = new StringBuilder(4096);
            sb.Append(EditorJsonUtility.ToJson(view));
            AppendTree(sb, view.rowTemplate != null ? view.rowTemplate.transform : null);
            AppendTree(sb, view.tabTemplate != null ? view.tabTemplate.transform : null);
            if (view.previewShop != null) sb.Append(EditorJsonUtility.ToJson(view.previewShop));
            sb.Append('|').Append(CountPreview(view.transform));
            return sb.ToString();
        }

        private static void AppendTree(StringBuilder sb, Transform root)
        {
            if (root == null) return;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                sb.Append(t.name).Append(t.gameObject.activeSelf);
                foreach (Component c in t.GetComponents<Component>())
                {
                    if (c != null) sb.Append(EditorJsonUtility.ToJson(c));
                }
            }
        }

        private static int CountPreview(Transform root)
        {
            int n = 0;
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (IsPreview(t.gameObject)) n++;
            }
            return n;
        }
    }
}
