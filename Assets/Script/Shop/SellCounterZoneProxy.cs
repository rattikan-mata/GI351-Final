using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พร็อกซีส่งต่อ Event การเหยียบจาก FoodDepositZone ไปให้ FoodSellCounter
    /// </summary>
    public class SellCounterZoneProxy : MonoBehaviour
    {
        public FoodSellCounter counter;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (counter != null) counter.OnZoneTriggerEnter(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (counter != null) counter.OnZoneTriggerStay(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (counter != null) counter.OnZoneTriggerExit(other);
        }
    }
}
