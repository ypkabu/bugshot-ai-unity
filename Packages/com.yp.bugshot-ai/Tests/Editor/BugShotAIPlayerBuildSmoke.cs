using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YP.BugShotAI.Tests
{
    public static class BugShotAIPlayerBuildSmoke
    {
        private const string BuildOutputArgument = "-bugshotBuildOutput";

        public static void BuildWindows64()
        {
            string outputPath = GetBuildOutputPath();
            string outputDirectory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled && File.Exists(scene.path))
                .Select(scene => scene.path)
                .ToArray();

            string generatedScenePath = null;
            if (scenes.Length == 0)
            {
                generatedScenePath = "Assets/__BugShotAIPlayerBuildSmoke.unity";
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                GameObject cameraObject = new GameObject("Main Camera");
                cameraObject.AddComponent<Camera>();
                EditorSceneManager.SaveScene(scene, generatedScenePath);
                scenes = new[] { generatedScenePath };
                Debug.LogWarning($"{BugShotAIConstants.LogPrefix} No enabled build scenes found. Generated a temporary scene for assembly compatibility smoke testing.");
            }

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            UnityEditor.Build.Reporting.BuildSummary summary;
            try
            {
                UnityEditor.Build.Reporting.BuildReport report = BuildPipeline.BuildPlayer(options);
                summary = report.summary;
            }
            finally
            {
                if (!string.IsNullOrEmpty(generatedScenePath))
                {
                    AssetDatabase.DeleteAsset(generatedScenePath);
                    AssetDatabase.Refresh();
                }
            }

            Debug.Log($"{BugShotAIConstants.LogPrefix} Player build smoke finished. Result={summary.result}, Errors={summary.totalErrors}, Warnings={summary.totalWarnings}, Output={outputPath}");
            EditorApplication.Exit(summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
        }

        private static string GetBuildOutputPath()
        {
            string explicitPath = GetArgumentValue(BuildOutputArgument);
            if (!string.IsNullOrWhiteSpace(explicitPath))
            {
                return Path.GetFullPath(explicitPath);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            return Path.Combine(projectRoot, "Builds", "BugShotAIPlayerSmoke", stamp, "BugShotAIPlayerSmoke.exe");
        }

        private static string GetArgumentValue(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
