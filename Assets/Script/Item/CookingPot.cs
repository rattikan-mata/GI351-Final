using System.Collections;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GI351.KitchenGame
{
    public enum PotState
    {
        Empty,      // ว่าง รอใส่เนื้อ
        Cooking,    // กำลังต้ม
        Done        // ต้มเสร็จแล้ว รอผู้เล่นมาหยิบ
    }

    [RequireComponent(typeof(Collider2D))]
    public class CookingPot : MonoBehaviour
    {
        [Header("Recipe Settings (ตั้งค่าสูตรอาหาร)")]
        [Tooltip("ชื่อของหม้อ (เช่น หม้อต้มไก่ หรือ หม้อต้มหมู)")]
        [SerializeField] private string potName = "Chicken Boiling Pot";

        [Tooltip("วัตถุดิบที่ต้องใช้ใส่ลงหม้อ")]
        [SerializeField] private ItemType requiredIngredient = ItemType.ChickenMeat;

        [Tooltip("Prefab อาหารปรุงสุกที่จะได้รับเมื่อต้มเสร็จ")]
        [SerializeField] private GameObject cookedDishPrefab;

        [Tooltip("ระยะเวลาในการต้ม (วินาที)")]
        [SerializeField] private float cookingDuration = 3f;

        [Header("Interaction Settings")]
        [Tooltip("ถ้าติ๊กถูก ต้องกดปุ่ม E เพื่อมีปฏิสัมพันธ์ ถ้าไม่ติ๊ก แค่เดินมาใกล้ๆ จะทำงานอัตโนมัติ")]
        [SerializeField] private bool requireKeyPress = true;

        [Header("Visual Feedback & Indicators")]
        [Tooltip("GameObject แสดงเอฟเฟกต์ควัน/ฟองน้ำเดือดขณะต้ม")]
        [SerializeField] private GameObject cookingEffect;

        [Tooltip("GameObject แสดงเมื่ออาหารพร้อมเสิร์ฟ (เช่น ไอคอนเครื่องหมายตกใจ หรือ จานอาหาร)")]
        [SerializeField] private GameObject readyIndicator;

        [Tooltip("Transform ตำแหน่งแสดงอาหารที่ต้มเสร็จบนหม้อ")]
        [SerializeField] private Transform foodDisplayPoint;

        [Header("Events")]
        public UnityEvent onCookingStarted;
        public UnityEvent onCookingFinished;
        public UnityEvent onFoodCollected;

        private PotState currentState = PotState.Empty;
        private float currentCookingTimer;
        private bool isPlayerInRange;
        private PlayerCarry cachedPlayerCarry;
        private GameObject spawnedDishDisplay;

        public PotState State => currentState;
        public string PotName => potName;
        public float CookingProgress => currentState == PotState.Cooking ? (currentCookingTimer / cookingDuration) : (currentState == PotState.Done ? 1f : 0f);

        private void Awake()
        {
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (currentState == PotState.Cooking)
            {
                currentCookingTimer += Time.deltaTime;
                if (currentCookingTimer >= cookingDuration)
                {
                    FinishCooking();
                }
            }

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

        public void Interact(PlayerCarry playerCarry)
        {
            if (playerCarry == null) return;

            switch (currentState)
            {
                case PotState.Empty:
                    // ตรวจสอบว่าในหลัง Player มีเนื้อชนิดที่ต้องการหรือไม่
                    if (playerCarry.HasItemOfType(requiredIngredient))
                    {
                        Item meatItem = playerCarry.PopItemOfType(requiredIngredient);
                        if (meatItem != null)
                        {
                            Destroy(meatItem.gameObject); // ทำลายเนื้อดิบเพื่อเอาลงหม้อ
                            StartCooking();
                        }
                    }
                    break;

                case PotState.Cooking:
                    // กำลังต้มอยู่ ยังทำอะไรไม่ได้
                    break;

                case PotState.Done:
                    // ต้มเสร็จแล้ว เช็คว่า Player แบกของเพิ่มได้ไหม
                    if (playerCarry.CanCarry() && cookedDishPrefab != null)
                    {
                        if (spawnedDishDisplay != null)
                        {
                            Destroy(spawnedDishDisplay);
                        }

                        // เสกจานอาหารสุกแล้วส่งเข้าหลัง Player ทันที
                        Vector3 spawnPos = foodDisplayPoint != null ? foodDisplayPoint.position : transform.position;
                        GameObject dishObj = Instantiate(cookedDishPrefab, spawnPos, Quaternion.identity);
                        if (dishObj.TryGetComponent<Item>(out var dishItem))
                        {
                            playerCarry.PickUpItem(dishItem);
                        }

                        currentState = PotState.Empty;
                        UpdateVisuals();
                        onFoodCollected?.Invoke();
                    }
                    break;
            }
        }

        private void StartCooking()
        {
            currentState = PotState.Cooking;
            currentCookingTimer = 0f;
            UpdateVisuals();
            onCookingStarted?.Invoke();
        }

        private void FinishCooking()
        {
            currentState = PotState.Done;
            currentCookingTimer = 0f;

            // แสดงตัวอย่างจานอาหารบนหม้อ
            if (cookedDishPrefab != null && foodDisplayPoint != null)
            {
                spawnedDishDisplay = Instantiate(cookedDishPrefab, foodDisplayPoint.position, Quaternion.identity, foodDisplayPoint);
                if (spawnedDishDisplay.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
                if (spawnedDishDisplay.TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;
            }

            UpdateVisuals();
            onCookingFinished?.Invoke();
        }

        private void UpdateVisuals()
        {
            if (cookingEffect != null)
            {
                cookingEffect.SetActive(currentState == PotState.Cooking);
            }

            if (readyIndicator != null)
            {
                readyIndicator.SetActive(currentState == PotState.Done);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<PlayerCarry>(out var playerCarry))
            {
                isPlayerInRange = true;
                cachedPlayerCarry = playerCarry;

                // ถ้าไม่ต้องกดปุ่ม ให้ปฏิสัมพันธ์ทันที
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
