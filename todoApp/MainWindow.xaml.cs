using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace todoApp
{
    public partial class MainWindow : Window
    {
        private TaskService _taskService = new TaskService();
        private string _profileId;
        private string _profileName;
        private string _profileColor;
        private DateTime _currentDate = DateTime.Today;
        private DateTime _selectedDate = DateTime.Today;
        private bool _isMonthView = true;
        private bool _taskPanelOpen = false;

        public MainWindow(string profileId, string profileName, string profileColor)
        {
            InitializeComponent();
            _profileId = profileId;
            _profileName = profileName;
            _profileColor = profileColor;
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Fade in
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            // Set profile info
            ProfileNameText.Text = _profileName;
            ProfileInitialsText.Text = GetInitials(_profileName);
            ProfileCircle.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(_profileColor)!;

            // Build calendar
            BuildCalendar();
        }

        private string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
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
            MonthYearText.Text = _currentDate.ToString("MMMM yyyy");

            var datesWithTasks = _taskService.GetDatesWithTasks(
                _profileId, _currentDate.Year, _currentDate.Month);

            // Day headers
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
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            // Calendar grid
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

                var cell = CreateDayCell(date, datesWithTasks);
                Grid.SetRow(cell, row);
                Grid.SetColumn(cell, col);
                CalendarGrid.Children.Add(cell);
            }
        }

        private void BuildWeekView()
        {
            DateTime startOfWeek = _currentDate.AddDays(-(int)_currentDate.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);
            MonthYearText.Text = $"{startOfWeek:MMM d} – {endOfWeek:MMM d, yyyy}";

            var datesWithTasks = _taskService.GetDatesWithTasks(
                _profileId, _currentDate.Year, _currentDate.Month);

            // Day headers
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
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(header, i);
                DayHeadersGrid.Children.Add(header);
            }

            // Week grid
            CalendarGrid.Children.Clear();
            CalendarGrid.ColumnDefinitions.Clear();
            CalendarGrid.RowDefinitions.Clear();

            for (int i = 0; i < 7; i++)
                CalendarGrid.ColumnDefinitions.Add(new ColumnDefinition());

            CalendarGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(120) });

            for (int i = 0; i < 7; i++)
            {
                DateTime date = startOfWeek.AddDays(i);
                var cell = CreateDayCell(date, datesWithTasks);
                Grid.SetRow(cell, 0);
                Grid.SetColumn(cell, i);
                CalendarGrid.Children.Add(cell);
            }
        }

        private Border CreateDayCell(DateTime date, List<DateTime> datesWithTasks)
        {
            bool isToday = date.Date == DateTime.Today;
            bool isSelected = date.Date == _selectedDate.Date;
            bool hasTasks = datesWithTasks.Contains(date.Date);

            var cell = new Border
            {
                Margin = new Thickness(2),
                CornerRadius = new CornerRadius(8),
                Cursor = Cursors.Hand,
                Background = isSelected
                    ? (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!
                    : isToday
                        ? (SolidColorBrush)new BrushConverter().ConvertFrom("#2A2A2A")!
                        : (SolidColorBrush)new BrushConverter().ConvertFrom("#1A1A1A")!,
                BorderBrush = isToday
                    ? (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!
                    : (SolidColorBrush)new BrushConverter().ConvertFrom("#333333")!,
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
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(dayText);

            if (hasTasks)
            {
                var dot = new Ellipse
                {
                    Width = 6,
                    Height = 6,
                    Fill = isSelected ? Brushes.White :
                        (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                stack.Children.Add(dot);
            }

            cell.Child = stack;

            cell.MouseLeftButtonUp += (s, e) =>
            {
                _selectedDate = date;
                BuildCalendar();
                OpenTaskPanel(date);
            };

            return cell;
        }

        // ─── Task Panel ──────────────────────────────────────────────────

        private void OpenTaskPanel(DateTime date)
        {
            SelectedDateText.Text = date.ToString("dddd, MMMM d");
            TaskTitleInput.Text = "";
            TaskNotesInput.Text = "";
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
            TasksList.Children.Clear();
            var tasks = _taskService.GetTasksForDate(_profileId, date);

            foreach (var task in tasks)
            {
                var taskCard = CreateTaskCard(task);
                TasksList.Children.Add(taskCard);
            }
        }

        private Border CreateTaskCard(TaskItem task)
        {
            var card = new Border
            {
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#2A2A2A")!,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Checkbox
            var checkbox = new CheckBox
            {
                IsChecked = task.IsCompleted,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 8, 0)
            };
            checkbox.Checked += (s, e) =>
            {
                task.IsCompleted = true;
                _taskService.UpdateTask(task);
                LoadTasksForDate(_selectedDate);
            };
            checkbox.Unchecked += (s, e) =>
            {
                task.IsCompleted = false;
                _taskService.UpdateTask(task);
                LoadTasksForDate(_selectedDate);
            };

            // Text
            var textStack = new StackPanel();
            var titleText = new TextBlock
            {
                Text = task.Title,
                FontSize = 13,
                Foreground = task.IsCompleted
                    ? (SolidColorBrush)new BrushConverter().ConvertFrom("#666666")!
                    : Brushes.White,
                TextDecorations = task.IsCompleted ? TextDecorations.Strikethrough : null,
                TextWrapping = TextWrapping.Wrap
            };
            textStack.Children.Add(titleText);

            if (!string.IsNullOrWhiteSpace(task.Notes))
            {
                var notesText = new TextBlock
                {
                    Text = task.Notes,
                    FontSize = 11,
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#888888")!,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 4, 0, 0)
                };
                textStack.Children.Add(notesText);
            }

            // Delete button
            var deleteBtn = new Button
            {
                Content = "✕",
                FontSize = 11,
                Width = 24,
                Height = 24,
                Background = Brushes.Transparent,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#666666")!,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Top
            };
            deleteBtn.Click += (s, e) =>
            {
                _taskService.DeleteTask(task.Id);
                LoadTasksForDate(_selectedDate);
                BuildCalendar();
            };

            Grid.SetColumn(checkbox, 0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(deleteBtn, 2);

            grid.Children.Add(checkbox);
            grid.Children.Add(textStack);
            grid.Children.Add(deleteBtn);

            card.Child = grid;
            return card;
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
                Date = _selectedDate
            };

            _taskService.AddTask(task);
            TaskTitleInput.Text = "";
            TaskNotesInput.Text = "";
            LoadTasksForDate(_selectedDate);
            BuildCalendar();
        }

        // ─── Navigation ──────────────────────────────────────────────────

        private void PrevBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _isMonthView
                ? _currentDate.AddMonths(-1)
                : _currentDate.AddDays(-7);
            BuildCalendar();
        }

        private void NextBtn_Click(object sender, RoutedEventArgs e)
        {
            _currentDate = _isMonthView
                ? _currentDate.AddMonths(1)
                : _currentDate.AddDays(7);
            BuildCalendar();
        }

        private void MonthViewBtn_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = true;
            MonthViewBtn.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!;
            MonthViewBtn.Foreground = Brushes.White;
            WeekViewBtn.Background = Brushes.Transparent;
            WeekViewBtn.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!;
            BuildCalendar();
        }

        private void WeekViewBtn_Click(object sender, RoutedEventArgs e)
        {
            _isMonthView = false;
            WeekViewBtn.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!;
            WeekViewBtn.Foreground = Brushes.White;
            MonthViewBtn.Background = Brushes.Transparent;
            MonthViewBtn.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!;
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
            var result = MessageBox.Show(
                "Are you sure you want to exit?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}