using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One input API that works with the new Input System, the legacy Input Manager,
/// keyboard (A/D/arrows, Space, S, Esc) and touch/mouse swipes.
/// </summary>
public static class InputAdapter
{
    const float SwipeDistance = 70f;

    static int frame = -1;
    static bool left, right, jump, slide, pause, confirm;
    static Vector2 downPos;
    static bool tracking;

    public static bool LeftPressed { get { Refresh(); return left; } }
    public static bool RightPressed { get { Refresh(); return right; } }
    public static bool JumpPressed { get { Refresh(); return jump; } }
    public static bool SlidePressed { get { Refresh(); return slide; } }
    public static bool PausePressed { get { Refresh(); return pause; } }
    public static bool ConfirmPressed { get { Refresh(); return confirm; } }

    static void Refresh()
    {
        if (frame == Time.frameCount) return;
        frame = Time.frameCount;
        left = right = jump = slide = pause = confirm = false;

        bool pressed = false, held = false, released = false;
        Vector2 pos = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            left = kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame;
            right = kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
            jump = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            slide = kb.sKey.wasPressedThisFrame || kb.downArrowKey.wasPressedThisFrame;
            pause = kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
            confirm = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
        }
        Pointer ptr = Pointer.current;
        if (ptr != null)
        {
            pressed = ptr.press.wasPressedThisFrame;
            held = ptr.press.isPressed;
            released = ptr.press.wasReleasedThisFrame;
            pos = ptr.position.ReadValue();
        }
#else
        left = Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow);
        right = Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow);
        jump = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        slide = Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        pause = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
        confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
        pressed = Input.GetMouseButtonDown(0);
        held = Input.GetMouseButton(0);
        released = Input.GetMouseButtonUp(0);
        pos = Input.mousePosition;
#endif

        // Swipe detection (touch or mouse drag)
        if (pressed)
        {
            downPos = pos;
            tracking = true;
        }
        else if (tracking && held)
        {
            Vector2 d = pos - downPos;
            if (d.magnitude > SwipeDistance)
            {
                if (Mathf.Abs(d.x) > Mathf.Abs(d.y))
                {
                    if (d.x < 0f) left = true; else right = true;
                }
                else
                {
                    if (d.y > 0f) jump = true; else slide = true;
                }
                tracking = false;
            }
        }
        if (released) tracking = false;
    }
}
