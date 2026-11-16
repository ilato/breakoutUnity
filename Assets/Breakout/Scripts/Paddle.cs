using UnityEngine;

/// <summary>
/// Raquete controlada pelo jogador. Move-se pelo teclado/controle ou seguindo o mouse.
/// Usa um Rigidbody2D cinemático (MovePosition) para colidir corretamente com a bola.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Paddle : MonoBehaviour
{
    [Header("Movimento")]
    public float speed = 14f;
    public float mouseFollowSpeed = 40f;
    [Tooltip("Borda interna da parede esquerda (x)")]
    public float leftLimit = -8f;
    [Tooltip("Borda interna da parede direita (x)")]
    public float rightLimit = 8f;

    [Header("Power-up: raquete maior")]
    public float wideMultiplier = 1.6f;
    public float wideDuration = 12f;

    Rigidbody2D rb;
    BoxCollider2D box;
    Camera cam;
    Vector3 baseScale;
    float startY;
    float targetX;
    float mouseTargetX;
    bool usingMouse;
    float wideTimer;

    /// <summary>Metade da largura atual da raquete (considera a escala).</summary>
    public float HalfWidth => box.size.x * Mathf.Abs(transform.localScale.x) * 0.5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        box = GetComponent<BoxCollider2D>();
        baseScale = transform.localScale;
        startY = transform.position.y;
        targetX = transform.position.x;
    }

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Atualiza o rastreio do mouse todo frame, mesmo quando não está sendo usado.
        bool mouseMoved = GameInput.MouseMoved(cam, out float mouseX);

        var gm = GameManager.Instance;
        if (gm == null || !gm.CanMovePaddle) return;

        float h = GameInput.Horizontal;
        if (Mathf.Abs(h) > 0.01f)
        {
            usingMouse = false;
            targetX += h * speed * Time.deltaTime;
        }
        else if (mouseMoved)
        {
            usingMouse = true;
            mouseTargetX = mouseX;
        }

        if (usingMouse)
            targetX = Mathf.MoveTowards(targetX, mouseTargetX, mouseFollowSpeed * Time.deltaTime);

        // Duração do power-up
        if (wideTimer > 0f)
        {
            wideTimer -= Time.deltaTime;
            if (wideTimer <= 0f) SetWide(false);
        }

        targetX = Mathf.Clamp(targetX, leftLimit + HalfWidth, rightLimit - HalfWidth);
    }

    void FixedUpdate()
    {
        rb.MovePosition(new Vector2(targetX, startY));
    }

    /// <summary>Volta a raquete para o centro e remove o power-up.</summary>
    public void ResetPaddle()
    {
        SetWide(false);
        usingMouse = false;
        targetX = 0f;
        transform.position = new Vector3(0f, startY, transform.position.z);
        rb.position = new Vector2(0f, startY);
    }

    /// <summary>Ativa (ou renova) o power-up de raquete maior.</summary>
    public void ActivateWide() => SetWide(true);

    void SetWide(bool on)
    {
        transform.localScale = on
            ? new Vector3(baseScale.x * wideMultiplier, baseScale.y, baseScale.z)
            : baseScale;
        wideTimer = on ? wideDuration : 0f;
    }
}
