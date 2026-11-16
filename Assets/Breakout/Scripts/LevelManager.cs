using UnityEngine;

/// <summary>
/// Monta as fases a partir de layouts em texto e instancia o prefab de tijolo.
/// Legenda: '.' = vazio, '1' = tijolo comum, '2' = resistente (2 golpes), '3' = blindado (3 golpes).
/// </summary>
public class LevelManager : MonoBehaviour
{
    [Header("Tijolos")]
    public Brick brickPrefab;
    public Vector2 brickSize = new Vector2(1.2f, 0.45f);
    public Vector2 spacing = new Vector2(0.1f, 0.12f);
    [Tooltip("Altura (y) do centro da primeira linha")]
    public float topY = 3.6f;

    [Header("Cores")]
    public Color[] rowColors =
    {
        new Color(0.94f, 0.33f, 0.36f), // vermelho
        new Color(0.98f, 0.60f, 0.25f), // laranja
        new Color(0.98f, 0.85f, 0.30f), // amarelo
        new Color(0.40f, 0.85f, 0.45f), // verde
        new Color(0.30f, 0.75f, 0.95f), // azul claro
        new Color(0.55f, 0.50f, 0.95f), // roxo
    };
    public Color strongColor = new Color(0.72f, 0.78f, 0.88f); // 2 golpes (prata)
    public Color armoredColor = new Color(0.95f, 0.72f, 0.20f); // 3 golpes (ouro)

    static readonly string[] LevelNames = { "Aquecimento", "Pirâmide", "Fortaleza" };

    static readonly string[][] Layouts =
    {
        new[]
        {
            "111111111111",
            "111111111111",
            "111111111111",
            "111111111111",
            "111111111111",
        },
        new[]
        {
            "222222222222",
            "111111111111",
            ".1111111111.",
            "..11111111..",
            "...111111...",
            "....1111....",
        },
        new[]
        {
            "3333....3333",
            "2222.11.2222",
            "111111111111",
            "1.2.1221.2.1",
            "111111111111",
            "..33....33..",
        },
    };

    public int LevelCount => Layouts.Length;
    public int RemainingBricks { get; private set; }

    public string GetLevelName(int index) =>
        index >= 0 && index < LevelNames.Length ? LevelNames[index] : "";

    /// <summary>Remove os tijolos atuais e monta a fase indicada.</summary>
    public void Build(int index)
    {
        Clear();
        if (brickPrefab == null || index < 0 || index >= Layouts.Length) return;

        string[] rows = Layouts[index];
        int cols = rows[0].Length;
        float totalWidth = cols * brickSize.x + (cols - 1) * spacing.x;
        float startX = -totalWidth / 2f + brickSize.x / 2f;

        for (int r = 0; r < rows.Length; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c];
                if (ch < '1' || ch > '9') continue;

                int hp = ch - '0';
                Vector3 pos = new Vector3(
                    startX + c * (brickSize.x + spacing.x),
                    topY - r * (brickSize.y + spacing.y),
                    0f);

                Brick brick = Instantiate(brickPrefab, pos, Quaternion.identity, transform);
                brick.name = $"Tijolo_{r}_{c}";
                brick.Setup(hp, ColorFor(r, hp));
                RemainingBricks++;
            }
        }
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
        RemainingBricks = 0;
    }

    public void NotifyBrickDestroyed()
    {
        RemainingBricks = Mathf.Max(0, RemainingBricks - 1);
    }

    Color ColorFor(int row, int hp)
    {
        if (hp >= 3) return armoredColor;
        if (hp == 2) return strongColor;
        return rowColors.Length > 0 ? rowColors[row % rowColors.Length] : Color.white;
    }
}
