using UnityEngine;

/// <summary>
/// Área invisível abaixo da raquete. Se a bola entrar, o jogador perde uma vida.
/// </summary>
[RequireComponent(typeof(BoxCollider2D))]
public class DeathZone : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Ball>() != null && GameManager.Instance != null)
            GameManager.Instance.OnBallLost();
    }
}
