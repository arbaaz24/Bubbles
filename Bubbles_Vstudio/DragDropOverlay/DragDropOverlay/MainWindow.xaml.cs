// =============================
// File: MainWindow.xaml.cs
// =============================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace DragDropOverlay
{
    public partial class MainWindow : Window
    {
        private List<string> _files = new();
        private Point? _dragStartPoint; // for starting OS drag when moving over the badge
        private bool _isDraggingFiles;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                FileManager.EnsureFolder();
                FileManager.EnsureSampleFile();
                ReloadFiles();

                // Start the pulse animation
                if (Resources["PulseStoryboard"] is Storyboard sb)
                {
                    sb.Begin(this, true);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init error: " + ex.Message);
            }
        }

        private void ReloadFiles()
        {
            _files = FileManager.GetFiles();
            CountText.Text = _files.Count == 1 ? "1 item" : $"{_files.Count} items";
        }

        private void StartFileDrag()
        {
            if (_isDraggingFiles) return; // prevent reentry

            var existing = _files.Where(File.Exists).ToArray();
            if (existing.Length == 0)
            {
                MessageBox.Show("No files in temp folder. Click 'Open Folder' and add some files.");
                return;
            }

            try
            {
                _isDraggingFiles = true;
                // Build a DataObject with the FileDrop format — this is the real CF_HDROP drag that other apps accept.
                var data = new DataObject(DataFormats.FileDrop, existing);

                // Effects: Copy (most uploaders copy). You can include Move if needed: Copy | Move.
                DragDrop.DoDragDrop(Badge, data, DragDropEffects.Copy);
            }
            finally
            {
                _isDraggingFiles = false;
            }
        }

        // =============================
        // Overlay movement (grab the empty background to move window)
        // =============================
        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If the click started on the badge, ignore (badge has its own handlers)
            if (e.OriginalSource is FrameworkElement fe && fe.Name == "Badge") return;
            try { DragMove(); } catch { /* ignore */ }
        }

        // =============================
        // Badge drag logic — click and move threshold to start file drag
        // =============================
        private void Badge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            Badge.CaptureMouse();
        }

        private void Badge_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) return;

            var pos = e.GetPosition(this);
            var dx = Math.Abs(pos.X - _dragStartPoint.Value.X);
            var dy = Math.Abs(pos.Y - _dragStartPoint.Value.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance || dy > SystemParameters.MinimumVerticalDragDistance)
            {
                // Start OS drag
                StartFileDrag();
                // After DoDragDrop returns, reset state
                _dragStartPoint = null;
                Badge.ReleaseMouseCapture();
            }
        }

        private void Badge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            Badge.ReleaseMouseCapture();
        }

        // =============================
        // Buttons
        // =============================
        private void Reload_Click(object sender, RoutedEventArgs e)
        {
            ReloadFiles();
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FileManager.EnsureFolder();
                Process.Start(new ProcessStartInfo
                {
                    FileName = FileManager.TempDir,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Open folder failed: " + ex.Message);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}