using System;
using UnityEngine;

[Serializable]
public sealed class TeamConfig
{
    public string Name;
    public Color Color;
    public Vector2 BasePosition;

    public TeamConfig(string name, Color color, Vector2 basePosition)
    {
        Name = name;
        Color = color;
        BasePosition = basePosition;
    }
}

public sealed class SimulationSettings
{
    public int GridWidth = 256;
    public int GridHeight = 256;
    public float CellSize = 0.1f;
    public int BallsPerTeam = 16;
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
    public float BaseSendInterval = 2f;
    public int MaxBallsPerTeam = 40;
    public float TurretRotationSpeed = 120f;
    public float TurretLength = 0.34f;
    public float TurretThickness = 0.08f;
    public TeamConfig[] Teams;

    public SimulationSettings()
    {
        Teams = new[]
        {
            new TeamConfig("红队", new Color(0.95f, 0.20f, 0.20f), Vector2.zero),
            new TeamConfig("蓝队", new Color(0.20f, 0.48f, 1.00f), Vector2.zero),
            new TeamConfig("绿队", new Color(0.20f, 0.85f, 0.38f), Vector2.zero),
            new TeamConfig("黄队", new Color(1.00f, 0.75f, 0.12f), Vector2.zero)
        };

        ResetBasePositions();
    }

    public void ResetBasePositions()
    {
        var mapWidth = GridWidth * CellSize;
        var mapHeight = GridHeight * CellSize;
        Teams[0].BasePosition = new Vector2(-mapWidth * 0.34f, -mapHeight * 0.34f);
        Teams[1].BasePosition = new Vector2(mapWidth * 0.34f, mapHeight * 0.34f);
        Teams[2].BasePosition = new Vector2(-mapWidth * 0.34f, mapHeight * 0.34f);
        Teams[3].BasePosition = new Vector2(mapWidth * 0.34f, -mapHeight * 0.34f);
    }
}
