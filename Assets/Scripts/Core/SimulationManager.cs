using System.Collections.Generic;
using UnityEngine;

public sealed class TeamRuntime
{
    public int Id;
    public string Name;
    public Color Color;
    public int Kills;
    public int Alive;
}

public sealed class SimulationManager : MonoBehaviour
{
    private readonly List<BallAgent> balls = new List<BallAgent>();
    private readonly List<BaseStation> bases = new List<BaseStation>();
    private readonly List<TeamRuntime> teams = new List<TeamRuntime>();
    private Sprite ballSprite;
    private Sprite baseCoreSprite;
    private Sprite baseEdgeSprite;
    private Sprite turretSprite;
    private bool initialized;

    public SimulationSettings Settings { get; private set; }
    public TerritoryGrid Grid { get; private set; }
    public IReadOnlyList<BallAgent> Balls => balls;
    public IReadOnlyList<BaseStation> Bases => bases;
    public IReadOnlyList<TeamRuntime> Teams => teams;
    public float ElapsedTime { get; private set; }
    public float SimulationSpeed { get; private set; } = 1f;
    public bool IsRunning { get; private set; } = true;
    public int WinnerTeamId { get; private set; } = -1;
    public bool IsFinished => WinnerTeamId >= 0 || (bases.Count > 0 && GetRemainingBaseCount() == 0);
    public bool IsDraw => bases.Count > 0 && GetRemainingBaseCount() == 0 && WinnerTeamId < 0;

    public void Initialize(SimulationSettings settings)
    {
        Settings = settings;
        var colors = new Color[settings.Teams.Length];
        for (var i = 0; i < settings.Teams.Length; i++)
        {
            colors[i] = settings.Teams[i].Color;
            teams.Add(new TeamRuntime
            {
                Id = i,
                Name = settings.Teams[i].Name,
                Color = settings.Teams[i].Color
            });
        }

        Grid = new TerritoryGrid(settings.GridWidth, settings.GridHeight, settings.CellSize, colors);
        CreateCamera(settings);
        ballSprite = CreateBallSprite();
        baseCoreSprite = CreateBaseCoreSprite();
        baseEdgeSprite = CreateBaseEdgeSprite();
        turretSprite = CreateTurretSprite();
        CreateBases();
        SpawnTeams();
        initialized = true;
        UpdateTeamStats();
    }

    private void Update()
    {
        if (!initialized || !IsRunning || IsFinished)
        {
            return;
        }

        var deltaTime = Time.deltaTime * SimulationSpeed;
        ElapsedTime += deltaTime;

        for (var i = 0; i < bases.Count; i++)
        {
            bases[i].Tick(deltaTime);
        }

        for (var i = 0; i < balls.Count; i++)
        {
            balls[i].Tick(deltaTime);
        }

        ResolveBaseCollisions();
        ResolveBallCollisions();

        for (var i = balls.Count - 1; i >= 0; i--)
        {
            if (!balls[i].IsDead)
            {
                continue;
            }

            Destroy(balls[i].gameObject);
            balls.RemoveAt(i);
        }

        Grid.ApplyVisualChanges();
        UpdateTeamStats();
        EvaluateWinner();
    }

    public BallAgent FindNearestEnemy(BallAgent source, float maxDistance)
    {
        var bestDistanceSquared = maxDistance * maxDistance;
        BallAgent best = null;

        for (var i = 0; i < balls.Count; i++)
        {
            var candidate = balls[i];
            if (candidate == source || candidate.IsDead || candidate.TeamId == source.TeamId)
            {
                continue;
            }

            var distanceSquared = ((Vector2)candidate.transform.position - (Vector2)source.transform.position).sqrMagnitude;
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                best = candidate;
            }
        }

