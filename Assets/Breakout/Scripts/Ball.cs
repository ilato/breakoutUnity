using UnityEngine;

/// <summary>
/// Bola do Breakout. Usa física 2D (Rigidbody2D dinâmico + material com quique total)
/// e corrige a velocidade a cada passo de física para manter o ritmo constante.
/// O ângulo de saída da raquete depende de onde a bola bate nela.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    [Header("Velocidade")]
    public float baseSpeed = 7f;
    public float speedIncreasePerLevel = 0.8f;
    public float speedIncreasePerHit = 0.05f;
    public float maxSpeed = 13f;

    [Header("Rebote")]
    [Range(10f, 80f)] public float maxBounceAngle = 60f;
    [Tooltip("Evita que a bola fique quase na horizontal")]
    [Range(0.05f, 0.6f)] public float minVerticalRatio = 0.25f;

    [Header("Saque")]
    public Paddle paddle;
    public Vector2 offsetFromPaddle = new Vector2(0f, 0.32f);

    Rigidbody2D rb;
    TrailRenderer trail;
    float currentSpeed;
    bool launched;

    public bool Launched => launched;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        trail = GetComponent<TrailRenderer>();
        currentSpeed = baseSpeed;
    }

    void Update()
    {
        // Enquanto não foi lançada, a bola acompanha a raquete.
        if (!launched && paddle != null)
            transform.position = paddle.transform.position + (Vector3)offsetFromPaddle;
    }

    void FixedUpdate()
    {
        if (!launched) return;

        Vector2 dir = rb.linearVelocity;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.up;
        dir.Normalize();

        // Impede trajetórias quase horizontais (a bola ficaria presa indo e voltando).
        if (Mathf.Abs(dir.y) < minVerticalRatio)
        {
            float signY = dir.y >= 0f ? 1f : -1f;
            float signX = dir.x >= 0f ? 1f : -1f;
            dir = new Vector2(signX * Mathf.Sqrt(1f - minVerticalRatio * minVerticalRatio), signY * minVerticalRatio);
        }

        // Impede trajetórias perfeitamente verticais.
        if (Mathf.Abs(dir.x) < 0.05f)
        {
            dir.x = Random.value < 0.5f ? -0.1f : 0.1f;
            dir.Normalize();
        }

        rb.linearVelocity = dir * currentSpeed;
    }

    /// <summary>Define a velocidade inicial de acordo com a fase.</summary>
    public void PrepareForLevel(int levelIndex)
    {
        currentSpeed = Mathf.Min(baseSpeed + levelIndex * speedIncreasePerLevel, maxSpeed);
    }

    /// <summary>Prende a bola na raquete, pronta para o saque.</summary>
    public void ResetToPaddle()
    {
        gameObject.SetActive(true);
        launched = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (paddle != null)
            transform.position = paddle.transform.position + (Vector3)offsetFromPaddle;
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = false;
        }
    }

    /// <summary>Lança a bola para cima com um pequeno ângulo aleatório.</summary>
    public void Launch()
    {
        launched = true;
        rb.simulated = true;
        rb.position = transform.position;
        float angle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
        rb.linearVelocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * currentSpeed;
        if (trail != null) trail.emitting = true;
    }

    /// <summary>Para a bola no lugar.</summary>
    public void Stop()
    {
        launched = false;
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        if (trail != null) trail.emitting = false;
    }

    public void Hide()
    {
        Stop();
        gameObject.SetActive(false);
    }

    public void SpeedUp()
    {
        currentSpeed = Mathf.Min(currentSpeed + speedIncreasePerHit, maxSpeed);
    }

    public void SlowDown()
    {
        currentSpeed = Mathf.Max(baseSpeed * 0.75f, currentSpeed * 0.7f);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!launched) return;
        var gm = GameManager.Instance;

        // Raquete: o ângulo depende do ponto de contato (centro = reto, pontas = inclinado).
        var hitPaddle = collision.collider.GetComponent<Paddle>();
        if (hitPaddle != null)
        {
            float offset = (transform.position.x - hitPaddle.transform.position.x) / hitPaddle.HalfWidth;
            offset = Mathf.Clamp(offset, -1f, 1f);
            float angle = offset * maxBounceAngle * Mathf.Deg2Rad;
            rb.linearVelocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * currentSpeed;
            if (gm != null) gm.sound.PlayPaddle();
            return;
        }

        // Tijolo
        var brick = collision.collider.GetComponent<Brick>();
        if (brick != null)
        {
            brick.Hit();
            return;
        }

        // Parede
        if (gm != null) gm.sound.PlayWall();
    }
}
