using UnityEngine;

public class EnemyIdleState : IAIState<EnemyController>
{
    public void Enter(EnemyController context)
    {
        context.Agent.isStopped = true;
        context.IdleTimer = context.IdleDuration;
    }

    public void Tick(EnemyController context, float deltaTime)
    {
        if (context.CanSeePlayer())
        {
            context.ChangeState<EnemyChaseState>();
            return;
        }

        context.IdleTimer -= deltaTime;
        if (context.IdleTimer <= 0f && context.PatrolPoints != null && context.PatrolPoints.Length > 0)
            context.ChangeState<EnemyPatrolState>();
    }

    public void Exit(EnemyController context) { }
}

public class EnemyPatrolState : IAIState<EnemyController>
{
    public void Enter(EnemyController context)
    {
        context.Agent.isStopped = false;
        context.Agent.speed = context.PatrolSpeed;
        MoveToCurrentPatrolPoint(context);
    }

    public void Tick(EnemyController context, float deltaTime)
    {
        if (context.CanSeePlayer())
        {
            context.ChangeState<EnemyChaseState>();
            return;
        }

        Transform point = context.GetCurrentPatrolPoint();
        if (point == null)
        {
            context.ChangeState<EnemyIdleState>();
            return;
        }

        if (!context.Agent.pathPending && context.Agent.remainingDistance <= context.PatrolPointTolerance)
        {
            context.AdvancePatrolPoint();
            MoveToCurrentPatrolPoint(context);
        }
    }

    public void Exit(EnemyController context) { }

    static void MoveToCurrentPatrolPoint(EnemyController context)
    {
        Transform point = context.GetCurrentPatrolPoint();
        if (point != null)
            context.Agent.SetDestination(point.position);
    }
}

public class EnemyChaseState : IAIState<EnemyController>
{
    public void Enter(EnemyController context)
    {
        context.Agent.isStopped = false;
        context.Agent.speed = context.ChaseSpeed;
        context.ResetSightTracking();
    }

    public void Tick(EnemyController context, float deltaTime)
    {
        context.CanSeePlayer();

        if (context.Player == null || context.HasLostPlayer() || !context.PlayerVisibleThisFrame)
        {
            context.ChangeState<EnemyReturnState>();
            return;
        }

        if (context.IsPlayerInAttackRange())
        {
            context.ChangeState<EnemyAttackState>();
            return;
        }

        context.Agent.isStopped = false;
        context.Agent.SetDestination(context.Player.position);
        context.FaceTarget(context.Player.position);
    }

    public void Exit(EnemyController context)
    {
        context.Agent.ResetPath();
    }
}

public class EnemyAttackState : IAIState<EnemyController>
{
    public void Enter(EnemyController context)
    {
        context.Agent.isStopped = true;
        context.AttackTimer = 0f;
        context.AttackPending = false;
        context.AttackWindupTimer = 0f;
    }

    public void Tick(EnemyController context, float deltaTime)
    {
        context.CanSeePlayer();

        if (context.Player == null || context.HasLostPlayer() || !context.PlayerVisibleThisFrame)
        {
            context.ChangeState<EnemyReturnState>();
            return;
        }

        if (!context.IsPlayerInAttackRange())
        {
            context.ChangeState<EnemyChaseState>();
            return;
        }

        context.Agent.isStopped = true;
        context.FaceTarget(context.Player.position);
        context.AttackTimer -= deltaTime;

        if (context.AttackPending)
        {
            context.AttackWindupTimer -= deltaTime;
            if (context.AttackWindupTimer <= 0f)
            {
                ApplyAttack(context);
                context.AttackPending = false;
                context.AttackTimer = context.AttackCooldown;
            }

            return;
        }

        if (context.AttackTimer <= 0f)
        {
            context.AttackPending = true;
            context.AttackWindupTimer = context.AttackWindup;
        }
    }

    public void Exit(EnemyController context)
    {
        context.AttackPending = false;
        context.Agent.isStopped = false;
    }

    static void ApplyAttack(EnemyController context)
    {
        if (context.Player == null)
            return;

        var playerHealth = context.Player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
            playerHealth.TakeDamage(context.AttackDamage);
    }
}

public class EnemyReturnState : IAIState<EnemyController>
{
    public void Enter(EnemyController context)
    {
        context.ResetSightTracking();
        context.Agent.isStopped = false;
        context.Agent.speed = context.ReturnSpeed;
        context.Agent.SetDestination(context.SpawnPosition);
    }

    public void Tick(EnemyController context, float deltaTime)
    {
        if (context.CanSeePlayer())
        {
            context.ChangeState<EnemyChaseState>();
            return;
        }

        if (!context.Agent.pathPending && context.Agent.remainingDistance <= context.PatrolPointTolerance)
        {
            if (context.PatrolPoints != null && context.PatrolPoints.Length > 0)
                context.ChangeState<EnemyPatrolState>();
            else
                context.ChangeState<EnemyIdleState>();
        }
    }

    public void Exit(EnemyController context) { }
}
