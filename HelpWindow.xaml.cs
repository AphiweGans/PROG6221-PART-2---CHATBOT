/*
 * File: HelpWindow.xaml.cs
 * Purpose: Provides a searchable, selectable list of topics for the user to pick from.
 *
 * Details:
 * - Loads topic keys and descriptions from ResponseHandler via reflection for UI
 *   previewing. If that fails, a reasonable fallback list is used.
 * - The UI exposes a preview pane that shows a truncated description. Selecting
 *   a topic and clicking Ask will return the selected topic to the caller.
 * - The search box filters the topics collection using a case-insensitive contains
 *   match. For large topic sets consider switching to a more scalable search/index.
 */
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Linq;
using System.Windows.Controls;

namespace CyberAware
{
    public partial class HelpWindow : Window
    {
        public string SelectedTopic { get; private set; } = string.Empty;

        private readonly ObservableCollection<string> topicsCollection = new();
        private readonly Dictionary<string, string> topicsDict = new(StringComparer.OrdinalIgnoreCase);
        private ICollectionView? topicsView;

        public HelpWindow()
        {
            InitializeComponent();

            LoadTopicsFromResponseHandler();

            topicsView = CollectionViewSource.GetDefaultView(topicsCollection);
            TopicsList.ItemsSource = topicsView;

            SearchBox.TextChanged += SearchBox_TextChanged;
            TopicsList.SelectionChanged += TopicsList_SelectionChanged;

            if (topicsCollection.Count > 0)
                TopicsList.SelectedIndex = 0;
        }

        private void LoadTopicsFromResponseHandler()
        {
            try
            {
                var handler = new ResponseHandler();
                var field = typeof(ResponseHandler).GetField("topics", BindingFlags.NonPublic | BindingFlags.Instance);
                if (field != null)
                {
                    var topicsObj = field.GetValue(handler) as Dictionary<string, string>;
                    if (topicsObj != null)
                    {
                        foreach (var kvp in topicsObj.OrderBy(k => k.Key))
                        {
                            topicsDict[kvp.Key] = kvp.Value;
                            topicsCollection.Add(kvp.Key);
                        }
                        return;
                    }
                }

                // fallback list
                var fallback = new[] { "password", "phishing", "malware", "safe browsing", "identity theft" };
                foreach (var t in fallback)
                {
                    topicsDict[t] = t;
                    topicsCollection.Add(t);
                }
            }
            catch
            {
                topicsCollection.Add("password");
                topicsCollection.Add("phishing");
                topicsCollection.Add("malware");
            }
        }

        private void SearchBox_TextChanged(object? sender, TextChangedEventArgs e)
        {
            if (topicsView == null) return;
            var query = SearchBox.Text?.Trim() ?? string.Empty;
            topicsView.Filter = item =>
            {
                if (string.IsNullOrEmpty(query)) return true;
                var s = item?.ToString() ?? string.Empty;
                return s.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
            };
            topicsView.Refresh();
        }

        private void TopicsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopicsList.SelectedItem is string key && topicsDict.TryGetValue(key, out var desc))
            {
                // show a short preview (first 600 characters) but ensure full wrap is available
                if (desc.Length > 600)
                    PreviewBox.Text = desc.Substring(0, 600) + "...";
                else
                    PreviewBox.Text = desc;
            }
            else
            {
                PreviewBox.Text = string.Empty;
            }
        }

        private void AskButton_Click(object sender, RoutedEventArgs e)
        {
            if (TopicsList.SelectedItem != null)
            {
                SelectedTopic = TopicsList.SelectedItem.ToString() ?? string.Empty;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show(this, "Please select a topic from the list.", "Select a topic", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Scroll helper used by the up/down buttons
        private void ScrollTopicsBy(double offsetChange)
        {
            var sv = FindVisualChild<System.Windows.Controls.ScrollViewer>(TopicsList);
            if (sv != null)
            {
                double target = Math.Max(0, Math.Min(sv.ScrollableHeight, sv.VerticalOffset + offsetChange));
                sv.ScrollToVerticalOffset(target);
            }
        }

        private void TopicsUpButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTopicsBy(-80);
        }

        private void TopicsDownButton_Click(object sender, RoutedEventArgs e)
        {
            ScrollTopicsBy(80);
        }

        private static T? FindVisualChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(depObj, i);
                if (child is T t) return t;
                var result = FindVisualChild<T>(child);
                if (result != null) return result;
            }
            return null;
        }
    }
}
