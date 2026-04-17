using System;
using System.Collections.Generic;
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

        // Recurrence state
        private RecurrenceType _selectedRecurrence = RecurrenceType.None;
        private DateTime? _recurrenceEndDate = null;
        private bool _isPickingEndDate = false;

        public MainWindow(string profileId, string profileName, string profileColor, string theme)
        {
            InitializeComponent();
            TopBar.MouseLeftButtonDown += TopBar_MouseLeftButtonDown;
            _profileId = profileId;
            _profileName = profileName;
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

            ProfileNameText.Text = _profileName;
            ProfileInitialsText.Text = GetInitials(_profileName);
            ProfileCircle.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(_profileColor)!;

            ProfileCircle.MouseLeftButtonUp += ProfileCircle_Clicked;
            ProfileNameText.MouseLeftButtonUp += ProfileCircle_Clicked;
            ProfileCircle.Cursor = Cursors.Hand;
            ProfileNameText.Cursor = Cursors.Hand;

            ApplyTheme(_currentTheme);
        }

        private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Don't drag when the user clicked the profile circle or name —
            // those have their own MouseLeftButtonUp handler for the settings popup.
            // DragMove() captures the mouse and prevents Up events from firing.
            if (e.OriginalSource is FrameworkElement src)
            {
                FrameworkElement? hit = src;
                while (hit != null)
                {
                    if (hit == ProfileCircle || hit == ProfileNameText)
                        return;
                    hit = hit.Parent as FrameworkElement;
                }
            }

            if (e.ButtonState == MouseButtonState.Pressed)
                this.DragMove();
        }

        private string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
        }

        // ─── Settings Popup ──────────────────────────────────────────────

        private void ProfileCircle_Clicked(object sender, MouseButtonEventArgs e)
        {
            var settings = new SettingsPopup(_currentProfile, this);
            settings.ThemeChanged += (newTheme) => ApplyTheme(newTheme);
            settings.ProfileDeleted += () =>
            {
                var profileWindow = new ProfileWindow();
                profileWindow.Show();
                this.Close();
            };
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
                RootBorder.Background = t.WindowBackground;
                TopBar.Background = t.TopBarBackground;
                ProfileNameText.Foreground = t.PrimaryText;
                MonthYearText.Foreground = t.PrimaryText;
                SelectedDateText.Foreground = t.PrimaryText;
                PrevBtn.Foreground = t.PrimaryText;
                PrevBtn.BorderBrush = t.ButtonBorder;
                NextBtn.Foreground = t.PrimaryText;
                NextBtn.BorderBrush = t.ButtonBorder;
                MonthViewBtn.Background = _isMonthView ? t.AccentColor : Brushes.Transparent;
                MonthViewBtn.Foreground = _isMonthView ? Brushes.White : t.SecondaryText;
                WeekViewBtn.Background = !_isMonthView ? t.AccentColor : Brushes.Transparent;
                WeekViewBtn.Foreground = !_isMonthView ? Brushes.White : t.SecondaryText;
                TaskPanel.Background = t.TaskPanelBackground;
                TaskTitleInput.Background = t.InputBackground;
                TaskTitleInput.Foreground = t.PrimaryText;
                TaskTitleInput.BorderBrush = t.InputBorder;
                TaskNotesInput.Background = t.InputBackground;
                TaskNotesInput.Foreground = t.PrimaryText;
                TaskNotesInput.BorderBrush = t.InputBorder;
                AddTaskBtn.Background = t.AccentColor;

                UpdateRecurrenceBtnTheme(t);

                BuildCalendar();
                if (_taskPanelOpen)
                    LoadTasksForDate(_selectedDate);

                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
                this.BeginAnimation(Window.OpacityProperty, fadeIn);
            };
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        // ─── Recurrence Buttons ───────────────────────────────────────────

        private void RecurrenceBtn_Click(object sender, MouseButtonEventArgs e)
        {
            var border = (Border)sender;
            string tag = border.Tag?.ToString() ?? "None";

            _selectedRecurrence = tag switch
            {
                "Daily" => RecurrenceType.Daily,
                "Weekly" => RecurrenceType.Weekly,
                "Monthly" => RecurrenceType.Monthly,
                _ => RecurrenceType.None
            };

            // Reset end date when switching
            _recurrenceEndDate = null;
            EndDateBtnText.Text = "Pick a date";

            // Cancel any ongoing end-date picking
            _isPickingEndDate = false;
            EndDatePickBanner.Visibility = Visibility.Collapsed;

            // Show/hide ends at row
            EndsAtRow.Visibility = _selectedRecurrence != RecurrenceType.None
                ? Visibility.Visible
                : Visibility.Collapsed;

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
                bool isActive = _selectedRecurrence == type;
                btn.Background = isActive ? t.AccentColor : t.ButtonBackground;
                if (btn.Child is TextBlock tb)
                    tb.Foreground = isActive ? Brushes.White : t.SecondaryText;
            }
        }

        // ─── End Date Picking (uses main calendar) ────────────────────────

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
            _recurrenceEndDate = null;
            _isPickingEndDate = false;
            EndsAtRow.Visibility = Visibility.Collapsed;
            EndDatePickBanner.Visibility = Visibility.Collapsed;
            EndDateBtnText.Text = "Pick a date";
            UpdateRecurrenceBtnTheme(ThemeService.GetTheme(_currentTheme));
        }

        // ─── Calendar Building ───────────────────────────────────────────

        private void BuildCalendar()
        {
            if (_isMonthView)
                BuildMonthView();
            else
                BuildWeekView();
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
                var header = new TextBlock
                {
                    Text = days[i],
                    FontSize = 12,
                    Foreground = t.SecondaryText,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            CalendarGrid.Children.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            CalendarGrid.RowDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());

            DateTime firstDay = new DateTime(_currentDate.Year, _currentDate.Month, 1);
            int startColumn = (int)firstDay.DayOfWeek;
            int daysInMonth = DateTime.DaysInMonth(_currentDate.Year, _currentDate.Month);
            int totalCells = startColumn + daysInMonth;
            int rows = (int)Math.Ceiling(totalCells / 7.0);

            for (int i = 0; i < rows; i++)
                CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(70) });

            for (int day = 1; day <= daysInMonth; day++)
            {
                DateTime date = new DateTime(_currentDate.Year, _currentDate.Month, day);
                int cellIndex = startColumn + day - 1;
                int row = cellIndex / 7;
                int col = cellIndex % 7;

                var cell = CreateDayCell(date, datesWithTasks, t);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                CalendarGrid.Children.Add(cell);
            }
        }

        private void BuildWeekView()
        {
            var t = ThemeService.GetTheme(_currentTheme);
            DateTime startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);
            MonthYearText.Text = $"{startOfWeek:MMM d} – {endOfWeek:MMM d, yyyy}";

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
                    Text = days[i],
                    FontSize = 12,
                    Foreground = t.SecondaryText,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            CalendarGrid.Children.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            CalendarGrid.RowDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });

            for (int i = 0; i < 7; i++)
            {
                DateTime date = startOfWeek.AddDays(i);
                var cell = CreateDayCell(date, datesWithTasks, t);
                Grid.SetRow(cell, 0);
                Grid.SetColumn(cell, i);
                CalendarGrid.Children.Add(cell);
            }
        }

        private Border CreateDayCell(DateTime date, List<DateTime> datesWithTasks, AppTheme t)
        {
            bool isToday = date.Date == DateTime.Today;
            bool isSelected = date.Date == _selectedDate.Date;
            bool hasTasks = datesWithTasks.Contains(date.Date);
            bool isChosenEndDate = _recurrenceEndDate.HasValue && date.Date == _recurrenceEndDate.Value.Date;

            var cell = new Border
            {
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                Background = isChosenEndDate ? t.AccentColor
                           : isSelected ? t.CalendarCellSelected
                           : isToday ? t.CalendarCellToday
                                        : t.CalendarCellBackground,
                BorderBrush = isToday ? t.AccentColor : t.CalendarCellBorder,
                BorderThickness = new Thickness(isToday ? 1.5 : 0.5)
            };

            var stack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(4)
            };

            var dayText = new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 14,
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (isSelected || isChosenEndDate) ? Brushes.White : t.PrimaryText,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(dayText);

            if (hasTasks)
            {
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = (isSelected || isChosenEndDate) ? Brushes.White : t.AccentColor,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stack.Children.Add(dot);
            }

            cell.Child = stack;

            cell.MouseLeftButtonUp += (s, e) =>
            {
                if (_isPickingEndDate)
                {
                    // End-date picking mode: set the end date and exit the mode
                    _recurrenceEndDate = date;
                    EndDateBtnText.Text = date.ToString("MMM d, yyyy");
                    _isPickingEndDate = false;
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
            TaskTitleInput.Text = "";
            TaskNotesInput.Text = "";
            ResetRecurrenceUI();
            LoadTasksForDate(date);

            if (!_taskPanelOpen)
            {
                _taskPanelOpen = true;
                var anim = new GridLengthAnimation
                {
                    From = new GridLength(0),
                    To = new GridLength(280),
                    Duration = new Duration(TimeSpan.FromSeconds(0.3))
                };
                TaskPanelColumn.BeginAnimation(ColumnDefinition.WidthProperty, anim);
            }
        }

        private void LoadTasksForDate(DateTime date)
        {
            var t = ThemeService.GetTheme(_currentTheme);
            TasksList.Children.Clear();
            var tasks = _taskService.GetTasksForDate(_profileId, date);

            foreach (var task in tasks)
                TasksList.Children.Add(CreateTaskCard(task, t));
        }

        private Border CreateTaskCard(TaskItem task, AppTheme t)
        {
            var card = new Border
            {
                Background = t.TaskCardBackground,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var outerStack = new StackPanel();
            var topGrid = new Grid();
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            bool isRecurring = task.Recurrence != RecurrenceType.None;
            DateTime occurrenceDate = task.Date;

            var checkbox = new CheckBox
            {
                IsChecked = task.IsCompleted,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 8, 0)
            };
            checkbox.Checked += (s, e) =>
            {
                if (isRecurring)
                    _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrenceDate, true);
                else { task.IsCompleted = true; _taskService.UpdateTask(task); }
                LoadTasksForDate(_selectedDate);
            };
            checkbox.Unchecked += (s, e) =>
            {
                if (isRecurring)
                    _taskService.SetRecurringOccurrenceCompleted(task.Id, occurrenceDate, false);
                else { task.IsCompleted = false; _taskService.UpdateTask(task); }
                LoadTasksForDate(_selectedDate);
            };

            var textStack = new StackPanel();
            var titleText = new TextBlock
            {
                Text = task.Title,
                FontSize = 13,
                Foreground = task.IsCompleted ? t.CompletedText : t.PrimaryText,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping = TextWrapping.Wrap
            };
            textStack.Children.Add(titleText);

            if (!string.IsNullOrWhiteSpace(task.Notes))
            {
                textStack.Children.Add(new TextBlock
                {
                    Text = task.Notes,
                    FontSize = 11,
                    Foreground = t.SecondaryText,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                });
            }

            if (isRecurring)
            {
                string badgeText = task.Recurrence switch
                {
                    RecurrenceType.Daily => "🔁 Daily",
                    RecurrenceType.Weekly => "📅 Weekly",
                    RecurrenceType.Monthly => "🗓 Monthly",
                    _ => ""
                };
                textStack.Children.Add(new TextBlock
                {
                    Text = badgeText,
                    FontSize = 10,
                    Foreground = t.AccentColor,
                    Margin = new Thickness(0, 4, 0, 0)
                });

                if (task.RecurrenceEndDate.HasValue)
                {
                    textStack.Children.Add(new TextBlock
                    {
                        Text = $"Ends {task.RecurrenceEndDate.Value:MMM d, yyyy}",
                        FontSize = 10,
                        Foreground = t.SecondaryText,
                        Margin = new Thickness(0, 2, 0, 0)
                    });
                }
            }

            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 11,
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = t.SecondaryText,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top
            };
            deleteBtn.Click += (s, e) => OnDeleteTask(task, isRecurring, occurrenceDate);

            Grid.SetColumn(checkbox, 0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(deleteBtn, 2);
            topGrid.Children.Add(checkbox);
            topGrid.Children.Add(textStack);
            topGrid.Children.Add(deleteBtn);
            outerStack.Children.Add(topGrid);
            card.Child = outerStack;
            return card;
        }

        private void OnDeleteTask(TaskItem task, bool isRecurring, DateTime occurrenceDate)
        {
            if (!isRecurring)
            {
                _taskService.DeleteTask(task.Id);
                LoadTasksForDate(_selectedDate);
                BuildCalendar();
                return;
            }

            var dialog = new Window
            {
                Title = "Delete Recurring Task",
                Width = 340,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                WindowStyle = WindowStyle.ToolWindow,
                ResizeMode = ResizeMode.NoResize,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#1F1F1F")!
            };

            var panel = new StackPanel { Margin = new Thickness(20) };
            panel.Children.Add(new TextBlock
            {
                Text = "Delete recurring task:",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 16)
            });

            var onlyBtn = new Button
            {
                Content = "Only this occurrence",
                Height = 34,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#2A2A2A")!,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Margin = new Thickness(0, 0, 0, 8),
                Cursor = Cursors.Hand
            };
            onlyBtn.Click += (s, e) =>
            {
                _taskService.DeleteRecurringOccurrence(task.Id, occurrenceDate);
                LoadTasksForDate(_selectedDate); BuildCalendar(); dialog.Close();
            };

            var futureBtn = new Button
            {
                Content = "This and all future occurrences",
                Height = 34,
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!,
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            futureBtn.Click += (s, e) =>
            {
                _taskService.DeleteRecurringFromDate(task.Id, occurrenceDate);
                LoadTasksForDate(_selectedDate); BuildCalendar(); dialog.Close();
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

            var task = new TaskItem
            {
                ProfileId = _profileId,
                Title = TaskTitleInput.Text.Trim(),
                Notes = TaskNotesInput.Text.Trim(),
                Date = _selectedDate,
                Recurrence = _selectedRecurrence,
                RecurrenceEndDate = _recurrenceEndDate
            };

            _taskService.AddTask(task);
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
            MonthViewBtn.Background = t.AccentColor; MonthViewBtn.Foreground = Brushes.White;
            WeekViewBtn.Background = Brushes.Transparent; WeekViewBtn.Foreground = t.SecondaryText;
            BuildCalendar();
        }

        private void WeekViewBtn_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = false;
            var t = ThemeService.GetTheme(_currentTheme);
            WeekViewBtn.Background = t.AccentColor; WeekViewBtn.Foreground = Brushes.White;
            MonthViewBtn.Background = Brushes.Transparent; MonthViewBtn.Foreground = t.SecondaryText;
            BuildCalendar();
        }

        private void LogoutBtn_Click(object sender, RoutedEventArgs e)
        {
            var profileWindow = new ProfileWindow();
            profileWindow.Show();
            this.Close();
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to exit?", "Confirm Exit",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
