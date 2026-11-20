using System.Collections;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// เอฟเฟกต์ภาพบน sprite ของสัตว์: squash ตอนโดนตี และ death pop ตอนตาย
    /// ทำกับ child "Visual" ที่ถือ SpriteRenderer เท่านั้น ไม่แตะ root ที่มี collider / Rigidbody2D (ไม่ชนกับ knockback / flee)
    /// ตัวเลขทั้งหมดอยู่ใน JuiceConfig ถูกเรียกจาก EnemyHealth (ถ้าสัตว์ไม่มีสคริปต์นี้ EnemyHealth จะเพิ่มให้ตอนเกิด)
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyJuice : MonoBehaviour
    {
        [Tooltip("Transform ของ child ที่ถือ SpriteRenderer (ว่าง = หา SpriteRenderer ตัวแรกที่เป็นลูก)")]
        [SerializeField] private Transform visual;

        private JuiceConfig config;
        private Coroutine squashRoutine;
        private bool dying;

        private Vector3 baseLocalScale = Vector3.one;
        private Vector3 baseLocalPosition;
        private float spriteBottom;
        private bool visualCached;

        public Transform Visual => visual;

        /// <summary>ตั้ง config (เรียกจาก EnemyHealth ตอน Awake)</summary>
        public void Setup(JuiceConfig juiceConfig)
        {
            config = JuiceDirector.Resolve(juiceConfig);
            CacheVisual();
        }

        private void Awake()
        {
            CacheVisual();
        }

        private void CacheVisual()
        {
            if (visualCached) return;

            if (visual == null)
            {
                SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
                // sprite อยู่บน root เอง (ไม่มี child) จะไม่บี้ root เพราะมี collider / HP bar อยู่
                if (sr != null && sr.transform != transform) visual = sr.transform;
            }

            if (visual != null)
            {
                baseLocalScale = visual.localScale;
                baseLocalPosition = visual.localPosition;

                SpriteRenderer sr = visual.GetComponent<SpriteRenderer>();
                spriteBottom = sr != null && sr.sprite != null ? sr.sprite.bounds.min.y : 0f;
            }
            visualCached = true;
        }

        /// <summary>บี้ sprite ตอนโดนตี (ตีซ้ำระหว่างบี้จะเริ่มใหม่จากสภาพปกติ ไม่สะสม)</summary>
        public void PlayHit()
        {
            if (dying || visual == null) return;
            JuiceConfig cfg = Cfg;
            if (!cfg.squashEnabled) return;

            if (squashRoutine != null) StopCoroutine(squashRoutine);
            ResetVisual();
            squashRoutine = StartCoroutine(SquashRoutine(cfg));
        }

        /// <summary>
        /// เล่น death pop: ขยายนิดหน่อยแล้วหดเป็น 0 จบแล้วเรียก onFinished (ปกติคือ Destroy)
        /// ไม่มี sprite ลูกให้ scale (หรือปิด deathPop) จะเรียก onFinished ทันที
        /// </summary>
        public void PlayDeath(System.Action onFinished)
        {
            JuiceConfig cfg = Cfg;
            dying = true;

            if (squashRoutine != null)
            {
                StopCoroutine(squashRoutine);
                squashRoutine = null;
            }
            ResetVisual();

            if (!cfg.deathPopEnabled)
            {
                onFinished?.Invoke();
                return;
            }

            // สัตว์ที่ sprite อยู่บน root: ตอนตาย collider ปิดไปแล้ว scale root ได้
            Transform target = visual != null ? visual : transform;
            StartCoroutine(DeathRoutine(cfg, target, onFinished));
        }

        private JuiceConfig Cfg => config != null ? config : (config = JuiceDirector.DefaultConfig);

        private IEnumerator SquashRoutine(JuiceConfig cfg)
        {
            float peak = Mathf.Max(0.001f, cfg.squashPeakTime);
            float end = Mathf.Max(peak + 0.001f, cfg.squashEndTime);
            Vector2 target = cfg.squashScale;

            yield return Tween.Run(end, k =>
            {
                if (visual == null) return;

                float time = k * end;
                float amount = time <= peak
                    ? Tween.Evaluate(Ease.OutQuad, time / peak)
                    : 1f - Tween.Evaluate(Ease.InOutSine, (time - peak) / (end - peak));

                float sx = Mathf.Lerp(1f, target.x, amount);
                float sy = Mathf.Lerp(1f, target.y, amount);
                visual.localScale = new Vector3(baseLocalScale.x * sx, baseLocalScale.y * sy, baseLocalScale.z);

                Vector3 pos = baseLocalPosition;
                if (cfg.squashKeepFeetPlanted)
                {
                    // ขอบล่างของ sprite คงที่: ตัวเตี้ยลงก็ขยับกลางตัวลงตาม (ปัดลงตารางพิกเซล)
                    pos.y += Tween.Snap(spriteBottom * baseLocalScale.y * (1f - sy), cfg.pixelsPerUnit);
                }
                visual.localPosition = pos;
            }, Ease.Linear, cfg.squashUnscaledTime);

            ResetVisual();
            squashRoutine = null;
        }

        private IEnumerator DeathRoutine(JuiceConfig cfg, Transform target, System.Action onFinished)
        {
            Vector3 start = target.localScale;
            float punch = cfg.deathPunchScale;

            yield return Tween.Run(cfg.deathPunchTime, k =>
            {
                if (target != null) target.localScale = start * Mathf.Lerp(1f, punch, k);
            }, cfg.deathPunchEase);

            yield return Tween.Run(cfg.deathShrinkTime, k =>
            {
                if (target != null) target.localScale = start * Mathf.Max(0f, Mathf.Lerp(punch, 0f, k));
            }, cfg.deathShrinkEase);

            if (target != null) target.localScale = Vector3.zero;
            onFinished?.Invoke();
        }

        private void ResetVisual()
        {
            if (visual == null) return;
            visual.localScale = baseLocalScale;
            visual.localPosition = baseLocalPosition;
        }

        private void OnDisable()
        {
            if (!dying) ResetVisual();
        }
    }
}
