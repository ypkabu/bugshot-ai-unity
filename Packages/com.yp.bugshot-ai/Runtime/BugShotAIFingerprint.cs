using System;
using System.Security.Cryptography;
using System.Text;

namespace YP.BugShotAI
{
    public static class BugShotAIFingerprint
    {
        public static string Generate(string logType, string condition, string stackTrace)
        {
            // Runtime details such as scene and FPS are excluded so one error keeps one cooldown identity.
            string key = $"{Normalize(logType)}\n{Normalize(condition)}\n{GetPrimaryStackLine(stackTrace)}";

            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                StringBuilder builder = new StringBuilder(16);
                for (int i = 0; i < 8 && i < hash.Length; i++)
                {
                    builder.Append(hash[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        public static string CreateReportId(DateTime utcNow, string fingerprint)
        {
            string suffix = string.IsNullOrEmpty(fingerprint) ? "unknown" : fingerprint.Substring(0, Math.Min(8, fingerprint.Length));
            return $"{utcNow:yyyyMMdd_HHmmss_fff}_{suffix}";
        }

        private static string GetPrimaryStackLine(string stackTrace)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
            {
                return string.Empty;
            }

            string[] lines = stackTrace.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (!line.StartsWith("UnityEngine.Debug", StringComparison.Ordinal))
                {
                    return Normalize(line);
                }
            }

            return Normalize(lines[0]);
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
