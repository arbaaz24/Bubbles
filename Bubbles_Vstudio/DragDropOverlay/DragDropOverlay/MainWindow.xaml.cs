// =============================
// File: MainWindow.xaml.cs
// =============================
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace DragDropOverlay
{
    public partial class MainWindow : Window
    {
        private List<string> _files = new();
        private Point? _dragStartPoint;
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
                ReloadFiles(); //Reads files from the folder and updates the UI count

                // So checking if in xaml there is a Storyboard with the name PulseStoryboard
                // and if it exists, assign it to sb and start it.
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

            if (_files.Count == 0)
            {
                Bubble.ToolTip = "No files in temp folder.";
            }
            else
            {
                var names = _files.Select(Path.GetFileName);
                Bubble.ToolTip = string.Join("\n", names);
            }
        }

        private void StartFileDrag()
        {
            if (_isDraggingFiles) return;

            var existing = _files.Where(File.Exists).ToArray();
            if (existing.Length == 0)
            {
                MessageBox.Show("No files in temp folder. Drag some files into the bubble.");
                return;
            }

            try
            {
                _isDraggingFiles = true;
                var data = new DataObject(DataFormats.FileDrop, existing);
                DragDrop.DoDragDrop(Bubble, data, DragDropEffects.Copy);
            }
            finally
            {
                _isDraggingFiles = false;
            }
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.Name == "Bubble") return;
            try { DragMove(); } catch { }
        }

        private void Bubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            Bubble.CaptureMouse();
        }

        private void Bubble_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) return;

            var pos = e.GetPosition(this);
            var dx = Math.Abs(pos.X - _dragStartPoint.Value.X);
            var dy = Math.Abs(pos.Y - _dragStartPoint.Value.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance || dy > SystemParameters.MinimumVerticalDragDistance)
            {
                StartFileDrag();
                _dragStartPoint = null;
                Bubble.ReleaseMouseCapture();
            }
        }

        private void Bubble_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            Bubble.ReleaseMouseCapture();
        }

        private void Reload_Click(object sender, RoutedEventArgs e) => ReloadFiles();

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to add to temp folder"
            };

            if (dlg.ShowDialog() == true)
            {
                FileManager.SaveDroppedFiles(dlg.FileNames);
                ReloadFiles();
            }
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

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        // Handle drag-over so cursor shows Copy effect
        private void Bubble_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        // Handle drop: copy files into temp dir
        private void Bubble_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileManager.SaveDroppedFiles(files);
                ReloadFiles();
            }
        }

        // Allow dropping anywhere on window background
        private void Window_DragOver(object sender, DragEventArgs e) => Bubble_DragOver(sender, e);
        private void Window_Drop(object sender, DragEventArgs e) => Bubble_Drop(sender, e);

        private void Bubble_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            if (_files == null || _files.Count == 0)
            {
                Bubble.ToolTip = "No files in temp folder.";
                return;
            }

            var names = _files.Select(Path.GetFileName).ToList();
            Bubble.ToolTip = string.Join("\n", names);
        }
    }
}