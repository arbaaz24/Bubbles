using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragDropOverlay
{
    public static class FileManager
    {
        public static readonly string TempDir = Path.Combine(Path.GetTempPath(), "DragDropOverlay");

        public static void EnsureFolder()
        {
            Directory.CreateDirectory(TempDir);
        }

        public static void EnsureSubFolder(int bubbleId)
        {
            EnsureFolder();
            var folderPath = GetSubFolderPath(bubbleId);
            // Idempotent: does nothing if it already exists
            Directory.CreateDirectory(folderPath);
        }

        public static void DeleteSubFolder(int bubbleId)
        {
            if (bubbleId == 0) return;
            var subDir = GetSubFolderPath(bubbleId);
            if (Directory.Exists(subDir))
            {
                Directory.Delete(subDir, true);
            }
        }

        private static string GetSubFolderPath(int bubbleId) =>
            bubbleId == 0 ? TempDir : Path.Combine(TempDir, $"Bubble_{bubbleId}");

        public static List<string> GetFiles(int bubbleId)
        {
            var folderPath = GetSubFolderPath(bubbleId);
            if (!Directory.Exists(folderPath))
                return new List<string>();
            return Directory.EnumerateFiles(folderPath).Where(File.Exists).ToList();
        }

        public static void EnsureSampleFile(int bubbleId = 0)
        {
            var folderPath = GetSubFolderPath(bubbleId);
            EnsureSubFolder(bubbleId);
            if (!Directory.EnumerateFiles(folderPath).Any())
            {
                var sample = Path.Combine(folderPath, "sample.txt");
                File.WriteAllText(sample, $"Bubble {bubbleId}: Drop me into Slack/WhatsApp Web/Drive to test.\r\n" + DateTime.Now);
            }
        }

        public static void SaveDroppedFiles(int bubbleId, string[] files)
        {
            var folderPath = GetSubFolderPath(bubbleId);
            EnsureSubFolder(bubbleId);
            foreach (var f in files)
            {
                try
                {
                    var fileName = Path.GetFileName(f);
                    var destPath = Path.Combine(folderPath, fileName);
                    File.Copy(f, destPath, overwrite: true);
                }
                catch { }
            }
        }
    }
}