using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace YP.BugShotAI.Tests
{
    public static class BugShotAICommandLineTestRunner
    {
        private const string ResultPathArgument = "-bugshotTestResults";

        public static void RunEditModeTests()
        {
            string resultPath = GetResultPath();
            string directory = Path.GetDirectoryName(resultPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            TestRunnerApi api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new ExitOnRunFinishedCallback(resultPath));
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode
            }));
        }

        private static string GetResultPath()
        {
            string fromArgs = GetArgumentValue(ResultPathArgument);
            if (!string.IsNullOrWhiteSpace(fromArgs))
            {
                return Path.GetFullPath(fromArgs);
            }

            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, "TestResults", "BugShotAI_EditMode.xml");
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

        private sealed class ExitOnRunFinishedCallback : ICallbacks
        {
            private readonly string resultPath;

            public ExitOnRunFinishedCallback(string resultPath)
            {
                this.resultPath = resultPath;
            }

            public void RunStarted(ITestAdaptor testsToRun)
            {
                Debug.Log($"{BugShotAIConstants.LogPrefix} EditMode tests started.");
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                TestRunnerApi.SaveResultToFile(result, resultPath);
                Debug.Log($"{BugShotAIConstants.LogPrefix} EditMode tests finished. Passed={result.PassCount}, Failed={result.FailCount}, Skipped={result.SkipCount}, Inconclusive={result.InconclusiveCount}. Results={resultPath}");
                EditorApplication.Exit(result.FailCount > 0 || result.TestStatus == TestStatus.Failed ? 1 : 0);
            }

            public void TestStarted(ITestAdaptor test)
            {
            }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.TestStatus == TestStatus.Failed)
                {
                    Debug.LogError($"{BugShotAIConstants.LogPrefix} Test failed: {result.FullName}\n{result.Message}\n{result.StackTrace}");
                }
            }
        }
    }
}
