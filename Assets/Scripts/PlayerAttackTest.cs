using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAttackTest : MonoBehaviour
{
    [SerializeField] private EnemyHealth enemy;
    [SerializeField] private float attackDistance = 3f;
    [SerializeField] private float damage = 20f;

    private PlayerHealth playerHealth;

    public float Damage => damage;

    public float AttackDistance => attackDistance;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
    }

    private void Update()
    {
        if (playerHealth != null && playerHealth.IsDead)
            return;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            TryAttackEnemy();
        }
    }

    private void TryAttackEnemy()
    {
        if (enemy == null)
            return;

        float distance =
            Vector3.Distance(
                transform.position,
                enemy.transform.position
            );

        if (distance <= attackDistance)
        {
            enemy.TakeDamage(damage);

            Debug.Log(
                "Player attacks Enemy!"
            );
        }
    }
}
