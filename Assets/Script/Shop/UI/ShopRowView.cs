using UnityEngine;
using UnityEngine.UI;

namespace GI351.KitchenGame
{
    /// <summary>
    /// แถวสินค้าต้นแบบใน prefab หน้าร้าน โค้ดก๊อปแถวนี้ 1 แถวต่อสินค้า 1 ชิ้น
    /// แต่งหน้าตาแถวนี้ได้เลย (ขนาด สี ตำแหน่ง ฟอนต์) ข้อความและสีปุ่มโค้ดเขียนทับตอนเล่น
    /// </summary>
    public class ShopRowView : MonoBehaviour
    {
        [Tooltip("ชื่อสินค้า")]
        public Text nameText;

        [Tooltip("คำอธิบายใต้ชื่อ (ซ่อนเองถ้าสินค้าไม่มีคำอธิบาย)")]
        public Text descText;

        [Tooltip("ราคา")]
        public Text priceText;

        [Tooltip("ปุ่มซื้อ")]
        public Button buyButton;

        [Tooltip("รูปพื้นปุ่มซื้อ (เปลี่ยนสีตามซื้อได้/ไม่ได้)")]
        public Image buyImage;

        [Tooltip("ข้อความบนปุ่มซื้อ")]
        public Text buyText;
    }
}
