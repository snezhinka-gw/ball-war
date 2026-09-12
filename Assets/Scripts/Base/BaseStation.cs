using UnityEngine;

public sealed class BaseStation : MonoBehaviour
{
    private SimulationManager manager;
    private SpriteRenderer coreRenderer;
    private SpriteRenderer edgeRenderer;
    private Transform turretPivot;
    private float sendTimer;

    public int TeamId { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public bool IsDestroyed { get; private set; }
    public float HealthRatio => MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);
    public float Radius => manager.Settings.BaseRadius;

    public void Initialize(
        SimulationManager owner,
        int teamId,
        Vector2 position,
        Sprite coreSprite,
        Sprite edgeSprite,
        Sprite turretSprite)
    {
        manager = owner;
        TeamId = teamId;
        MaxHealth = manager.Settings.BaseHealth;
        Health = MaxHealth;
        sendTimer = manager.Settings.BaseSendInterval * 0.5f;
        transform.position = position;

        var teamColor = manager.Settings.Teams[TeamId].Color;

        var coreObject = new GameObject("圆形核心");
        coreObject.transform.SetParent(transform, false);
        coreRenderer = coreObject.AddComponent<SpriteRenderer>();
        coreRenderer.sprite = coreSprite;
        coreRenderer.color = new Color(0.12f, 0.14f, 0.18f, 1f);
        coreRenderer.sortingOrder = 2;
        coreObject.transform.localScale = Vector3.one * (manager.Settings.BaseRadius * 2f);

        var edgeObject = new GameObject("阵营边缘");
        edgeObject.transform.SetParent(transform, false);
        edgeRenderer = edgeObject.AddComponent<SpriteRenderer>();
        edgeRenderer.sprite = edgeSprite;
        edgeRenderer.color = teamColor;
        edgeRenderer.sortingOrder = 3;
        edgeObject.transform.localScale = Vector3.one * (manager.Settings.BaseRadius * 2.08f);

        var turretObject = new GameObject("匀速顺时针射击器");
        turretObject.transform.SetParent(transform, false);
        turretPivot = turretObject.transform;
        var turretRenderer = turretObject.AddComponent<SpriteRenderer>();
        turretRenderer.sprite = turretSprite;
        turretRenderer.color = Color.Lerp(Color.white, teamColor, 0.55f);
        turretRenderer.sortingOrder = 4;
        turretObject.transform.localScale = new Vector3(
            manager.Settings.TurretLength,
            manager.Settings.TurretThickness,
            1f);
    }

    public void Tick(float deltaTime)
    {
        if (IsDestroyed)
        {
            return;
        }

        turretPivot.Rotate(Vector3.forward, -manager.Settings.TurretRotationSpeed * deltaTime);
        sendTimer -= deltaTime;
        if (sendTimer > 0f)
        {
            return;
        }

        if (manager.TrySendBallFromBase(this))
        {
            sendTimer = manager.Settings.BaseSendInterval;
        }
        else
        {
            sendTimer = 0.25f;
        }
    }

    public bool SendNow()
    {
        return !IsDestroyed && manager.TrySendBallFromBase(this);
    }

    public Vector2 TurretDirection => turretPivot == null ? Vector2.right : turretPivot.right;

    public void AimToward(Vector2 worldDirection)
    {
        if (turretPivot == null || worldDirection.sqrMagnitude < 0.0001f)
        {
            return;
        }

        var angle = Mathf.Atan2(worldDirection.y, worldDirection.x) * Mathf.Rad2Deg;
        turretPivot.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    public void ReceiveDamage(float amount, BallAgent attacker)
    {
        if (IsDestroyed || amount <= 0f)
        {
            return;
        }

        Health = Mathf.Max(0f, Health - amount);
        if (Health > 0f)
        {
            return;
        }

        IsDestroyed = true;
        if (coreRenderer != null)
        {
            coreRenderer.color = new Color(0.18f, 0.18f, 0.2f, 0.55f);
        }

        if (edgeRenderer != null)
        {
            edgeRenderer.color = new Color(0.18f, 0.18f, 0.2f, 0.55f);
        }

        manager.NotifyBaseDestroyed(this, attacker);
    }
}
