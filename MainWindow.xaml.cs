/*
 * File: MainWindow.xaml.cs
 * Purpose: Main WPF window for the CyberAware UI.
 *
 * Responsibilities:
 * - Manage the startup sequence (blank overlay -> greeting audio -> name prompt -> show UI).
 * - Render chat messages as styled bubbles for user and bot.
 * - Host the Topics panel and allow fuzzy matching/search for quick access to topics.
 * - Bridge user messages to ResponseHandler and update UI elements (memory sidebar, sentiment alerts).
 *
 * Integration notes:
 * - Uses ResponseHandler to produce replies; ResponseHandler exposes events for sentiment and favorite updates.
 * - The UI intentionally avoids blocking calls; audio playback runs on a background thread and UI uses Dispatcher
 *   for thread-safe updates.
 * - For unit testing, move UI-agnostic logic into helper types to allow testing without WPF.
 */
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CyberAware
{
    public partial class MainWindow : Window
    {
        private readonly ResponseHandler responseHandler = new();
        private string userName = string.Empty;
        private readonly System.Collections.Generic.List<string> availableTopics = new();

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            // Subscribe to sentiment notifications so the UI can react empathetically
            try
            {
                responseHandler.SentimentDetected += OnSentimentDetected;
                responseHandler.FavoriteUpdated += (u, fav) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        try { UpdateMemorySidebar(); } catch { }
                    });
                };
            }
            catch
            {
                // ignore if subscription fails for any reason
            }
        }

        private void OnSentimentDetected(string user, string sentiment, string originalInput)
        {
            // Ensure UI updates happen on the UI thread
            Dispatcher.Invoke(() =>
            {
                // Add a gentle, empathetic message tailored to the sentiment
                string supportive = sentiment switch
                {
                    "worried" => "I can sense you're feeling worried. That's completely normal — I can walk you through this step-by-step.",
                    "curious" => "Great question — I love curiosity! I'll explain in a bit more detail so you can explore further.",
                    "frustrated" => "I'm sorry this is frustrating. Let's slow down and break this into simpler steps together.",
                    "confused" => "No problem — I'll explain this more clearly and give examples.",
                    _ => "I'm here to help. Let's continue."
                };

                ChatPanel.Children.Add(new TextBlock
                {
                    Text = $"Bot (supportive): {supportive}",
                    Foreground = Brushes.LightYellow,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(2)
                });

                // show a Sentiment alert in the sidebar (amber) for a short period
                try
                {
                    SentimentAlert.Text = $"Sentiment Detected: {sentiment}";
                    SentimentAlert.Visibility = Visibility.Visible;
                    // hide after a short delay
                    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
                    timer.Tick += (s, ev) =>
                    {
                        SentimentAlert.Visibility = Visibility.Collapsed;
                        timer.Stop();
                    };
                    timer.Start();
                }
                catch { }

                ScrollToEnd();
            });
        }

        private async void MainWindow_Loaded(object? sender, RoutedEventArgs e)
        {
            // Desired startup sequence:
            // 1. Blank window displayed
            // 2. Play greeting audio
            // 3. Prompt user for name
            // 4. Show final banner and welcome message
            // Load topics panel immediately so users can see topics without typing "help"
            LoadTopicsPanel();

            // Wire search box event
            TopicSearchBox.TextChanged += TopicSearchBox_TextChanged;
            TopicsPanel.SelectionChanged += TopicsPanel_SelectionChanged;

            // Show black overlay so the window appears blank while the greeting plays
            try { BlackOverlay.Visibility = Visibility.Visible; } catch { }
            // allow the UI to render the overlay
            await Task.Delay(150);

            // 2. Play greeting audio first (run on background thread and wait)
            await Task.Run(() => PlayGreetingAudio());

            // hide the black overlay now that the greeting finished
            try { BlackOverlay.Visibility = Visibility.Collapsed; } catch { }

            // 3. Ask user for name using a simple dialog after the audio finishes
            userName = PromptForName();

            // show name in the Memory panel
            try { NameTextBlock.Text = $"Name: {userName}"; } catch { }

            // Persist/check user
            bool isReturning = false;
            try
            {
                var usersFile = Path.Combine(AppContext.BaseDirectory, "assets", "users.txt");
                if (File.Exists(usersFile))
                {
                    var users = File.ReadAllLines(usersFile);
                    foreach (var u in users)
                    {
                        if (string.Equals(u?.Trim(), userName, StringComparison.OrdinalIgnoreCase))
                        {
                            isReturning = true;
                            break;
                        }
                    }
                }
                else
                {
                    var dir = Path.GetDirectoryName(usersFile);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);
                }

                if (!isReturning)
                {
                    File.AppendAllText(usersFile, userName + Environment.NewLine);
                }
            }
            catch
            {
                // ignore
            }

            // 4. Show the main UI and banner/welcome message
            try { RootGrid.Visibility = Visibility.Visible; } catch { }
            ShowBanner();

            if (isReturning)
            {
                ChatPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"Welcome back {userName}! Type 'help' to see available topics.",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 16,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(2)
                });
            }
            else
            {
                ChatPanel.Children.Add(new System.Windows.Controls.TextBlock
                {
                    Text = $"Hello {userName}, type 'help' to see available topics.",
                    Foreground = System.Windows.Media.Brushes.White,
                    FontSize = 16,
                    FontWeight = System.Windows.FontWeights.Bold,
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new System.Windows.Thickness(2)
                });
            }
        }

        private string PromptForName()
        {
            var dlg = new NamePromptWindow();
            if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.UserName))
                return dlg.UserName.Trim();
            return "User";
        }

        private void ShowBanner()
        {
            // Show a polished ASCII header in the top header area
            try
            {
                HeaderAscii.Text = "  ██████╗  CyberAware - Awareness is Your Firewall  ";
            }
            catch { }

            AddBotMessage("Welcome to CyberAware — I can help you with passwords, phishing, scams, privacy and more. Type 'help' to see a list of topics.");
        }

        private void PlayGreetingAudio()
        {
            try
            {
                var gm = new GreetingManager();
                gm.ShowGreeting();
            }
            catch
            {
                // ignore
            }
        }

        private void AddUserMessage(string text)
        {
            // create a right-aligned rounded bubble for user
            var border = new System.Windows.Controls.Border
            {
                // Use a distinct blue/teal accent for user messages so they contrast with the bot
                Background = new SolidColorBrush(Color.FromRgb(10, 132, 255)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(3, 80, 150)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Right,
                MaxWidth = Math.Max(200, ChatScroll.ActualWidth - 120)
            };
            var tb = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.SemiBold
            };
            border.Child = tb;
            ChatPanel.Children.Add(border);
            ScrollToEnd();
            UpdateMemorySidebar();
        }

        private void AddBotMessage(string text)
        {
            // create a left-aligned rounded bubble for bot with emerald accent
            var border = new System.Windows.Controls.Border
            {
                Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10),
                Margin = new Thickness(6),
                HorizontalAlignment = HorizontalAlignment.Left,
                MaxWidth = Math.Max(200, ChatScroll.ActualWidth - 120)
            };
            var tb = new TextBlock
            {
                Text = text,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };

            // If this is the goodbye message, make it more prominent so the user clearly sees it
            if (!string.IsNullOrWhiteSpace(text) && text.IndexOf("goodbye", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                // larger, bolder text and stronger border to draw attention
                tb.FontSize = 18;
                tb.FontWeight = FontWeights.Bold;
                tb.Foreground = Brushes.LightGreen;
                border.BorderBrush = new SolidColorBrush(Color.FromRgb(34, 177, 76));
                border.BorderThickness = new Thickness(2);
            }
            border.Child = tb;
            ChatPanel.Children.Add(border);
            ScrollToEnd();
            UpdateMemorySidebar();
        }

        private void ScrollToEnd()
        {
            ChatScroll.ScrollToEnd();
        }

        private void UpdateMemorySidebar()
        {
            try
            {
                NameTextBlock.Text = $"Name: {userName}";
            }
            catch { }

            try
            {
                var fav = responseHandler.GetFavoriteTopic(userName);
                FavoriteTopicTextBlock.Text = $"Favorite Topic: {fav ?? "-"}";
            }
            catch { FavoriteTopicTextBlock.Text = "Favorite Topic: -"; }

            try
            {
                var last = responseHandler.GetLastTopic(userName);
                LastTopicTextBlock.Text = $"Last Topic: {last ?? "-"}";
            }
            catch { LastTopicTextBlock.Text = "Last Topic: -"; }
        }

        private void LoadTopicsPanel()
        {
            try
            {
                var handler = new ResponseHandler();
                var field = typeof(ResponseHandler).GetField("topics", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var topicsObj = field.GetValue(handler) as System.Collections.Generic.Dictionary<string, string>;
                    if (topicsObj != null)
                    {
                        foreach (var key in topicsObj.Keys.OrderBy(k => k))
                        {
                            availableTopics.Add(key);
                            TopicsPanel.Items.Add(key);
                        }
                        return;
                    }
                }

                // fallback
                var fallback = new[] { "password", "phishing", "malware", "safe browsing", "identity theft" };
                foreach (var t in fallback)
                {
                    availableTopics.Add(t);
                    TopicsPanel.Items.Add(t);
                }
            }
            catch
            {
                TopicsPanel.Items.Add("password");
                TopicsPanel.Items.Add("phishing");
                TopicsPanel.Items.Add("malware");
            }
        }

        private void TopicSearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var q = TopicSearchBox.Text?.Trim() ?? string.Empty;
            TopicsPanel.Items.Clear();
            foreach (var t in availableTopics)
            {
                if (string.IsNullOrEmpty(q) || t.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    TopicsPanel.Items.Add(t);
            }
            if (TopicsPanel.Items.Count > 0)
                TopicsPanel.SelectedIndex = 0;
        }

        private void TopicsPanel_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (TopicsPanel.SelectedItem is string topic && !string.IsNullOrWhiteSpace(topic))
            {
                // show as user message and fetch response
                AddUserMessage(topic);
                var response = responseHandler.GetResponse(topic, userName);
                AddBotMessage(response);
            }
        }

        // Scroll helper for TopicsPanel used by the up/down buttons
        private void ScrollListBoxBy(ListBox listBox, double offsetChange)
        {
            if (listBox == null) return;
            // find the internal ScrollViewer
            var sv = FindVisualChild<System.Windows.Controls.ScrollViewer>(listBox);
            if (sv != null)
            {
                double target = Math.Max(0, Math.Min(sv.ScrollableHeight, sv.VerticalOffset + offsetChange));
                sv.ScrollToVerticalOffset(target);
            }
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

        private void TopicsScrollUp_Click(object sender, RoutedEventArgs e)
        {
            ScrollListBoxBy(TopicsPanel, -80); // scroll up roughly one item height
        }

        private void TopicsScrollDown_Click(object sender, RoutedEventArgs e)
        {
            ScrollListBoxBy(TopicsPanel, 80); // scroll down
        }

        // Local Levenshtein distance for fuzzy matching user input against available topics
        private int LevenshteinDistance(string a, string b)
        {
            a = a ?? string.Empty;
            b = b ?? string.Empty;
            int[,] dp = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) dp[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) dp[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                    dp[i, j] = Math.Min(Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                                        dp[i - 1, j - 1] + cost);
                }
            }
            return dp[a.Length, b.Length];
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var input = InputBox.Text?.Trim();
            if (string.IsNullOrWhiteSpace(input)) return;
            AddUserMessage(input);
            InputBox.Clear();

            // Remember current favorite topic so we can detect changes after handling the input
            var previousFav = responseHandler.GetFavoriteTopic(userName);

            // If the user specifically asks for help, show a polished HelpWindow with topics
            if (string.Equals(input.Trim(), "help", StringComparison.OrdinalIgnoreCase))
            {
                var help = new HelpWindow();
                help.Owner = this;
                if (help.ShowDialog() == true)
                {
                    var topic = help.SelectedTopic;
                    if (!string.IsNullOrWhiteSpace(topic))
                    {
                        AddUserMessage(topic);
                        var response = responseHandler.GetResponse(topic, userName);
                        AddBotMessage(response);
                    }
                }
                return;
            }

            // Check for fuzzy match against available topics (whole input or individual words)
            string lowerInput = input.ToLowerInvariant();
            string? bestMatch = null;
            int bestDistance = int.MaxValue;

            foreach (var t in availableTopics)
            {
                var topicLower = t.ToLowerInvariant();
                int dist = LevenshteinDistance(lowerInput, topicLower);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestMatch = t;
                }

                // Also compare individual words
                foreach (var w in lowerInput.Split(' '))
                {
                    int wd = LevenshteinDistance(w, topicLower);
                    if (wd < bestDistance)
                    {
                        bestDistance = wd;
                        bestMatch = t;
                    }
                }
            }

            // If a close fuzzy match is found, answer for that topic automatically
            if (bestMatch != null && bestDistance <= 2)
            {
                AddBotMessage($"I think you meant '{bestMatch}'. Here's some information:");
                var resp = responseHandler.GetResponse(bestMatch, userName);
                AddBotMessage(resp);
                return;
            }

            // Allow multiple questions separated by common separators
            var normalized = input.Replace(" and ", ",", StringComparison.OrdinalIgnoreCase)
                                  .Replace(" then ", ",", StringComparison.OrdinalIgnoreCase);
            var parts = normalized.Split(new[] { '?', ';', ',', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var part in parts)
            {
                var question = part.Trim();
                if (string.IsNullOrWhiteSpace(question)) continue;

                // Use response handler - note: console interactive prompts inside ResponseHandler may not show in GUI
                string response = responseHandler.GetResponse(question, userName);
                AddBotMessage(response);
            }

            // If the user declared a favourite topic in their message (e.g. "my favourite topic is phishing"),
            // the ResponseHandler stores it in session memory. Detect this change and update the TopicsPanel
            // so the favourite topic is selected and visible in the topics list.
            try
            {
                var newFav = responseHandler.GetFavoriteTopic(userName);
                if (!string.IsNullOrWhiteSpace(newFav) && !string.Equals(newFav, previousFav, StringComparison.OrdinalIgnoreCase))
                {
                    // If the topic exists in the available topics list, select and scroll it into view
                    for (int i = 0; i < TopicsPanel.Items.Count; i++)
                    {
                        if (string.Equals(TopicsPanel.Items[i]?.ToString(), newFav, StringComparison.OrdinalIgnoreCase))
                        {
                            TopicsPanel.SelectedIndex = i;
                            TopicsPanel.ScrollIntoView(TopicsPanel.Items[i]);
                            break;
                        }
                    }
                    UpdateMemorySidebar();
                    // Also add a prominent bot-style message at the top of the chat to indicate the favourite topic
                    try
                    {
                        var topBorder = new System.Windows.Controls.Border
                        {
                            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
                            BorderBrush = new SolidColorBrush(Color.FromRgb(46, 204, 113)),
                            BorderThickness = new Thickness(1.5),
                            CornerRadius = new CornerRadius(12),
                            Padding = new Thickness(10),
                            Margin = new Thickness(6),
                            HorizontalAlignment = HorizontalAlignment.Left,
                            MaxWidth = Math.Max(200, ChatScroll.ActualWidth - 120)
                        };
                        var topTb = new System.Windows.Controls.TextBlock
                        {
                            Text = $"Favourite topic set: {newFav}",
                            Foreground = System.Windows.Media.Brushes.LightGreen,
                            FontWeight = System.Windows.FontWeights.SemiBold,
                            TextWrapping = System.Windows.TextWrapping.Wrap
                        };
                        topBorder.Child = topTb;
                        // Insert at the top of the chat panel so it appears first
                        ChatPanel.Children.Insert(0, topBorder);
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ChatPanel.Children.Clear();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Display a polite, professional goodbye message in the chat pane
                AddBotMessage("Goodbye — it was a pleasure having you. Thank you for using CyberAware. Stay safe online.");

                // Ensure the goodbye message is visible at the bottom of the chat before closing.
                try
                {
                    // Force scroll to end and bring the last message into view
                    ScrollToEnd();
                    if (ChatPanel.Children.Count > 0)
                    {
                        var lastObj = ChatPanel.Children[ChatPanel.Children.Count - 1];
                        if (lastObj is System.Windows.FrameworkElement fe)
                        {
                            fe.BringIntoView();
                        }
                    }
                }
                catch { }

                // Give the user a moment to read the message before closing the application
                var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                timer.Tick += (s, ev) =>
                {
                    timer.Stop();
                    Application.Current.Shutdown();
                };
                timer.Start();
            }
            catch
            {
                // Fallback: ensure the app still shuts down if something goes wrong
                Application.Current.Shutdown();
            }
        }
    }
}
