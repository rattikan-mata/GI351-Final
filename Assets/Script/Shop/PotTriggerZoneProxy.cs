using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พร็อกซีส่งต่อ Event การเหยียบจากจุดยืนของหม้อต้ม ไปให้ CookingPot
    /// </summary>
    public class PotTriggerZoneProxy : MonoBehaviour
    {
        public CookingPot pot;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerEnter(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerStay(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (pot != null) pot.OnZoneTriggerExit(other);
        }
    }
}
