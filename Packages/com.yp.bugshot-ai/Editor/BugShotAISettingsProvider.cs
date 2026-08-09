using UnityEditor;
using UnityEngine;

namespace YP.BugShotAI.Editor
{
    internal static class BugShotAISettingsProvider
    {
        private static BugShotAISettings settings;

        [SettingsProvider]
        public static SettingsProvider CreateProvider()
        {
            return new SettingsProvider("Project/BugShot AI", SettingsScope.Project)
            {
                label = "BugShot AI",
                guiHandler = DrawSettings
            };
        }

        private static void DrawSettings(string searchContext)
        {
            if (settings == null)
            {
                settings = BugShotAIEditorSettingsUtility.LoadSettings();
            }

            EditorGUILayout.LabelField("BugShot AI", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Project-wide capture, privacy, and storage settings.", MessageType.Info);

            BugShotAIEditorSettingsUtility.DrawSettingsFields(settings);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save Settings", GUILayout.Height(24f)))
                {
                    BugShotAIEditorSettingsUtility.SaveSettings(settings);
                    BugShotAIEditorSettingsUtility.Log("Saved project settings.");
                }

                if (GUILayout.Button("Reset Defaults", GUILayout.Height(24f)))
                {
                    settings = BugShotAISettings.CreateDefault();
                    BugShotAIEditorSettingsUtility.SaveSettings(settings);
                    BugShotAIEditorSettingsUtility.Log("Reset project settings to defaults.");
                }
            }
        }
    }
}
