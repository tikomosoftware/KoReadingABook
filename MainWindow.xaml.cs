using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace KoReadingABook
{
    public partial class MainWindow : Window
    {
        private readonly WindowService _windowService;
        private readonly MouseService _mouseService;
        private readonly DispatcherTimer _windowTimer;
        private readonly DispatcherTimer _mouseTimer;
        private readonly Random _random;
        public ObservableCollection<LogEntry> Logs { get; set; }

        private bool _isRunning = false;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;

            _windowService = new WindowService();
            _mouseService = new MouseService();
            _random = new Random();
            Logs = new ObservableCollection<LogEntry>();
            LogListView.ItemsSource = Logs;

            // Window Timer setup (Default 15s)
            _windowTimer = new DispatcherTimer();
            _windowTimer.Tick += WindowTimer_Tick;
            
            // Mouse Timer setup (1s)
            _mouseTimer = new DispatcherTimer();
            _mouseTimer.Interval = TimeSpan.FromSeconds(1);
            _mouseTimer.Tick += MouseTimer_Tick;

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initial selection 15s (Index 1)
            IntervalComboBox.SelectedIndex = 1;
            UpdateTimerInterval();

            var desktopWorkingArea = SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width - 10;
            this.Top = desktopWorkingArea.Bottom - this.Height - 10;

            // Initial manual start state
            StopButton.Background = System.Windows.Media.Brushes.Gray;
            StatusTextBlock.Text = "Ready";
            AddLog("System", "Application loaded. Press Start.");
        }

        private void IntervalComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            UpdateTimerInterval();
        }

        private void UpdateTimerInterval()
        {
            if (IntervalComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item && 
                int.TryParse(item.Tag.ToString(), out int seconds))
            {
                _windowTimer.Interval = TimeSpan.FromSeconds(seconds);
                
                // If running, restart to apply immediately (optional, or just waiting for next tick)
                // For better feedback, we can update the status text
                if (_isRunning)
                {
                   NextRunTextBlock.Text = $"Interval: {seconds}s";
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartProcess();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            StopProcess();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Logs.Clear();
        }

        private void StartProcess()
        {
            if (_isRunning) return;

            _isRunning = true;
            _windowTimer.Start();
            _mouseTimer.Start();
            
            StartButton.IsEnabled = false;
            StartButton.Background = System.Windows.Media.Brushes.Gray;
            StopButton.IsEnabled = true;
            StopButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // Red

            StatusTextBlock.Text = "Running";
            StatusTextBlock.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Green
            NextRunTextBlock.Text = "Active. Monitoring...";
            
            AddLog("System", "Service started. Mouse & Window tasks active.");
        }

        private void StopProcess()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _windowTimer.Stop();
            _mouseTimer.Stop();

            StartButton.IsEnabled = true;
            StartButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // Green
            StopButton.IsEnabled = false;
            StopButton.Background = System.Windows.Media.Brushes.Gray;

            StatusTextBlock.Text = "Stopped";
            StatusTextBlock.Foreground = System.Windows.Media.Brushes.Gray;
            NextRunTextBlock.Text = "";

            AddLog("System", "Service stopped.");
        }

        private void MouseTimer_Tick(object? sender, EventArgs e)
        {
            try 
            {
                string status = _mouseService.PerformCircularMove();
                if (!string.IsNullOrEmpty(status))
                {
                    // Filter "Paused" status to avoid spam
                    if (status == "Paused") return;

                    // Log interaction or resumption
                    if (status.Contains("User interaction") || status.Contains("resumed"))
                    {
                        AddLog("Mouse", status);
                    }
                }
            }
            catch (Exception ex)
            {
                // Optionally log mouse errors, but keep it quiet to avoid spam
                // AddLog("Mouse", $"Error: {ex.Message}");
            }
        }

        private void WindowTimer_Tick(object? sender, EventArgs e)
        {
            PerformRandomActivation();
        }

        private void PerformRandomActivation()
        {
            if (_mouseService.IsPaused)
            {
                AddLog("Window", "Switch skipped (User active).");
                return;
            }

            try
            {
                // 0. Get current active info
                IntPtr currentHwnd = _windowService.GetActiveWindowHandle();
                string currentTitle = _windowService.GetWindowTitle(currentHwnd);
                if (string.IsNullOrEmpty(currentTitle)) currentTitle = "(None/System)";

                // 1. Enumerate and Filter
                var targets = _windowService.GetTargetWindows();
                AddLog("Window", $"Targets: {targets.Count} (excl. active)");

                if (targets.Count == 0)
                {
                    AddLog("Window", "No targets available.");
                    return;
                }

                // 2. Select Random
                int index = _random.Next(targets.Count);
                var selected = targets[index];
                
                AddLog("Info", $"Switching: [{Truncate(currentTitle)}] -> [{Truncate(selected.Title)}]");

                // 3. Activate
                try
                {
                    _windowService.ActivateWindow(selected.Handle);
                }
                catch (Exception ex)
                {
                    AddLog("Error", $"Activate failed: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                AddLog("Error", $"Unexpected: {ex.Message}");
            }
            finally
            {
                // Update specific next run text if needed, or keep generic
            }
        }

        private string Truncate(string s, int max = 20)
        {
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Length <= max) return s;
            return s.Substring(0, max) + "...";
        }

        private void AddLog(string type, string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                Type = type,
                Message = message
            };

            // Needs UI thread if called from non-UI thread
            Logs.Insert(0, entry); 
        }
    }
}