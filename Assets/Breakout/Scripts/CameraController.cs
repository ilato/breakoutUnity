using UnityEngine;

/// <summary>
/// Ajusta o zoom da câmera para a arena caber em qualquer proporção de tela
/// e faz o efeito de tremida (screen shake).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public float minOrthographicSize = 5f;
    [Tooltip("Metade da largura que precisa caber na tela (arena + paredes)")]
    public float requiredHalfWidth = 8.5f;

    Camera cam;
    Vector3 basePosition;
    float shakeTimer, shakeDuration, shakeStrength;

    void Awake()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    void LateUpdate()
    {
        cam.orthographicSize = Mathf.Max(minOrthographicSize, requiredHalfWidth / cam.aspect);

        Vector3 offset = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shakeTimer / shakeDuration);
            offset = (Vector3)(Random.insideUnitCircle * shakeStrength * k);
        }
        transform.position = basePosition + offset;
    }

    public void Shake(float strength, float duration)
    {
        if (strength < shakeStrength * Mathf.Clamp01(shakeTimer / Mathf.Max(shakeDuration, 0.0001f))) return;
        shakeStrength = strength;
        shakeDuration = Mathf.Max(0.01f, duration);
        shakeTimer = shakeDuration;
    }
}
