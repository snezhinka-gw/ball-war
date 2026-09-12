using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private int gridWidth = 256;
    [SerializeField] private int gridHeight = 256;
    [SerializeField] private float cellSize = 0.1f;
    [SerializeField] private int ballsPerTeam = 16;

    private SimulationSettings settings;
    private Vector2 settingsScroll;
    private bool gameStarted;

    private void Start()
    {
        Application.targetFrameRate = 60;
        settings = CreateDefaultSettings();
    }

    private void OnGUI()
    {
        if (gameStarted)
        {
            return;
        }

        DrawStartPage();
    }

    private void DrawStartPage()
    {
        if (settings == null)
        {
            settings = CreateDefaultSettings();
        }

        var width = Mathf.Min(Screen.width - 32f, 920f);
        var height = Mathf.Min(Screen.height - 32f, 820f);
        var left = (Screen.width - width) * 0.5f;
        var top = (Screen.height - height) * 0.5f;

        GUILayout.BeginArea(new Rect(left, top, width, height), GUI.skin.box);
        GUILayout.Label("球球领土战争");
        GUILayout.Label("自动模拟模式 · 调整参数后开始比赛");
        GUILayout.Space(8f);

        settingsScroll = GUILayout.BeginScrollView(settingsScroll, GUILayout.ExpandHeight(true));
        GUILayout.Label("地图与比赛参数");
        GUILayout.Label($"地图尺寸：{settings.GridWidth} × {settings.GridHeight}（已扩大地图）");
        settings.BallsPerTeam = DrawIntSlider("初始球球数量", settings.BallsPerTeam, 8, 40);
        settings.MaxBallsPerTeam = Mathf.Max(
            settings.BallsPerTeam,
            DrawIntSlider("每阵营球球上限", settings.MaxBallsPerTeam, 16, 80));
        settings.BaseHealth = DrawFloatSlider("基地最大血量", settings.BaseHealth, 40f, 300f, "0");
        settings.BaseCollisionDamage = DrawFloatSlider("基地单次碰撞伤害", settings.BaseCollisionDamage, 4f, 40f, "0.0");
        settings.BaseSendInterval = DrawFloatSlider("基地自动发球间隔", settings.BaseSendInterval, 0.5f, 6f, "0.00");
        settings.BallSpeed = DrawFloatSlider("球球移动速度", settings.BallSpeed, 0.5f, 3f, "0.00");
        settings.TurretRotationSpeed = DrawFloatSlider("射击器转速", settings.TurretRotationSpeed, 30f, 240f, "0");

        GUILayout.Space(10f);
        GUILayout.Label("阵营基地设置");
        for (var i = 0; i < settings.Teams.Length; i++)
        {
            DrawTeamSettings(settings.Teams[i]);
        }

        GUILayout.EndScrollView();
        GUILayout.Space(8f);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("恢复默认设置", GUILayout.Height(34f)))
        {
            settings = CreateDefaultSettings();
        }

        if (GUILayout.Button("开始比赛", GUILayout.Height(34f)))
        {
            StartGame();
        }
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    private void DrawTeamSettings(TeamConfig team)
    {
        GUILayout.BeginVertical(GUI.skin.box);
        var oldColor = GUI.color;
        GUI.color = team.Color;
        GUILayout.Label($"{team.Name} · 基地位置和颜色");
        GUI.color = oldColor;

        var halfWidth = settings.GridWidth * settings.CellSize * 0.5f;
        var halfHeight = settings.GridHeight * settings.CellSize * 0.5f;
        var positionLimitX = Mathf.Max(0.5f, halfWidth - settings.BaseRadius - settings.BallRadius);
        var positionLimitY = Mathf.Max(0.5f, halfHeight - settings.BaseRadius - settings.BallRadius);
        team.BasePosition = new Vector2(
            DrawFloatSlider("位置 X", team.BasePosition.x, -positionLimitX, positionLimitX, "0.00"),
            DrawFloatSlider("位置 Y", team.BasePosition.y, -positionLimitY, positionLimitY, "0.00"));

        team.Color = new Color(
            DrawFloatSlider("颜色 R", team.Color.r, 0f, 1f, "0.00"),
            DrawFloatSlider("颜色 G", team.Color.g, 0f, 1f, "0.00"),
            DrawFloatSlider("颜色 B", team.Color.b, 0f, 1f, "0.00"),
            1f);
        GUILayout.EndVertical();
    }

    private static int DrawIntSlider(string label, int value, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}：{value}", GUILayout.Width(180f));
        var result = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return Mathf.RoundToInt(result);
    }

    private static float DrawFloatSlider(string label, float value, float min, float max, string format)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}：{value.ToString(format)}", GUILayout.Width(180f));
        var result = GUILayout.HorizontalSlider(value, min, max);
        GUILayout.EndHorizontal();
        return result;
    }

    private SimulationSettings CreateDefaultSettings()
    {
        var result = new SimulationSettings
        {
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            CellSize = cellSize,
            BallsPerTeam = ballsPerTeam
        };
        result.ResetBasePositions();
        return result;
    }

    private void StartGame()
    {
        settings.BallsPerTeam = Mathf.Max(1, settings.BallsPerTeam);
        settings.MaxBallsPerTeam = Mathf.Max(settings.BallsPerTeam, settings.MaxBallsPerTeam);
        for (var i = 0; i < settings.Teams.Length; i++)
        {
            var team = settings.Teams[i];
            var limitX = settings.GridWidth * settings.CellSize * 0.5f - settings.BaseRadius - settings.BallRadius;
            var limitY = settings.GridHeight * settings.CellSize * 0.5f - settings.BaseRadius - settings.BallRadius;
            team.BasePosition = new Vector2(
                Mathf.Clamp(team.BasePosition.x, -limitX, limitX),
                Mathf.Clamp(team.BasePosition.y, -limitY, limitY));
        }

        var managerObject = new GameObject("SimulationManager");
        var manager = managerObject.AddComponent<SimulationManager>();
        manager.Initialize(settings);

        var hudObject = new GameObject("SimulationHUD");
        var hud = hudObject.AddComponent<SimulationHUD>();
        hud.Initialize(manager);
        gameStarted = true;
    }
}
