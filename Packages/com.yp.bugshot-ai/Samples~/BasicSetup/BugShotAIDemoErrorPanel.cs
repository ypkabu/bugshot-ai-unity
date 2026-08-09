using System;
using UnityEngine;
using YP.BugShotAI;

public sealed class BugShotAIDemoErrorPanel : MonoBehaviour
{
    [SerializeField] private Rect panelRect = new Rect(16f, 16f, 380f, 300f);

    private void OnGUI()
    {
        GUILayout.BeginArea(panelRect, GUI.skin.box);
        GUILayout.Label("BugShot AI Demo Error Panel");
        GUILayout.Label("Use these buttons in Play Mode to verify capture behavior.");

        DrawButton(
            "NullReferenceException",
            "Checks Exception capture, stackTrace, screenshot, and prompt output.",
            TriggerNullReferenceException);

        DrawButton(
            "IndexOutOfRangeException",
            "Checks another exception type and report history ordering.",
            TriggerIndexOutOfRangeException);

        DrawButton(
            "Debug.LogError",
            "Checks normal Error capture without throwing an exception.",
            TriggerLogError);

        DrawButton(
            "Long StackTrace Exception",
            "Checks long stackTrace capture and truncation limits.",
            TriggerLongStackTraceException);

        DrawButton(
            "Duplicate Error Burst",
            "Checks fingerprint duplicate suppression during cooldown.",
            TriggerDuplicateErrorBurst);

        GUILayout.EndArea();
    }

    private static void DrawButton(string label, string description, Action action)
    {
        GUILayout.Space(4f);
        GUILayout.Label(description);
        if (GUILayout.Button(label, GUILayout.Height(28f)))
        {
            action?.Invoke();
        }
    }

    private static void TriggerNullReferenceException()
    {
        BugShotAIEventLogger.Record("Demo", "Clicked NullReferenceException button");

        try
        {
            CauseNullReference();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void TriggerIndexOutOfRangeException()
    {
        BugShotAIEventLogger.Record("Demo", "Clicked IndexOutOfRangeException button");

        try
        {
            CauseIndexOutOfRange();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void TriggerLogError()
    {
        BugShotAIEventLogger.Record("Demo", "Clicked Debug.LogError button");
        Debug.LogError("BugShot AI demo Debug.LogError.");
    }

    private static void TriggerLongStackTraceException()
    {
        BugShotAIEventLogger.Record("Demo", "Clicked Long StackTrace Exception button");

        try
        {
            ThrowDeepException(14);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void TriggerDuplicateErrorBurst()
    {
        BugShotAIEventLogger.Record("Demo", "Clicked Duplicate Error Burst button");

        for (int i = 0; i < 5; i++)
        {
            Debug.LogError("BugShot AI duplicate suppression demo error.");
        }
    }

    private static void CauseNullReference()
    {
        string missing = null;
        Debug.Log(missing.ToString());
    }

    private static void CauseIndexOutOfRange()
    {
        int[] values = { 1, 2, 3 };
        Debug.Log(values[10]);
    }

    private static void ThrowDeepException(int remainingDepth)
    {
        if (remainingDepth <= 0)
        {
            throw new InvalidOperationException("BugShot AI demo exception with a deliberately long stack trace.");
        }

        ThrowDeepException(remainingDepth - 1);
    }
}
