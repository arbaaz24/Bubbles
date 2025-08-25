// =============================
// File: MainWindow.xaml.cs
// =============================
//using Microsoft.Win32;  // Commented out - used only for debugging functions
using System;
using System.Collections.Generic;
//using System.Diagnostics;  // Commented out - used only for debugging functions
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
                // Restore bubbles from existing folders
                RestoreExistingBubbles();

                // If no bubbles exist, create the default bubble
                if (BubblesPanel.Children.Count == 0)
                {
                    AddBubble(1);
                    ReloadFiles(1);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Init error: " + ex.Message);
            }
        }

        // Add this new method to restore bubbles from existing folders
        private void RestoreExistingBubbles()
        {
            // First check for files in the root directory (bubble 1)
            var rootFiles = FileManager.GetFiles(1);
            if (rootFiles.Count > 0)
            {
                AddBubble(1);
                ReloadFiles(1);
            }

            // Then check for existing bubble folders (2-7)
            for (int i = 2; i <= MaxBubbles; i++)
            {
                var files = FileManager.GetFiles(i);
                if (files.Count > 0)
                {
                    AddBubble(i);
                    ReloadFiles(i);
                }
            }
        }

        private void AddBubble_Click(object sender, RoutedEventArgs e)
        {
            if (BubblesPanel.Children.Count >= MaxBubbles) return;

            // Find the lowest available ID from 1 to 7
            int newId = GetNextAvailableBubbleId();
            if (newId <= MaxBubbles)
            {
                AddBubble(newId);
                ReloadFiles(newId);
            }
        }

        private int GetNextAvailableBubbleId()
        {
            for (int i = 1; i <= MaxBubbles; i++)
            {
                if (!_bubbleFiles.ContainsKey(i))
                    return i;
            }
            return MaxBubbles + 1; // Return invalid ID if all slots are taken
        }

        // Update the AddBubble method to insert bubbles in the correct position
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

            var grid = new Grid();

            // Left 'Clear' button (clears files only)
            var clearBtn = new Button
            {
                Content = "Clear",
                Width = 34,
                Height = 18,
                Padding = new Thickness(0),
                Margin = new Thickness(-4, -6, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Black,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = bubbleId,
                ToolTip = "Clear files in this bubble"
            };
            clearBtn.Click += ClearBubble_Click;

            // Right '✕' close button (removes bubble)
            var closeBtn = new Button
            {
                Content = "✕",
                Width = 20,
                Height = 20,
                Padding = new Thickness(0),
                Margin = new Thickness(0, -6, -4, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Foreground = Brushes.Red,
                FontWeight = FontWeights.Bold,
                Cursor = Cursors.Hand,
                Tag = bubbleId,
                ToolTip = "Pop bubble"
            };
            closeBtn.Click += CloseBubble_Click;

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
                Foreground = (Brush)(new BrushConverter().ConvertFromString("#F0F0F0") ?? Brushes.White),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(dragLabel);
            stack.Children.Add(countText);

            grid.Children.Add(clearBtn);
            grid.Children.Add(closeBtn);
            grid.Children.Add(stack);

            bubble.Child = grid;

            bubble.MouseLeftButtonDown += Bubble_MouseLeftButtonDown;
            bubble.MouseMove += Bubble_MouseMove;
            bubble.MouseLeftButtonUp += Bubble_MouseLeftButtonUp;
            bubble.DragOver += Bubble_DragOver;
            bubble.Drop += Bubble_Drop;

            if (Resources["PulseStoryboard"] is Storyboard pulse)
            {
                var anim = pulse.Clone();
                foreach (var tl in anim.Children.OfType<DoubleAnimation>())
                    Storyboard.SetTarget(tl, bubble);
                anim.Begin();
            }

            // Insert bubble at the correct position to maintain sorted order
            InsertBubbleInOrder(bubble, bubbleId);
            _bubbleFiles[bubbleId] = new List<string>();
            UpdateAddBubbleButtonState();
        }

        // Add this new method to insert bubbles in ascending order
        private void InsertBubbleInOrder(Border newBubble, int bubbleId)
        {
            int insertIndex = 0;

            // Find the correct position to insert the new bubble
            for (int i = 0; i < BubblesPanel.Children.Count; i++)
            {
                if (BubblesPanel.Children[i] is Border existingBubble &&
                    existingBubble.Tag is int existingId)
                {
                    if (bubbleId < existingId)
                    {
                        insertIndex = i;
                        break;
                    }
                    insertIndex = i + 1;
                }
            }

            BubblesPanel.Children.Insert(insertIndex, newBubble);
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

        //***** COMMENTED OUT DEBUGGING METHODS - UNCOMMENT IF NEEDED FOR DEBUGGING *****

        //private void Reload_Click(object sender, RoutedEventArgs e) => ReloadAll();

        //private void ReloadAll()
        //{
        //    foreach (var id in _bubbleFiles.Keys.ToList())
        //        ReloadFiles(id);
        //}

        //private void AddFiles_Click(object sender, RoutedEventArgs e)
        //{
        //    if (!_bubbleFiles.ContainsKey(1))
        //        AddBubble(1);

        //    var dlg = new OpenFileDialog
        //    {
        //        Multiselect = true,
        //        Title = "Select files to add to bubble 1"
        //    };
        //    if (dlg.ShowDialog() == true)
        //    {
        //        FileManager.SaveDroppedFiles(1, dlg.FileNames);
        //        ReloadFiles(1);
        //    }
        //}

        //private void OpenFolder_Click(object sender, RoutedEventArgs e)
        //{
        //    try
        //    {
        //        FileManager.EnsureFolder();
        //        Process.Start(new ProcessStartInfo
        //        {
        //            FileName = FileManager.TempDir,
        //            UseShellExecute = true,
        //            Verb = "open"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Open folder failed: " + ex.Message);
        //    }
        //}

        //private void Kill_Click(object sender, RoutedEventArgs e)
        //{
        //    Application.Current.Shutdown();
        //}

        //***** END COMMENTED OUT DEBUGGING METHODS *****

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
                //ReloadAll(); // Commented out - no longer needed without ReloadAll method
                e.Handled = true; // Prevent Window_Drop from also adding to bubble 0
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            FileManager.SaveDroppedFiles(1, files);
            ReloadFiles(1);
            //ReloadAll(); // Commented out - no longer needed without ReloadAll method
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // Optionally, you can set e.Effects to show a copy/move cursor
            e.Effects = DragDropEffects.Copy;
            e.Handled = true;
        }

        private void CloseBubble_Click(object? sender, RoutedEventArgs e)
        {
            if (sender is Button b && b.Tag is int id)
            {
                // Remove bubble from panel
                var bubble = BubblesPanel.Children
                    .OfType<Border>()
                    .FirstOrDefault(bd => bd.Tag is int bid && bid == id);
                if (bubble != null)
                    BubblesPanel.Children.Remove(bubble);

                // Remove files from dictionary (but keep files on disk)
                _bubbleFiles.Remove(id);

                // Don't clear files or delete folder - files persist for next session
                // FileManager.ClearBubble(id);  // Commented out
                // FileManager.DeleteBubbleFolder(id);  // Commented out

                UpdateAddBubbleButtonState();

                // Ensure remaining bubbles are still in correct order
                //SortBubblesInPanel(); // Commented out - redundant since InsertBubbleInOrder handles sorting
            }
        }

        //***** COMMENTED OUT REDUNDANT METHOD - InsertBubbleInOrder already handles sorting *****

        //private void SortBubblesInPanel()
        //{
        //    var bubbles = BubblesPanel.Children
        //        .OfType<Border>()
        //        .Where(b => b.Tag is int)
        //        .OrderBy(b => (int)b.Tag)
        //        .ToList();
        //    
        //    BubblesPanel.Children.Clear();
        //    
        //    foreach (var bubble in bubbles)
        //    {
        //        BubblesPanel.Children.Add(bubble);
        //    }
        //}

        //***** END COMMENTED OUT REDUNDANT METHOD *****
    }
}