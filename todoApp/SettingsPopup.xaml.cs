using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace todoApp
{
    public partial class SettingsPopup : Window
    {
        private Profile _profile;
        private ProfileService _profileService = new ProfileService();
        private bool _isDark;

        public event Action<string>? ThemeChanged;
        public event Action? ProfileDeleted;

        public SettingsPopup(Profile profile, Window owner)
        {
            InitializeComponent();
            _profile = profile;
            _isDark = profile.Theme == "Dark";

            Owner = owner;
            Left = owner.Left + 16;
            Top = owner.Top + 55;

            Loaded += SettingsPopup_Loaded;
        }

        private void SettingsPopup_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            ProfileNameText.Text = _profile.Name;
            ProfileInitialsText.Text = GetInitials(_profile.Name);
            ProfileCircle.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(_profile.Color)!;

            ApplyThemeToPopup();
        }

        // ─── Apply theme colors to every element in the popup ────────────

        private void ApplyThemeToPopup()
        {
            var t = ThemeService.GetTheme(_isDark ? "Dark" : "Light");

            // Window border
            RootBorder.Background = t.TopBarBackground;
            RootBorder.BorderBrush = t.ButtonBorder;

            // Header
            HeaderText.Foreground = t.PrimaryText;
            CloseBtn.Foreground = t.SecondaryText;

            // Dividers
            Divider1.Background = t.ButtonBorder;
            Divider2.Background = t.ButtonBorder;
            Divider3.Background = t.ButtonBorder;

            // Profile name
            ProfileNameText.Foreground = t.PrimaryText;
            ProfileSubText.Foreground = t.SecondaryText;

            // Theme label
            ThemeLabelText.Foreground = t.PrimaryText;
            ThemeSubText.Foreground = t.SecondaryText;

            // Delete button
            DeleteProfileBtn.Background = t.ButtonBackground;
            DeleteProfileBtn.BorderBrush = t.ButtonBorder;

            // Toggle position & color
            UpdateToggleUI();
        }

        private void UpdateToggleUI()
        {
            if (_isDark)
            {
                ToggleTrack.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#E50914")!;
                ToggleThumb.HorizontalAlignment = HorizontalAlignment.Right;
                ToggleThumb.Margin = new Thickness(0, 0, 3, 0);
                ThemeSubText.Text = "Currently Dark";
            }
            else
            {
                ToggleTrack.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#1A56DB")!;
                ToggleThumb.HorizontalAlignment = HorizontalAlignment.Left;
                ToggleThumb.Margin = new Thickness(3, 0, 0, 0);
                ThemeSubText.Text = "Currently Light";
            }
        }

        private void ThemeToggle_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _isDark = !_isDark;
            string newTheme = _isDark ? "Dark" : "Light";

            // Save to profile
            _profile.Theme = newTheme;
            var profiles = _profileService.LoadProfiles();
            var index = profiles.FindIndex(p => p.Id == _profile.Id);
            if (index >= 0)
            {
                profiles[index] = _profile;
                _profileService.SaveProfiles(profiles);
            }

            // Re-theme the popup itself immediately
            ApplyThemeToPopup();

            // Notify the main window
            ThemeChanged?.Invoke(newTheme);
        }

        private void DeleteProfileBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to delete '{_profile.Name}'?\nAll tasks will also be deleted.",
                "Delete Profile",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                var profiles = _profileService.LoadProfiles();
                profiles.RemoveAll(p => p.Id == _profile.Id);
                _profileService.SaveProfiles(profiles);

                var taskService = new TaskService();
                var tasks = taskService.LoadTasks();
                tasks.RemoveAll(t => t.ProfileId == _profile.Id);
                taskService.SaveTasks(tasks);

                ProfileDeleted?.Invoke();
                this.Close();
            }
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
            fadeOut.Completed += (s, ev) => this.Close();
            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }

        private string GetInitials(string name)
        {
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length > 0 ? name[0].ToString().ToUpper() : "?";
        }
    }
}
