using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Collider2D))]
    public class FoodSellCounter : MonoBehaviour
    {
        [Header("Counter Settings (ปรับได้ใน Inspector)")]
        [Tooltip("จำนวนจานอาหารสูงสุดที่วางบนโต๊ะนี้ได้")]
        [SerializeField] private int maxPlates = 3;

        [Tooltip("จุดที่ลูกค้าจะเดินมาต่อแถว/ยืนรอรับอาหาร")]
        [SerializeField] private Transform customerWaitPoint;

        [Tooltip("ตำแหน่งวางจานบนโต๊ะแต่ละช่อง (ถ้ามีหลายช่อง ลาก Transform มาใส่)")]
        [SerializeField] private Transform[] plateSlots;

        [Header("Interaction Settings")]
        [Tooltip("ต้องกดปุ่ม E เพื่อวางอาหารบนโต๊ะ")]
        [SerializeField] private bool requireKeyPress = true;

        [Header("Events")]
        public UnityEvent<int> onDishesCountChanged;
        public UnityEvent<Item> onDishSold;

        private readonly List<Item> dishesOnCounter = new List<Item>();
        private bool isPlayerInRange;
        private PlayerCarry cachedPlayerCarry;

        public int DishesCount => dishesOnCounter.Count;
        public int MaxPlates => maxPlates;
        public Transform CustomerWaitPoint => customerWaitPoint != null ? customerWaitPoint : transform;

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            // ถ้าไม่ได้กำหนดจุดยืนของลูกค้า ให้สร้างจุดไว้ข้างหน้าโต๊ะอัตโนมัติ
            if (customerWaitPoint == null)
            {
                var point = new GameObject("CustomerWaitPoint");
                point.transform.SetParent(transform);
                point.transform.localPosition = new Vector3(0f, -1f, 0f);
                customerWaitPoint = point.transform;
            }
        }

        private void Update()
        {
            if (isPlayerInRange && requireKeyPress)
            {
                CheckInteractInput();
            }
        }

        private void CheckInteractInput()
        {
            bool interactPressed = false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                interactPressed = true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (!interactPressed && Input.GetKeyDown(KeyCode.E))
            {
                interactPressed = true;
            }
#endif

            if (interactPressed && cachedPlayerCarry != null)
            {
                Interact(cachedPlayerCarry);
            }
        }

        /// <summary>
        /// เมื่อ Player ปฏิสัมพันธ์กับโต๊ะขายอาหาร
        /// </summary>
        public void Interact(PlayerCarry playerCarry)
        {
            if (playerCarry == null) return;

            // 1. ถ้าบนโต๊ะยังมีที่ว่าง และ Player มีอาหารต้มสุกอยู่บนหลัง ให้นำมาวางบนโต๊ะ
            if (dishesOnCounter.Count < maxPlates)
            {
                // ตรวจหาอาหารสุก (CookedChicken หรือ CookedPig)
                Item cookedItem = null;
                if (playerCarry.HasItemOfType(ItemType.CookedChicken))
                {
                    cookedItem = playerCarry.PopItemOfType(ItemType.CookedChicken);
                }
                else if (playerCarry.HasItemOfType(ItemType.CookedPig))
                {
                    cookedItem = playerCarry.PopItemOfType(ItemType.CookedPig);
                }

                if (cookedItem != null)
                {
                    PlaceDishOnCounter(cookedItem);
                    return;
                }
            }

            // 2. ถ้า Player ไม่ได้ถืออาหารสุก แต่มีอาหารบนโต๊ะ และ Player ยังแบกของได้ สามารถหยิบอาหารกลับคืนได้
            if (dishesOnCounter.Count > 0 && playerCarry.CanCarry())
            {
                Item returnItem = TakeDishFromCounter();
                if (returnItem != null)
                {
                    playerCarry.PickUpItem(returnItem);
                }
            }
        }

        /// <summary>
        /// วางจานอาหารลงบนโต๊ะ
        /// </summary>
        public void PlaceDishOnCounter(Item dish)
        {
            if (dish == null) return;

            dishesOnCounter.Add(dish);
            dish.SetCarriedState(true);

            // หาตำแหน่ง Slot ที่จะวาง
            int slotIndex = dishesOnCounter.Count - 1;
            if (plateSlots != null && slotIndex < plateSlots.Length && plateSlots[slotIndex] != null)
            {
                dish.transform.SetParent(plateSlots[slotIndex]);
                dish.transform.localPosition = Vector3.zero;
            }
            else
            {
                dish.transform.SetParent(transform);
                dish.transform.localPosition = new Vector3((slotIndex - 0.5f) * 0.6f, 0.2f, -0.01f);
            }

            dish.transform.localRotation = Quaternion.identity;
            onDishesCountChanged?.Invoke(dishesOnCounter.Count);
        }

        /// <summary>
        /// หยิบจานอาหารออกจากโต๊ะ (เช่น Player มาหยิบคืน)
        /// </summary>
        public Item TakeDishFromCounter()
        {
            if (dishesOnCounter.Count == 0) return null;

            int lastIdx = dishesOnCounter.Count - 1;
            Item dish = dishesOnCounter[lastIdx];
            dishesOnCounter.RemoveAt(lastIdx);

            dish.transform.SetParent(null);
            dish.SetCarriedState(false);

            onDishesCountChanged?.Invoke(dishesOnCounter.Count);
            return dish;
        }

        /// <summary>
        /// ให้ลูกค้าหยิบซื้ออาหารจากโต๊ะ
        /// </summary>
        public Item CustomerBuyDish(ItemType? preferredType = null)
        {
            if (dishesOnCounter.Count == 0) return null;

            Item selectedDish = null;

            // ถ้าลูกค้าต้องการอาหารเฉพาะเจาะจง ให้ค้นหาประเภทนั้นก่อน
            if (preferredType.HasValue)
            {
                for (int i = 0; i < dishesOnCounter.Count; i++)
                {
                    if (dishesOnCounter[i].Type == preferredType.Value)
                    {
                        selectedDish = dishesOnCounter[i];
                        dishesOnCounter.RemoveAt(i);
                        break;
                    }
                }
            }

            // ถ้าหาไม่พบ หรือลูกค้ากินอะไรก็ได้ ให้หยิบจานแรกสุด
            if (selectedDish == null && dishesOnCounter.Count > 0)
            {
                selectedDish = dishesOnCounter[0];
                dishesOnCounter.RemoveAt(0);
            }

            if (selectedDish != null)
            {
                selectedDish.transform.SetParent(null);
                onDishesCountChanged?.Invoke(dishesOnCounter.Count);
                onDishSold?.Invoke(selectedDish);
            }

            return selectedDish;
        }

        public bool HasFoodAvailable(ItemType? preferredType = null)
        {
            if (dishesOnCounter.Count == 0) return false;
            if (!preferredType.HasValue) return true;

            foreach (var dish in dishesOnCounter)
            {
                if (dish != null && dish.Type == preferredType.Value) return true;
            }
            return false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<PlayerCarry>(out var playerCarry))
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                if (!requireKeyPress)
                {
                    Interact(playerCarry);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.TryGetComponent<PlayerCarry>(out var playerCarry))
            {
                if (cachedPlayerCarry == playerCarry)
                {
                    isPlayerInRange = false;
                    cachedPlayerCarry = null;
                }
            }
        }
    }
}
