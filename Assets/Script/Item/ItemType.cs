using UnityEngine;

namespace GI351.KitchenGame
{
    // เพิ่มค่าใหม่ต่อท้ายเสมอ (Unity เก็บเป็นตัวเลข แทรกกลางแล้ว Prefab/ฉากที่ตั้งไว้จะเลื่อนผิดตัว)
    public enum ItemType
    {
        ChickenMeat,      // เนื้อไก่ดิบ
        PigMeat,          // เนื้อหมูดิบ
        CookedChicken,    // อาหารไก่ต้ม
        CookedPig,        // อาหารหมูต้ม
        UnconsciousThief, // โจรที่สลบ (ยกขึ้นหลังได้ / เอาไปยัดใส่หม้อต้มได้)
        SpecialDish,      // อาหารพิเศษราคาแพง x10 (ทำจากโจรที่สลบ)
        MixedDish,        // อาหารผสมเนื้อจากเครื่องผสมเนื้อ (ราคาเนื้อรวมกัน + 20%) นับเป็นอาหารปรุงสุก
        FrogMeat,         // เนื้อกบดิบ (สัตว์ตัวที่ 3 โซน Deep Forest)
        CookedFrog        // อาหารกบ
    }

    /// <summary>
    /// ตารางกลางของเนื้อดิบ -> อาหาร  เพิ่มสัตว์ใหม่: เพิ่มค่าใน ItemType แล้วเพิ่มคู่ในตาราง Recipes ที่นี่ที่เดียว
    /// (หม้อ เมนูลูกค้า และการแบกของ อ่านจากตารางนี้)
    /// </summary>
    public static class FoodTypes
    {
        private struct Recipe
        {
            public ItemType raw;
            public ItemType dish;
            public string shortName;
            public Color color;
        }

        private static readonly Recipe[] Recipes =
        {
            new Recipe { raw = ItemType.ChickenMeat, dish = ItemType.CookedChicken, shortName = "Chicken", color = new Color(1f, 0.85f, 0.2f, 1f) },
            new Recipe { raw = ItemType.PigMeat,     dish = ItemType.CookedPig,     shortName = "Pork",    color = new Color(1f, 0.55f, 0.65f, 1f) },
            new Recipe { raw = ItemType.FrogMeat,    dish = ItemType.CookedFrog,    shortName = "Frog",    color = new Color(0.45f, 0.85f, 0.4f, 1f) },
        };

        /// <summary>เป็นเนื้อดิบจากสัตว์หรือไม่</summary>
        public static bool IsRawMeat(ItemType type)
        {
            foreach (Recipe r in Recipes) if (r.raw == type) return true;
            return false;
        }

        /// <summary>เป็นอาหารปรุงสุกที่วางขายได้หรือไม่ (รวมอาหารพิเศษ/อาหารผสม)</summary>
        public static bool IsCookedDish(ItemType type)
        {
            if (type == ItemType.SpecialDish || type == ItemType.MixedDish) return true;
            foreach (Recipe r in Recipes) if (r.dish == type) return true;
            return false;
        }

        /// <summary>อาหารที่ได้จากเนื้อดิบนี้ (ไม่ใช่เนื้อดิบ = false)</summary>
        public static bool TryGetDish(ItemType raw, out ItemType dish)
        {
            foreach (Recipe r in Recipes)
            {
                if (r.raw == raw) { dish = r.dish; return true; }
            }
            dish = ItemType.CookedChicken;
            return false;
        }

        /// <summary>ชื่อสั้นของอาหาร (ไว้โชว์บนป้าย)</summary>
        public static string ShortName(ItemType dish)
        {
            if (dish == ItemType.MixedDish) return "Mix";
            foreach (Recipe r in Recipes) if (r.dish == dish) return r.shortName;
            return "Any";
        }

        /// <summary>สีประจำอาหาร</summary>
        public static Color DishColor(ItemType dish)
        {
            if (dish == ItemType.MixedDish) return new Color(0.8f, 0.6f, 1f, 1f);
            foreach (Recipe r in Recipes) if (r.dish == dish) return r.color;
            return Color.white;
        }
    }
}
