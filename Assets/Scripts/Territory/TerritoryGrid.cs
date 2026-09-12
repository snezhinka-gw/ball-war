using UnityEngine;

public sealed class TerritoryGrid
{
    public const int Neutral = -1;

    private readonly int width;
    private readonly int height;
    private readonly float cellSize;
    private readonly Color[] teamColors;
    private readonly int[,] owners;
    private readonly int[] counts;
    private readonly TerritoryRenderer renderer;

    public int Width => width;
    public int Height => height;
    public Vector2 WorldMin => new Vector2(-width * cellSize * 0.5f, -height * cellSize * 0.5f);
    public Vector2 WorldMax => new Vector2(width * cellSize * 0.5f, height * cellSize * 0.5f);

    public TerritoryGrid(int width, int height, float cellSize, Color[] teamColors)
    {
        this.width = width;
        this.height = height;
        this.cellSize = cellSize;
        this.teamColors = teamColors;
        owners = new int[width, height];
        counts = new int[teamColors.Length];
        renderer = new TerritoryRenderer(width, height, cellSize, new Color(0.075f, 0.085f, 0.11f, 1f));

        for (var x = 0; x < width; x++)
        {
            for (var y = 0; y < height; y++)
            {
                owners[x, y] = Neutral;
            }
        }
    }

    public bool TryClaimWorld(Vector2 worldPosition, int teamId)
    {
        if (!WorldToCell(worldPosition, out var cell))
        {
            return false;
        }

        return TryClaimCell(cell.x, cell.y, teamId);
    }

    public bool TryClaimCell(int x, int y, int teamId)
    {
        if (x < 0 || x >= width || y < 0 || y >= height || teamId < 0 || teamId >= teamColors.Length)
        {
            return false;
        }

        if (owners[x, y] == teamId)
        {
            return false;
        }

        var previousOwner = owners[x, y];
        if (previousOwner >= 0)
        {
            counts[previousOwner]--;
        }

        owners[x, y] = teamId;
        counts[teamId]++;
        renderer.SetCell(x, y, Darken(teamColors[teamId]));
        return true;
    }

    public void SeedTeamArea(int teamId, Vector2Int center, int radius)
    {
        var radiusSquared = radius * radius;
        for (var x = center.x - radius; x <= center.x + radius; x++)
        {
            for (var y = center.y - radius; y <= center.y + radius; y++)
            {
                var dx = x - center.x;
                var dy = y - center.y;
                if (dx * dx + dy * dy <= radiusSquared)
                {
                    TryClaimCell(x, y, teamId);
                }
            }
        }
    }

    public bool WorldToCell(Vector2 worldPosition, out Vector2Int cell)
    {
        var local = worldPosition - WorldMin;
        cell = new Vector2Int(
            Mathf.FloorToInt(local.x / cellSize),
            Mathf.FloorToInt(local.y / cellSize));
        return cell.x >= 0 && cell.x < width && cell.y >= 0 && cell.y < height;
    }

    public Vector2 ClampWorldPosition(Vector2 position, float padding)
    {
        return new Vector2(
            Mathf.Clamp(position.x, WorldMin.x + padding, WorldMax.x - padding),
            Mathf.Clamp(position.y, WorldMin.y + padding, WorldMax.y - padding));
    }

    public int GetCount(int teamId)
    {
        return teamId >= 0 && teamId < counts.Length ? counts[teamId] : 0;
    }

    public float GetShare(int teamId)
    {
        var total = width * height;
        return total == 0 ? 0f : (float)GetCount(teamId) / total;
    }

    public void ApplyVisualChanges()
    {
        renderer.ApplyIfDirty();
    }

    private static Color Darken(Color color)
    {
        return new Color(color.r * 0.42f, color.g * 0.42f, color.b * 0.42f, 1f);
    }
}
