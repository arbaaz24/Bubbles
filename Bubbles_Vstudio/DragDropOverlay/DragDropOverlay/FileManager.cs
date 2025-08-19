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

        public static void EnsureSubFolder(int bubbleId)
        {
            EnsureFolder();
            if (bubbleId == 0) return; // root
            var path = GetBubblePath(bubbleId);
            Directory.CreateDirectory(path); // idempotent
        }

        private static string GetBubblePath(int bubbleId) =>
            bubbleId == 0 ? TempDir : Path.Combine(TempDir, $"Bubble_{bubbleId}");

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
    }
}
