using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using todoApp;

namespace todoApp
{
    public partial class ProfileWindow : Window
    {
        private ProfileService _profileService = new ProfileService();
        private bool _isDarkTheme = true;

        public ProfileWindow()
        {
            InitializeComponent();
            Loaded += ProfileWindow_Loaded;
        }

        private void ProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.Opacity = 0;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            LoadProfiles();
        }

        private void LoadProfiles()
        {
            ProfilesRow.Children.Clear();

            var profiles = _profileService.LoadProfiles();

            foreach (var profile in profiles)
            {
                var vm = new ProfileViewModel(profile);
                var card = CreateProfileCard(vm);
                ProfilesRow.Children.Add(card);
            }

            ProfilesRow.Children.Add(CreateAddCard());
        }

        private StackPanel CreateProfileCard(ProfileViewModel vm)
        {
            var card = new StackPanel
            {
                Margin = new Thickness(16, 0, 16, 0),
                Cursor = Cursors.Hand
            };

            var border = new Border
            {
                Width = 80,
                Height = 80,
                CornerRadius = new CornerRadius(8),
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(vm.Color)!
            };

            var initials = new TextBlock
            {
                Text = vm.Initials,
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = initials;

            var name = new TextBlock
            {
                Text = vm.Name,
                FontSize = 13,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            card.Children.Add(border);
            card.Children.Add(name);

            card.MouseLeftButtonUp += (s, e) =>
            {
                MessageBox.Show($"Welcome, {vm.Name}!");
            };

            return card;
        }

        private StackPanel CreateAddCard()
        {
            var card = new StackPanel
            {
                Margin = new Thickness(16, 0, 16, 0),
                Cursor = Cursors.Hand
            };

            var border = new Border
            {
                Width = 80,
                Height = 80,
                CornerRadius = new CornerRadius(8),
                BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#444444")!,
                BorderThickness = new Thickness(2)
            };

            var plus = new TextBlock
            {
                Text = "+",
                FontSize = 36,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = plus;

            var label = new TextBlock
            {
                Text = "Add Profile",
                FontSize = 13,
                Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#AAAAAA")!,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 8, 0, 0)
            };

            card.Children.Add(border);
            card.Children.Add(label);

            card.MouseLeftButtonUp += AddProfile_Clicked;

            return card;
        }

        private void AddProfile_Clicked(object sender, MouseButtonEventArgs e)
        {
            var dialog = new AddProfileDialog();
            dialog.Owner = this;
            if (dialog.ShowDialog() == true)
            {
                var newProfile = _profileService.CreateProfile(dialog.ProfileName);
                var profiles = _profileService.LoadProfiles();
                profiles.Add(newProfile);
                _profileService.SaveProfiles(profiles);
                LoadProfiles();
            }
        }

        private void Profile_Clicked(object sender, MouseButtonEventArgs e) { }

        private void ThemeToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _isDarkTheme = !_isDarkTheme;

            if (_isDarkTheme)
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#141414")!;
                TitleText.Foreground = Brushes.White;
                ThemeToggleBtn.Content = "☀";
            }
            else
            {
                this.Background = (SolidColorBrush)new BrushConverter().ConvertFrom("#F0F0F0")!;
                TitleText.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#141414")!;
                ThemeToggleBtn.Content = "🌙";
            }
        }
    }

    public class ProfileViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
        public string Initials { get; set; }

        public ProfileViewModel(Profile profile)
        {
            Id = profile.Id;
            Name = profile.Name;
            Color = profile.Color;
            Initials = GetInitials(profile.Name);
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