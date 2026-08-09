using System;
using System.IO;
using System.Text;

namespace YP.BugShotAI
{
    public static class BugShotAITextUtility
    {
        public static string NormalizePath(string path)
        {
            return string.IsNullOrEmpty(path) ? null : path.Replace('\\', '/');
        }

        public static string NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }

        public static string Truncate(string value, int maxChars)
        {
            if (value == null || maxChars <= 0 || value.Length <= maxChars)
            {
                return value;
            }

            return value.Substring(0, maxChars) + $"\n... <truncated {value.Length - maxChars} chars>";
        }

        public static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "untitled";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(fileName.Length);

            for (int i = 0; i < fileName.Length; i++)
            {
                char c = fileName[i];
                bool invalid = char.IsControl(c) || Array.IndexOf(invalidChars, c) >= 0;
                builder.Append(invalid ? '_' : c);
            }

            string sanitized = builder.ToString().Trim().Trim('.');
            return string.IsNullOrWhiteSpace(sanitized) ? "untitled" : sanitized;
        }

        public static string UnknownIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
        }

        public static bool StartsWithInternalPrefix(string condition)
        {
            return !string.IsNullOrEmpty(condition)
                   && condition.StartsWith(BugShotAIConstants.LogPrefix, StringComparison.Ordinal);
        }
    }
}
