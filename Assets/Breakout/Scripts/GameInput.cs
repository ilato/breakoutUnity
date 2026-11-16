using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Centraliza a leitura das entradas do jogador (teclado, mouse e controle).
/// Usa o Input System (padrão do Unity 6) e, se ele estiver desativado,
/// cai para o Input Manager antigo.
/// </summary>
public static class GameInput
{
    static Vector2 lastMousePos;
    static bool mouseInitialized;

    /// <summary>Eixo horizontal entre -1 e 1 (setas, A/D, analógico ou direcional).</summary>
    public static float Horizontal
    {
        get
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) v -= 1f;
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) v += 1f;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                float stick = g.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > 0.2f) v += stick;
                if (g.dpad.left.isPressed) v -= 1f;
                if (g.dpad.right.isPressed) v += 1f;
            }
#else
            v = Input.GetAxisRaw("Horizontal");
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }
    }

    /// <summary>Lançar a bola: Espaço, clique esquerdo ou botão A do controle.</summary>
    public static bool LaunchPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
#endif
        }
    }

    /// <summary>Confirmar em menus: o mesmo que lançar, mais a tecla Enter.</summary>
    public static bool ConfirmPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            bool enter = Keyboard.current != null &&
                         (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
#else
            bool enter = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
            return enter || LaunchPressed;
        }
    }

    /// <summary>Pausar / continuar: Esc, P ou Start.</summary>
    public static bool PausePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null &&
                    (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
        }
    }

    /// <summary>Voltar ao menu (usado na pausa): M ou Select.</summary>
    public static bool MenuPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            return (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }
    }

    /// <summary>
    /// Retorna true se o mouse se mexeu neste frame e devolve a posição X dele no mundo.
    /// Deve ser chamado uma vez por frame.
    /// </summary>
    public static bool MouseMoved(Camera cam, out float worldX)
    {
        worldX = 0f;
        if (cam == null) return false;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return false;
        Vector2 pos = Mouse.current.position.ReadValue();
#else
        Vector2 pos = Input.mousePosition;
#endif
        if (!mouseInitialized)
        {
            lastMousePos = pos;
            mouseInitialized = true;
            return false;
        }

        bool moved = (pos - lastMousePos).sqrMagnitude > 1f;
        lastMousePos = pos;
        if (!moved) return false;

        worldX = cam.ScreenToWorldPoint(new Vector3(pos.x, pos.y, -cam.transform.position.z)).x;
        return true;
    }
}
