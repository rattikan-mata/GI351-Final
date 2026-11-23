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
    /// ล็อกตามโซน: ถ้าสัตว์ที่ให้วัตถุดิบนั้นอยู่ในโซนที่ประตูยังไม่เปิด จะยังซื้อหม้อไม่ได้
    /// (ลูกค้าสั่งเมนูทันทีที่มีหม้อ ถ้าซื้อหม้อก่อนเปิดประตู ลูกค้าจะสั่งของที่ยังล่าไม่ได้แล้วเดินหนี)
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

            // สถานะตอนเล่น (ไม่เซฟ)
            [NonSerialized] public AreaGate gate;
            [NonSerialized] public string areaName;
            [NonSerialized] public bool lockedByArea;
            [NonSerialized] public string savedLockedText;
            [NonSerialized] public string savedDescription;
        }

        [Tooltip("สินค้าไหน (id) ปลดหม้ออะไร")]
        [SerializeField] private List<Binding> bindings = new List<Binding>
        {
            new Binding { itemId = "pork_pot", potIngredient = ItemType.PigMeat }
        };

        [Header("Area Lock (ล็อกตามประตูโซน)")]
        [Tooltip("ต้องเปิดประตูโซนของสัตว์ตัวนั้นก่อน ถึงจะซื้อหม้อได้ (หาโซนเองจาก Hunt Area ที่สัตว์ดรอปวัตถุดิบนั้น)\n" +
                 "ปิดตัวเลือกนี้ = ซื้อหม้อได้ตั้งแต่ก่อนเปิดประตู")]
        [SerializeField] private bool lockUntilAreaOpen = true;

        [Tooltip("ข้อความบนปุ่มตอนโซนยังไม่เปิด")]
        [SerializeField] private string areaLockedText = "LOCKED";

        [Tooltip("คำอธิบายใต้ชื่อสินค้าตอนโซนยังไม่เปิด ({0} = ชื่อโซน) ปล่อยว่าง = ใช้คำอธิบายเดิม")]
        [SerializeField] private string areaLockedDescription = "Open {0} first";

        private ShopStall stall;
        private bool areasResolved;

        private void Awake()
        {
            stall = GetComponent<ShopStall>();
        }

        private void OnEnable()
        {
            if (stall == null) stall = GetComponent<ShopStall>();
            if (stall == null) return;
            stall.ItemPurchased += HandlePurchased;
            stall.onOpened.AddListener(Sync);
            if (areasResolved) ListenToGates(true);
        }

        private void OnDisable()
        {
            if (stall == null) return;
            stall.ItemPurchased -= HandlePurchased;
            stall.onOpened.RemoveListener(Sync);
            ListenToGates(false);
        }

        private void Start()
        {
            ResolveAreas();
            ListenToGates(true);
            Sync();
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

        /// <summary>อัปเดตสินค้าหม้อทุกชิ้น: หม้อที่ปลดแล้วขึ้นว่าซื้อแล้ว หม้อที่โซนยังไม่เปิดขึ้นว่าล็อก</summary>
        private void Sync()
        {
            if (stall == null) return;
            bool changed = false;
            foreach (Binding b in bindings)
            {
                ShopItemData item = stall.FindItem(b.itemId);
                if (item == null) continue;

                CookingPot pot = FindPot(b.potIngredient);
                bool owned = pot != null && pot.IsUnlocked;
                if (owned)
                {
                    int stock = Mathf.Max(1, item.stock);
                    if (item.stock <= 0) item.stock = 1;
                    if (item.sold < stock)
                    {
                        item.sold = stock;
                        changed = true;
                    }
                }

                if (ApplyAreaLock(b, item, owned)) changed = true;
            }
            if (changed && stall.IsOpen) stall.RebuildList();
        }

        /// <summary>ล็อก/ปลดสินค้าตามประตูโซน คืน true ถ้าสถานะเปลี่ยน (สินค้าที่ตั้ง Locked ไว้เองใน Inspector จะไม่ถูกแตะ)</summary>
        private bool ApplyAreaLock(Binding b, ShopItemData item, bool owned)
        {
            bool shouldLock = lockUntilAreaOpen && !owned && b.gate != null && !b.gate.IsUnlocked;
            if (shouldLock == b.lockedByArea) return false;

            if (shouldLock)
            {
                if (item.locked) return false; // ล็อกอยู่แล้วด้วยเหตุผลอื่น (เช่น SOON)

                b.savedLockedText = item.lockedText;
                b.savedDescription = item.description;
                item.locked = true;
                if (!string.IsNullOrEmpty(areaLockedText)) item.lockedText = areaLockedText;
                if (!string.IsNullOrEmpty(areaLockedDescription) && !string.IsNullOrEmpty(b.areaName))
                {
                    item.description = string.Format(areaLockedDescription, b.areaName);
                }
            }
            else
            {
                item.locked = false;
                item.lockedText = b.savedLockedText;
                item.description = b.savedDescription;
            }

            b.lockedByArea = shouldLock;
            return true;
        }

        /// <summary>หาว่าวัตถุดิบของแต่ละหม้อมาจากโซนไหน แล้วจำประตูของโซนนั้นไว้ (โซนที่ไม่มีประตู = ไม่ล็อก)</summary>
        private void ResolveAreas()
        {
            areasResolved = true;
            HuntArea[] areas = FindObjectsByType<HuntArea>(FindObjectsInactive.Include);
            foreach (Binding b in bindings)
            {
                b.gate = null;
                b.areaName = "";
                foreach (HuntArea area in areas)
                {
                    if (area == null || !area.DropsMeat(b.potIngredient)) continue;

                    // มีโซนที่ล่าได้อยู่แล้วโดยไม่ต้องเปิดประตู = ไม่ต้องล็อก
                    if (area.Gate == null)
                    {
                        b.gate = null;
                        b.areaName = "";
                        break;
                    }
                    if (b.gate == null)
                    {
                        b.gate = area.Gate;
                        b.areaName = area.AreaName;
                    }
                }
            }
        }

        private void ListenToGates(bool listen)
        {
            foreach (Binding b in bindings)
            {
                if (b.gate == null) continue;
                b.gate.onUnlocked.RemoveListener(Sync);
                if (listen && !b.gate.IsUnlocked) b.gate.onUnlocked.AddListener(Sync);
            }
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
