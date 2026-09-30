using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Attack,
        Flee,
        Heal,
        Dead
    }

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyHealth health;

    [Header("Patrol")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waypointTolerance = 0.5f;
    [SerializeField] private float waypointHealAmount = 20f;

    [Header("Chase")]
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float lostPlayerDelay = 2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackExitRange = 2.75f;
    [SerializeField] private float attackDamage = 10f;
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Flee")]
    [SerializeField] private Transform safePoint;
    [SerializeField] private float fleeSpeed = 5f;
    [SerializeField] private float lowHealthThreshold = 30f;
    [SerializeField] private float safeDistance = 12f;

    [Header("Heal (Bonus)")]
    [SerializeField] private bool enableHeal = true;
    [SerializeField] private float healRate = 10f;
    [SerializeField] private float healUntilHealth = 70f;

    [Header("Debug")]
    [SerializeField] private EnemyState currentState;

    private int currentPatrolIndex = 0;

    private float lostPlayerTimer = 0f;
    private float nextAttackTime = 0f;

    private bool fleeTriggered = false;

    // State sebelum masuk Flee, dipakai untuk kembali ke state terakhir setelah Heal.
    private EnemyState stateBeforeFlee = EnemyState.Patrol;

    private PlayerHealth playerHealth;

    public EnemyState CurrentState => currentState;

    public float LowHealthThreshold => lowHealthThreshold;

    public bool EnableHeal => enableHeal;

    public float WaypointHealAmount => waypointHealAmount;

    public float HealRate => healRate;

    public float HealUntilHealth => healUntilHealth;

    private bool PlayerAlive =>
        playerHealth == null || !playerHealth.IsDead;

    private void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (perception == null)
            perception = GetComponent<EnemyPerception>();

        if (health == null)
            health = GetComponent<EnemyHealth>();

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        ChangeState(EnemyState.Patrol);
    }

    private void Update()
    {
        // ================================
        // GLOBAL TRANSITION PRIORITY
        // ================================

        if (health.IsDead)
        {
            ChangeState(EnemyState.Dead);
            return;
        }

        if (!fleeTriggered &&
            health.CurrentHealth <= lowHealthThreshold &&
            currentState != EnemyState.Flee)
        {
            fleeTriggered = true;
            stateBeforeFlee = currentState;
            ChangeState(EnemyState.Flee);
        }

        // ================================
        // UPDATE CURRENT STATE
        // ================================

        switch (currentState)
        {
            case EnemyState.Patrol:
                UpdatePatrol();
                break;

            case EnemyState.Chase:
                UpdateChase();
                break;

            case EnemyState.Attack:
                UpdateAttack();
                break;

            case EnemyState.Flee:
                UpdateFlee();
                break;

            case EnemyState.Heal:
                UpdateHeal();
                break;

            case EnemyState.Dead:
                UpdateDead();
                break;
        }
    }

    // =====================================
    // CHANGE STATE
    // =====================================

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState)
            return;

        ExitState(currentState);

        currentState = newState;

        Debug.Log(
            gameObject.name +
            " → State: " +
            currentState
        );

        EnterState(currentState);
    }

    // =====================================
    // ENTER STATE
    // =====================================

    private void EnterState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Patrol:

                agent.isStopped = false;
                agent.speed = patrolSpeed;

                SetPatrolDestination();

                break;

            case EnemyState.Chase:

                agent.isStopped = false;
                agent.speed = chaseSpeed;

                lostPlayerTimer = 0f;

                break;

            case EnemyState.Attack:

                agent.isStopped = true;
                agent.ResetPath();

                break;

            case EnemyState.Flee:

                agent.isStopped = false;
                agent.speed = fleeSpeed;

                if (safePoint != null)
                {
                    agent.SetDestination(
                        safePoint.position
                    );
                }

                break;

            case EnemyState.Heal:

                agent.isStopped = true;
                agent.ResetPath();

                break;

            case EnemyState.Dead:

                agent.isStopped = true;
                agent.ResetPath();

                break;
        }
    }

    // =====================================
    // EXIT STATE
    // =====================================

    private void ExitState(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Attack:
            case EnemyState.Heal:
                agent.isStopped = false;
                break;
        }
    }

    // =====================================
    // PATROL
    // =====================================

    private void UpdatePatrol()
    {
        if (perception.CanSeePlayer && PlayerAlive)
        {
            ChangeState(EnemyState.Chase);
            return;
        }

        if (patrolPoints == null ||
            patrolPoints.Length == 0)
            return;

        if (!agent.pathPending &&
            agent.remainingDistance <=
            waypointTolerance)
        {
            // Bonus: Enemy pulih sedikit setiap tiba di waypoint patrol.
            if (waypointHealAmount > 0f &&
                health.CurrentHealth < health.MaxHealth)
            {
                health.Heal(waypointHealAmount);

                Debug.Log(
                    gameObject.name +
                    " heals +" + waypointHealAmount +
                    " at waypoint. HP: " + health.CurrentHealth
                );
            }

            currentPatrolIndex++;

            if (currentPatrolIndex >=
                patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }

            SetPatrolDestination();
        }
    }

    private void SetPatrolDestination()
    {
        if (patrolPoints == null ||
            patrolPoints.Length == 0)
            return;

        Transform point =
            patrolPoints[currentPatrolIndex];

        if (point != null)
        {
            agent.SetDestination(
                point.position
            );
        }
    }

    // =====================================
    // CHASE
    // =====================================

    private void UpdateChase()
    {
        if (!PlayerAlive)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        if (perception.CanSeePlayer)
        {
            lostPlayerTimer = 0f;

            agent.SetDestination(
                player.position
            );

            if (distance <= attackRange)
            {
                ChangeState(
                    EnemyState.Attack
                );

                return;
            }
        }
        else
        {
            lostPlayerTimer +=
                Time.deltaTime;

            if (lostPlayerTimer >=
                lostPlayerDelay)
            {
                ChangeState(
                    EnemyState.Patrol
                );

                return;
            }
        }
    }

    // =====================================
    // ATTACK
    // =====================================

    private void UpdateAttack()
    {
        if (!PlayerAlive)
        {
            ChangeState(EnemyState.Patrol);
            return;
        }

        float distance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        FacePlayer();

        if (!perception.CanSeePlayer ||
            distance > attackExitRange)
        {
            ChangeState(
                EnemyState.Chase
            );

            return;
        }

        if (Time.time >= nextAttackTime)
        {
            AttackPlayer();

            nextAttackTime =
                Time.time +
                attackCooldown;
        }
    }

    private void AttackPlayer()
    {
        Debug.Log("Enemy attacks Player!");

        if (playerHealth != null)
        {
            playerHealth.TakeDamage(
                attackDamage
            );
        }
    }

    private void FacePlayer()
    {
        Vector3 direction =
            player.position -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );

        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                10f * Time.deltaTime
            );
    }

    // =====================================
    // FLEE
    // =====================================

    private void UpdateFlee()
    {
        if (safePoint == null)
            return;

        float playerDistance =
            Vector3.Distance(
                transform.position,
                player.position
            );

        // Jarak ke SafePoint diukur pada bidang datar (abaikan tinggi Y)
        // karena pivot Enemy berada di tengah kapsul, bukan di lantai.
        Vector3 toSafePoint =
            safePoint.position -
            transform.position;

        toSafePoint.y = 0f;

        float safePointDistance =
            toSafePoint.magnitude;

        // Dengan Heal aktif: Enemy selalu menuju SafePoint lalu memulihkan diri.
        if (enableHeal)
        {
            if (safePointDistance <= 1f)
            {
                ChangeState(
                    EnemyState.Heal
                );
            }

            return;
        }

        if (playerDistance >= safeDistance ||
            safePointDistance <= 1f)
        {
            ChangeState(
                EnemyState.Patrol
            );

            return;
        }
    }

    // =====================================
    // HEAL (BONUS)
    // =====================================

    private void UpdateHeal()
    {
        health.Heal(
            healRate * Time.deltaTime
        );

        if (health.CurrentHealth >= healUntilHealth)
        {
            // HP sudah pulih: Flee boleh dipicu lagi di masa depan.
            fleeTriggered = false;

            // Kembali ke state terakhir. Attack tidak dilanjutkan langsung;
            // Enemy kembali ke Chase dan mengevaluasi ulang Player.
            ChangeState(
                stateBeforeFlee == EnemyState.Patrol
                    ? EnemyState.Patrol
                    : EnemyState.Chase
            );
        }
    }

    // =====================================
    // DEAD
    // =====================================

    private void UpdateDead()
    {
        // Tidak melakukan action.
    }

    // =====================================
    // DEBUG GIZMOS
    // =====================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(
            transform.position,
            attackRange
        );

        Gizmos.color = Color.magenta;

        Gizmos.DrawWireSphere(
            transform.position,
            attackExitRange
        );
    }
}
