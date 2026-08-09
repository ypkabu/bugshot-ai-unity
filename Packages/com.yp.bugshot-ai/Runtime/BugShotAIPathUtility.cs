using System.IO;
using UnityEngine;

namespace YP.BugShotAI
{
    public static class BugShotAIPathUtility
    {
        public static string GetProjectRootPath()
        {
            string dataPath = Application.dataPath;
            if (string.IsNullOrEmpty(dataPath))
            {
                return null;
            }

            DirectoryInfo assetsDirectory = Directory.GetParent(dataPath);
            return assetsDirectory == null ? null : assetsDirectory.FullName;
        }

        public static string GetReportsRootPath(BugShotAISettings settings)
        {
            if (settings == null)
            {
                settings = BugShotAISettings.CreateDefault();
            }

            settings.Validate();

            if (!string.IsNullOrWhiteSpace(settings.customOutputDirectory))
            {
                return settings.customOutputDirectory;
            }

            return Path.Combine(Application.persistentDataPath, settings.outputFolderName);
        }

        public static string ToRelativePath(string rootPath, string fullPath)
        {
            if (string.IsNullOrEmpty(rootPath) || string.IsNullOrEmpty(fullPath))
            {
                return BugShotAITextUtility.NormalizePath(fullPath);
            }

            string normalizedRoot = BugShotAITextUtility.NormalizePath(Path.GetFullPath(rootPath)).TrimEnd('/');
            string normalizedFull = BugShotAITextUtility.NormalizePath(Path.GetFullPath(fullPath));

            if (normalizedFull.StartsWith(normalizedRoot + "/", System.StringComparison.OrdinalIgnoreCase))
            {
                return normalizedFull.Substring(normalizedRoot.Length + 1);
            }

            return normalizedFull;
        }
    }
}
