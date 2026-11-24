using System;
using UnityEngine;

namespace GI351.KitchenGame
{
    /// <summary>
    /// ภาพลูกค้า: สุ่มหน้าตา (ชาย/หญิง) ตอนเกิด แล้วสลับท่ายืน/ท่าเดินตามความเร็ว
    /// ภาพวาดหันขวา (การหันซ้าย CustomerAI พลิก flipX ให้เอง) สีตัว (โจร/หงุดหงิด/โดนตี) CustomerAI ตั้งเองเหมือนเดิม
    /// </summary>
    public class CustomerSpriteAnimator : MonoBehaviour
    {
        [Serializable]
        public class Look
        {
            public string name = "Customer";
            [Tooltip("ท่ายืน (สลับไปมาช้าๆ)")]
            public Sprite[] idle;
            [Tooltip("ท่าเดิน: เฟรม 0 = ยืนตรง, 1-2 = ก้าวเท้า (ว่าง = ใช้ท่ายืน)")]
            public Sprite[] walk;
            [Tooltip("ท่าตี (ลูกจ้างล่าสัตว์) เล่นครั้งเดียวตอนเรียก PlayAttack")]
            public Sprite[] hit;
        }

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Rigidbody2D body;

        [Tooltip("หน้าตาลูกค้าที่สุ่มได้")]
        [SerializeField] private Look[] looks;

        [SerializeField] private float walkFps = 8f;
        [SerializeField] private float idleFps = 1.5f;

        [Tooltip("ความเร็วขั้นต่ำที่นับว่ากำลังเดิน")]
        [SerializeField] private float moveThreshold = 0.15f;

        private static readonly int[] WalkCycle = { 1, 0, 2, 0 };

        [Tooltip("ท่าตีเล่นนานกี่วินาที")]
        [SerializeField] private float hitDuration = 0.3f;

        private Look look;
        private float timer;
        private float hitUntil = -1f;

        /// <summary>เล่นท่าตี 1 รอบ (ถ้าหน้าตานี้มีท่าตี)</summary>
        public void PlayAttack()
        {
            if (look != null && look.hit != null && look.hit.Length > 0) hitUntil = Time.time + hitDuration;
        }
        private Vector3 lastPos;

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (looks != null && looks.Length > 0) look = looks[UnityEngine.Random.Range(0, looks.Length)];
            timer = UnityEngine.Random.value * 4f;   // ไม่ให้ทุกคนก้าวพร้อมกัน
            lastPos = transform.position;
            if (look != null && spriteRenderer != null && look.idle != null && look.idle.Length > 0) spriteRenderer.sprite = look.idle[0];
        }

        private void LateUpdate()
        {
            if (look == null || spriteRenderer == null) return;

            Vector2 v = body != null && body.simulated ? body.linearVelocity : Vector2.zero;
            if (v.sqrMagnitude < 0.0001f && Time.deltaTime > 0f) v = (transform.position - lastPos) / Time.deltaTime;
            lastPos = transform.position;

            if (Time.time < hitUntil)
            {
                float k = 1f - (hitUntil - Time.time) / Mathf.Max(0.01f, hitDuration);
                Sprite h = look.hit[Mathf.Clamp((int)(k * look.hit.Length), 0, look.hit.Length - 1)];
                if (spriteRenderer.sprite != h) spriteRenderer.sprite = h;
                return;
            }

            bool moving = v.sqrMagnitude > moveThreshold * moveThreshold;
            Sprite[] set = moving && look.walk != null && look.walk.Length > 0 ? look.walk : look.idle;
            if (set == null || set.Length == 0) return;

            timer += Time.deltaTime * (moving ? walkFps : idleFps);
            int i = moving && set.Length >= 3 ? WalkCycle[(int)timer % WalkCycle.Length] : (int)timer % set.Length;
            Sprite s = set[Mathf.Clamp(i, 0, set.Length - 1)];
            if (spriteRenderer.sprite != s) spriteRenderer.sprite = s;
        }
    }
}
