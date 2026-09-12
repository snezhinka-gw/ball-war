using UnityEngine;

public sealed class TerritoryRenderer
{
    private readonly Texture2D texture;
    private readonly bool[] changedRows;
    private bool dirty;

    public TerritoryRenderer(int width, int height, float cellSize, Color neutralColor)
    {
        texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "TerritoryTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        var pixels = new Color[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            pixels[i] = neutralColor;
        }

        texture.SetPixels(pixels);
        texture.Apply(false);
        changedRows = new bool[height];

        var mapObject = new GameObject("TerritoryMap");
        var renderer = mapObject.AddComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            1f);
        renderer.color = Color.white;
        renderer.sortingOrder = -10;
        mapObject.transform.localScale = Vector3.one * cellSize;
    }

    public void SetCell(int x, int y, Color color)
    {
        texture.SetPixel(x, y, color);
        changedRows[y] = true;
        dirty = true;
    }

    public void ApplyIfDirty()
    {
        if (!dirty)
        {
            return;
        }

        texture.Apply(false);
        dirty = false;
        for (var i = 0; i < changedRows.Length; i++)
        {
            changedRows[i] = false;
        }
    }
}
