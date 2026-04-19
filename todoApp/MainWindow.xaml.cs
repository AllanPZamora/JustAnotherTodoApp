using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace todoApp
{
    public partial class MainWindow : Window
    {
        private TaskService _taskService = new TaskService();
        private ProfileService _profileService = new ProfileService();
        private string _profileId;
        private string _profileName;
        private string _profileColor;
        private string _currentTheme;
        private Profile _currentProfile;
        private DateTime _currentDate = DateTime.Today;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isMonthView = true;
        private bool _taskPanelOpen = false;
        private bool _isTasksView = false;   // false = Calendar (default), true = Tasks dashboard
        private TaskItem? _selectedDashboardTask = null;
        private DateTime  _selectedDashboardDate  = DateTime.Today;
        private bool _detailPanelOpen = false;

        // Recurrence state
        private RecurrenceType _selectedRecurrence = RecurrenceType.None;
        private DateTime? _recurrenceEndDate = null;
        private bool _isPickingEndDate = false;

        public MainWindow(string profileId, string profileName, string profileColor, string theme)
        {
            InitializeComponent();
            TopBar.MouseLeftButtonDown += TopBar_MouseLeftButtonDown;
            _profileId    = profileId;
            _profileName  = profileName;
            _profileColor = profileColor;
            _currentTheme = theme;
            _currentProfile = _profileService.LoadProfiles().Find(p => p.Id == profileId)!;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            ProfileNameText.Text     = _profileName;
            ProfileInitialsText.Text = GetInitials(_profileName);
            ProfileCircle.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(_profileColor)!;

            ProfileCircle.MouseLeftButtonUp  += ProfileCircle_Clicked;
            ProfileNameText.MouseLeftButtonUp += ProfileCircle_Clicked;
            ProfileCircle.Cursor  = Cursors.Hand;
            ProfileNameText.Cursor = Cursors.Hand;

            ApplyTheme(_currentTheme);
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement src)
            {
                FrameworkElement? hit = src;
                while (hit != null)
                {
                    if (hit == ProfileCircle || hit == ProfileNameText) return;
                    hit = hit.Parent as FrameworkElement;
                }
            }
            if (e.ButtonState == MouseButtonState.Pressed) this.DragMove();
        }

        private string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
        }

        // ─── Settings Popup ──────────────────────────────────────────────

        private void ProfileCircle_Clicked(object sender, MouseButtonEventArgs e)
        {
            var settings = new SettingsPopup(_currentProfile, this);
            settings.ThemeChanged   += (newTheme) => ApplyTheme(newTheme);
            settings.ProfileDeleted += () => { new ProfileWindow().Show(); this.Close(); };
            settings.Show();
        }

        // ─── Theme ───────────────────────────────────────────────────────

        private void ApplyTheme(string themeName)
        {
            _currentTheme = themeName;
            var t = ThemeService.GetTheme(themeName);

            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, e) =>
            {
                RootBorder.Background       = t.WindowBackground;
                TopBar.Background           = t.TopBarBackground;
                ProfileNameText.Foreground  = t.PrimaryText;
                MonthYearText.Foreground    = t.PrimaryText;
                SelectedDateText.Foreground = t.PrimaryText;
                PrevBtn.Foreground          = t.PrimaryText;
                PrevBtn.BorderBrush         = t.ButtonBorder;
                NextBtn.Foreground          = t.PrimaryText;
                NextBtn.BorderBrush         = t.ButtonBorder;

                // Calendar sub-toggle
                MonthViewBtn.Background = _isMonthView  ? t.AccentColor : t.TopBarButtonBackground;
                MonthViewBtn.Foreground = _isMonthView  ? Brushes.White  : t.TopBarButtonText;
                WeekViewBtn.Background  = !_isMonthView ? t.AccentColor : t.TopBarButtonBackground;
                WeekViewBtn.Foreground  = !_isMonthView ? Brushes.White  : t.TopBarButtonText;

                // Mode toggle
                TasksViewBtn.Background    = _isTasksView  ? t.AccentColor : t.TopBarButtonBackground;
                TasksViewBtn.Foreground    = _isTasksView  ? Brushes.White  : t.TopBarButtonText;
                CalendarViewBtn.Background = !_isTasksView ? t.AccentColor : t.TopBarButtonBackground;
                CalendarViewBtn.Foreground = !_isTasksView ? Brushes.White  : t.TopBarButtonText;
                ModeToggleBorder.Background     = t.TopBarButtonBackground;
                ModeToggleBorder.BorderBrush    = t.ButtonBorder;
                CalViewToggleBorder.Background  = t.TopBarButtonBackground;
                CalViewToggleBorder.BorderBrush = t.ButtonBorder;
                LogoutBtn.Background  = t.TopBarButtonBackground;
                LogoutBtn.BorderBrush = t.ButtonBorder;
                LogoutBtn.Foreground  = t.TopBarButtonText;
                CloseBtn.Background   = t.TopBarButtonBackground;
                CloseBtn.BorderBrush  = t.ButtonBorder;
                CloseBtn.Foreground   = t.TopBarButtonText;

                TaskPanel.Background        = t.TaskPanelBackground;
                TaskTitleInput.Background   = t.InputBackground;
                TaskTitleInput.Foreground   = t.PrimaryText;
                TaskTitleInput.BorderBrush  = t.InputBorder;
                TaskNotesInput.Background   = t.InputBackground;
                TaskNotesInput.Foreground   = t.PrimaryText;
                TaskNotesInput.BorderBrush  = t.InputBorder;
                AddTaskBtn.Background       = t.AccentColor;

                UpdateRecurrenceBtnTheme(t);

                // Always update detail panel colors regardless of current view
                DetailPanel.Background = t.TaskPanelBackground;
                DetailDivider.Background = t.CalendarCellBorder;
                DetailTitle.Foreground = t.PrimaryText;
                DetailDate.Foreground  = t.SecondaryText;
                DetailDateBadge.Background = t.ButtonBackground;
                DetailNotes.Foreground = t.PrimaryText;

                if (_isTasksView)
                {
                    BuildDashboard();
                    if (_selectedDashboardTask != null)
                        ShowTaskDetail(_selectedDashboardTask, _selectedDashboardDate, t);
                }
                else BuildCalendar();

                if (_taskPanelOpen) LoadTasksForDate(_selectedDate);

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            };
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        // ─── Mode Toggle (Tasks / Calendar) ──────────────────────────────

        private void TasksViewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isTasksView) return;
            _isTasksView = true;
            var t = ThemeService.GetTheme(_currentTheme);
            TasksViewBtn.Background    = t.AccentColor;             TasksViewBtn.Foreground    = Brushes.White;
            CalendarViewBtn.Background = t.TopBarButtonBackground; CalendarViewBtn.Foreground = t.TopBarButtonText;

            CalendarView.Visibility    = Visibility.Collapsed;
            TasksDashboard.Visibility  = Visibility.Visible;
            CalViewToggleBorder.Visibility = Visibility.Collapsed;
            BuildDashboard();
        }

        private void CalendarViewBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!_isTasksView) return;
            _isTasksView = false;
            var t = ThemeService.GetTheme(_currentTheme);
            CalendarViewBtn.Background = t.AccentColor;             CalendarViewBtn.Foreground = Brushes.White;
            TasksViewBtn.Background    = t.TopBarButtonBackground; TasksViewBtn.Foreground    = t.TopBarButtonText;

            TasksDashboard.Visibility  = Visibility.Collapsed;
            CalendarView.Visibility    = Visibility.Visible;
            CalViewToggleBorder.Visibility = Visibility.Visible;
            _detailPanelOpen = false;
            _selectedDashboardTask = null;
            BuildCalendar();
        }

        // ─── Tasks Dashboard ─────────────────────────────────────────────

        private void BuildDashboard()
        {
            DashboardPanel.Children.Clear();
            var t = ThemeService.GetTheme(_currentTheme);

            // ── Section A: Today ─────────────────────────────────────────
            var todayTasks = _taskService.GetTasksForDate(_profileId, DateTime.Today);

            AddDashboardSectionHeader("📌  Today", DateTime.Today.ToString("dddd, MMMM d"), t, isToday: true);

            if (todayTasks.Count == 0)
                AddEmptyState("No tasks for today — enjoy your day!", t);
            else
                foreach (var task in todayTasks)
                    DashboardPanel.Children.Add(CreateDashboardCard(task, DateTime.Today, t, refreshAll: true));

            // ── Section B: Upcoming (next 7 days) ────────────────────────
            AddSectionDivider(t);
            AddDashboardSectionHeader("🗓  Upcoming", "Next 7 days", t, isToday: false);

            bool hasUpcoming = false;
            for (int d = 1; d <= 7; d++)
            {
                var date  = DateTime.Today.AddDays(d);
                var tasks = _taskService.GetTasksForDate(_profileId, date);
                if (tasks.Count == 0) continue;

                hasUpcoming = true;

                // Date sub-header
                var dateLabel = new TextBlock
                {
                    Text       = date.ToString("ddd, MMM d"),
                    FontSize   = 12,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = t.SecondaryText,
                    Margin     = new Thickness(0, 10, 0, 4)
                };
                DashboardPanel.Children.Add(dateLabel);

                foreach (var task in tasks)
                    DashboardPanel.Children.Add(CreateDashboardCard(task, date, t, refreshAll: true));
            }

            if (!hasUpcoming)
                AddEmptyState("Nothing scheduled for the next 7 days.", t);

            // ── Section C: Backlog ────────────────────────────────────────
            var backlog = GetBacklogTasks();
            if (backlog.Count > 0)
            {
                AddSectionDivider(t);
                AddDashboardSectionHeader("📥  Backlog", "Tasks without a date", t, isToday: false);
                foreach (var task in backlog)
                    DashboardPanel.Children.Add(CreateDashboardCard(task, task.Date, t, refreshAll: true));
            }
        }

        private List<TaskItem> GetBacklogTasks()
        {
            // "Backlog" = non-recurring tasks with a date far in the past (before today-7)
            // or any task explicitly set to the epoch sentinel date
            var cutoff = DateTime.Today.AddDays(-365);   // arbitrary "old enough" threshold
            return _taskService.LoadTasks()
                .Where(t => t.ProfileId == _profileId
                         && t.Recurrence == RecurrenceType.None
                         && t.Date.Date < DateTime.Today.AddDays(-7)
                         && !t.IsCompleted)
                .OrderBy(t => t.Date)
                .ToList();
        }

        private void AddDashboardSectionHeader(string title, string subtitle, AppTheme t, bool isToday)
        {
            var border = new Border
            {
                Background    = isToday ? t.AccentColor : t.ButtonBackground,
                CornerRadius  = new CornerRadius(10),
                Padding       = new Thickness(14, 10, 14, 10),
                Margin        = new Thickness(0, 0, 0, 8)
            };

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text       = title,
                FontSize   = 16,
                FontWeight = FontWeights.Bold,
                Foreground = isToday ? Brushes.White : t.PrimaryText
            });
            stack.Children.Add(new TextBlock
            {
                Text       = subtitle,
                FontSize   = 12,
                Foreground = isToday
                    ? new SolidColorBrush(Color.FromArgb(200, 255, 255, 255))
                    : t.SecondaryText,
                Margin = new Thickness(0, 2, 0, 0)
            });

            border.Child = stack;
            DashboardPanel.Children.Add(border);
        }

        private void AddSectionDivider(AppTheme t)
        {
            DashboardPanel.Children.Add(new Border
            {
                Height     = 1,
                Background = t.CalendarCellBorder,
                Margin     = new Thickness(0, 16, 0, 16)
            });
        }

        private void AddEmptyState(string message, AppTheme t)
        {
            DashboardPanel.Children.Add(new TextBlock
            {
                Text       = message,
                FontSize   = 13,
                Foreground = t.SecondaryText,
                Margin     = new Thickness(4, 4, 0, 4),
                FontStyle  = FontStyles.Italic
            });
        }

        private Border CreateDashboardCard(TaskItem task, DateTime date, AppTheme t, bool refreshAll)
        {
            bool isRecurring    = task.Recurrence != RecurrenceType.None;
            DateTime occurrence = date;

            var card = new Border
            {
                Background    = t.TaskCardBackground,
                CornerRadius  = new CornerRadius(8),
                Padding       = new Thickness(12, 10, 12, 10),
                Margin        = new Thickness(0, 0, 0, 6)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkbox = new CheckBox
            {
                IsChecked         = task.IsCompleted,
                VerticalAlignment = VerticalAlignment.Center,
                Margin            = new Thickness(0, 0, 10, 0)
            };
            checkbox.Checked += (s, e) =>
            {
                if (isRecurring) _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrence, true);
                else { task.IsCompleted = true; _taskService.UpdateTask(task); }
                if (refreshAll) BuildDashboard();
            };
            checkbox.Unchecked += (s, e) =>
            {
                if (isRecurring) _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrence, false);
                else { task.IsCompleted = false; _taskService.UpdateTask(task); }
                if (refreshAll) BuildDashboard();
            };

            var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var title = new TextBlock
            {
                Text            = task.Title,
                FontSize        = 13,
                Foreground      = task.IsCompleted ? t.CompletedText : t.PrimaryText,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping    = TextWrapping.Wrap
            };
            textStack.Children.Add(title);

            if (!string.IsNullOrWhiteSpace(task.Notes))
                textStack.Children.Add(new TextBlock
                {
                    Text         = task.Notes,
                    FontSize     = 11,
                    Foreground   = t.SecondaryText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 3, 0, 0)
                });

            if (isRecurring)
            {
                string badge = task.Recurrence switch
                {
                    RecurrenceType.Daily   => "🔁 Daily",
                    RecurrenceType.Weekly  => "📅 Weekly",
                    RecurrenceType.Monthly => "🗓 Monthly",
                    _                      => ""
                };
                textStack.Children.Add(new TextBlock
                {
                    Text       = badge,
                    FontSize   = 10,
                    Foreground = t.AccentColor,
                    Margin     = new Thickness(0, 3, 0, 0)
                });
            }

            var deleteBtn = new Button
            {
                Content           = "✕",
                FontSize          = 11,
                Width             = 22, Height = 22,
                Background        = Brushes.Transparent,
                Foreground        = t.SecondaryText,
                BorderThickness   = new Thickness(0),
                Cursor            = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center
            };
            deleteBtn.Click += (s, e) =>
            {
                OnDeleteTask(task, isRecurring, occurrence);
                BuildDashboard();
            };

            Grid.SetColumn(checkbox,  0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(deleteBtn, 2);
            grid.Children.Add(checkbox);
            grid.Children.Add(textStack);
            grid.Children.Add(deleteBtn);

            card.Child = grid;

            // Highlight selected card
            bool isSelected = _selectedDashboardTask?.Id == task.Id
                              && _selectedDashboardDate.Date == date.Date;
            card.BorderThickness = new Thickness(1.5);
            card.BorderBrush     = isSelected ? t.AccentColor : Brushes.Transparent;
            card.Cursor          = Cursors.Hand;

            // Click → show detail on the right
            card.MouseLeftButtonUp += (s, e) =>
            {
                _selectedDashboardTask = task;
                _selectedDashboardDate = date;
                ShowTaskDetail(task, date, ThemeService.GetTheme(_currentTheme));
                BuildDashboard();
            };

            return card;
        }

        private void ShowTaskDetail(TaskItem task, DateTime date, AppTheme t)
        {
            DetailPanel.Background  = t.TaskPanelBackground;
            DetailTitle.Text        = task.Title;
            DetailTitle.Foreground  = t.PrimaryText;

            string dateStr = date.ToString("dddd, MMMM d, yyyy");
            if (task.Recurrence != RecurrenceType.None)
            {
                string badge = task.Recurrence switch
                {
                    RecurrenceType.Daily   => "🔁 Daily",
                    RecurrenceType.Weekly  => "📅 Weekly",
                    RecurrenceType.Monthly => "🗓 Monthly",
                    _                      => ""
                };
                dateStr += $"  ·  {badge}";
                if (task.RecurrenceEndDate.HasValue)
                    dateStr += $"  ·  ends {task.RecurrenceEndDate.Value:MMM d, yyyy}";
            }
            DetailDate.Text         = dateStr;
            DetailDate.Foreground   = t.SecondaryText;
            DetailDateBadge.Background = t.ButtonBackground;

            DetailDivider.Background = t.CalendarCellBorder;

            bool hasNotes           = !string.IsNullOrWhiteSpace(task.Notes);
            DetailNotes.Text        = hasNotes ? task.Notes : "No description.";
            DetailNotes.Foreground  = hasNotes ? t.PrimaryText : t.SecondaryText;
            DetailNotes.FontStyle   = hasNotes ? FontStyles.Normal : FontStyles.Italic;

            _detailPanelOpen = true;
            DetailPanel.Visibility = Visibility.Visible;
        }

        // ─── Recurrence Buttons ───────────────────────────────────────────

        private void RecurrenceBtn_Click(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            string tag = border.Tag?.ToString() ?? "None";

            _selectedRecurrence = tag switch
            {
                "Daily"   => RecurrenceType.Daily,
                "Weekly"  => RecurrenceType.Weekly,
                "Monthly" => RecurrenceType.Monthly,
                _         => RecurrenceType.None
            };

            _recurrenceEndDate = null;
            EndDateBtnText.Text = "Pick a date";
            _isPickingEndDate   = false;
            EndDatePickBanner.Visibility = Visibility.Collapsed;
            EndsAtRow.Visibility = _selectedRecurrence != RecurrenceType.None
                ? Visibility.Visible : Visibility.Collapsed;

            UpdateRecurrenceBtnTheme(ThemeService.GetTheme(_currentTheme));
        }

        private void UpdateRecurrenceBtnTheme(AppTheme t)
        {
            var buttons = new[]
            {
                (BtnNone,    RecurrenceType.None),
                (BtnDaily,   RecurrenceType.Daily),
                (BtnWeekly,  RecurrenceType.Weekly),
                (BtnMonthly, RecurrenceType.Monthly)
            };

            foreach (var (btn, type) in buttons)
            {
                bool isActive   = _selectedRecurrence == type;
                btn.Background  = isActive ? t.AccentColor : t.ButtonBackground;
                if (btn.Child is TextBlock tb)
                    tb.Foreground = isActive ? Brushes.White : t.SecondaryText;
            }
        }

        // ─── End Date Picking ─────────────────────────────────────────────

        private void EndDateBtn_Click(object sender, MouseButtonEventArgs e)
        {
            _isPickingEndDate = true;
            EndDatePickBanner.Visibility = Visibility.Visible;
            BuildCalendar();
        }

        private void CancelEndDateBtn_Click(object sender, RoutedEventArgs e)
        {
            _isPickingEndDate = false;
            EndDatePickBanner.Visibility = Visibility.Collapsed;
            BuildCalendar();
        }

        private void ResetRecurrenceUI()
        {
            _selectedRecurrence = RecurrenceType.None;
            _recurrenceEndDate  = null;
            _isPickingEndDate   = false;
            EndsAtRow.Visibility         = Visibility.Collapsed;
            EndDatePickBanner.Visibility = Visibility.Collapsed;
            EndDateBtnText.Text          = "Pick a date";
            UpdateRecurrenceBtnTheme(ThemeService.GetTheme(_currentTheme));
        }

        // ─── Calendar Building ───────────────────────────────────────────

        private void BuildCalendar()
        {
            if (_isMonthView) BuildMonthView();
            else              BuildWeekView();
        }

        private void BuildMonthView()
        {
            var t = ThemeService.GetTheme(_currentTheme);
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy");

            var datesWithTasks = _taskService.GetDatesWithTasks(
                _profileId, _currentDate.Year, _currentDate.Month);

            DayHeadersGrid.Children.Clear();
            DayHeadersGrid.ColumnDefinitions.Clear();
            string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                DayHeadersGrid.ColumnDefinitions.Add(new ColumnDefinition());
                Grid.SetColumn(new TextBlock
                {
                    Text                = days[i], FontSize = 12,
                    Foreground          = t.SecondaryText,
                    HorizontalAlignment = HorizontalAlignment.Center
                }.Also(tb => { Grid.SetColumn(tb, i); DayHeadersGrid.Children.Add(tb); }), i);
            }

            CalendarGrid.Children.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            CalendarGrid.RowDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());

            DateTime firstDay   = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int startColumn     = (int)firstDay.DayOfWeek;
            int daysInMonth     = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            int rows            = (int)Math.Ceiling((startColumn + daysInMonth) / 7.0);

            for (int i = 0; i < rows; i++)
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date     = new DateTime(_currentDate.Year, _currentDate.Month, day);
                int cellIndex     = startColumn + day - 1;
                var cell          = CreateDayCell(date, datesWithTasks, t);
                Grid.SetRow(cell, cellIndex / 7);
                Grid.SetColumn(cell, cellIndex % 7);
                CalendarGrid.Children.Add(cell);
            }
        }

        private void BuildWeekView()
        {
            var t             = ThemeService.GetTheme(_currentTheme);
            DateTime start    = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
            DateTime end      = start.AddDays(6);
            MonthYearText.Text = $"{start:MMM d} – {end:MMM d, yyyy}";

            var datesWithTasks = _taskService.GetDatesWithTasks(
                _profileId, _currentDate.Year, _currentDate.Month);

            DayHeadersGrid.Children.Clear();
            DayHeadersGrid.ColumnDefinitions.Clear();
            string[] days = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };
            for (int i = 0; i < 7; i++)
            {
                DayHeadersGrid.ColumnDefinitions.Add(new ColumnDefinition());
                var header = new TextBlock
                {
                    Text                = days[i], FontSize = 12,
                    Foreground          = t.SecondaryText,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            CalendarGrid.Children.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            CalendarGrid.RowDefinitions.Clear();
            for (int i = 0; i < 7; i++) CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());
            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });

            for (int i = 0; i < 7; i++)
            {
                var date = start.AddDays(i);
                var cell = CreateDayCell(date, datesWithTasks, t);
                Grid.SetRow(cell, 0); Grid.SetColumn(cell, i);
                CalendarGrid.Children.Add(cell);
            }
        }

        private Border CreateDayCell(DateTime date, List<DateTime> datesWithTasks, AppTheme t)
        {
            bool isToday        = date.Date == DateTime.Today;
            bool isSelected     = date.Date == _selectedDate.Date;
            bool hasTasks       = datesWithTasks.Contains(date.Date);
            bool isChosenEndDate = _recurrenceEndDate.HasValue && date.Date == _recurrenceEndDate.Value.Date;

            var cell = new Border
            {
                Margin          = new Thickness(2),
                CornerRadius    = new CornerRadius(8),
                Cursor          = Cursors.Hand,
                Background      = isChosenEndDate ? t.AccentColor
                                : isSelected      ? t.CalendarCellSelected
                                : isToday         ? t.CalendarCellToday
                                                  : t.CalendarCellBackground,
                BorderBrush     = isToday ? t.AccentColor : t.CalendarCellBorder,
                BorderThickness = new Thickness(isToday ? 1.5 : 0.5)
            };

            var stack = new StackPanel
            {
                VerticalAlignment   = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin              = new Thickness(4)
            };

            stack.Children.Add(new TextBlock
            {
                Text                = date.Day.ToString(),
                FontSize            = 14,
                FontWeight          = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground          = (isSelected || isChosenEndDate) ? Brushes.White : t.PrimaryText,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            if (hasTasks)
                stack.Children.Add(new Ellipse
                {
                    Width               = 6, Height = 6,
                    Fill                = (isSelected || isChosenEndDate) ? Brushes.White : t.AccentColor,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin              = new Thickness(0, 4, 0, 0)
                });

            cell.Child = stack;
            cell.MouseLeftButtonUp += (s, e) =>
            {
                if (_isPickingEndDate)
                {
                    _recurrenceEndDate           = date;
                    EndDateBtnText.Text          = date.ToString("MMM d, yyyy");
                    _isPickingEndDate            = false;
                    EndDatePickBanner.Visibility = Visibility.Collapsed;
                    BuildCalendar();
                }
                else
                {
                    _selectedDate = date;
                    BuildCalendar();
                    OpenTaskPanel(date);
                }
            };

            return cell;
        }

        // ─── Task Panel ──────────────────────────────────────────────────

        private void OpenTaskPanel(DateTime date)
        {
            SelectedDateText.Text = date.ToString("dddd, MMMM d");
            TaskTitleInput.Text   = "";
            TaskNotesInput.Text   = "";
            ResetRecurrenceUI();
            LoadTasksForDate(date);

            if (!_taskPanelOpen)
            {
                _taskPanelOpen = true;
                var anim = new GridLengthAnimation
                {
                    From     = new GridLength(0),
                    To       = new GridLength(280),
                    Duration = new Duration(TimeSpan.FromSeconds(0.3))
                };
                TaskPanelColumn.BeginAnimation(ColumnDefinition.WidthProperty, anim);
            }
        }

        private void LoadTasksForDate(DateTime date)
        {
            var t = ThemeService.GetTheme(_currentTheme);
            TasksList.Children.Clear();
            foreach (var task in _taskService.GetTasksForDate(_profileId, date))
                TasksList.Children.Add(CreateTaskCard(task, t));
        }

        private Border CreateTaskCard(TaskItem task, AppTheme t)
        {
            bool isRecurring    = task.Recurrence != RecurrenceType.None;
            DateTime occurrence = task.Date;

            var card = new Border
            {
                Background   = t.TaskCardBackground,
                CornerRadius = new CornerRadius(8),
                Padding      = new Thickness(10),
                Margin       = new Thickness(0, 0, 0, 8)
            };

            var outerStack = new StackPanel();
            var topGrid    = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var checkbox = new CheckBox
            {
                IsChecked         = task.IsCompleted,
                VerticalAlignment = VerticalAlignment.Top,
                Margin            = new Thickness(0, 2, 8, 0)
            };
            checkbox.Checked += (s, e) =>
            {
                if (isRecurring) _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrence, true);
                else { task.IsCompleted = true; _taskService.UpdateTask(task); }
                LoadTasksForDate(_selectedDate);
            };
            checkbox.Unchecked += (s, e) =>
            {
                if (isRecurring) _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrence, false);
                else { task.IsCompleted = false; _taskService.UpdateTask(task); }
                LoadTasksForDate(_selectedDate);
            };

            var textStack = new StackPanel();
            textStack.Children.Add(new TextBlock
            {
                Text            = task.Title,
                FontSize        = 13,
                Foreground      = task.IsCompleted ? t.CompletedText : t.PrimaryText,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping    = TextWrapping.Wrap
            });

            if (!string.IsNullOrWhiteSpace(task.Notes))
                textStack.Children.Add(new TextBlock
                {
                    Text         = task.Notes,
                    FontSize     = 11,
                    Foreground   = t.SecondaryText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin       = new Thickness(0, 4, 0, 0)
                });

            if (isRecurring)
            {
                string badge = task.Recurrence switch
                {
                    RecurrenceType.Daily   => "🔁 Daily",
                    RecurrenceType.Weekly  => "📅 Weekly",
                    RecurrenceType.Monthly => "🗓 Monthly",
                    _                      => ""
                };
                textStack.Children.Add(new TextBlock
                    { Text = badge, FontSize = 10, Foreground = t.AccentColor, Margin = new Thickness(0, 4, 0, 0) });

                if (task.RecurrenceEndDate.HasValue)
                    textStack.Children.Add(new TextBlock
                    {
                        Text       = $"Ends {task.RecurrenceEndDate.Value:MMM d, yyyy}",
                        FontSize   = 10,
                        Foreground = t.SecondaryText,
                        Margin     = new Thickness(0, 2, 0, 0)
                    });
            }

            var deleteBtn = new Button
            {
                Content           = "✕", FontSize = 11,
                Width             = 24, Height = 24,
                Background        = Brushes.Transparent,
                Foreground        = t.SecondaryText,
                BorderThickness   = new Thickness(0),
                Cursor            = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top
            };
            deleteBtn.Click += (s, e) => OnDeleteTask(task, isRecurring, occurrence);

            Grid.SetColumn(checkbox,  0); Grid.SetColumn(textStack, 1); Grid.SetColumn(deleteBtn, 2);
            topGrid.Children.Add(checkbox); topGrid.Children.Add(textStack); topGrid.Children.Add(deleteBtn);
            outerStack.Children.Add(topGrid);
            card.Child = outerStack;
            return card;
        }

        private void OnDeleteTask(TaskItem task, bool isRecurring, DateTime occurrenceDate)
        {
            if (!isRecurring)
            {
                _taskService.DeleteTask(task.Id);
                if (_taskPanelOpen) LoadTasksForDate(_selectedDate);
                if (_isTasksView) BuildDashboard(); else BuildCalendar();
                return;
            }

            var dialog = new Window
            {
                Title = "Delete Recurring Task", Width = 340, Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this, WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#1F1F1F")!
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = "Delete recurring task:", FontSize = 13, FontWeight = FontWeights.Bold,
                Foreground = Brushes.White, Margin = new Thickness(0, 0, 0, 16)
            });

            var onlyBtn = new Button
            {
                Content = "Only this occurrence", Height = 34,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#2A2A2A")!,
                Foreground = Brushes.White, BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 8), Cursor = Cursors.Hand
            };
            onlyBtn.Click += (s, e) =>
            {
                _taskService.DeleteRecurringOccurrence(task.Id, occurrenceDate);
                if (_taskPanelOpen) LoadTasksForDate(_selectedDate);
                if (_isTasksView) BuildDashboard(); else BuildCalendar();
                dialog.Close();
            };

            var futureBtn = new Button
            {
                Content = "This and all future occurrences", Height = 34,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!,
                Foreground = Brushes.White, BorderThickness = new Thickness(0), Cursor = Cursors.Hand
            };
            futureBtn.Click += (s, e) =>
            {
                _taskService.DeleteRecurringFromDate(task.Id, occurrenceDate);
                if (_taskPanelOpen) LoadTasksForDate(_selectedDate);
                if (_isTasksView) BuildDashboard(); else BuildCalendar();
                dialog.Close();
            };

            panel.Children.Add(onlyBtn);
            panel.Children.Add(futureBtn);
            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void AddTaskBtn_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TaskTitleInput.Text))
            {
                MessageBox.Show("Please enter a task title!", "Oops");
                return;
            }

            _taskService.AddTask(new TaskItem
            {
                ProfileId         = _profileId,
                Title             = TaskTitleInput.Text.Trim(),
                Notes             = TaskNotesInput.Text.Trim(),
                Date              = _selectedDate,
                Recurrence        = _selectedRecurrence,
                RecurrenceEndDate = _recurrenceEndDate
            });

            TaskTitleInput.Text = "";
            TaskNotesInput.Text = "";
            ResetRecurrenceUI();
            LoadTasksForDate(_selectedDate);
            BuildCalendar();
        }

        // ─── Navigation ──────────────────────────────────────────────────

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _isMonthView ? _currentDate.AddMonths(-1) : _currentDate.AddDays(-7);
            BuildCalendar();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _isMonthView ? _currentDate.AddMonths(1) : _currentDate.AddDays(7);
            BuildCalendar();
        }

        private void MonthViewBtn_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = true;
            var t = ThemeService.GetTheme(_currentTheme);
            MonthViewBtn.Background = t.AccentColor;             MonthViewBtn.Foreground = Brushes.White;
            WeekViewBtn.Background  = t.TopBarButtonBackground; WeekViewBtn.Foreground  = t.TopBarButtonText;
            BuildCalendar();
        }

        private void WeekViewBtn_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = false;
            var t = ThemeService.GetTheme(_currentTheme);
            WeekViewBtn.Background  = t.AccentColor;             WeekViewBtn.Foreground  = Brushes.White;
            MonthViewBtn.Background = t.TopBarButtonBackground; MonthViewBtn.Foreground = t.TopBarButtonText;
            BuildCalendar();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            new ProfileWindow().Show();
            this.Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) Application.Current.Shutdown();
        }
    }

    // tiny helper to avoid temp variables when wiring up headers
    internal static class Extensions
    {
        public static T Also<T>(this T obj, Action<T> action) { action(obj); return obj; }
    }
}
