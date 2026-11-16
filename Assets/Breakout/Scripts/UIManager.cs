using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Interface: HUD (pontos, fase, vidas), mensagens centrais (menu, pausa, fim de jogo)
/// e avisos rápidos (power-ups).
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    public Text scoreText;
    public Text levelText;
    public Text livesText;

    [Header("Mensagens")]
    public GameObject messagePanel;
    public Image overlay;
    public Text titleText;
    public Text subtitleText;
    public Text hintText;

    [Header("Aviso rápido")]
    public Text toastText;

    const float ToastDuration = 1.5f;
    float toastTimer;

    public void UpdateHUD(int score, int levelNumber, int levelCount, int lives)
    {
        scoreText.text = "PONTOS  " + score;
        levelText.text = $"FASE {levelNumber}/{levelCount}";
        livesText.text = "VIDAS  " + lives;
    }

    public void ShowMessage(string title, string subtitle, string hint = "", bool dim = true)
    {
        messagePanel.SetActive(true);
        overlay.enabled = dim;
        titleText.text = title;
        subtitleText.text = subtitle;
        hintText.text = hint;
    }

    public void HideMessage()
    {
        messagePanel.SetActive(false);
    }

    public void ShowToast(string message, Color color)
    {
        toastText.text = message;
        toastText.color = color;
        toastText.enabled = true;
        toastTimer = ToastDuration;
    }

    void Update()
    {
        // Subtítulo piscando ("Pressione ESPAÇO...")
        if (messagePanel.activeSelf)
        {
            Color c = subtitleText.color;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 2.5f));
            subtitleText.color = c;
        }

        // Aviso rápido: aparece com um "pop" e some aos poucos
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            Color c = toastText.color;
            c.a = Mathf.Clamp01(toastTimer / 0.4f);
            toastText.color = c;
            float pop = Mathf.Clamp01((toastTimer - (ToastDuration - 0.15f)) / 0.15f);
            toastText.transform.localScale = Vector3.one * (1f + 0.25f * pop);
            if (toastTimer <= 0f) toastText.enabled = false;
        }
    }
}
