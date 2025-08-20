// =============================
// File: MainWindow.xaml.cs
// =============================
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

        // Update EnsureSubFolder to handle bubble 1 properly
        public static void EnsureSubFolder(int bubbleId)
        {
            EnsureFolder();
            if (bubbleId == 1) return; // root folder already ensured
            var path = GetBubblePath(bubbleId);
            Directory.CreateDirectory(path); // idempotent
        }

        // Update the GetBubblePath method to handle bubble 1 as root for consistency
        private static string GetBubblePath(int bubbleId) =>
            bubbleId == 1 ? TempDir : Path.Combine(TempDir, $"Bubble_{bubbleId}");

        public static List<string> GetFiles(int bubbleId)
        {
            var path = GetBubblePath(bubbleId);
            if (!Directory.Exists(path)) return new List<string>();
            return Directory.EnumerateFiles(path).Where(File.Exists).ToList();
        }

        // Save incoming files into temp folder
        public static void SaveDroppedFiles(int bubbleId, string[] files)
        {
            EnsureSubFolder(bubbleId);
            var path = GetBubblePath(bubbleId);
            foreach (var f in files)
            {
                try
                {
                    var name = Path.GetFileName(f);
                    var dest = Path.Combine(path, name);
                    File.Copy(f, dest, overwrite: true);
                }
                catch { /* ignore individual file errors */ }
            }
        }

        public static void ClearBubble(int bubbleId)
        {
            EnsureSubFolder(bubbleId);
            var path = GetBubblePath(bubbleId);
            if (!Directory.Exists(path)) return;
            foreach (var f in Directory.EnumerateFiles(path))
            {
                try { File.Delete(f); } catch { }
            }
        }

        public static void DeleteBubbleFolder(int bubbleId)
        {
            if (bubbleId == 0) return; // Don't delete root folder
            var path = GetBubblePath(bubbleId);
            if (!Directory.Exists(path)) return;
            try
            {
                Directory.Delete(path, recursive: true);
            }
            catch { /* ignore deletion errors */ }
        }

        // Add this new method to check if a bubble folder exists and has files
        public static bool BubbleHasFiles(int bubbleId)
        {
            var path = GetBubblePath(bubbleId);
            return Directory.Exists(path) && Directory.EnumerateFiles(path).Any();
        }
    }
}
