using UnityEngine;

public enum PowerUpType
{
    WidePaddle, // raquete maior
    ExtraLife,  // vida extra
    SlowBall,   // bola mais lenta
}

/// <summary>
/// Cápsula que cai de um tijolo destruído. Se a raquete pegar, aplica o efeito.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(Rigidbody2D))]
public class PowerUp : MonoBehaviour
{
    public PowerUpType type;
    public float fallSpeed = 2.6f;

    public static readonly Color WideColor = new Color(0.35f, 0.95f, 0.45f);
    public static readonly Color LifeColor = new Color(1.00f, 0.45f, 0.75f);
    public static readonly Color SlowColor = new Color(0.35f, 0.65f, 1.00f);

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector3 baseScale;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    public void Setup(PowerUpType newType)
    {
        type = newType;
        sr.color = type switch
        {
            PowerUpType.WidePaddle => WideColor,
            PowerUpType.ExtraLife => LifeColor,
            _ => SlowColor,
        };
        rb.linearVelocity = Vector2.down * fallSpeed;
    }

    void Update()
    {
        // Pequena pulsação para chamar atenção
        float pulse = 1f + 0.12f * Mathf.Sin(Time.time * 10f);
        transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);

        if (transform.position.y < -6.5f) Destroy(gameObject);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Paddle>() == null) return;
        if (GameManager.Instance != null) GameManager.Instance.ApplyPowerUp(type);
        Destroy(gameObject);
    }
}
