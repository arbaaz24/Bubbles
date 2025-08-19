// =============================
// File: FileManager.cs
// Purpose: handle temp folders, list files, and add dropped files per bubble
// =============================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DragDropOverlay
{
    public static class FileManager
    {
        private static string GetBubbleDir(int bubbleIndex)
        {
            return Path.Combine(Path.GetTempPath(), $"DragDropOverlay_Bubble{bubbleIndex}");
        }

        public static void EnsureFolder(int bubbleIndex)
        {
            Directory.CreateDirectory(GetBubbleDir(bubbleIndex));
        }

        public static List<string> GetFiles(int bubbleIndex)
        {
            EnsureFolder(bubbleIndex);
            return Directory.EnumerateFiles(GetBubbleDir(bubbleIndex)).Where(File.Exists).ToList();
        }

        public static void ClearFiles(int bubbleIndex)
        {
            EnsureFolder(bubbleIndex);
            foreach (var f in Directory.GetFiles(GetBubbleDir(bubbleIndex)))
            {
                try { File.Delete(f); } catch { }
            }
        }

        public static void EnsureSampleFile(int bubbleIndex)
        {
            EnsureFolder(bubbleIndex);
            if (!Directory.EnumerateFiles(GetBubbleDir(bubbleIndex)).Any())
            {
                var sample = Path.Combine(GetBubbleDir(bubbleIndex), $"sample{bubbleIndex}.txt");
                File.WriteAllText(sample, $"Sample file for Bubble {bubbleIndex} - {DateTime.Now}");
            }
        }

        public static void SaveDroppedFiles(int bubbleIndex, string[] files)
        {
            EnsureFolder(bubbleIndex);
            foreach (var f in files)
            {
                try
                {
                    var fileName = Path.GetFileName(f);
                    var destPath = Path.Combine(GetBubbleDir(bubbleIndex), fileName);
                    File.Copy(f, destPath, overwrite: true);
                }
                catch { }
            }
        }

        public static string GetFolderPath(int bubbleIndex) => GetBubbleDir(bubbleIndex);
    }
}
