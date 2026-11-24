using System;
using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// อัปเกรดฝั่งร้านแบบเลเวล (คู่กับ HuntUpgrades ที่ดูแลฝั่งล่า): อยู่คู่กับ ShopStall ผูก id สินค้า -> ค่าที่จะเปลี่ยน
    ///   CustomersPerMinute = ลูกค้ามากี่คนต่อนาที (CustomerSpawner ทุกตัว) ตอนนี้ไม่ขายแล้ว ลูกค้ามาตามของบนแผงเอง
    ///   DishesPerCustomer  = ลูกค้าซื้อได้สูงสุดคนละกี่จาน (ไม่ขายแล้ว)
    ///   ShelfCapacity      = แผงขายทุกแผงจุอาหารได้กี่จาน    /// ค่าของแต่ละเลเวลและราคาตั้งที่ ShopStall > Items > Levels  ตอนเริ่มเกมจะ apply ค่า Lv1 ให้ (ค่าในตารางเป็นหลัก)
    /// (ความเร็วหม้อ/ราคาอาหาร อัปที่แผงของหม้อแต่ละใบ: PotUpgrade)
    /// </summary>
    [RequireComponent(typeof(ShopStall))]
    public class StoreUpgrades : MonoBehaviour
    {
        public enum Stat
        {
            CustomersPerMinute,
            DishesPerCustomer,
            ShelfCapacity
        }

        [Serializable]
        public class Binding
        {
            [Tooltip("id ของสินค้าในร้าน (ShopStall > Items > Id)")]
            public string itemId = "";

            [Tooltip("ค่าที่สินค้านี้เปลี่ยน")]
            public Stat stat = Stat.CustomersPerMinute;
        }

        [Tooltip("สินค้าไหน (id) เปลี่ยนค่าอะไร")]
        [SerializeField] private List<Binding> bindings = new List<Binding>
        {
            new Binding { itemId = "shelf_size", stat = Stat.ShelfCapacity }
        };

        private ShopStall stall;

        private void Awake()
        {
            stall = GetComponent<ShopStall>();
        }

        private void OnEnable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            if (stall != null) stall.ItemPurchased += HandlePurchased;
        }

        private void OnDisable()
        {
            if (stall != null) stall.ItemPurchased -= HandlePurchased;
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

        private void Apply(Binding b)
        {
            if (b == null || stall == null) return;
            ShopItemData item = stall.FindItem(b.itemId);
            if (item == null || !item.IsLeveled) return;

            float value = item.CurrentValue;
            switch (b.stat)
            {
                case Stat.CustomersPerMinute:
                    foreach (CustomerSpawner s in FindObjectsByType<CustomerSpawner>(FindObjectsInactive.Include))
                    {
                        s.SetCustomersPerMinute(value);
                    }
                    break;

                case Stat.DishesPerCustomer:
                    foreach (CustomerSpawner s in FindObjectsByType<CustomerSpawner>(FindObjectsInactive.Include))
                    {
                        s.SetMaxDishesPerCustomer(Mathf.RoundToInt(value));
                    }
                    break;

                case Stat.ShelfCapacity:
                    foreach (FoodSellCounter c in FindObjectsByType<FoodSellCounter>(FindObjectsInactive.Include))
                    {
                        c.SetMaxPlates(Mathf.RoundToInt(value));
                    }
                    break;            }
        }
    }
}
