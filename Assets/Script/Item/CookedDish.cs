using UnityEngine;

namespace GI351.KitchenGame
{
    public class CookedDish : Item
    {
        [Header("Cooked Dish Details")]
        [Tooltip("ชื่อเมนูอาหาร")]
        [SerializeField] private string dishName = "Delicious Dish";

        [Tooltip("คำอธิบายหรือดาวความอร่อย")]
        [SerializeField] private int dishQuality = 1;

        public string DishName => dishName;
        public int DishQuality => dishQuality;
    }
}
