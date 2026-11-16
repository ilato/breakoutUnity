using UnityEngine;

/// <summary>Estados possíveis do jogo.</summary>
public enum GameState
{
    Menu,          // tela inicial
    Serving,       // bola presa na raquete, esperando o lançamento
    Playing,       // bola em jogo
    Paused,        // jogo pausado
    LevelComplete, // fase concluída, transição para a próxima
    GameOver,      // sem vidas
    Victory,       // todas as fases concluídas
}

/// <summary>
/// Controla o fluxo do jogo (máquina de estados), pontuação, vidas, fases,
/// power-ups e recorde (salvo com PlayerPrefs).
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referências da cena")]
    public Paddle paddle;
    public Ball ball;
    public LevelManager levelManager;
    public UIManager ui;
    public SoundManager sound;
    public CameraController cameraController;

    [Header("Prefabs e efeitos")]
    public PowerUp powerUpPrefab;
    public Sprite debrisSprite;
    public Material spriteMaterial;

    [Header("Regras")]
    public int startingLives = 3;
    public int maxLives = 5;
    [Range(0f, 1f)] public float powerUpChance = 0.15f;
    public int levelCompleteBonus = 1000;
    public float levelCompleteDelay = 2.5f;

    public GameState State { get; private set; } = GameState.Menu;
    public int Score { get; private set; }
    public int Lives { get; private set; }
    public int LevelIndex { get; private set; }

    /// <summary>A raquete só se move durante o saque e o jogo.</summary>
    public bool CanMovePaddle => State == GameState.Serving || State == GameState.Playing;

    const string HighScoreKey = "Breakout_HighScore";

    int highScore;
    float stateTimer;
    GameState stateBeforePause;
    Transform powerUpParent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Time.timeScale = 1f;
        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        powerUpParent = new GameObject("PowerUps").transform;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        GoToMenu();
    }

    void Update()
    {
        stateTimer += Time.unscaledDeltaTime;

        switch (State)
        {
            case GameState.Menu:
                if (GameInput.ConfirmPressed) StartNewGame();
                else if (GameInput.PausePressed) QuitGame();
                break;

            case GameState.Serving:
                if (GameInput.LaunchPressed)
                {
                    ball.Launch();
                    sound.PlayLaunch();
                    ui.HideMessage();
                    SetState(GameState.Playing);
                }
                else if (GameInput.PausePressed) Pause();
                break;

            case GameState.Playing:
                if (GameInput.PausePressed) Pause();
                break;

            case GameState.Paused:
                if (GameInput.PausePressed) Resume();
                else if (GameInput.MenuPressed) GoToMenu();
                break;

            case GameState.LevelComplete:
                if (stateTimer >= levelCompleteDelay) LoadLevel(LevelIndex + 1);
                break;

            case GameState.GameOver:
            case GameState.Victory:
                if (stateTimer > 1f && GameInput.ConfirmPressed) GoToMenu();
                break;
        }
    }

    void SetState(GameState newState)
    {
        State = newState;
        stateTimer = 0f;
    }

    // ---------------------------------------------------------------- Fluxo

    void GoToMenu()
    {
        Time.timeScale = 1f;
        ClearPowerUps();
        LevelIndex = 0;
        levelManager.Build(0);
        paddle.ResetPaddle();
        ball.PrepareForLevel(0);
        ball.ResetToPaddle();
        SetState(GameState.Menu);

        ui.UpdateHUD(0, 1, levelManager.LevelCount, startingLives);
        ui.ShowMessage(
            "BREAKOUT",
            "Pressione ESPAÇO para jogar",
            "Mover: SETAS / A D / mouse    |    Lançar: ESPAÇO / clique    |    Pausar: ESC\n" +
            "Power-ups:  VERDE = raquete maior    ROSA = vida extra    AZUL = bola lenta\n" +
            "Recorde: " + highScore);
    }

    void StartNewGame()
    {
        Score = 0;
        Lives = startingLives;
        LoadLevel(0);
    }

    void LoadLevel(int index)
    {
        ClearPowerUps();
        LevelIndex = index;
        levelManager.Build(index);
        paddle.ResetPaddle();
        ball.PrepareForLevel(index);
        ball.ResetToPaddle();
        SetState(GameState.Serving);
        RefreshHUD();
        ui.ShowMessage($"FASE {index + 1}", "ESPAÇO para lançar", levelManager.GetLevelName(index), false);
    }

    void CompleteLevel()
    {
        ball.Stop();
        ClearPowerUps();
        AddScore(levelCompleteBonus);
        sound.PlayLevelComplete();

        if (LevelIndex + 1 >= levelManager.LevelCount)
        {
            EndGame(true);
            return;
        }

        SetState(GameState.LevelComplete);
        ui.ShowMessage("FASE CONCLUÍDA!", "Bônus +" + levelCompleteBonus, "Preparando a próxima fase...", false);
    }

    void EndGame(bool victory)
    {
        ball.Hide();
        if (!victory) sound.PlayGameOver();

        bool newRecord = Score > highScore;
        if (newRecord)
        {
            highScore = Score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        SetState(victory ? GameState.Victory : GameState.GameOver);
        ui.ShowMessage(
            victory ? "VOCÊ VENCEU!" : "FIM DE JOGO",
            "ESPAÇO para voltar ao menu",
            "Pontuação: " + Score + "\n" + (newRecord ? "NOVO RECORDE!" : "Recorde: " + highScore));
    }

    void Pause()
    {
        stateBeforePause = State;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
        ui.ShowMessage("PAUSADO", "ESC para continuar", "M para voltar ao menu");
    }

    void Resume()
    {
        Time.timeScale = 1f;
        SetState(stateBeforePause);
        if (State == GameState.Serving) ui.ShowMessage("", "ESPAÇO para lançar", "", false);
        else ui.HideMessage();
    }

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- Eventos do jogo

    public void OnBrickDamaged(Brick brick)
    {
        AddScore(10);
        ball.SpeedUp();
        sound.PlayBrickHard();
        cameraController.Shake(0.04f, 0.08f);
    }

    public void OnBrickDestroyed(Brick brick)
    {
        AddScore(brick.Points);
        ball.SpeedUp();
        sound.PlayBrick();
        cameraController.Shake(0.07f, 0.1f);
        Debris.Burst(debrisSprite, spriteMaterial, brick.transform.position, brick.baseColor, 8);

        if (powerUpPrefab != null && Random.value < powerUpChance)
            SpawnPowerUp(brick.transform.position);

        levelManager.NotifyBrickDestroyed();
        if (levelManager.RemainingBricks <= 0) CompleteLevel();
    }

    public void OnBallLost()
    {
        if (State != GameState.Playing) return;

        Lives--;
        RefreshHUD();
        sound.PlayLose();
        cameraController.Shake(0.25f, 0.35f);
        ClearPowerUps();

        if (Lives <= 0)
        {
            EndGame(false);
            return;
        }

        paddle.ResetPaddle();
        ball.PrepareForLevel(LevelIndex);
        ball.ResetToPaddle();
        SetState(GameState.Serving);
        ui.ShowMessage("", "ESPAÇO para lançar", Lives == 1 ? "Última vida!" : $"Restam {Lives} vidas", false);
    }

    public void ApplyPowerUp(PowerUpType type)
    {
        if (State != GameState.Playing) return;

        switch (type)
        {
            case PowerUpType.WidePaddle:
                paddle.ActivateWide();
                ui.ShowToast("RAQUETE MAIOR!", PowerUp.WideColor);
                break;
            case PowerUpType.ExtraLife:
                Lives = Mathf.Min(maxLives, Lives + 1);
                ui.ShowToast("VIDA EXTRA!", PowerUp.LifeColor);
                break;
            case PowerUpType.SlowBall:
                ball.SlowDown();
                ui.ShowToast("BOLA LENTA!", PowerUp.SlowColor);
                break;
        }
        sound.PlayPowerUp();
        AddScore(50);
    }

    // ---------------------------------------------------------------- Auxiliares

    void SpawnPowerUp(Vector3 position)
    {
        var type = (PowerUpType)Random.Range(0, 3);
        if (type == PowerUpType.ExtraLife && Lives >= maxLives) type = PowerUpType.WidePaddle;

        PowerUp p = Instantiate(powerUpPrefab, position, Quaternion.identity, powerUpParent);
        p.Setup(type);
    }

    void ClearPowerUps()
    {
        if (powerUpParent == null) return;
        foreach (Transform child in powerUpParent) Destroy(child.gameObject);
    }

    void AddScore(int amount)
    {
        Score += amount;
        RefreshHUD();
    }

    void RefreshHUD()
    {
        ui.UpdateHUD(Score, LevelIndex + 1, levelManager.LevelCount, Lives);
    }
}
