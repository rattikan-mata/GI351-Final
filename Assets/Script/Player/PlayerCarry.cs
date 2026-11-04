using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    public class PlayerCarry : MonoBehaviour
    {
        [Header("Carry Capacity Settings")]
        [Tooltip("จำนวนชิ้นสูงสุดที่ Player สามารถแบกได้ (ปรับได้ใน Inspector)")]
        [SerializeField] private int maxCarryCapacity = 3;

        [Header("Visual Stack Settings")]
        [Tooltip("ตำแหน่งจุดเริ่มต้นที่ของจะวางซ้อนบนหลัง/หัว Player")]
        [SerializeField] private Transform carryPoint;

        [Tooltip("ระยะห่างของการซ้อนของแต่ละชิ้น (แกน X, Y, Z)")]
        [SerializeField] private Vector3 stackOffset = new Vector3(0f, 0.45f, -0.02f);

        [Header("Controls")]
        [Tooltip("ปุ่มสำหรับกดทิ้งของชิ้นบนสุด (เช่น ปุ่ม Q)")]
        [SerializeField] private bool enableDropKey = true;

        [Header("Events")]
        public UnityEvent<int, int> onCarryCountChanged; // (currentCount, maxCapacity)

        private readonly List<Item> carriedItems = new List<Item>();
        private SpriteRenderer playerRenderer;

        public int MaxCarryCapacity => maxCarryCapacity;
        public int CurrentCarriedCount => carriedItems.Count;
        public IReadOnlyList<Item> CarriedItems => carriedItems;

        /// <summary>
        /// ระยะซ้อนของกองในพิกัดโลก (ให้หม้อ/โต๊ะกองของด้วยระยะเดียวกับที่แบก)
        /// </summary>
        public Vector3 WorldStackOffset => carryPoint != null ? carryPoint.TransformVector(stackOffset) : stackOffset;

        private void Awake()
        {
            playerRenderer = GetComponentInChildren<SpriteRenderer>();

            // ถ้าไม่ได้กำหนด carryPoint ใน Inspector ให้สร้างจุด CarryPoint ให้อัตโนมัติ
            if (carryPoint == null)
            {
                var point = new GameObject("CarryPoint");
                point.transform.SetParent(transform);
                point.transform.localPosition = new Vector3(0f, 0.6f, -0.01f);
                carryPoint = point.transform;
            }
        }

        private void Start()
        {
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
        }

        private void Update()
        {
            if (enableDropKey)
            {
                CheckDropInput();
            }
        }

        private void CheckDropInput()
        {
            bool dropPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                dropPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!dropPressed && Input.GetKeyDown(KeyCode.Q))
            {
                dropPressed = true;
            }
#endif

            if (dropPressed && carriedItems.Count > 0)
            {
                DropTopItem();
            }
        }

        /// <summary>
        /// เช็คว่ายังสามารถแบกของเพิ่มได้หรือไม่
        /// </summary>
        public bool CanCarry()
        {
            return carriedItems.Count < maxCarryCapacity;
        }

        /// <summary>
        /// เก็บไอเทมขึ้นมาแบกไว้บนหลัง
        /// </summary>
        public bool PickUpItem(Item item)
        {
            if (item == null || !CanCarry()) return false;

            carriedItems.Add(item);
            item.SetCarriedState(true);

            // เซ็ต Parent มาที่ CarryPoint ของตัว Player
            item.transform.SetParent(carryPoint);
            item.transform.localRotation = Quaternion.identity;

            UpdateStackPositions();
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
            return true;
        }

        /// <summary>
        /// นำไอเทมชิ้นบนสุดออกจากหลัง Player (ส่งให้หม้อต้ม หรือ วางบนโต๊ะขาย)
        /// </summary>
        public Item PopTopItem()
        {
            if (carriedItems.Count == 0) return null;

            int lastIndex = carriedItems.Count - 1;
            Item item = carriedItems[lastIndex];
            carriedItems.RemoveAt(lastIndex);

            item.transform.SetParent(null);
            item.SetCarriedState(false);

            UpdateStackPositions();
            onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
            return item;
        }

        /// <summary>
        /// หาและนำไอเทมประเภทที่ต้องการออกจากหลัง Player (เช่น หาเนื้อไก่เพื่อใส่หม้อต้ม)
        /// </summary>
        public Item PopItemOfType(ItemType type)
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                if (carriedItems[i].Type == type)
                {
                    Item item = carriedItems[i];
                    carriedItems.RemoveAt(i);

                    item.transform.SetParent(null);
                    item.SetCarriedState(false);

                    UpdateStackPositions();
                    onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// ตรวจสอบว่าในหลังมีไอเทมประเภทนี้หรือไม่
        /// </summary>
        public bool HasItemOfType(ItemType type)
        {
            foreach (var item in carriedItems)
            {
                if (item.Type == type) return true;
            }
            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าไอเทมนี้เป็นอาหารปรุงสุกหรือไม่ (ไม่ใช่เนื้อดิบ)
        /// </summary>
        public static bool IsCookedFood(Item item)
        {
            if (item == null) return false;
            if (item is CookedDish) return true;
            if (item.Type == ItemType.CookedChicken || item.Type == ItemType.CookedPig) return true;
            if (item.Type != ItemType.ChickenMeat && item.Type != ItemType.PigMeat) return true;
            return false;
        }

        /// <summary>
        /// ตรวจสอบว่าในหลังมีอาหารปรุงสุกหรือไม่
        /// </summary>
        public bool HasAnyCookedFood()
        {
            foreach (var item in carriedItems)
            {
                if (IsCookedFood(item)) return true;
            }
            return false;
        }

        /// <summary>
        /// ดึงอาหารปรุงสุกชิ้นใดก็ได้ออกจากหลังผู้เล่น (สำหรับส่งให้จุดขาย)
        /// </summary>
        public Item PopAnyCookedFood()
        {
            for (int i = carriedItems.Count - 1; i >= 0; i--)
            {
                Item item = carriedItems[i];
                if (IsCookedFood(item))
                {
                    carriedItems.RemoveAt(i);
                    item.transform.SetParent(null);
                    item.SetCarriedState(false);

                    UpdateStackPositions();
                    onCarryCountChanged?.Invoke(carriedItems.Count, maxCarryCapacity);
                    return item;
                }
            }
            return null;
        }

        /// <summary>
        /// ดูข้อมูลไอเทมชิ้นบนสุดโดยไม่ต้องหยิบออก
        /// </summary>
        public Item PeekTopItem()
        {
            if (carriedItems.Count == 0) return null;
            return carriedItems[carriedItems.Count - 1];
        }

        /// <summary>
        /// ทิ้งของชิ้นบนสุดลงพื้นข้างหน้า Player
        /// </summary>
        public void DropTopItem()
        {
            Item item = PopTopItem();
            if (item != null)
            {
                Vector3 dropPos = transform.position + (Vector3)GetFacingDirection() * 0.8f;
                item.transform.position = dropPos;
                item.BlockPickupUntilExit(this);
            }
        }

        /// <summary>
        /// ล้างและทำลายไอเทมทั้งหมดที่ผู้เล่นกำลังแบกอยู่ (เช่น ตอนผู้เล่นตาย)
        /// ทำให้ของที่แบกอยู่หายไปทั้งหมด โดยไม่กระทบเงินหรือของอย่างอื่นในเกม
        /// </summary>
        public void ClearCarriedItems()
        {
            for (int i = 0; i < carriedItems.Count; i++)
            {
                if (carriedItems[i] != null)
                {
                    Destroy(carriedItems[i].gameObject);
                }
            }
            carriedItems.Clear();
            onCarryCountChanged?.Invoke(0, maxCarryCapacity);
        }

        private Vector2 GetFacingDirection()
        {
            if (TryGetComponent<PlayerMovement>(out var movement))
            {
                return movement.LastFacingDirection;
            }
            return Vector2.down;
        }

        /// <summary>
        /// จัดเรียงตำแหน่งและความสูง (Sorting Order) ของไอเทมทั้งหมดที่อยู่บนหลัง
        /// </summary>
        private void UpdateStackPositions()
        {
            int baseSortingOrder = playerRenderer != null ? playerRenderer.sortingOrder : 0;

            for (int i = 0; i < carriedItems.Count; i++)
            {
                Item item = carriedItems[i];
                if (item == null) continue;

                // ตำแหน่งซ้อนขึ้นไปทีละชั้น
                item.transform.localPosition = stackOffset * i;

                // ปรับ Sorting Order ให้ชิ้นที่อยู่สูงกว่า วาดทับชิ้นที่อยู่ต่ำกว่า
                if (item.SpriteRenderer != null)
                {
                    item.SpriteRenderer.sortingOrder = baseSortingOrder + 1 + i;
                }
            }
        }
    }
}
