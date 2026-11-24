using UnityEngine;
using UnityEngine.Rendering;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ตัวที่เดินไปมา (ผู้เล่น ลูกค้า ลูกจ้าง สัตว์): ทั้งตัวรวมของที่ถือเป็นก้อนเดียว (Sorting Group)
    /// แล้วเรียงชั้นตามตำแหน่งเท้าทุกเฟรม เดินอยู่หลังโต๊ะ = ถูกโต๊ะบัง, เดินอยู่หน้าโต๊ะ = ทับโต๊ะ
    /// </summary>
    [RequireComponent(typeof(SortingGroup))]
    [DefaultExecutionOrder(1000)]
    public class YSortActor : MonoBehaviour
    {
        [Tooltip("ภาพหลักของตัว (ใช้ขอบล่างของภาพเป็นตำแหน่งเท้า) ว่างไว้ = หาเองจากลูก")]
        [SerializeField] private SpriteRenderer footRenderer;

        [Tooltip("ยกจุดเท้าขึ้นจากขอบล่างของภาพ (หน่วยโลก) ภาพที่มีขอบใสด้านล่างให้ใส่ค่านี้")]
        [SerializeField] private float footLift = 0.05f;

        private SortingGroup group;

        /// <summary>ตำแหน่ง Y ของเท้าในโลก</summary>
        public float FootY => footRenderer != null ? footRenderer.bounds.min.y + footLift : transform.position.y;

        /// <summary>จุดกลางเท้า (ใช้เช็กว่ายืนอยู่ในวงหรือยัง ให้ตรงกับเท้าที่เห็น ไม่ใช่กลางตัว)</summary>
        public Vector2 FootPosition => new Vector2(transform.position.x, FootY + 0.1f);

        private void Awake()
        {
            group = GetComponent<SortingGroup>();
            if (footRenderer == null) footRenderer = GetComponentInChildren<SpriteRenderer>();
            Apply();
        }

        private void LateUpdate()
        {
            Apply();
        }

        private void Apply()
        {
            if (group == null) return;
            int order = YSort.OrderAt(FootY);
            if (group.sortingOrder != order) group.sortingOrder = order;
        }
    }
}
