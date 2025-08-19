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
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace DragDropOverlay
{
    public partial class MainWindow : Window
    {
        private readonly Dictionary<int, List<string>> _bubbleFiles = new();
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
                // Root bubble
                AddBubble(0);
                ReloadFiles(0);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init error: " + ex.Message);
            }
        }

        private void AddBubble_Click(object sender, RoutedEventArgs e)
        {
            if (BubblesPanel.Children.Count >= MaxBubbles) return;
            int newId = ++_currentBubbleId;
            AddBubble(newId);
            ReloadFiles(newId);
        }

        private void AddBubble(int bubbleId)
        {
            if (_bubbleFiles.ContainsKey(bubbleId)) return;
            if (BubblesPanel.Children.Count >= MaxBubbles) return;

            FileManager.EnsureSubFolder(bubbleId);

            var bubble = new Border
            {
                Style = (Style)Resources["BubbleStyle"],
                Tag = bubbleId,
                ToolTip = "Loading.."
            };

            // Inner layout
            var grid = new Grid();

            // Clear button
            var clearBtn = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(45, -5, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = bubbleId,
                ToolTip = "Clear files in this bubble"
            };
            clearBtn.Click += ClearBubble_Click;

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dragLabel = new TextBlock
            {
                Text = "Drag Files",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var countText = new TextBlock
            {
                Name = "CountText",
                Text = "0 items",
                FontSize = 14,
                Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F0F0"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(dragLabel);
            stack.Children.Add(countText);

            grid.Children.Add(clearBtn);
            grid.Children.Add(stack);

            bubble.Child = grid;

            // Events
            bubble.MouseLeftButtonDown += Bubble_MouseLeftButtonDown;
            bubble.MouseMove += Bubble_MouseMove;
            bubble.MouseLeftButtonUp += Bubble_MouseLeftButtonUp;
            bubble.DragOver += Bubble_DragOver;
            bubble.Drop += Bubble_Drop;

            // Optional pulse animation per bubble
            if (Resources["PulseStoryboard"] is Storyboard pulse)
            {
                var anim = pulse.Clone();
                foreach (var tl in anim.Children.OfType<DoubleAnimation>())
                {
                    Storyboard.SetTarget(tl, bubble);
                }
                anim.Begin();
            }

            BubblesPanel.Children.Add(bubble);
            _bubbleFiles[bubbleId] = new List<string>();

            UpdateAddBubbleButtonState();
        }

        private void UpdateAddBubbleButtonState() =>
            AddBubbleButton.IsEnabled = BubblesPanel.Children.Count < MaxBubbles;

        private void ClearBubble_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                FileManager.ClearBubble(id);
                ReloadFiles(id);
            }
        }

        private void Reload_Click(object sender, RoutedEventArgs e) => ReloadAll();

        private void ReloadAll()
        {
            foreach (var id in _bubbleFiles.Keys.ToList())
                ReloadFiles(id);
        }

        private void ReloadFiles(int bubbleId)
        {
            var files = FileManager.GetFiles(bubbleId);
            _bubbleFiles[bubbleId] = files;

            var bubble = BubblesPanel.Children
                .OfType<Border>()
                .FirstOrDefault(b => b.Tag is int id && id == bubbleId);

            if (bubble == null) return;

            var countText = FindDescendant<TextBlock>(bubble, "CountText");
            if (countText != null)
                countText.Text = files.Count == 1 ? "1 item" : $"{files.Count} items";

            bubble.ToolTip = files.Count == 0
                ? $"Bubble {bubbleId}: No files"
                : $"Bubble {bubbleId}:\n" + string.Join("\n", files.Select(Path.GetFileName));
        }

        private static T? FindDescendant<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T fe && fe.Name == name)
                    return fe;
                var result = FindDescendant<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            if (!_bubbleFiles.ContainsKey(0))
                AddBubble(0);

            var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Select files to add to bubble 0"
            };
            if (dlg.ShowDialog() == true)
            {
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

        private void Root_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement fe && fe.Tag is int) return;
            try { DragMove(); } catch { }
        }

        private void Bubble_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = e.GetPosition(this);
            if (sender is Border b) b.CaptureMouse();
        }

        private void Bubble_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed || _dragStartPoint == null) return;

            var pos = e.GetPosition(this);
            if (Math.Abs(pos.X - _dragStartPoint.Value.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(pos.Y - _dragStartPoint.Value.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                if (sender is Border b && b.Tag is int id)
                    StartFileDrag(id);
                _dragStartPoint = null;
                if (sender is Border bb) bb.ReleaseMouseCapture();
            }
        }

        private void Bubble_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _dragStartPoint = null;
            if (sender is Border b) b.ReleaseMouseCapture();
        }

        private void StartFileDrag(int bubbleId)
        {
            if (_isDraggingFiles) return;
            var files = _bubbleFiles.TryGetValue(bubbleId, out var list) ? list : new List<string>();
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
                    .OfType<Border>()
                    .FirstOrDefault(b => b.Tag is int id && id == bubbleId);
                if (bubble != null)
                    DragDrop.DoDragDrop(bubble, data, DragDropEffects.Copy);
            }
            finally
            {
                _isDraggingFiles = false;
            }
        }

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
            if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
                sender is Border b && b.Tag is int id)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                FileManager.SaveDroppedFiles(id, files);
                ReloadFiles(id);
                e.Handled = true; // Prevent Window_Drop from also adding to bubble 0
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            FileManager.SaveDroppedFiles(0, files);
            ReloadFiles(0);
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // Example: Show copy effect if files are being dragged
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void Kill_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}