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
                var found = Object.FindObjectsByType<CookingPot>();
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

                ItemType dish = pot.OutputDishType;
                if (!result.Contains(dish)) result.Add(dish);
            }

            var mixers = MeatMixer.AllMixers;
            if (mixers.Count == 0)
            {
#if UNITY_2023_1_OR_NEWER
                var found = Object.FindObjectsByType<MeatMixer>();
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

            // สั่งได้เฉพาะเมนูที่มีชั้นขาย (ถ้าฉากยังไม่มีชั้นเลย ไม่ต้องกรอง)
            if (FoodSellCounter.All.Count > 0)
            {
                result.RemoveAll(d => !FoodSellCounter.AnyCounterSells(d));
            }

            return result;
        }

        /// <summary>
        /// สุ่มเมนูที่สั่งได้ ถ้าไม่มีเครื่องไหนเลยจะคืน null (กินอะไรก็ได้)
        /// เลือกเมนูที่ชั้นขายยังมีที่ว่างให้ยืนรอก่อน (ชั้นหมูคนเต็ม ลูกค้าใหม่จะอยากกินไก่แทน)
        /// </summary>
        public static ItemType? PickRandomDish()
        {
            var list = GetAvailableDishes();
            if (list.Count == 0) return null;

            var withRoom = list.FindAll(d =>
            {
                FoodSellCounter c = FoodSellCounter.FindCounterFor(d);
                return c != null && !c.IsQueueFull;
            });
            if (withRoom.Count > 0) list = withRoom;

            return list[Random.Range(0, list.Count)];
        }

        public static string GetShortName(ItemType dish) => FoodTypes.ShortName(dish);

        public static Color GetColor(ItemType dish) => FoodTypes.DishColor(dish);
    }
}

