using UnityEngine;

public sealed class BaseStation : MonoBehaviour
{
    private SimulationManager manager;
    private SpriteRenderer spriteRenderer;
    private float sendTimer;

    public int TeamId { get; private set; }
    public float MaxHealth { get; private set; }
    public float Health { get; private set; }
    public bool IsDestroyed { get; private set; }
    public float HealthRatio => MaxHealth <= 0f ? 0f : Mathf.Clamp01(Health / MaxHealth);
    public float Radius => manager.Settings.BaseRadius;

    public void Initialize(SimulationManager owner, int teamId, Vector2 position, Sprite baseSprite)
    {
        manager = owner;
        TeamId = teamId;
        MaxHealth = manager.Settings.BaseHealth;
        Health = MaxHealth;
        sendTimer = manager.Settings.BaseSendInterval * 0.5f;
        transform.position = position;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = baseSprite;
        spriteRenderer.color = manager.Settings.Teams[TeamId].Color;
        spriteRenderer.sortingOrder = 2;
        transform.localScale = Vector3.one * (manager.Settings.BaseRadius * 2f);
    }

    public void Tick(float deltaTime)
    {
        if (IsDestroyed)
        {
            return;
        }

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
        if (spriteRenderer != null)
        {
            spriteRenderer.color = new Color(0.18f, 0.18f, 0.2f, 0.55f);
        }

        manager.NotifyBaseDestroyed(this, attacker);
    }
}
