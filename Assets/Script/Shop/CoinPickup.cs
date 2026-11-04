using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(Collider2D))]
    public class CoinPickup : MonoBehaviour
    {
        [Header("Coin Value (มูลค่าของเหรียญ)")]
        [Tooltip("จำนวนเงินที่จะได้รับเมื่อเก็บเหรียญนี้")]
        [SerializeField] private int coinValue = 10;

        [Header("Pickup Settings")]
        [Tooltip("ระยะแม่เหล็กดูดเหรียญเข้าหา Player อัตโนมัติเมื่ออยู่ใกล้")]
        [SerializeField] private float magnetDistance = 2.5f;

        [Tooltip("ความเร็วในการดูดเข้าหา Player")]
        [SerializeField] private float magnetSpeed = 8f;

        [Tooltip("เสียงตอนเก็บเหรียญได้ (ถ้ามี)")]
        [SerializeField] private AudioClip pickupSound;

        [Tooltip("เวลาคงอยู่ของเหรียญก่อนหายไป (0 คืออยู่ตลอดไป)")]
        [SerializeField] private float lifetime = 60f;

        private Rigidbody2D rb;
        private Transform targetPlayer;
        private bool isCollected = false;

        public int Value
        {
            get => coinValue;
            set => coinValue = value;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            Collider2D col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void Start()
        {
            // ค้นหา Player เพื่อให้ระบบดูดเหรียญทำงานได้
            // Player prefab ไม่ได้ตั้ง Tag "Player" ไว้ จึงค้นหาจาก PlayerMovement เป็นหลัก
            var playerMovement = FindAnyObjectByType<PlayerMovement>();
            if (playerMovement != null)
            {
                targetPlayer = playerMovement.transform;
            }
            else
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    targetPlayer = player.transform;
                }
            }

            if (lifetime > 0f)
            {
                Destroy(gameObject, lifetime);
            }
        }

        private void Update()
        {
            if (isCollected) return;

            // ระบบแม่เหล็กดูดเหรียญเข้าหาตัวผู้เล่นอัตโนมัติเมื่อเข้าใกล้
            if (targetPlayer != null && magnetDistance > 0f)
            {
                float dist = Vector2.Distance(transform.position, targetPlayer.position);
                if (dist <= magnetDistance)
                {
                    Vector3 moveDir = (targetPlayer.position - transform.position).normalized;
                    transform.position += moveDir * magnetSpeed * Time.deltaTime;

                    // หมุนเหรียญเบาๆ ขณะถูกดูด
                    transform.Rotate(0f, 0f, 360f * Time.deltaTime);

                    if (dist < 0.4f)
                    {
                        Collect();
                    }
                }
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected) return;

            if (other.CompareTag("Player") || other.GetComponentInParent<PlayerMovement>() != null || other.GetComponentInParent<PlayerCarry>() != null)
            {
                Collect();
            }
        }

        public void Collect()
        {
            if (isCollected) return;
            isCollected = true;

            // เพิ่มเงินเข้าระบบ MoneyManager
            if (MoneyManager.Instance != null)
            {
                MoneyManager.Instance.AddMoney(coinValue);
            }

            if (pickupSound != null)
            {
                AudioSource.PlayClipAtPoint(pickupSound, transform.position);
            }

            Destroy(gameObject);
        }
    }
}
