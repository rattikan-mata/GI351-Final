using System.Collections.Generic;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// พนักงานล่าสัตว์: เดินออกจากร้านไปโซนล่า ตีสัตว์ เก็บเนื้อ แล้วเดินกลับมาใส่หม้อให้
    /// จ้างจากร้าน Staff โซนละคน (StaffHirer ส่งชนิดเนื้อของโซนมาให้ตอนจ้าง) ล่าเฉพาะโซนตัวเอง
    /// ถ้าไม่ได้กำหนดโซน: เลือกโซนที่ชั้นขายของเมนูนั้นเหลือน้อยสุด และไม่ล่าหมู (ตั้งได้ที่ Hunt Aggressive Animals)
    /// สัตว์ที่ลูกจ้างตีไม่ทำให้จอหยุด/กล้องสั่น และหมูไม่โกรธไล่ผู้เล่น
    /// อ่อนกว่าผู้เล่น: เดินช้ากว่า ถือได้น้อยกว่า ตีช้า (ตั้งได้ด้านล่าง)
    /// ทางเดิน: ร้าน -> Shop Exit Path (ทางเชื่อมร้านกับโซนแรก) -> ประตูโซนที่ต้องผ่าน -> กลางโซน แล้วย้อนทางเดิมกลับมาที่หม้อ
    /// </summary>
    public class HunterStaff : StaffBase
    {
        [Header("Work (การทำงาน)")]
        [Tooltip("ความเร็วเดิน (ผู้เล่นเดิน 5, สัตว์ตกใจวิ่งหนี 3.5)")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Tooltip("ถือเนื้อได้ครั้งละกี่ชิ้น (กระเป๋าผู้เล่นเริ่มที่ 12)")]
        [Min(1)] [SerializeField] private int carryCapacity = 4;

        [Tooltip("ดาเมจต่อการตี 1 ครั้ง (ไก่มีเลือด 30, ควาย 150)")]
        [Min(1)] [SerializeField] private int damage = 30;

        [Tooltip("ตีได้ทุกกี่วินาที")]
        [Min(0.1f)] [SerializeField] private float attackInterval = 0.8f;

        [Tooltip("ระยะตี (เมตร)")]
        [Min(0.3f)] [SerializeField] private float attackRange = 1.1f;

        [Tooltip("มองหาเนื้อที่ตกพื้นในระยะนี้ (เมตร)")]
        [Min(0.5f)] [SerializeField] private float meatSearchRadius = 4f;

        [Tooltip("ใส่เนื้อลงหม้อ 1 ชิ้นทุกกี่วินาที")]
        [Min(0.02f)] [SerializeField] private float feedInterval = 0.15f;

        [Tooltip("อยู่ในโซนล่านานสุดกี่วินาที แล้วกลับมาพร้อมของที่มี (กันติดอยู่ในโซนที่ไม่มีสัตว์)")]
        [Min(5f)] [SerializeField] private float maxHuntTime = 60f;

        [Tooltip("ล่าสัตว์ที่สู้กลับ (หมู) ด้วยไหม")]
        [SerializeField] private bool huntAggressiveAnimals = false;

        [Tooltip("ช่วงเวลาระหว่างก้าวของเสียงเดิน (วินาที) ตั้ง 0 เพื่อปิด")]
        [SerializeField] private float stepInterval = 0.38f;

        [Header("Path (ทางเดิน)")]
        [Tooltip("จุดทางเดินจากร้านไปโซนล่าแรก (พิกัดโลก) เรียงจากฝั่งร้านไปฝั่งโซน เช่น ปากทางเชื่อมฝั่งร้าน แล้วปากทางฝั่งโซน")]
        [SerializeField] private Vector2[] shopExitPath = { new Vector2(-2.5f, -1f), new Vector2(-5.5f, -1f) };

        [Header("Carry (เนื้อที่ถืออยู่)")]
        [SerializeField] private Vector2 carryOffset = new Vector2(0f, 2.0f);
        [SerializeField] private float carryStackSpacing = 0.2f;

        private enum State { Idle, GoingOut, Hunting, Returning, Feeding }

        private readonly List<Item> carried = new List<Item>();
        private readonly List<Vector3> path = new List<Vector3>();
        private readonly List<Vector3> outPath = new List<Vector3>();
        private int pathIndex;
        private State state = State.Idle;
        private HuntArea targetArea;
        private CookingPot targetPot;
        private ItemType targetMeat;
        private EnemyHealth targetAnimal;
        private Item targetItem;
        private float timer;
        private float attackTimer;
        private float huntStartTime;
        private float scanTimer;
        private float stepTimer;
        private bool movedThisFrame;

        private static readonly List<EnemyHealth> animals = new List<EnemyHealth>();

        /// <summary>จำนวนเนื้อที่ถืออยู่</summary>
        public int CarriedCount => carried.Count;

        /// <summary>สถานะตอนนี้ (ไว้ดูตอนทดสอบ)</summary>
        public string CurrentState => state + (targetArea != null ? " @ " + targetArea.AreaName : "")
            + (targetItem != null ? " item" + (Vector2)targetItem.transform.position : "")
            + (targetAnimal != null ? " animal" + (Vector2)targetAnimal.transform.position + " hp" + targetAnimal.CurrentHealth : "");

        // ลูกจ้างประจำโซน: ล่าเฉพาะสัตว์ที่ดรอปเนื้อนี้ (null = เลือกโซนเองตามของบนแผง)
        private ItemType? assignedMeat;

        /// <summary>ประจำโซนไหน (ชนิดเนื้อของโซนนั้น) null = ยังไม่กำหนด</summary>
        public ItemType? AssignedMeat => assignedMeat;

        /// <summary>เริ่มงาน: โผล่ที่ตำแหน่งนี้แล้วไปหางานเอง  meat = ประจำโซนที่ดรอปเนื้อนี้ (null = เลือกเอง)</summary>
        public void Init(Vector3 spawnPosition, ItemType? meat = null, int carryOverride = 0, int damageOverride = 0)
        {
            transform.position = spawnPosition;
            assignedMeat = meat;
            if (carryOverride > 0) carryCapacity = carryOverride;
            if (damageOverride > 0) damage = damageOverride;
            state = State.Idle;
            timer = 0f;
        }

        private void OnDisable()
        {
            // เลิกงานกลางทาง: วางเนื้อที่ถือลงพื้นให้ผู้เล่นเก็บได้
            foreach (Item meat in carried)
            {
                if (meat == null) continue;
                meat.transform.SetParent(null, true);
                meat.SetCarriedState(false);
            }
            carried.Clear();
        }

        private void Update()
        {
            UpdateVisual();
            carried.RemoveAll(m => m == null);
            movedThisFrame = false;

            switch (state)
            {
                case State.Idle: UpdateIdle(); break;
                case State.GoingOut: UpdateGoingOut(); break;
                case State.Hunting: UpdateHunting(); break;
                case State.Returning: UpdateReturning(); break;
                case State.Feeding: UpdateFeeding(); break;
            }

            UpdateFootsteps();
            LayoutCarried();
        }

        // ---------------- สถานะ ----------------

        private void UpdateIdle()
        {
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = 0.5f;

            // มีเนื้อค้างอยู่ในมือ (หม้อเต็มตอนไปถึง): ไปใส่หม้อก่อน
            if (carried.Count > 0 && targetPot != null)
            {
                state = State.Feeding;
                return;
            }

            if (!PickJob()) return;

            BuildOutPath(targetArea);
            path.Clear();
            path.AddRange(outPath);
            pathIndex = 0;
            state = State.GoingOut;
        }

        private void UpdateGoingOut()
        {
            if (!JobStillValid()) { StartReturn(); return; }
            if (FollowPath())
            {
                state = State.Hunting;
                huntStartTime = Time.time;
                targetAnimal = null;
                targetItem = null;
            }
        }

        private void UpdateHunting()
        {
            if (!JobStillValid() || carried.Count >= carryCapacity || Time.time - huntStartTime > maxHuntTime)
            {
                StartReturn();
                return;
            }

            attackTimer -= Time.deltaTime;
            scanTimer -= Time.deltaTime;
            if (scanTimer <= 0f)
            {
                scanTimer = 0.3f;
                // เก็บเนื้อที่ตกอยู่ใกล้ๆ ก่อน แล้วค่อยหาสัตว์ตัวต่อไป
                targetItem = FindLooseMeat();
                if (targetItem == null && (targetAnimal == null || targetAnimal.IsDead)) targetAnimal = FindNearestAnimal();
            }

            if (targetItem != null)
            {
                if (targetItem.IsCarried || !IsLoose(targetItem)) { targetItem = null; return; }
                if (MoveTo(targetItem.transform.position, 0.45f)) PickUp(targetItem);
                return;
            }

            // สัตว์วิ่งออกนอกโซนตัวเองแล้ว (ประตูเปิดสัตว์เดินข้ามโซนได้): ปล่อยไป หาตัวอื่นในโซน
            if (targetAnimal != null && !targetAnimal.IsDead && !InHuntZone(targetAnimal.transform.position))
            {
                targetAnimal = null;
                scanTimer = 0f;
                return;
            }

            if (targetAnimal != null && !targetAnimal.IsDead)
            {
                if (MoveTo(targetAnimal.transform.position, attackRange) && attackTimer <= 0f)
                {
                    attackTimer = attackInterval;
                    FaceDirection(targetAnimal.transform.position.x - transform.position.x);
                    AudioManager.PlayAt(SoundId.Attack, transform.position);
                    targetAnimal.TakeDamageFromStaff(damage);
                    if (targetAnimal.IsDead) scanTimer = 0.6f;   // รอเนื้อตกพื้นแป๊บหนึ่งก่อนค่อยมองหา
                }
                return;
            }

            // ไม่มีสัตว์: ยืนรอกลางโซน
            MoveTo(targetArea.transform.position, 0.5f);
        }

        private void UpdateReturning()
        {
            if (FollowPath()) state = State.Feeding;
        }

        private void UpdateFeeding()
        {
            if (targetPot == null || !targetPot.IsUnlocked)
            {
                state = State.Idle;
                return;
            }
            if (!MoveTo(targetPot.GetStandZonePosition(), 0.6f)) return;

            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = feedInterval;

            for (int i = carried.Count - 1; i >= 0; i--)
            {
                Item meat = carried[i];
                if (meat == null || meat.Type != targetPot.RequiredIngredient) continue;
                if (!targetPot.TryFeedItem(meat)) return;   // หม้อเต็ม: ยืนรอ
                carried.RemoveAt(i);
                return;
            }

            // ใส่หมดแล้ว (หรือเหลือแต่เนื้อที่หม้อนี้ไม่รับ) -> ไปหางานใหม่
            if (carried.Count > 0)
            {
                // เนื้อไม่ตรงหม้อ (ไม่ควรเกิด) วางทิ้งให้ผู้เล่นเก็บ
                OnDisable();
            }
            state = State.Idle;
            timer = 0.2f;
        }

        private void StartReturn()
        {
            path.Clear();
            for (int i = outPath.Count - 2; i >= 0; i--) path.Add(outPath[i]);   // ย้อนทางเดิม (ไม่ต้องกลับไปกลางโซน)
            pathIndex = 0;
            targetAnimal = null;
            targetItem = null;
            state = State.Returning;
        }

        // ---------------- เลือกงาน ----------------

        /// <summary>เลือกโซนที่เปิดแล้ว หม้อของเนื้อนั้นปลดแล้ว และชั้นขายเมนูนั้นมีของน้อยสุด</summary>
        private bool PickJob()
        {
            targetArea = null;
            targetPot = null;
            int bestScore = int.MaxValue;

            foreach (HuntArea area in HuntArea.All)
            {
                if (area == null || !area.IsOpen || area.Spawner == null || area.Spawner.SpawnPrefab == null) continue;
                EnemyHealth animal = area.Spawner.SpawnPrefab.GetComponentInChildren<EnemyHealth>(true);
                if (animal == null || animal.MeatDropPrefab == null) continue;
                Item meat = animal.MeatDropPrefab.GetComponentInChildren<Item>(true);
                if (meat == null) continue;
                if (assignedMeat.HasValue)
                {
                    if (meat.Type != assignedMeat.Value) continue;   // ลูกจ้างประจำโซน: ล่าเฉพาะโซนตัวเอง (รวมหมู)
                }
                else if (!huntAggressiveAnimals && animal.GetComponent<PigEnemy>() != null)
                {
                    continue;
                }

                CookingPot pot = FindPotFor(meat.Type);
                if (pot == null || pot.CookingItemsCount >= pot.MaxCapacity) continue;

                int stock = pot.CookingItemsCount + pot.TrayCount;
                foreach (FoodSellCounter c in FoodSellCounter.All)
                {
                    if (c != null && c.IsOpen && c.Accepts(pot.OutputDishType)) stock += c.DishesCount;
                }
                if (stock < bestScore)
                {
                    bestScore = stock;
                    targetArea = area;
                    targetPot = pot;
                    targetMeat = meat.Type;
                }
            }
            return targetArea != null;
        }

        private static CookingPot FindPotFor(ItemType meat)
        {
            foreach (CookingPot p in CookingPot.AllPots)
            {
                if (p != null && p.isActiveAndEnabled && p.IsUnlocked && p.RequiredIngredient == meat) return p;
            }
            return null;
        }

        private bool JobStillValid()
        {
            return targetArea != null && targetArea.IsOpen && targetPot != null && targetPot.IsUnlocked;
        }

        /// <summary>ทางเดินจากร้านไปกลางโซน: ทางออกจากร้าน -> ประตูของทุกโซนที่ต้องผ่าน -> กลางโซนเป้าหมาย</summary>
        private void BuildOutPath(HuntArea target)
        {
            outPath.Clear();
            foreach (Vector2 p in shopExitPath) outPath.Add(p);

            // ไล่ย้อนจากโซนเป้าหมายไปโซนแรก ผ่านประตูที่ต้องเปิดก่อน
            var chain = new List<HuntArea>();
            HuntArea a = target;
            for (int guard = 0; a != null && a.Gate != null && guard < 10; guard++)
            {
                chain.Insert(0, a);
                a = AreaOfGate(a.Gate.RequiredGate);
            }
            foreach (HuntArea step in chain) outPath.Add(step.Gate.transform.position);
            outPath.Add(target.transform.position);
        }

        /// <summary>โซนที่ประตูนี้เปิดเข้าไป (null = โซนเริ่มต้นที่ไม่มีประตู)</summary>
        private static HuntArea AreaOfGate(AreaGate gate)
        {
            foreach (HuntArea area in HuntArea.All)
            {
                if (area != null && area.Gate == gate) return area;
            }
            return null;
        }

        // ---------------- หาเป้าหมายในโซน ----------------

        private EnemyHealth FindNearestAnimal()
        {
            animals.Clear();
            animals.AddRange(FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude));
            EnemyHealth best = null;
            float bestSqr = float.MaxValue;
            foreach (EnemyHealth e in animals)
            {
                if (e == null || e.IsDead || !InHuntZone(e.transform.position)) continue;
                Item meat = e.MeatDropPrefab != null ? e.MeatDropPrefab.GetComponentInChildren<Item>(true) : null;
                if (meat == null || meat.Type != targetMeat) continue;
                float sqr = ((Vector2)e.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = e; }
            }
            return best;
        }

        /// <summary>จุดนี้อยู่ในโซนล่าของลูกจ้าง (กรอบโซน ไม่รวมทางเชื่อมไปโซนอื่น)</summary>
        private bool InHuntZone(Vector2 p)
        {
            return targetArea != null && targetArea.InnerRect.Contains(p);
        }

        private Item FindLooseMeat()
        {
            Item best = null;
            float bestSqr = meatSearchRadius * meatSearchRadius;
            foreach (Item it in FindObjectsByType<Item>(FindObjectsInactive.Exclude))
            {
                if (it == null || it.Type != targetMeat || !IsLoose(it)) continue;
                float sqr = ((Vector2)it.transform.position - (Vector2)transform.position).sqrMagnitude;
                if (sqr < bestSqr) { bestSqr = sqr; best = it; }
            }
            return best;
        }

        /// <summary>เนื้อตกพื้นนิ่งแล้ว ไม่มีใครถือ และไม่ได้กำลังลอยเข้าหาผู้เล่น</summary>
        private static bool IsLoose(Item it)
        {
            if (it.IsCarried || it.GetComponent<MeatDropFx>() != null) return false;
            Collider2D col = it.GetComponent<Collider2D>();
            return col != null && col.enabled;
        }

        private void PickUp(Item meat)
        {
            meat.SetCarriedState(true);
            meat.transform.SetParent(transform, true);
            carried.Add(meat);
            targetItem = null;
            AudioManager.PlayAt(SoundId.ItemPickup, transform.position);
        }

        // ---------------- เดิน / ถือของ ----------------

        /// <summary>เดินตามทางทีละจุด คืน true เมื่อถึงจุดสุดท้าย</summary>
        private bool FollowPath()
        {
            if (pathIndex >= path.Count) return true;
            if (MoveTo(path[pathIndex], 0.3f)) pathIndex++;
            return pathIndex >= path.Count;
        }

        /// <summary>เดินตรงไปหาเป้า คืน true เมื่อเข้าระยะแล้ว</summary>
        private bool MoveTo(Vector3 target, float reach)
        {
            Vector2 delta = (Vector2)target - (Vector2)transform.position;
            if (delta.sqrMagnitude <= reach * reach) return true;

            float stepLen = moveSpeed * Time.deltaTime;
            Vector2 step = delta.magnitude <= stepLen ? delta : delta.normalized * stepLen;
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

        /// <summary>เรียงเนื้อที่ถือซ้อนกันเหนือหัว</summary>
        private void LayoutCarried()
        {
            int baseOrder = bodyRenderer != null ? bodyRenderer.sortingOrder + 1 : 10;
            for (int i = 0; i < carried.Count; i++)
            {
                Item meat = carried[i];
                if (meat == null) continue;
                Vector3 p = transform.position + new Vector3(carryOffset.x, carryOffset.y + carryStackSpacing * i, 0f);
                meat.transform.position = new Vector3(p.x, p.y, meat.transform.position.z);
                if (meat.SpriteRenderer != null) meat.SpriteRenderer.sortingOrder = baseOrder + i;
            }
        }
    }
}
