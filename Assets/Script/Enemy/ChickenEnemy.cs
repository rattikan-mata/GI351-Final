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
