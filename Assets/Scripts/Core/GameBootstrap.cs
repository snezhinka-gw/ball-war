using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    [SerializeField] private int gridWidth = 128;
    [SerializeField] private int gridHeight = 128;
    [SerializeField] private float cellSize = 0.1f;
    [SerializeField] private int ballsPerTeam = 10;
    [SerializeField] private float matchDuration = 300f;

    private void Start()
    {
        Application.targetFrameRate = 60;

        var settings = new SimulationSettings
        {
            GridWidth = gridWidth,
            GridHeight = gridHeight,
            CellSize = cellSize,
            BallsPerTeam = ballsPerTeam,
            MatchDuration = matchDuration
        };

        var managerObject = new GameObject("SimulationManager");
        var manager = managerObject.AddComponent<SimulationManager>();
        manager.Initialize(settings);

        var hudObject = new GameObject("SimulationHUD");
        var hud = hudObject.AddComponent<SimulationHUD>();
        hud.Initialize(manager);
    }
}
