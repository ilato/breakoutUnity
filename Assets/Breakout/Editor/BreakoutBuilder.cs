using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Monta automaticamente o jogo: gera os sprites, materiais, prefabs e a cena "Breakout".
/// Roda sozinho na primeira vez que o projeto abre (se a cena ainda não existir)
/// e pode ser executado de novo pelo menu: Breakout > Montar Jogo.
/// </summary>
public static class BreakoutBuilder
{
    const string Root = "Assets/Breakout";
    const string ArtDir = Root + "/Art";
    const string PrefabDir = Root + "/Prefabs";
    const string PhysicsDir = Root + "/Physics";
    const string SceneDir = Root + "/Scenes";
    const string ScenePath = SceneDir + "/Breakout.unity";
    const string AutoBuildKey = "Breakout_AutoBuildTried";

    static Material spriteMat;

    // ------------------------------------------------------------------ Entrada

    [InitializeOnLoadMethod]
    static void AutoBuildOnFirstOpen()
    {
        if (File.Exists(ScenePath) || SessionState.GetBool(AutoBuildKey, false)) return;
        SessionState.SetBool(AutoBuildKey, true);
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                Build();
        };
    }

    [MenuItem("Breakout/Montar Jogo", priority = 0)]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[Breakout] Saia do modo Play antes de montar o jogo.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolders();
        spriteMat = LoadSpriteMaterial();

        // Sprites (gerados por código)
        Sprite square = MakeSprite("Square", 32, 32, 32f, 0f, false);
        Sprite ballSprite = MakeSprite("Ball", 64, 64, 64f / 0.3f, 32f, true);  // 0.3 unidade de diâmetro
        Sprite brickSprite = MakeSprite("Brick", 96, 36, 80f, 7f, true);       // 1.2 x 0.45
        Sprite paddleSprite = MakeSprite("Paddle", 160, 24, 80f, 12f, true);   // 2.0 x 0.3
        Sprite powerSprite = MakeSprite("PowerUp", 48, 20, 80f, 10f, true);    // 0.6 x 0.25

        PhysicsMaterial2D bounce = CreateBounceMaterial();
        Brick brickPrefab = CreateBrickPrefab(brickSprite);
        PowerUp powerUpPrefab = CreatePowerUpPrefab(powerSprite);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------------- Câmera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.04f, 0.045f, 0.08f);
        camGo.AddComponent<AudioListener>();
        var camController = camGo.AddComponent<CameraController>();

        // ---------------- Arena
        var arena = new GameObject("Arena").transform;
        CreateSpriteObject("Fundo", square, new Color(0.08f, 0.09f, 0.15f),
            new Vector3(0f, -0.475f, 0f), new Vector3(16f, 10.05f, 1f), -10, arena);

        Color wallColor = new Color(0.35f, 0.40f, 0.58f);
        CreateSpriteObject("Parede Esquerda", square, wallColor,
            new Vector3(-8.15f, -0.475f, 0f), new Vector3(0.3f, 10.05f, 1f), 0, arena).AddComponent<BoxCollider2D>();
        CreateSpriteObject("Parede Direita", square, wallColor,
            new Vector3(8.15f, -0.475f, 0f), new Vector3(0.3f, 10.05f, 1f), 0, arena).AddComponent<BoxCollider2D>();
        CreateSpriteObject("Teto", square, wallColor,
            new Vector3(0f, 4.4f, 0f), new Vector3(16.6f, 0.3f, 1f), 0, arena).AddComponent<BoxCollider2D>();

        var deathGo = new GameObject("Zona de Morte");
        deathGo.transform.SetParent(arena, false);
        deathGo.transform.position = new Vector3(0f, -5.7f, 0f);
        var deathCol = deathGo.AddComponent<BoxCollider2D>();
        deathCol.isTrigger = true;
        deathCol.size = new Vector2(20f, 0.6f);
        deathGo.AddComponent<DeathZone>();

        // ---------------- Raquete
        var paddleGo = CreateSpriteObject("Raquete", paddleSprite, new Color(0.55f, 0.85f, 1f),
            new Vector3(0f, -4.2f, 0f), Vector3.one, 2);
        var paddleRb = paddleGo.AddComponent<Rigidbody2D>();
        paddleRb.bodyType = RigidbodyType2D.Kinematic;
        paddleRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        paddleGo.AddComponent<BoxCollider2D>();
        var paddle = paddleGo.AddComponent<Paddle>();

        // ---------------- Bola
        var ballGo = CreateSpriteObject("Bola", ballSprite, Color.white,
            new Vector3(0f, -3.88f, 0f), Vector3.one, 4);
        var ballRb = ballGo.AddComponent<Rigidbody2D>();
        ballRb.bodyType = RigidbodyType2D.Dynamic;
        ballRb.gravityScale = 0f;
        ballRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ballRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        ballRb.constraints = RigidbodyConstraints2D.FreezeRotation;
        ballRb.sharedMaterial = bounce;
        var ballCol = ballGo.AddComponent<CircleCollider2D>();
        ballCol.sharedMaterial = bounce;
        if (spriteMat != null)
        {
            var trail = ballGo.AddComponent<TrailRenderer>();
            trail.time = 0.12f;
            trail.minVertexDistance = 0.03f;
            trail.widthMultiplier = 0.26f;
            trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
            trail.sharedMaterial = spriteMat;
            trail.sortingOrder = 3;
            trail.emitting = false;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.6f, 0.85f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
        }
        var ball = ballGo.AddComponent<Ball>();
        ball.paddle = paddle;

        // ---------------- Fases
        var levelGo = new GameObject("Fases");
        var levelManager = levelGo.AddComponent<LevelManager>();
        levelManager.brickPrefab = brickPrefab;

        // ---------------- Interface
        UIManager ui = CreateUI();

        // ---------------- Game Manager
        var gmGo = new GameObject("GameManager");
        var gm = gmGo.AddComponent<GameManager>();
        var sound = gmGo.AddComponent<SoundManager>();
        gm.paddle = paddle;
        gm.ball = ball;
        gm.levelManager = levelManager;
        gm.ui = ui;
        gm.sound = sound;
        gm.cameraController = camController;
        gm.powerUpPrefab = powerUpPrefab;
        gm.debrisSprite = square;
        gm.spriteMaterial = spriteMat;

        // ---------------- Salvar cena e configurar o build
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        PlayerSettings.productName = "Breakout";
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.resizableWindow = true;

        AssetDatabase.SaveAssets();
        Selection.activeGameObject = gmGo;
        Debug.Log("[Breakout] Jogo montado com sucesso! Cena: " + ScenePath + " — aperte Play.");
    }

    // ------------------------------------------------------------------ Interface

    static UIManager CreateUI()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var canvasGo = new GameObject("Interface");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var ui = canvasGo.AddComponent<UIManager>();
        Transform root = canvasGo.transform;

        // HUD (faixa acima do teto)
        Color hudColor = new Color(0.85f, 0.9f, 1f);
        ui.scoreText = CreateText(root, "Pontos", font, 32, TextAnchor.UpperLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -6f), new Vector2(700f, 44f), hudColor, FontStyle.Bold);
        ui.levelText = CreateText(root, "Fase", font, 32, TextAnchor.UpperCenter,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(700f, 44f), hudColor, FontStyle.Bold);
        ui.livesText = CreateText(root, "Vidas", font, 32, TextAnchor.UpperRight,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -6f), new Vector2(700f, 44f), hudColor, FontStyle.Bold);

        // Painel de mensagens
        var panel = new GameObject("Mensagens", typeof(RectTransform));
        panel.transform.SetParent(root, false);
        Stretch((RectTransform)panel.transform);
        ui.messagePanel = panel;

        var overlayGo = new GameObject("Escurecer", typeof(RectTransform));
        overlayGo.transform.SetParent(panel.transform, false);
        Stretch((RectTransform)overlayGo.transform);
        var overlay = overlayGo.AddComponent<Image>();
        overlay.color = new Color(0.02f, 0.02f, 0.05f, 0.72f);
        overlay.raycastTarget = false;
        ui.overlay = overlay;

        Vector2 center = new Vector2(0.5f, 0.5f);
        ui.titleText = CreateText(panel.transform, "Titulo", font, 120, TextAnchor.MiddleCenter,
            center, center, new Vector2(0f, -40f), new Vector2(1800f, 160f), new Color(1f, 0.85f, 0.35f), FontStyle.Bold);
        ui.subtitleText = CreateText(panel.transform, "Subtitulo", font, 46, TextAnchor.MiddleCenter,
            center, center, new Vector2(0f, -150f), new Vector2(1800f, 70f), Color.white);
        ui.hintText = CreateText(panel.transform, "Dica", font, 30, TextAnchor.UpperCenter,
            center, new Vector2(0.5f, 1f), new Vector2(0f, -205f), new Vector2(1800f, 220f), new Color(0.75f, 0.8f, 0.92f));

        // Aviso rápido de power-up
        ui.toastText = CreateText(root, "Aviso", font, 56, TextAnchor.MiddleCenter,
            center, center, new Vector2(0f, -60f), new Vector2(1200f, 90f), Color.white, FontStyle.Bold);
        ui.toastText.enabled = false;

        return ui;
    }

    static Text CreateText(Transform parent, string name, Font font, int fontSize, TextAnchor align,
        Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size, Color color, FontStyle style = FontStyle.Normal)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = align;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "";

        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.6f);
        shadow.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // ------------------------------------------------------------------ Objetos e prefabs

    static GameObject CreateSpriteObject(string name, Sprite sprite, Color color, Vector3 position,
        Vector3 scale, int sortingOrder, Transform parent = null)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
        if (spriteMat != null) sr.sharedMaterial = spriteMat;
        return go;
    }

    static Brick CreateBrickPrefab(Sprite sprite)
    {
        var go = new GameObject("Brick");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 1;
        if (spriteMat != null) sr.sharedMaterial = spriteMat;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<Brick>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/Brick.prefab");
        Object.DestroyImmediate(go);
        return prefab.GetComponent<Brick>();
    }

    static PowerUp CreatePowerUpPrefab(Sprite sprite)
    {
        var go = new GameObject("PowerUp");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 3;
        if (spriteMat != null) sr.sharedMaterial = spriteMat;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<PowerUp>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabDir + "/PowerUp.prefab");
        Object.DestroyImmediate(go);
        return prefab.GetComponent<PowerUp>();
    }

    static PhysicsMaterial2D CreateBounceMaterial()
    {
        string path = PhysicsDir + "/BallBounce.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("BallBounce");
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.bounciness = 1f;
        mat.friction = 0f;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Material LoadSpriteMaterial()
    {
        // URP: material de sprite sem iluminação (cores exatas, não depende de luzes 2D)
        var mat = AssetDatabase.LoadAssetAtPath<Material>(
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
        if (mat == null) mat = AssetDatabase.GetBuiltinExtraResource<Material>("Sprites-Default.mat");
        return mat;
    }

    // ------------------------------------------------------------------ Sprites

    /// <summary>
    /// Gera uma textura PNG em tons de cinza (para ser colorida pelo SpriteRenderer),
    /// com cantos arredondados opcionais e leve sombreamento, e importa como Sprite.
    /// </summary>
    static Sprite MakeSprite(string name, int w, int h, float pixelsPerUnit, float radius, bool shaded)
    {
        string path = ArtDir + "/" + name + ".png";
        var pixels = new Color32[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float alpha = 1f, shade = 1f;
                if (radius > 0f)
                {
                    float cx = x + 0.5f - w * 0.5f;
                    float cy = y + 0.5f - h * 0.5f;
                    float qx = Mathf.Abs(cx) - (w * 0.5f - radius);
                    float qy = Mathf.Abs(cy) - (h * 0.5f - radius);
                    float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
                    float d = outside + inside - radius; // distância até a borda (negativo = dentro)
                    alpha = Mathf.Clamp01(0.5f - d);

                    if (shaded)
                    {
                        float t = (y + 0.5f) / h;
                        shade = 0.72f + 0.28f * t;                         // mais claro em cima
                        if (d > -2f) shade *= 0.82f;                       // contorno
                        else if (y >= h - 5) shade = Mathf.Min(1f, shade + 0.12f); // brilho
                    }
                }
                byte s = (byte)Mathf.RoundToInt(Mathf.Clamp01(shade) * 255f);
                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                pixels[y * w + x] = new Color32(s, s, s, a);
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ------------------------------------------------------------------ Pastas

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Breakout");
        EnsureFolder(Root, "Art");
        EnsureFolder(Root, "Prefabs");
        EnsureFolder(Root, "Physics");
        EnsureFolder(Root, "Scenes");
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
