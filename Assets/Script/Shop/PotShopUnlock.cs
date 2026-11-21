using System;
using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ซื้อหม้อจากร้าน (แท็บ Cook): อยู่คู่กับ ShopStall ผูก id สินค้า -> หม้อที่ใช้วัตถุดิบนั้น (เช่น pork_pot -> หม้อที่ใช้ PigMeat)
    /// ซื้อสำเร็จ = ปลดล็อกหม้อทันที (ร้านหักเงินไปแล้ว ไม่หักซ้ำ) ชั้นขายที่ผูกกับหม้อนั้นก็ปลดตามไปด้วย
    /// หม้อที่ปลดอยู่แล้ว สินค้าจะขึ้น OWNED กดซื้อซ้ำไม่ได้
    /// ผูกด้วยวัตถุดิบ (ไม่ใช่ลากหม้อมาใส่) เพราะ Shop Stall เป็น Prefab อ้างถึงของในฉากตรงๆ ไม่ได้
    /// </summary>
    [RequireComponent(typeof(ShopStall))]
    public class PotShopUnlock : MonoBehaviour
    {
        [Serializable]
        public class Binding
        {
            [Tooltip("id ของสินค้าในร้าน (ShopStall > Items > Id)")]
            public string itemId = "";

            [Tooltip("ปลดหม้อที่ใช้วัตถุดิบนี้ (เช่น PigMeat = หม้อหมู)")]
            public ItemType potIngredient = ItemType.PigMeat;
        }

        [Tooltip("สินค้าไหน (id) ปลดหม้ออะไร")]
        [SerializeField] private List<Binding> bindings = new List<Binding>
        {
            new Binding { itemId = "pork_pot", potIngredient = ItemType.PigMeat }
        };

        private ShopStall stall;

        private void Awake()
        {
            stall = GetComponent<ShopStall>();
        }

        private void OnEnable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            if (stall == null) return;
            stall.ItemPurchased += HandlePurchased;
            stall.onOpened.AddListener(SyncOwned);
        }

        private void OnDisable()
        {
            if (stall == null) return;
            stall.ItemPurchased -= HandlePurchased;
            stall.onOpened.RemoveListener(SyncOwned);
        }

        private void Start()
        {
            SyncOwned();
        }

        private void HandlePurchased(ShopItemData item)
        {
            if (item == null) return;
            foreach (Binding b in bindings)
            {
                if (b.itemId != item.id) continue;
                CookingPot pot = FindPot(b.potIngredient);
                if (pot != null) pot.UnlockPot();
                else Debug.LogWarning("[PotShopUnlock] ไม่พบหม้อที่ใช้ " + b.potIngredient + " ในฉาก", this);
            }
        }

        /// <summary>หม้อที่ปลดอยู่แล้ว (ซื้อแล้ว หรือไม่ต้องซื้อ) ให้สินค้าขึ้นว่าซื้อแล้ว</summary>
        private void SyncOwned()
        {
            if (stall == null) return;
            bool changed = false;
            foreach (Binding b in bindings)
            {
                ShopItemData item = stall.FindItem(b.itemId);
                CookingPot pot = FindPot(b.potIngredient);
                if (item == null || pot == null || !pot.IsUnlocked) continue;
                int stock = Mathf.Max(1, item.stock);
                if (item.stock <= 0) item.stock = 1;
                if (item.sold < stock)
                {
                    item.sold = stock;
                    changed = true;
                }
            }
            if (changed && stall.IsOpen) stall.RebuildList();
        }

        private static CookingPot FindPot(ItemType ingredient)
        {
            foreach (CookingPot p in FindObjectsByType<CookingPot>(FindObjectsInactive.Include))
            {
                if (p != null && p.RequiredIngredient == ingredient) return p;
            }
            return null;
        }
    }
}
