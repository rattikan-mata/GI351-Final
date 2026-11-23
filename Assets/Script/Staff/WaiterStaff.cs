using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พนักงานเสิร์ฟ: เดินไปหยิบอาหารที่ต้มเสร็จจากถาดของหม้อ แล้วเอาไปวางบนชั้นขายของเมนูนั้นให้
    /// ผู้เล่นแค่เอาเนื้อมาใส่หม้อแล้วไปล่าต่อได้เลย ไม่ต้องรออาหารสุก
    /// อ่อนกว่าผู้เล่น: เดินช้ากว่า และถือได้ครั้งละไม่กี่จาน (ตั้งได้ด้านล่าง)
    /// เลือกงานจากชั้นที่อาหารเหลือน้อยสุดก่อน ถ้าชั้นเต็มจะยืนรอจนมีที่ว่าง
    /// </summary>
    public class WaiterStaff : StaffBase
    {
        [Header("Work (การทำงาน)")]
        [Tooltip("ความเร็วเดิน (ผู้เล่นเดิน 5)")]
        [SerializeField] private float moveSpeed = 3f;

        [Tooltip("ช่วงเวลาระหว่างก้าวของเสียงเดินพนักงาน (วินาที) ตั้ง 0 เพื่อปิด")]
        [SerializeField] private float stepInterval = 0.38f;

        [Tooltip("ถืออาหารได้ครั้งละกี่จาน")]
        [Min(1)] [SerializeField] private int carryCapacity = 6;

        [Tooltip("หยิบ/วางอาหาร 1 จานทุกกี่วินาที")]
        [Min(0.02f)] [SerializeField] private float handleInterval = 0.15f;

        [Tooltip("ถ้าหม้อยังต้มอยู่ จะยืนรอที่ถาดจนได้อย่างน้อยกี่จานก่อนค่อยเดินไปส่ง (กันเดินไปกลับทีละจาน)")]
        [Min(1)] [SerializeField] private int minLoad = 3;

        [Tooltip("ยืนรอที่ถาดได้นานสุดกี่วินาที แล้วไปส่งเท่าที่มี")]
        [Min(0f)] [SerializeField] private float maxWaitAtPot = 6f;

        [Tooltip("ต้องเข้าใกล้ถาด/ชั้นเท่าไหร่ถึงจะหยิบ/วางได้ (เมตร)")]
        [Min(0.1f)] [SerializeField] private float reachDistance = 0.9f;

        [Tooltip("ตอนว่างงาน หางานใหม่ทุกกี่วินาที")]
        [Min(0.05f)] [SerializeField] private float idleCheckInterval = 0.4f;

        [Header("Carry (อาหารที่ถืออยู่)")]
        [Tooltip("ตำแหน่งจานใบแรกเทียบกับตัวพนักงาน (เมตร)")]
        [SerializeField] private Vector2 carryOffset = new Vector2(0f, 0.55f);

        [Tooltip("ระยะซ้อนของจานแต่ละใบ (เมตร)")]
        [SerializeField] private float carryStackSpacing = 0.12f;

        private enum State { Idle, ToPot, Loading, ToShelf, Unloading }

        private readonly List<Item> carried = new List<Item>();
        private State state = State.Idle;
        private CookingPot targetPot;
        private FoodSellCounter targetShelf;
        private float timer;
        private float loadStartTime;
        private float stepTimer;
        private bool movedThisFrame;

        /// <summary>จำนวนจานที่ถืออยู่</summary>
        public int CarriedCount => carried.Count;

        /// <summary>เริ่มงาน: โผล่ที่ตำแหน่งนี้แล้วเดินไปหางานเอง</summary>
        public void Init(Vector3 spawnPosition)
        {
            transform.position = spawnPosition;
            state = State.Idle;
            timer = 0f;
        }

        private void OnDisable()
        {
            // เลิกงานกลางทาง: วางอาหารที่ถืออยู่ลงพื้นให้ผู้เล่นเก็บเองได้
            for (int i = 0; i < carried.Count; i++)
            {
                Item dish = carried[i];
                if (dish == null) continue;
                dish.transform.SetParent(null, true);
                dish.SetCarriedState(false);
            }
            carried.Clear();
        }

        private void Update()
        {
            UpdateVisual();
            carried.RemoveAll(d => d == null);
            movedThisFrame = false;

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.ToPot: UpdateToPot(); break;
                case State.Loading: UpdateLoading(); break;
                case State.ToShelf: UpdateToShelf(); break;
                case State.Unloading: UpdateUnloading(); break;
            }

            UpdateFootsteps();
            LayoutCarried();
        }

        // ---------------- สถานะ ----------------

        private void UpdateIdle()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = idleCheckInterval;

            // มีของค้างในมือ (ชั้นเต็มตอนไปถึง) หาชั้นที่ยังมีที่ให้ก่อน
            if (carried.Count > 0)
            {
                FoodSellCounter shelf = FindShelfWithRoom(carried[carried.Count - 1].Type);
                if (shelf != null)
                {
                    targetShelf = shelf;
                    state = State.ToShelf;
                }
                return;
            }

            if (FindJob(out targetPot, out targetShelf)) state = State.ToPot;
        }

        private void UpdateToPot()
        {
            if (targetPot == null || targetShelf == null || targetPot.TrayCount == 0)
            {
                state = State.Idle;
                timer = 0f;
                return;
            }

            if (MoveTo(targetPot.TrayPosition))
            {
                state = State.Loading;
                timer = 0f;
                loadStartTime = Time.time;
            }
        }

        private void UpdateLoading()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = handleInterval;

            bool canTake = targetPot != null && targetShelf != null
                && carried.Count < carryCapacity
                && carried.Count < targetShelf.FreePlates
                && targetPot.TrayCount > 0;

            Item dish = canTake ? targetPot.TakeDishFromTray() : null;
            if (dish != null)
            {
                dish.SetCarriedState(true);
                dish.transform.SetParent(transform, true);
                carried.Add(dish);
                AudioManager.PlayAt(SoundId.ItemPickup, transform.position);
                return;
            }

            // ถาดว่างแต่หม้อยังต้มอยู่ และในมือยังน้อย: ยืนรอจานถัดไปก่อน (ไม่เกิน Max Wait At Pot)
            bool moreComing = targetPot != null && targetShelf != null
                && targetPot.CookingItemsCount > 0
                && carried.Count < Mathf.Min(minLoad, carryCapacity)
                && carried.Count < targetShelf.FreePlates;
            if (carried.Count > 0 && moreComing && Time.time - loadStartTime < maxWaitAtPot) return;

            // หยิบต่อไม่ได้แล้ว: มีของก็ไปส่ง ไม่มีก็กลับไปหางานใหม่
            state = carried.Count > 0 ? State.ToShelf : State.Idle;
            timer = 0f;
        }

        private void UpdateToShelf()
        {
            if (targetShelf == null || !targetShelf.isActiveAndEnabled)
            {
                state = State.Idle;
                timer = 0f;
                return;
            }

            if (MoveTo(targetShelf.GetDepositPosition()))
            {
                state = State.Unloading;
                timer = 0f;
            }
        }

        private void UpdateUnloading()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = handleInterval;

            if (targetShelf != null && targetShelf.FreePlates > 0)
            {
                for (int i = carried.Count - 1; i >= 0; i--)
                {
                    Item dish = carried[i];
                    if (!targetShelf.Accepts(dish.Type)) continue;

                    carried.RemoveAt(i);
                    targetShelf.PlaceFoodOnCounter(dish);
                    AudioManager.PlayAt(SoundId.FoodPlace, targetShelf.transform.position);
                    return;
                }
            }

            // วางครบแล้ว / ชั้นเต็ม / ของที่เหลือชั้นนี้ไม่รับ -> กลับไปคิดงานใหม่ (ของที่ค้างจะถูกพาไปชั้นอื่นหรือยืนรอ)
            state = State.Idle;
            timer = carried.Count > 0 ? idleCheckInterval : 0f;
        }

        // ---------------- หางาน ----------------

        /// <summary>เลือกหม้อที่มีอาหารบนถาด และชั้นของเมนูนั้นยังมีที่ว่าง โดยเอาชั้นที่อาหารเหลือน้อยสุดก่อน</summary>
        private bool FindJob(out CookingPot pot, out FoodSellCounter shelf)
        {
            pot = null;
            shelf = null;
            int bestStock = int.MaxValue;
            int bestTray = 0;

            for (int i = 0; i < CookingPot.AllPots.Count; i++)
            {
                CookingPot p = CookingPot.AllPots[i];
                if (p == null || !p.isActiveAndEnabled || !p.IsUnlocked) continue;

                int tray = p.TrayCount;
                if (tray == 0) continue;

                FoodSellCounter s = FindShelfWithRoom(p.OutputDishType);
                if (s == null) continue;

                int stock = s.DishesCount;
                if (stock < bestStock || (stock == bestStock && tray > bestTray))
                {
                    bestStock = stock;
                    bestTray = tray;
                    pot = p;
                    shelf = s;
                }
            }
            return pot != null;
        }

        /// <summary>ชั้นที่ขายเมนูนี้ เปิดอยู่ และมีที่ว่างมากสุด (null = ไม่มี)</summary>
        private static FoodSellCounter FindShelfWithRoom(ItemType dish)
        {
            FoodSellCounter best = null;
            int bestFree = 0;
            foreach (FoodSellCounter c in FoodSellCounter.All)
            {
                if (c == null || !c.isActiveAndEnabled || !c.IsOpen || !c.Accepts(dish)) continue;
                int free = c.FreePlates;
                if (free > bestFree)
                {
                    bestFree = free;
                    best = c;
                }
            }
            return best;
        }

        // ---------------- เดิน / ถือของ ----------------

        /// <summary>เดินตรงไปหาเป้า คืน true เมื่อเข้าระยะหยิบ/วางแล้ว</summary>
        private bool MoveTo(Vector3 target)
        {
            Vector2 delta = (Vector2)target - (Vector2)transform.position;
            if (delta.sqrMagnitude <= reachDistance * reachDistance) return true;

            Vector2 step = delta.normalized * (moveSpeed * Time.deltaTime);
            transform.position += new Vector3(step.x, step.y, 0f);
            FaceDirection(delta.x);
            movedThisFrame = true;
            return false;
        }

        private void UpdateFootsteps()
        {
            if (stepInterval <= 0f) return;
            if (movedThisFrame)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    AudioManager.PlayAt(SoundId.Footstep, transform.position);
                    stepTimer = stepInterval;
                }
            }
            else
            {
                stepTimer = 0f;
            }
        }

        /// <summary>เรียงจานที่ถือซ้อนกันเหนือหัว</summary>
        private void LayoutCarried()
        {
            int baseOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 1 : 10;
            for (int i = 0; i < carried.Count; i++)
            {
                Item dish = carried[i];
                if (dish == null) continue;

                Vector3 p = transform.position + new Vector3(carryOffset.x, carryOffset.y + carryStackSpacing * i, 0f);
                dish.transform.position = new Vector3(p.x, p.y, dish.transform.position.z);
                if (dish.SpriteRenderer != null) dish.SpriteRenderer.sortingOrder = baseOrder + i;
            }
        }
    }
}
