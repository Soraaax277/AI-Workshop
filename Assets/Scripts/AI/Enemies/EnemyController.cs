using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyController : MonoBehaviour
{
    public enum EnemyStateId
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Return
    }

    [Header("Detection")]
    [SerializeField] float detectionRange = 12f;
    [SerializeField] float attackRange = 2f;
    [SerializeField] float loseTargetRange = 18f;
    [SerializeField] float fieldOfView = 140f;
    [SerializeField] float loseSightDuration = 0f;
    [SerializeField] LayerMask obstructionMask;
    [SerializeField] string playerTag = "Player";

    static readonly float[] PlayerBodySampleHeights = { 0.2f, 1f, 1.6f };

    [Header("Movement")]
    [SerializeField] float patrolSpeed = 2f;
    [SerializeField] float chaseSpeed = 4.5f;
    [SerializeField] float returnSpeed = 3f;
    [SerializeField] float idleDuration = 2f;
    [SerializeField] Transform[] patrolPoints;
    [SerializeField] float patrolPointTolerance = 0.75f;

    [Header("Combat")]
    [SerializeField] float attackDamage = 15f;
    [SerializeField] float attackCooldown = 1.25f;
    [SerializeField] float attackWindup = 0.35f;

    NavMeshAgent agent;
    EnemyHealth health;
    AIStateMachine<EnemyController> stateMachine;

    Transform player;
    Vector3 spawnPosition;
    int patrolIndex;
    float idleTimer;
    float attackTimer;
    float attackWindupTimer;
    bool attackPending;
    float loseSightTimer;
    bool playerVisibleThisFrame;
    int resolvedObstructionMask;

    public NavMeshAgent Agent => agent;
    public Transform Player => player;
    public Vector3 SpawnPosition => spawnPosition;
    public int PatrolIndex { get => patrolIndex; set => patrolIndex = value; }
    public float IdleTimer { get => idleTimer; set => idleTimer = value; }
    public float IdleDuration => idleDuration;
    public float AttackTimer { get => attackTimer; set => attackTimer = value; }
    public float AttackWindupTimer { get => attackWindupTimer; set => attackWindupTimer = value; }
    public bool AttackPending { get => attackPending; set => attackPending = value; }
    public float AttackWindup => attackWindup;
    public float AttackCooldown => attackCooldown;
    public float AttackDamage => attackDamage;
    public float AttackRange => attackRange;
    public float PatrolSpeed => patrolSpeed;
    public float ChaseSpeed => chaseSpeed;
    public float ReturnSpeed => returnSpeed;
    public float PatrolPointTolerance => patrolPointTolerance;
    public Transform[] PatrolPoints => patrolPoints;
    public float DetectionRange => detectionRange;
    public float LoseTargetRange => loseTargetRange;
    public float FieldOfView => fieldOfView;
    public float LoseSightDuration => loseSightDuration;
    public bool PlayerVisibleThisFrame => playerVisibleThisFrame;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        health = GetComponent<EnemyHealth>();
        spawnPosition = transform.position;
        resolvedObstructionMask = BuildObstructionMask();
        gameObject.layer = LayerMask.NameToLayer("Enemy");

        stateMachine = new AIStateMachine<EnemyController>();
        stateMachine.Initialize(
            this,
            new EnemyIdleState(),
            new EnemyPatrolState(),
            new EnemyChaseState(),
            new EnemyAttackState(),
            new EnemyReturnState()
        );
    }

    void Start()
    {
        var playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
            player = playerObject.transform;

        if (patrolPoints == null || patrolPoints.Length == 0)
            stateMachine.Start<EnemyIdleState>();
        else
            stateMachine.Start<EnemyPatrolState>();
    }

    void Update()
    {
        if (!health.IsAlive)
            return;

        if (player == null)
        {
            var playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
                player = playerObject.transform;
        }

        stateMachine.Tick(Time.deltaTime);
    }

    public void ChangeState<TState>() where TState : IAIState<EnemyController>
    {
        stateMachine.ChangeState(typeof(TState));
    }

    public bool CanSeePlayer()
    {
        playerVisibleThisFrame = EvaluatePlayerVisibility();
        return playerVisibleThisFrame;
    }

    public bool HasLineOfSightToPlayer()
    {
        if (player == null)
            return false;

        Vector3 origin = GetEyePosition();

        for (int i = 0; i < PlayerBodySampleHeights.Length; i++)
        {
            Vector3 target = player.position + Vector3.up * PlayerBodySampleHeights[i];
            if (HasClearLineOfSight(origin, target))
                return true;
        }

        return false;
    }

    Vector3 GetEyePosition()
    {
        return transform.position + Vector3.up * 1.4f;
    }

    int BuildObstructionMask()
    {
        if (obstructionMask.value != 0)
            return obstructionMask.value;

        int mask = Physics.DefaultRaycastLayers;

        int playerLayer = LayerMask.NameToLayer("Player");
        if (playerLayer >= 0)
            mask &= ~(1 << playerLayer);

        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            mask &= ~(1 << enemyLayer);

        int ignoreRaycastLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreRaycastLayer >= 0)
            mask &= ~(1 << ignoreRaycastLayer);

        return mask;
    }

    public void UpdateSightTracking(float deltaTime)
    {
        if (playerVisibleThisFrame)
            loseSightTimer = 0f;
        else
            loseSightTimer += deltaTime;
    }

    public void ResetSightTracking()
    {
        loseSightTimer = 0f;
        playerVisibleThisFrame = false;
    }

    public bool HasLostSight()
    {
        return loseSightTimer >= loseSightDuration;
    }

    bool EvaluatePlayerVisibility()
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance > detectionRange)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > fieldOfView * 0.5f)
            return false;

        return HasLineOfSightToPlayer();
    }

    bool HasClearLineOfSight(Vector3 origin, Vector3 target)
    {
        Vector3 direction = target - origin;
        float distance = direction.magnitude;
        if (distance <= 0.01f)
            return true;

        return !Physics.Raycast(
            origin,
            direction.normalized,
            distance,
            resolvedObstructionMask,
            QueryTriggerInteraction.Ignore);
    }

    public bool IsPlayerInAttackRange()
    {
        if (player == null)
            return false;

        return Vector3.Distance(transform.position, player.position) <= attackRange;
    }

    public bool HasLostPlayer()
    {
        if (player == null)
            return true;

        return Vector3.Distance(transform.position, player.position) > loseTargetRange;
    }

    public Transform GetCurrentPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return null;

        return patrolPoints[Mathf.Clamp(patrolIndex, 0, patrolPoints.Length - 1)];
    }

    public void AdvancePatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(direction.normalized),
            Time.deltaTime * 8f
        );
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (player != null && Application.isPlaying)
        {
            Vector3 origin = GetEyePosition();
            Gizmos.color = playerVisibleThisFrame ? Color.green : Color.magenta;

            for (int i = 0; i < PlayerBodySampleHeights.Length; i++)
            {
                Vector3 target = player.position + Vector3.up * PlayerBodySampleHeights[i];
                bool clear = HasClearLineOfSight(origin, target);
                Gizmos.color = clear ? Color.green : Color.red;
                Gizmos.DrawLine(origin, target);
            }
        }

        if (patrolPoints == null)
            return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
                continue;

            Gizmos.DrawSphere(patrolPoints[i].position, 0.25f);
            int next = (i + 1) % patrolPoints.Length;
            if (patrolPoints[next] != null)
                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
        }
    }
}
