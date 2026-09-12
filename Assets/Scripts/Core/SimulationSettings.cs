using System;
using UnityEngine;

[Serializable]
public sealed class TeamConfig
{
    public string Name;
    public Color Color;

    public TeamConfig(string name, Color color)
    {
        Name = name;
        Color = color;
    }
}

public sealed class SimulationSettings
{
    public int GridWidth = 128;
    public int GridHeight = 128;
    public float CellSize = 0.1f;
    public int BallsPerTeam = 10;
    public float MatchDuration = 300f;
    public int SpawnRadius = 7;
    public float BallSpeed = 1.15f;
    public float BallRadius = 0.095f;
    public float BallHealth = 10f;
    public float BallAttack = 2.5f;
    public float BallAttackRange = 0.36f;
    public float BallAttackInterval = 0.55f;
    public float BallDetectionRange = 2.4f;
    public float BaseHealth = 100f;
    public float BaseRadius = 0.58f;
    public float BaseCollisionDamage = 12f;
    public float BaseSendInterval = 2.5f;
    public int MaxBallsPerTeam = 24;
    public TeamConfig[] Teams;

    public SimulationSettings()
    {
        Teams = new[]
        {
            new TeamConfig("红队", new Color(0.95f, 0.20f, 0.20f)),
            new TeamConfig("蓝队", new Color(0.20f, 0.48f, 1.00f)),
            new TeamConfig("绿队", new Color(0.20f, 0.85f, 0.38f)),
            new TeamConfig("黄队", new Color(1.00f, 0.75f, 0.12f))
        };
    }
}
