using UnityEngine;

namespace GI351.KitchenGame
{
    public enum ItemType
    {
        ChickenMeat,      // เนื้อไก่ดิบ
        PigMeat,          // เนื้อหมูดิบ
        CookedChicken,    // อาหารไก่ต้ม
        CookedPig,        // อาหารหมูต้ม
        UnconsciousThief, // โจรที่สลบ (ยกขึ้นหลังได้ / เอาไปยัดใส่หม้อต้มได้)
        SpecialDish,      // อาหารพิเศษราคาแพง x10 (ทำจากโจรที่สลบ)
        MixedDish         // อาหารผสมเนื้อจากเครื่องผสมเนื้อ (ราคาเนื้อรวมกัน + 20%) นับเป็นอาหารปรุงสุก
    }
}
