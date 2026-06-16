using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using YP.BugShotAI;

public sealed class BugShotAIDemoBugTrigger : MonoBehaviour
{
    private void Update()
    {
        if (WasMoveRightPressed())
        {
            BugShotAIEventLogger.Record("Player", "Pressed move right");
        }

        if (WasJumpPressed())
        {
            BugShotAIEventLogger.Record("Player", "Pressed jump");
        }

        if (WasDashPressed())
        {
            BugShotAIEventLogger.Record("Player", "Pressed dash");
        }

        if (WasDemoBugPressed())
        {
            TriggerDemoBug();
        }
    }

    private static bool WasMoveRightPressed()
    {
        return GetLegacyKeyDown(KeyCode.D)
               || GetLegacyKeyDown(KeyCode.RightArrow)
               || GetInputSystemMoveRightDown();
    }

    private static bool WasJumpPressed()
    {
        return GetLegacyKeyDown(KeyCode.Space)
               || GetInputSystemJumpDown();
    }

    private static bool WasDashPressed()
    {
        return GetLegacyKeyDown(KeyCode.LeftShift)
               || GetInputSystemDashDown();
    }

    private static bool WasDemoBugPressed()
    {
        return GetLegacyKeyDown(KeyCode.B)
               || GetInputSystemDemoBugDown();
    }

    private static void TriggerDemoBug()
    {
        BugShotAIEventLogger.Record("Player", "Moved to right platform");
        BugShotAIEventLogger.Record("Player", "Pressed dash before jump");
        BugShotAIEventLogger.Record("Player", "Jumped near platform edge");
        BugShotAIEventLogger.Record("Bug", "Player Y position dropped below expected floor height");
        Debug.LogError("Player fell through the floor near the right platform.");
    }

    private static bool GetLegacyKeyDown(KeyCode keyCode)
    {
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(keyCode);
#else
        return false;
#endif
    }

    private static bool GetInputSystemMoveRightDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null
               && (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    private static bool GetInputSystemJumpDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static bool GetInputSystemDashDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    private static bool GetInputSystemDemoBugDown()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.bKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
