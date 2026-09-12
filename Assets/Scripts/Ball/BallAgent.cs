using UnityEngine;

public sealed class BallAgent : MonoBehaviour
{
    private SimulationManager manager;
    private Vector2 heading;
    private float scanTimer;
    private float attackTimer;
    private BallAgent target;
    private SpriteRenderer spriteRenderer;

    public int TeamId { get; private set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public int KillCount { get; private set; }
    public Vector2 CurrentVelocity => heading * manager.Settings.BallSpeed;

    public void Initialize(
        SimulationManager owner,
        int teamId,
        Vector2 position,
        Vector2 initialDirection,
        Sprite ballSprite)
    {
        manager = owner;
        TeamId = teamId;
        transform.position = position;
        MaxHealth = manager.Settings.BallHealth;
        Health = MaxHealth;
        heading = initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : Vector2.right;
        scanTimer = Random.Range(0f, 0.25f);
        attackTimer = Random.Range(0f, manager.Settings.BallAttackInterval);

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = ballSprite;
        spriteRenderer.color = manager.Settings.Teams[TeamId].Color;
        spriteRenderer.sortingOrder = 10;
        transform.localScale = Vector3.one * (manager.Settings.BallRadius * 2f);
    }

    public void Tick(float deltaTime)
    {
        if (IsDead)
        {
            return;
        }

        scanTimer -= deltaTime;
        attackTimer -= deltaTime;

        if (scanTimer <= 0f)
        {
            target = manager.FindNearestEnemy(this, manager.Settings.BallDetectionRange);
            scanTimer = 0.18f;
        }

        if (target != null && !target.IsDead)
        {
            var offset = (Vector2)target.transform.position - (Vector2)transform.position;
            if (offset.sqrMagnitude <= manager.Settings.BallAttackRange * manager.Settings.BallAttackRange)
            {
                if (attackTimer <= 0f)
                {
                    manager.ResolveAttack(this, target);
                    attackTimer = manager.Settings.BallAttackInterval;
                }
            }
        }

        var nextPosition = (Vector2)transform.position + CurrentVelocity * deltaTime;
        var min = manager.Grid.WorldMin + Vector2.one * manager.Settings.BallRadius;
        var max = manager.Grid.WorldMax - Vector2.one * manager.Settings.BallRadius;

        if (nextPosition.x <= min.x && heading.x < 0f)
        {
            nextPosition.x = min.x;
            heading.x = -heading.x;
        }
        else if (nextPosition.x >= max.x && heading.x > 0f)
        {
            nextPosition.x = max.x;
            heading.x = -heading.x;
        }

        if (nextPosition.y <= min.y && heading.y < 0f)
        {
            nextPosition.y = min.y;
            heading.y = -heading.y;
        }
        else if (nextPosition.y >= max.y && heading.y > 0f)
        {
            nextPosition.y = max.y;
            heading.y = -heading.y;
        }

        transform.position = manager.Grid.ClampWorldPosition(nextPosition, manager.Settings.BallRadius);
        manager.Grid.TryClaimWorld(transform.position, TeamId);
    }

    public void SetVelocity(Vector2 velocity)
    {
        if (velocity.sqrMagnitude > 0.0001f)
        {
            heading = velocity.normalized;
        }
    }

    public void BounceFromNormal(Vector2 normal)
    {
        if (normal.sqrMagnitude > 0.0001f)
        {
            heading = Vector2.Reflect(heading, normal.normalized).normalized;
        }
    }

    public void Eliminate()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        gameObject.SetActive(false);
    }

    public void ReceiveDamage(float amount, BallAgent attacker)
    {
        if (IsDead)
        {
            return;
        }

        Health -= amount;
        if (Health <= 0f)
        {
            IsDead = true;
            if (attacker != null)
            {
                attacker.KillCount++;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(
                    spriteRenderer.color.r,
                    spriteRenderer.color.g,
                    spriteRenderer.color.b,
                    0.25f);
            }
            gameObject.SetActive(false);
        }
    }

}
