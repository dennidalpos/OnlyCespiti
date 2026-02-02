using System;
using System.IO;
using System.Security;

namespace GestioneCespiti.Utils
{
    public static class PathValidator
    {
        public static string ValidateAndGetSafePath(string basePath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                throw new ArgumentException("Base path cannot be empty", nameof(basePath));

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be empty", nameof(fileName));

            string fullBasePath = Path.GetFullPath(basePath);
            string combinedPath = Path.Combine(basePath, fileName);
            string fullCombinedPath = Path.GetFullPath(combinedPath);
            string normalizedBasePath = EnsureTrailingSeparator(fullBasePath);

            if (!fullCombinedPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new SecurityException($"Path traversal detected: {fileName}");
            }

            return fullCombinedPath;
        }

        public static void EnsureDirectoryExists(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path cannot be empty", nameof(path));

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static bool IsPathSafe(string basePath, string testPath)
        {
            try
            {
                string fullBasePath = Path.GetFullPath(basePath);
                string fullTestPath = Path.GetFullPath(testPath);
                string normalizedBasePath = EnsureTrailingSeparator(fullBasePath);
                return fullTestPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            char separator = Path.DirectorySeparatorChar;
            return path.EndsWith(separator) ? path : path + separator;
        }
    }
}
