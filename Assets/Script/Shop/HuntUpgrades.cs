using System;
using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ร้านอัปเกรดการล่าแบบเลเวล: อยู่คู่กับ ShopStall (สินค้าแบบอัปเลเวล) ถือเลเวลปัจจุบันจากสินค้าแต่ละชิ้นแล้ว apply ค่าไปที่
    /// กระบอง (Weapon.damage) / กระเป๋า (PlayerCarry.maxCarryCapacity) / รองเท้า (PlayerMovement.moveSpeed) / ถุงมือ (Weapon.attackSpeed)
    /// ค่าของแต่ละเลเวลและราคาตั้งที่ ShopStall > Items > Levels ส่วนสคริปต์นี้ผูกแค่ id สินค้า -> ค่าที่จะเปลี่ยน
    /// ตอนเริ่มเกมจะ apply ค่า Lv1 ให้ด้วย (ค่าในตารางเป็นหลัก) และอาวุธที่เพิ่งเปิดใช้งาน/สลับมาจะได้ดาเมจตามเลเวลทันที
    /// </summary>
    [RequireComponent(typeof(ShopStall))]
    public class HuntUpgrades : MonoBehaviour
    {
        public enum Stat
        {
            WeaponDamage,
            CarryCapacity,
            MoveSpeed,
            AttackSpeed
        }

        [Serializable]
        public class Binding
        {
            [Tooltip("id ของสินค้าในร้าน (ShopStall > Items > Id)")]
            public string itemId = "";

            [Tooltip("ค่าที่สินค้านี้เปลี่ยน")]
            public Stat stat = Stat.WeaponDamage;
        }

        [Tooltip("สินค้าไหน (id) เปลี่ยนค่าอะไร")]
        [SerializeField] private List<Binding> bindings = new List<Binding>
        {
            new Binding { itemId = "club", stat = Stat.WeaponDamage },
            new Binding { itemId = "bag", stat = Stat.CarryCapacity },
            new Binding { itemId = "boots", stat = Stat.MoveSpeed },
            new Binding { itemId = "gloves", stat = Stat.AttackSpeed }
        };

        private ShopStall stall;

        private static HuntUpgrades active;

        private void Awake()
        {
            stall = GetComponent<ShopStall>();
        }

        private void OnEnable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            active = this;
            if (stall != null) stall.ItemPurchased += HandlePurchased;
        }

        private void OnDisable()
        {
            if (stall != null) stall.ItemPurchased -= HandlePurchased;
            if (active == this) active = null;
        }

        private void Start()
        {
            ApplyAll();
        }

        private void HandlePurchased(ShopItemData item)
        {
            if (item == null) return;
            for (int i = 0; i < bindings.Count; i++)
            {
                if (bindings[i].itemId == item.id) Apply(bindings[i]);
            }
        }

        /// <summary>apply ค่าของเลเวลปัจจุบันของทุกสินค้า</summary>
        public void ApplyAll()
        {
            for (int i = 0; i < bindings.Count; i++) Apply(bindings[i]);
        }

        /// <summary>เลเวลปัจจุบันของสินค้า id นี้ (0 = ไม่เจอสินค้า)</summary>
        public int GetLevel(string itemId)
        {
            ShopItemData item = stall != null ? stall.FindItem(itemId) : null;
            return item != null && item.IsLeveled ? item.Level : 0;
        }

        private void Apply(Binding b)
        {
            if (b == null || stall == null) return;
            ShopItemData item = stall.FindItem(b.itemId);
            if (item == null || !item.IsLeveled) return;

            float value = item.CurrentValue;
            switch (b.stat)
            {
                case Stat.WeaponDamage:
                    int damage = Mathf.RoundToInt(value);
                    foreach (Weapon w in FindObjectsByType<Weapon>(FindObjectsInactive.Include))
                    {
                        w.SetDamage(damage);
                    }
                    break;

                case Stat.CarryCapacity:
                    foreach (PlayerCarry c in FindObjectsByType<PlayerCarry>(FindObjectsInactive.Include))
                    {
                        c.SetMaxCarryCapacity(Mathf.RoundToInt(value));
                    }
                    break;

                case Stat.MoveSpeed:
                    foreach (PlayerMovement m in FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include))
                    {
                        m.SetMoveSpeed(value);
                    }
                    // หางตอนวิ่ง: สีตามเลเวลรองเท้า
                    foreach (RunTrail t in FindObjectsByType<RunTrail>(FindObjectsInactive.Include))
                    {
                        t.SetLevel(item.Level);
                    }
                    break;

                case Stat.AttackSpeed:
                    foreach (Weapon w in FindObjectsByType<Weapon>(FindObjectsInactive.Include))
                    {
                        w.SetAttackSpeed(value);
                    }
                    break;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// เครื่องมือเช็คสเกล: คลิกขวาที่ Hunt Upgrades > Print Hits-To-Kill Table
        /// พิมพ์ตาราง "ตีกี่ทีตาย" ของสัตว์ทุกตัวใน Prefab เทียบทุกเลเวลกระบอง
        /// เลเวลไหนอัปแล้วไม่มีสัตว์ตัวไหนตายเร็วขึ้นเลย จะขึ้นเตือนสีเหลือง (= ซื้อแล้วไม่ได้อะไร ควรปรับดาเมจ/เลือด)
        /// </summary>
        [ContextMenu("Print Hits-To-Kill Table")]
        private void PrintHitsToKillTable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            ShopItemData club = null;
            foreach (Binding b in bindings)
            {
                if (b.stat != Stat.WeaponDamage) continue;
                club = stall != null ? stall.FindItem(b.itemId) : null;
                if (club != null) break;
            }
            if (club == null || !club.IsLeveled)
            {
                Debug.LogWarning("[HuntUpgrades] ไม่พบสินค้ากระบองแบบเลเวล", this);
                return;
            }

            var rows = new List<KeyValuePair<string, int>>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject go = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go == null || !go.TryGetComponent(out EnemyHealth h)) continue;
                rows.Add(new KeyValuePair<string, int>(go.name, h.MaxHealth));
            }

            var sb = new System.Text.StringBuilder("[HuntUpgrades] Hits-to-kill per club level\n");
            sb.Append("Damage:".PadRight(16));
            foreach (ShopItemLevel lv in club.levels) sb.Append(Mathf.RoundToInt(lv.value).ToString().PadLeft(6));
            sb.AppendLine();

            var improved = new bool[club.levels.Count];
            foreach (var row in rows)
            {
                sb.Append((row.Key + " " + row.Value + "hp").PadRight(16));
                int prev = int.MaxValue;
                for (int i = 0; i < club.levels.Count; i++)
                {
                    int dmg = Mathf.Max(1, Mathf.RoundToInt(club.levels[i].value));
                    int hits = Mathf.CeilToInt((float)row.Value / dmg);
                    if (i > 0 && hits < prev) improved[i] = true;
                    prev = hits;
                    sb.Append(hits.ToString().PadLeft(6));
                }
                sb.AppendLine();
            }

            bool ok = true;
            for (int i = 1; i < improved.Length; i++)
            {
                if (improved[i]) continue;
                ok = false;
                sb.AppendLine("<color=yellow>Lv" + (i + 1) + " (" + Mathf.RoundToInt(club.levels[i].value) + " dmg) ไม่ทำให้สัตว์ตัวไหนตายเร็วขึ้นเลย</color>");
            }
            if (ok) sb.AppendLine("ทุกเลเวลมีสัตว์อย่างน้อย 1 ตัวที่ตายเร็วขึ้น");
            Debug.Log(sb.ToString(), this);
        }
#endif

        /// <summary>เรียกจาก Weapon ตอนเปิดใช้งาน: ให้อาวุธนี้ได้ดาเมจตามเลเวลกระบองที่ซื้อไว้ (ถ้าไม่มีร้านในฉากก็ไม่ทำอะไร)</summary>
        public static void ApplyToWeapon(Weapon weapon)
        {
            if (active == null || weapon == null || active.stall == null) return;

            for (int i = 0; i < active.bindings.Count; i++)
            {
                Binding b = active.bindings[i];
                if (b.stat != Stat.WeaponDamage && b.stat != Stat.AttackSpeed) continue;
                ShopItemData item = active.stall.FindItem(b.itemId);
                if (item == null || !item.IsLeveled) continue;
                if (b.stat == Stat.WeaponDamage) weapon.SetDamage(Mathf.RoundToInt(item.CurrentValue));
                else weapon.SetAttackSpeed(item.CurrentValue);
            }
        }
    }
}
