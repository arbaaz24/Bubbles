// =============================
// File: FileManager.cs
// Purpose: create temp folder, list files, (optionally) seed a sample file
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

        public static List<string> GetFiles()
        {
            EnsureFolder();
            // Return only existing files (ignore temp locks)
            var files = Directory.EnumerateFiles(TempDir).Where(File.Exists).ToList();
            return files;
        }

        public static void EnsureSampleFile()
        {
            EnsureFolder();
            var files = Directory.EnumerateFiles(TempDir);
            if (!files.Any())
            {
                var sample = Path.Combine(TempDir, "sample.txt");
                File.WriteAllText(sample, "Drop me into Slack/WhatsApp Web/Drive to test.\r\n" + DateTime.Now);
            }
        }
    }
}