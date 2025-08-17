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
            return Directory.EnumerateFiles(TempDir).Where(File.Exists).ToList();
        }

        public static void EnsureSampleFile()
        {
            EnsureFolder();
            if (!Directory.EnumerateFiles(TempDir).Any())
            {
                var sample = Path.Combine(TempDir, "sample.txt");
                File.WriteAllText(sample, "Drop me into Slack/WhatsApp Web/Drive to test.\r\n" + DateTime.Now);
            }
        }

        // Save incoming files into temp folder
        public static void SaveDroppedFiles(string[] files)
        {
            EnsureFolder();
            foreach (var f in files)
            {
                try
                {
                    var fileName = Path.GetFileName(f);
                    var destPath = Path.Combine(TempDir, fileName);
                    File.Copy(f, destPath, overwrite: true);
                }
                catch { /* ignore individual file errors */ }
            }
        }
    }
}
