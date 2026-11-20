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
                    break;

                case Stat.AttackSpeed:
                    foreach (Weapon w in FindObjectsByType<Weapon>(FindObjectsInactive.Include))
                    {
                        w.SetAttackSpeed(value);
                    }
                    break;
            }
        }

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
