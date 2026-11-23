using UnityEngine;

namespace GI351.KitchenGame
{
    [RequireComponent(typeof(EnemyHealth))]
    [RequireComponent(typeof(EnemyWander))]
    public class ChickenEnemy : MonoBehaviour
    {
        [Header("Chicken Specific Settings")]
        [Tooltip("เสียงหรือเอฟเฟกต์เฉพาะของไก่")]
        [SerializeField] private AudioClip cluckSound;

        [Tooltip("ไก่ร้องเองเป็นระยะ (วินาที ต่ำสุด-สูงสุด) ตั้ง 0 เพื่อปิด")]
        [SerializeField] private Vector2 cluckInterval = new Vector2(5f, 12f);

        private float nextCluckTime;

        private void Update()
        {
            if (cluckInterval.y <= 0f) return;
            if (Time.time < nextCluckTime) return;

            nextCluckTime = Time.time + Random.Range(Mathf.Min(cluckInterval.x, cluckInterval.y), Mathf.Max(cluckInterval.x, cluckInterval.y));
            AudioManager.Play(SoundId.ChickenCluck, cluckSound, transform.position);
        }

        private EnemyHealth health;
        private EnemyWander wander;

        public EnemyHealth Health => health;
        public EnemyWander Wander => wander;

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            wander = GetComponent<EnemyWander>();
        }
    }
}
