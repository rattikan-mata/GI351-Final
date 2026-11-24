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
                    // กระบองเต็มเลเวล: สัตว์ทุกโซนเกิดเยอะขึ้น (ค่าต่อโซนตั้งที่ HuntArea > Max Club Spawn Bonus)
                    HuntArea.SetClubMaxed(item.IsMaxLevel);
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
        /// เช็คสเกล "สัตว์ทุกตัวลด 3 ที -> 2 ที -> 1 ที ทีละเลเวล" ไม่ตรงจะขึ้นเตือนสีเหลือง
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

            // สเกล: สัตว์ทุกตัวต้องลดจาก 3 ที -> 2 ที -> 1 ที ทีละเลเวลติดกัน (ไม่ข้าม ไม่ค้าง)
            // ยกเว้นตัวที่ตีได้ไม่เกิน 3 ทีตั้งแต่ Lv1 (สัตว์ตัวแรก) เริ่มที่กี่ทีก็ได้แต่ต้องลดทีละ 1
            rows.Sort((a, b) => a.Value.CompareTo(b.Value));
            bool ok = true;
            int lastOneHitLv = 0;
            var warnings = new System.Text.StringBuilder();
            for (int r = 0; r < rows.Count; r++)
            {
                var row = rows[r];
                sb.Append((row.Key + " " + row.Value + "hp").PadRight(16));
                int[] hits = new int[club.levels.Count];
                for (int i = 0; i < club.levels.Count; i++)
                {
                    hits[i] = HitsToKill(row.Value, club.levels[i].value);
                    sb.Append(hits[i].ToString().PadLeft(6));
                }
                sb.AppendLine();

                int start = System.Array.FindIndex(hits, h => h <= 3);
                if (start < 0)
                {
                    ok = false;
                    warnings.AppendLine("<color=yellow>" + row.Key + " กระบองเลเวลสูงสุดยังตีเกิน 3 ที</color>");
                    continue;
                }
                if (start > 0 && hits[start] != 3)
                {
                    ok = false;
                    warnings.AppendLine("<color=yellow>" + row.Key + " ข้ามจาก " + hits[start - 1] + " ทีไป " + hits[start] + " ที ตอน Lv" + (start + 1) + " (ควรเป็น 3 ที)</color>");
                }
                for (int i = start + 1; i < hits.Length && hits[i - 1] > 1; i++)
                {
                    if (hits[i] != hits[i - 1] - 1)
                    {
                        ok = false;
                        warnings.AppendLine("<color=yellow>" + row.Key + " Lv" + i + " -> Lv" + (i + 1) + " ควรลดจาก " + hits[i - 1] + " เหลือ " + (hits[i - 1] - 1) + " ที (ได้ " + hits[i] + ")</color>");
                    }
                }
                int oneHit = System.Array.IndexOf(hits, 1);
                lastOneHitLv = Mathf.Max(lastOneHitLv, oneHit + 1);
            }
            sb.Append(warnings);
            if (ok) sb.AppendLine("ตรงสเกล: สัตว์ทุกตัวลด 3 ที -> 2 ที -> 1 ที ทีละเลเวล");
            if (lastOneHitLv > 0 && lastOneHitLv < club.levels.Count)
            {
                sb.AppendLine("Lv" + (lastOneHitLv + 1) + " ขึ้นไป ยังไม่มีสัตว์รองรับ (รอสัตว์ตัวใหม่)");
            }
            Debug.Log(sb.ToString(), this);
        }

        private static int HitsToKill(int hp, float damage)
        {
            return Mathf.CeilToInt((float)hp / Mathf.Max(1, Mathf.RoundToInt(damage)));
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
