using UnityEngine;

/// <summary>
/// Tijolo destrutível. Pode precisar de mais de um golpe (hitPoints).
/// Escurece conforme leva dano e pisca em branco ao ser atingido.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Brick : MonoBehaviour
{
    public int hitPoints = 1;
    public Color baseColor = Color.white;
    public int pointsPerHitPoint = 100;

    const float FlashTime = 0.08f;

    SpriteRenderer sr;
    int maxHitPoints = 1;
    float flashTimer;
    bool destroyed;

    /// <summary>Pontos ganhos ao destruir este tijolo.</summary>
    public int Points => pointsPerHitPoint * maxHitPoints;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        maxHitPoints = Mathf.Max(1, hitPoints);
    }

    public void Setup(int hp, Color color)
    {
        hitPoints = Mathf.Max(1, hp);
        maxHitPoints = hitPoints;
        baseColor = color;
        sr.color = CurrentColor();
    }

    public void Hit()
    {
        if (destroyed) return;

        hitPoints--;
        flashTimer = FlashTime;
        var gm = GameManager.Instance;

        if (hitPoints <= 0)
        {
            destroyed = true;
            if (gm != null) gm.OnBrickDestroyed(this);
            Destroy(gameObject);
        }
        else if (gm != null)
        {
            gm.OnBrickDamaged(this);
        }
    }

    void Update()
    {
        if (flashTimer > 0f)
        {
            flashTimer -= Time.deltaTime;
            sr.color = Color.Lerp(CurrentColor(), Color.white, Mathf.Clamp01(flashTimer / FlashTime));
        }
    }

    Color CurrentColor()
    {
        float t = maxHitPoints <= 1 ? 1f : (float)hitPoints / maxHitPoints;
        Color c = Color.Lerp(baseColor * 0.45f, baseColor, t);
        c.a = 1f;
        return c;
    }
}
