using UnityEngine;
using YP.BugShotAI;

public sealed class BugShotAITestError : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            BugShotAIEventLogger.Record("Test", "Pressed B to trigger a test error");
            Debug.LogError("BugShotAI test error: player clipped through the floor.");
        }
    }
}
