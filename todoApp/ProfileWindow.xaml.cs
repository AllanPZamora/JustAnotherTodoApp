using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace todoApp
{
    public partial class ProfileWindow : Window
    {
        private ProfileService _profileService = new ProfileService();

        // Theme brushes
        private readonly SolidColorBrush DarkBackground = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        private readonly SolidColorBrush CardBorder = new SolidColorBrush(Color.FromRgb(68, 68, 68));
        private readonly SolidColorBrush SecondaryText = new SolidColorBrush(Color.FromRgb(170, 170, 170));

        public ProfileWindow()
        {
            InitializeComponent();
            Loaded += ProfileWindow_Loaded;
        }

        private void ProfileWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Fade in animation
            this.Opacity = 0;

            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.5));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            LoadProfiles();
        }

        // Close button logic
        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to exit?",
                "Confirm Exit",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
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
                Width = 90,
                Height = 90,
                CornerRadius = new CornerRadius(12),
                Background = (SolidColorBrush)new BrushConverter().ConvertFrom(vm.Color)!,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    BlurRadius = 15,
                    ShadowDepth = 0,
                    Opacity = 0.5
                }
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
                Foreground = SecondaryText,
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
                Width = 90,
                Height = 90,
                CornerRadius = new CornerRadius(12),
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 42)),
                BorderBrush = CardBorder,
                BorderThickness = new Thickness(1)
            };

            var plus = new TextBlock
            {
                Text = "+",
                FontSize = 36,
                Foreground = SecondaryText,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            border.Child = plus;

            var label = new TextBlock
            {
                Text = "Add Profile",
                FontSize = 13,
                Foreground = SecondaryText,
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