        return best;
    }

    public void ResolveAttack(BallAgent attacker, BallAgent target)
    {
        if (attacker == null || target == null || attacker.IsDead || target.IsDead)
        {
            return;
        }

        target.ReceiveDamage(Settings.BallAttack, attacker);
    }

    public float GetTerritoryShare(int teamId)
    {
        return Grid == null ? 0f : Grid.GetShare(teamId);
    }

    public int GetAliveCount(int teamId)
    {
        var count = 0;
        for (var i = 0; i < balls.Count; i++)
        {
            if (!balls[i].IsDead && balls[i].TeamId == teamId)
            {
                count++;
            }
        }

        return count;
    }

    public int GetRemainingBaseCount()
    {
        var count = 0;
        for (var i = 0; i < bases.Count; i++)
        {
            if (!bases[i].IsDestroyed)
            {
                count++;
            }
        }

        return count;
    }

    public bool SendBall(int teamId)
    {
        return teamId >= 0 && teamId < bases.Count && bases[teamId].SendNow();
    }

    public bool TrySendBallFromBase(BaseStation source)
    {
        if (source == null || source.IsDestroyed || GetAliveCount(source.TeamId) >= Settings.MaxBallsPerTeam)
        {
            return false;
        }

        var direction = source.TurretDirection.normalized;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector2.up;
        }

        var spawnPosition = (Vector2)source.transform.position +
                            direction * (Settings.BaseRadius + Settings.BallRadius + 0.025f);
        CreateBall(source.TeamId, spawnPosition, direction, $"{teams[source.TeamId].Name}_基地派出的小球");
        return true;
    }

    public void NotifyBaseDestroyed(BaseStation destroyedBase, BallAgent attacker)
    {
        for (var i = 0; i < balls.Count; i++)
        {
            if (balls[i].TeamId == destroyedBase.TeamId)
            {
                balls[i].Eliminate();
            }
        }
    }

    private void ResolveBallCollisions()
    {
        var minimumDistance = Settings.BallRadius * 2f;
        var minimumDistanceSquared = minimumDistance * minimumDistance;

        for (var i = 0; i < balls.Count; i++)
        {
            var first = balls[i];
            if (first.IsDead)
            {
                continue;
            }

            for (var j = i + 1; j < balls.Count; j++)
            {
                var second = balls[j];
                if (second.IsDead)
                {
                    continue;
                }

                var offset = (Vector2)second.transform.position - (Vector2)first.transform.position;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > minimumDistanceSquared)
                {
                    continue;
                }

                var distance = Mathf.Sqrt(distanceSquared);
                var normal = distance > 0.0001f
                    ? offset / distance
                    : (second.CurrentVelocity - first.CurrentVelocity).normalized;
                if (normal.sqrMagnitude < 0.0001f)
                {
                    normal = Vector2.right;
                }

                var overlap = minimumDistance - distance;
                var correction = normal * (overlap * 0.5f + 0.0001f);
                first.transform.position = ClampBallPosition((Vector2)first.transform.position - correction);
                second.transform.position = ClampBallPosition((Vector2)second.transform.position + correction);

                var relativeVelocity = Vector2.Dot(second.CurrentVelocity - first.CurrentVelocity, normal);
                if (relativeVelocity >= 0f)
                {
                    continue;
                }

                // Equal-mass elastic collision: exchange the velocity components along the collision normal.
                first.SetVelocity(first.CurrentVelocity + normal * relativeVelocity);
                second.SetVelocity(second.CurrentVelocity - normal * relativeVelocity);
            }
        }
    }

    private void ResolveBaseCollisions()
    {
        var ballRadius = Settings.BallRadius;
        for (var i = 0; i < balls.Count; i++)
        {
            var ball = balls[i];
            if (ball.IsDead)
            {
                continue;
            }

            for (var j = 0; j < bases.Count; j++)
            {
                var targetBase = bases[j];
                if (targetBase.IsDestroyed || targetBase.TeamId == ball.TeamId)
                {
                    continue;
                }

                var offset = (Vector2)ball.transform.position - (Vector2)targetBase.transform.position;
                var minimumDistance = targetBase.Radius + ballRadius;
                var distanceSquared = offset.sqrMagnitude;
                if (distanceSquared > minimumDistance * minimumDistance)
                {
                    continue;
                }

                var distance = Mathf.Sqrt(distanceSquared);
                var normal = distance > 0.0001f
                    ? offset / distance
                    : -ball.CurrentVelocity.normalized;
                if (normal.sqrMagnitude < 0.0001f)
                {
                    normal = Vector2.up;
                }

                ball.transform.position = Grid.ClampWorldPosition(
                    (Vector2)targetBase.transform.position + normal * (minimumDistance + 0.0001f),
                    ballRadius);
                if (Vector2.Dot(ball.CurrentVelocity, normal) < 0f)
                {
                    ball.BounceFromNormal(normal);
                    targetBase.ReceiveDamage(Settings.BaseCollisionDamage, ball);
                }
            }
        }
    }

    private Vector2 ClampBallPosition(Vector2 position)
    {
        return Grid.ClampWorldPosition(position, Settings.BallRadius);
    }

    public void ToggleRunning()
    {
        if (!IsFinished)
        {
            IsRunning = !IsRunning;
        }
    }

    public void SetSpeed(float speed)
    {
        SimulationSpeed = Mathf.Clamp(speed, 0.25f, 8f);
    }

    public void Restart()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }

    private void SpawnTeams()
    {
        for (var teamId = 0; teamId < teams.Count; teamId++)
        {
            var basePosition = (Vector2)bases[teamId].transform.position;
            Grid.WorldToCell(basePosition, out var centerCell);
            Grid.SeedTeamArea(teamId, centerCell, Settings.SpawnRadius);

            var direction = bases[teamId].TurretDirection.normalized;
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector2.up;
            }

            for (var i = 0; i < Settings.BallsPerTeam; i++)
            {
                var angle = (Mathf.PI * 2f * i) / Mathf.Max(1, Settings.BallsPerTeam);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.25f;
                CreateBall(teamId, basePosition + offset, direction, $"{teams[teamId].Name}_初始球球_{i + 1}");
            }
        }

        Grid.ApplyVisualChanges();
    }

    private void CreateBases()
    {
        var positions = GetBasePositions();
        for (var teamId = 0; teamId < teams.Count; teamId++)
        {
            var baseObject = new GameObject($"{teams[teamId].Name}_基地");
            var baseStation = baseObject.AddComponent<BaseStation>();
            var basePosition = Grid.ClampWorldPosition(
                positions[teamId],
                Settings.BaseRadius + Settings.BallRadius);
            baseStation.Initialize(this, teamId, basePosition, baseCoreSprite, baseEdgeSprite, turretSprite);
            baseStation.AimToward(-basePosition);
            bases.Add(baseStation);
        }
    }

    private void CreateBall(int teamId, Vector2 position, Vector2 direction, string objectName)
    {
        var ballObject = new GameObject(objectName);
        var agent = ballObject.AddComponent<BallAgent>();
        agent.Initialize(this, teamId, position, direction, ballSprite);
        balls.Add(agent);
    }

    private Vector2[] GetBasePositions()
    {
        var positions = new Vector2[teams.Count];
        for (var i = 0; i < teams.Count; i++)
        {
            positions[i] = Settings.Teams[i].BasePosition;
        }

        return positions;
    }

    private void UpdateTeamStats()
    {
        for (var i = 0; i < teams.Count; i++)
        {
            teams[i].Alive = GetAliveCount(i);
            teams[i].Kills = 0;
        }

        for (var i = 0; i < balls.Count; i++)
        {
            if (balls[i].TeamId >= 0 && balls[i].TeamId < teams.Count)
            {
                teams[balls[i].TeamId].Kills += balls[i].KillCount;
            }
        }
    }

    private void EvaluateWinner()
    {
        var remainingBases = 0;
        var lastTeam = -1;
        for (var i = 0; i < bases.Count; i++)
        {
            if (!bases[i].IsDestroyed)
            {
                remainingBases++;
                lastTeam = bases[i].TeamId;
            }
        }

        if (remainingBases == 1)
        {
            WinnerTeamId = lastTeam;
            IsRunning = false;
        }
        else if (remainingBases == 0)
        {
            IsRunning = false;
        }
    }

    private static void CreateCamera(SimulationSettings settings)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = settings.GridHeight * settings.CellSize * 0.58f;
        camera.backgroundColor = new Color(0.025f, 0.03f, 0.045f, 1f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    private static Sprite CreateBallSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BallSprite",
            filterMode = FilterMode.Bilinear
        };
        var center = (size - 1) * 0.5f;
        var radius = size * 0.46f;
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateBaseCoreSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BaseCoreSprite",
            filterMode = FilterMode.Bilinear
        };
        var center = (size - 1) * 0.5f;
        var radius = size * 0.46f;
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateBaseEdgeSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BaseEdgeSprite",
            filterMode = FilterMode.Bilinear
        };
        var center = (size - 1) * 0.5f;
        var outerRadius = size * 0.49f;
        var innerRadius = size * 0.42f;
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                texture.SetPixel(x, y, distance <= outerRadius && distance >= innerRadius
                    ? Color.white
                    : Color.clear);
            }
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateTurretSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "BaseTurretSprite",
            filterMode = FilterMode.Bilinear
        };
        for (var x = 0; x < size; x++)
        {
            for (var y = 0; y < size; y++)
            {
                var inBarrel = x >= 5 && x <= 58 && y >= 27 && y <= 36;
                var inMuzzle = x >= 52 && x <= 61 && y >= 24 && y <= 39;
                texture.SetPixel(x, y, inBarrel || inMuzzle ? Color.white : Color.clear);
            }
        }

        texture.Apply(false);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.10f, 0.5f), size);
    }
}
