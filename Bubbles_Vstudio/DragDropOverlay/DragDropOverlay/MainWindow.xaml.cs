using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DragDropOverlay
{
    public partial class MainWindow : Window
    {
        private Dictionary<int, List<String>> _bubbleFiles = new();
        private Point? _dragStartPoint;
        private bool _isDraggingFiles;
        private int _currentBubbleId = 0;
        private const int MaxBubbles = 7;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                FileManager.EnsureSubFolder(0);
                AddBubble(0);
                ReloadFiles(0);

                if (Resources["PulseStoryboard"] is Storyboard sb)
                {
                    sb.Begin(this, true);
                }

                UpdateAddBubbleButtonState();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init error: " + ex.Message);
            }
        }

        private void AddBubble(int bubbleId)
        {
            if (BubblesPanel.Children.Count >= MaxBubbles) return;

            // Ensure directory for this bubble
            FileManager.EnsureSubFolder(bubbleId);

            var bubble = new Border
            {
                Style = (Style)Resources["BubbleStyle"],
                Tag = bubbleId,
                Name = $"Bubble_{bubbleId}",
                AllowDrop = true
            };

            // Content layout (count + remove button)
            var grid = new Grid();
            var countText = new TextBlock
            {
                Name = "CountText",
                Text = "0 items",
                Foreground = Brushes.White,
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            };

            var removeButton = new Button
            {
                Content = "×",
                Tag = bubbleId,
                Width = 16,
                Height = 16,
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                ToolTip = "Remove bubble"
            };
            removeButton.Click += RemoveBubble_Click;

            grid.Children.Add(countText);
            if (bubbleId != 0) // keep default bubble not removable
                grid.Children.Add(removeButton);

            bubble.Child = grid;

            // Input & DnD handlers
            bubble.MouseLeftButtonDown += Bubble_MouseLeftButtonDown;
            bubble.MouseMove += Bubble_MouseMove;
            bubble.MouseLeftButtonUp += Bubble_MouseLeftButtonUp;
            bubble.DragOver += Bubble_DragOver;
            bubble.Drop += Bubble_Drop;

            BubblesPanel.Children.Add(bubble);
            _bubbleFiles[bubbleId] = new List<string>();

            UpdateAddBubbleButtonState();
        }

        private void AddBubble_Click(object sender, RoutedEventArgs e)
        {
            int newBubbleId = ++_currentBubbleId;
            FileManager.EnsureSubFolder(newBubbleId);
            AddBubble(newBubbleId);
            ReloadFiles(newBubbleId);
        }

        private void RemoveBubble_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int bubbleId && bubbleId != 0)
            {
                var bubbleToRemove = BubblesPanel.Children
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(b => b.Tag is int id && id == bubbleId);

                if (bubbleToRemove != null)
                {
                    BubblesPanel.Children.Remove(bubbleToRemove);
                    _bubbleFiles.Remove(bubbleId);

                    try
                    {
                        FileManager.DeleteSubFolder(bubbleId);
                    }
                    catch { /* Ignore deletion errors */ }

                    UpdateAddBubbleButtonState();
                }
            }
        }

        private void UpdateAddBubbleButtonState()
        {
            AddBubbleButton.IsEnabled = BubblesPanel.Children.Count < MaxBubbles;
        }

        private void ReloadFiles(int bubbleId)
        {
            var files = FileManager.GetFiles(bubbleId);
            _bubbleFiles[bubbleId] = files;

            var bubble = BubblesPanel.Children
                .OfType<FrameworkElement>()
                .FirstOrDefault(b => b.Tag is int id && id == bubbleId);

            if (bubble != null)
            {
                var countText = bubble.FindName("CountText") as TextBlock;
                if (countText != null)
                {
                    countText.Text = files.Count == 1 ? "1 item" : $"{files.Count} items";
                }

                bubble.ToolTip = files.Count == 0
                    ? $"Bubble {bubbleId}: No files"
                    : $"Bubble {bubbleId}:\n" + string.Join("\n", files.Select(Path.GetFileName));
            }
        }

        private void ReloadFiles() => ReloadAllBubbles();

        private void ReloadAllBubbles()
        {
            foreach (var bubbleId in _bubbleFiles.Keys.ToList())
            {
                ReloadFiles(bubbleId);
            }
        }

        private void StartFileDrag(int bubbleId)
        {
            if (_isDraggingFiles) return;

            var files = _bubbleFiles.ContainsKey(bubbleId) ? _bubbleFiles[bubbleId] : new List<string>();
            var existing = files.Where(File.Exists).ToArray();

            if (existing.Length == 0)
            {
                MessageBox.Show($"No files in bubble {bubbleId}. Drag some files into the bubble.");
                return;
            }

            try
            {
                _isDraggingFiles = true;
                var data = new DataObject(DataFormats.FileDrop, existing);

                var bubble = BubblesPanel.Children
                    .OfType<FrameworkElement>()
                    .FirstOrDefault(b => b.Tag is int id && id == bubbleId);

                if (bubble != null)
                {
                    DragDrop.DoDragDrop(bubble, data, DragDropEffects.Copy);
                }
            }
            finally
            {
                _isDraggingFiles = false;
            }
        }

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.Name?.StartsWith("Bubble_") == true) return;
            try { DragMove(); } catch { }
        }

        private void Bubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            if (sender is Border bubble)
            {
                bubble.CaptureMouse();
            }
        }

        private void Bubble_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) return;

            var pos = e.GetPosition(this);
            var dx = Math.Abs(pos.X - _dragStartPoint.Value.X);
            var dy = Math.Abs(pos.Y - _dragStartPoint.Value.Y);

            if (dx > SystemParameters.MinimumHorizontalDragDistance || dy > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is Border bubble && bubble.Tag is int bubbleId)
                {
                    StartFileDrag(bubbleId);
                }
                _dragStartPoint = null;
                if (sender is Border border)
                {
                    border.ReleaseMouseCapture();
                }
            }
        }

        private void Bubble_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            if (sender is Border bubble)
            {
                bubble.ReleaseMouseCapture();
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e) => ReloadAllBubbles();

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to add to temp folder"
            };

            if (dlg.ShowDialog() == true && BubblesPanel.Children.Count > 0)
            {
                // Add to first bubble by default
                FileManager.SaveDroppedFiles(0, dlg.FileNames);
                ReloadFiles(0);
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

        private void Bubble_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Bubble_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && sender is Border bubble && bubble.Tag is int bubbleId)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileManager.SaveDroppedFiles(bubbleId, files);
                ReloadFiles(bubbleId);
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e) => Bubble_DragOver(sender, e);

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) && BubblesPanel.Children.Count > 0)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                // Add to first bubble by default when dropping on window
                FileManager.SaveDroppedFiles(0, files);
                ReloadFiles(0);
            }
        }
    }
}