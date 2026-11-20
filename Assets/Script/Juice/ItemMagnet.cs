using System.Collections;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ของที่ดรอปจากสัตว์ลอยเข้าตัวผู้เล่นเองแบบ Juice King:
    /// ตกพื้นเสร็จ (MeatDropFx จบ) รอ magnetDelayAfterLanding -> ผู้เล่นเข้าใกล้ในระยะ magnetRadius และยังแบกไหว
    /// -> ป๊อปขยายนิดนึง (เตรียมตัว) -> พุ่งโค้งเข้าหากองบนหลังแบบเร่งขึ้นเรื่อยๆ และหดลง -> เก็บขึ้นหลัง + กองบนหลังเด้ง
    /// ใส่ให้ด้วยโค้ดตอนสัตว์ตาย (MeatDropFx.Spawn) เก็บขึ้นหลังแล้วสคริปต์นี้ลบตัวเอง ของที่กด Q ทิ้งทีหลังจึงไม่ดูดกลับ
    /// ค่าทั้งหมดอยู่ที่ JuiceConfig หัวข้อ 4) Magnet Pickup
    /// </summary>
    [RequireComponent(typeof(Item))]
    public class ItemMagnet : MonoBehaviour
    {
        private JuiceConfig config;
        private Item item;
        private Collider2D[] colliders;
        private float groundTime = -1f;
        private bool flying;
        private PlayerCarry reservedBy;

        public void Init(JuiceConfig cfg)
        {
            config = JuiceDirector.Resolve(cfg);
        }

        private void Awake()
        {
            item = GetComponent<Item>();
            colliders = GetComponents<Collider2D>();
        }

        private void Update()
        {
            if (flying) return;
            if (config == null) config = JuiceDirector.Resolve(null);

            // ถูกเก็บไปแล้ว (เดินชนเก็บเอง / มีระบบอื่นถือไว้) ไม่ต้องดูดอีก
            if (item == null || item.IsCarried)
            {
                Destroy(this);
                return;
            }

            // ยังบินกระเด็นอยู่
            if (TryGetComponent(out MeatDropFx _))
            {
                groundTime = -1f;
                return;
            }

            if (groundTime < 0f) groundTime = Time.time;
            if (Time.time - groundTime < config.magnetDelayAfterLanding) return;

            PlayerCarry target = FindTarget();
            if (target == null || !target.TryReserveIncoming()) return;

            reservedBy = target;
            StartCoroutine(Fly(target));
        }

        private PlayerCarry FindTarget()
        {
            PlayerCarry best = null;
            float bestSqr = config.magnetRadius * config.magnetRadius;
            Vector2 pos = transform.position;

            for (int i = 0; i < PlayerCarry.All.Count; i++)
            {
                PlayerCarry pc = PlayerCarry.All[i];
                if (pc == null || !pc.CanCarry() || item.IsPickupBlockedFor(pc)) continue;
                if (pc.TryGetComponent(out PlayerHealth health) && health.IsDead) continue;

                float sqr = ((Vector2)pc.transform.position - pos).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    bestSqr = sqr;
                    best = pc;
                }
            }
            return best;
        }

        private IEnumerator Fly(PlayerCarry target)
        {
            flying = true;
            SetColliders(false);

            JuiceConfig cfg = config;
            float ppu = cfg.pixelsPerUnit;
            float z = transform.position.z;
            Vector3 baseScale = transform.localScale;

            // 1) เตรียมตัว: ป๊อปขยายแล้วกลับ
            if (cfg.magnetAnticipationTime > 0f && cfg.magnetAnticipationScale > 1f)
            {
                yield return Tween.Run(cfg.magnetAnticipationTime, k =>
                {
                    transform.localScale = baseScale * Mathf.Lerp(1f, cfg.magnetAnticipationScale, Mathf.Sin(k * Mathf.PI));
                });
            }

            // 2) พุ่งโค้งเข้าหาช่องว่างบนกอง (เป้าขยับตามผู้เล่นทุกเฟรม) เร่งขึ้นเรื่อยๆ และหดลง
            Vector2 start = transform.position;
            float duration = Mathf.Max(0.05f, cfg.magnetFlyTime);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (target == null || !target.isActiveAndEnabled) break;

                float t = Mathf.Clamp01(elapsed / duration);
                float e = Tween.Evaluate(cfg.magnetFlyEase, t);
                Vector2 end = target.NextStackWorldPosition;
                Vector2 p = Vector2.LerpUnclamped(start, end, e) + Vector2.up * (cfg.magnetArcHeight * Mathf.Sin(t * Mathf.PI));
                p = Tween.Snap(p, ppu);
                transform.position = new Vector3(p.x, p.y, z);
                transform.localScale = baseScale * Mathf.Lerp(1f, cfg.magnetEndScale, t);

                yield return null;
                elapsed += Time.deltaTime;
            }

            transform.localScale = baseScale;
            flying = false;

            // 3) ถึงตัว: เก็บขึ้นหลัง + กองเด้ง
            if (target != null && target.isActiveAndEnabled)
            {
                reservedBy = null;
                if (target.PickUpReservedItem(item, cfg.magnetEndScale))
                {
                    target.PunchStack(cfg.magnetStackPunchScale, cfg.magnetStackPunchTime);
                    Destroy(this);
                    yield break;
                }
            }

            // เก็บไม่ได้ (ผู้เล่นหาย/เต็ม) วางลงพื้นตรงนั้นแล้วรอดูดใหม่
            ReleaseReservation();
            SetColliders(true);
            groundTime = Time.time;
        }

        private void SetColliders(bool enable)
        {
            if (colliders == null) return;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = enable;
            }
        }

        private void ReleaseReservation()
        {
            if (reservedBy != null) reservedBy.ReleaseIncoming();
            reservedBy = null;
        }

        private void OnDestroy()
        {
            // ถูกทำลายกลางทาง (despawn / ถอดสคริปต์) คืนช่องที่จองไว้ และเปิด collider กลับถ้ายังไม่ได้ถูกแบก
            ReleaseReservation();
            if (flying && item != null && !item.IsCarried) SetColliders(true);
        }
    }
}
