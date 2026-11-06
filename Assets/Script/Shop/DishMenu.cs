using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เมนูอาหารที่ลูกค้าสั่งได้ในตอนนี้ ขึ้นกับว่าเครื่องทำอาหารไหนถูกปลดล็อก (ซื้อ) แล้วบ้าง
    /// - หม้อไก่ที่ปลดแล้ว  -> ไก่ต้ม
    /// - หม้อหมูที่ปลดแล้ว  -> หมูต้ม
    /// - เครื่องผสมเนื้อที่ปลดแล้ว -> อาหารผสม
    /// (อาหารพิเศษจากโจรไม่อยู่ในเมนูสั่ง แต่ลูกค้าทุกคนกินได้)
    /// </summary>
    public static class DishMenu
    {
        public static List<ItemType> GetAvailableDishes()
        {
            var result = new List<ItemType>();

            var pots = CookingPot.AllPots;
            if (pots.Count == 0)
            {
#if UNITY_2023_1_OR_NEWER
                var found = Object.FindObjectsByType<CookingPot>(FindObjectsSortMode.None);
#else
                var found = Object.FindObjectsOfType<CookingPot>();
#endif
                if (found != null)
                {
                    foreach (var p in found)
                    {
                        if (p != null && !pots.Contains(p)) pots.Add(p);
                    }
                }
            }

            foreach (var pot in pots)
            {
                if (pot == null || !pot.IsUnlocked) continue;

                ItemType dish = pot.IsPigPot() ? ItemType.CookedPig : ItemType.CookedChicken;
                if (!result.Contains(dish)) result.Add(dish);
            }

            var mixers = MeatMixer.AllMixers;
            if (mixers.Count == 0)
            {
#if UNITY_2023_1_OR_NEWER
                var found = Object.FindObjectsByType<MeatMixer>(FindObjectsSortMode.None);
#else
                var found = Object.FindObjectsOfType<MeatMixer>();
#endif
                if (found != null)
                {
                    foreach (var m in found)
                    {
                        if (m != null && !mixers.Contains(m)) mixers.Add(m);
                    }
                }
            }

            foreach (var mixer in mixers)
            {
                if (mixer != null && mixer.IsUnlocked && !result.Contains(ItemType.MixedDish))
                {
                    result.Add(ItemType.MixedDish);
                }
            }

            return result;
        }

        /// <summary>สุ่มเมนูที่สั่งได้ ถ้าไม่มีเครื่องไหนเลยจะคืน null (กินอะไรก็ได้)</summary>
        public static ItemType? PickRandomDish()
        {
            var list = GetAvailableDishes();
            if (list.Count == 0) return null;
            return list[Random.Range(0, list.Count)];
        }

        public static string GetShortName(ItemType dish)
        {
            switch (dish)
            {
                case ItemType.CookedChicken: return "Chicken";
                case ItemType.CookedPig: return "Pork";
                case ItemType.MixedDish: return "Mix";
                default: return "Any";
            }
        }

        public static Color GetColor(ItemType dish)
        {
            switch (dish)
            {
                case ItemType.CookedChicken: return new Color(1f, 0.85f, 0.2f, 1f);
                case ItemType.CookedPig: return new Color(1f, 0.55f, 0.65f, 1f);
                case ItemType.MixedDish: return new Color(0.8f, 0.6f, 1f, 1f);
                default: return Color.white;
            }
        }
    }
}

