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

namespace DragDropOverlay
{
    public partial class MainWindow : Window
    {
        private Dictionary<int, List<string>> _files = new();
        private Point? _dragStartPoint;
        private bool _isDraggingFiles;
        private int _activeBubble = 1;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            for (int i = 1; i <= 3; i++)
            {
                FileManager.EnsureFolder(i);
                FileManager.EnsureSampleFile(i);
            }
            ReloadFiles();
        }

        private void ReloadFiles()
        {
            for (int i = 1; i <= 3; i++)
            {
                _files[i] = FileManager.GetFiles(i);
            }

            CountText1.Text = _files[1].Count == 1 ? "1 item" : $"{_files[1].Count} items";
            CountText2.Text = _files[2].Count == 1 ? "1 item" : $"{_files[2].Count} items";
            CountText3.Text = _files[3].Count == 1 ? "1 item" : $"{_files[3].Count} items";
        }

        private void StartFileDrag(int bubbleIndex)
        {
            if (_isDraggingFiles) return;
            var existing = _files[bubbleIndex].Where(File.Exists).ToArray();
            if (existing.Length == 0)
            {
                MessageBox.Show($"No files in Bubble {bubbleIndex}. Drop some files into the bubble.");
                return;
            }

            try
            {
                _isDraggingFiles = true;
                var data = new DataObject(DataFormats.FileDrop, existing);
                DragDrop.DoDragDrop(this, data, DragDropEffects.Copy);
            }
            finally
            {
                _isDraggingFiles = false;
            }
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try { DragMove(); } catch { }
        }

        private void Badge_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            if (sender == Badge1) _activeBubble = 1;
            else if (sender == Badge2) _activeBubble = 2;
            else if (sender == Badge3) _activeBubble = 3;
            (sender as UIElement)?.CaptureMouse();
        }

        private void Badge_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) return;

            var pos = e.GetPosition(this);
            var dx = Math.Abs(pos.X - _dragStartPoint.Value.X);
            var dy = Math.Abs(pos.Y - _dragStartPoint.Value.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance || dy > SystemParameters.MinimumVerticalDragDistance)
            {
                StartFileDrag(_activeBubble);
                _dragStartPoint = null;
                (sender as UIElement)?.ReleaseMouseCapture();
            }
        }

        private void Badge_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            (sender as UIElement)?.ReleaseMouseCapture();
        }

        private void Reload_Click(object sender, RoutedEventArgs e) => ReloadFiles();

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog { Multiselect = true, Title = "Select files to add to Bubble 1" };
            if (dlg.ShowDialog() == true)
            {
                FileManager.SaveDroppedFiles(1, dlg.FileNames);
                ReloadFiles();
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e) => Close();

        private void Badge_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Badge_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);

            int bubbleIndex = sender == Badge1 ? 1 : sender == Badge2 ? 2 : 3;
            FileManager.SaveDroppedFiles(bubbleIndex, files);
            ReloadFiles();
        }

        private void Clear1_Click(object sender, RoutedEventArgs e)
        {
            FileManager.ClearFiles(1);
            ReloadFiles();
        }

        private void Clear2_Click(object sender, RoutedEventArgs e)
        {
            FileManager.ClearFiles(2);
            ReloadFiles();
        }

        private void Clear3_Click(object sender, RoutedEventArgs e)
        {
            FileManager.ClearFiles(3);
            ReloadFiles();
        }
    }
}