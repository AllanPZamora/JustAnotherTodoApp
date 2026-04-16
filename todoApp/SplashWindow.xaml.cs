using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Media;

namespace todoApp
{
    public partial class SplashWindow : Window
    {
        public SplashWindow()
        {
            InitializeComponent();
            Loaded += SplashWindow_Loaded;
        }

        private async void SplashWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Fade in
            this.Opacity = 0;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.8));
            this.BeginAnimation(Window.OpacityProperty, fadeIn);

            // Scale animation
            var scaleAnim = new DoubleAnimation(0.8, 1, TimeSpan.FromSeconds(0.6))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };

            LogoScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            LogoScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // Loading animation
            _ = AnimateLoadingText();

            await Task.Delay(2200);
            FadeToProfileScreen();
        }

        private async Task AnimateLoadingText()
        {
            string[] dots = { ".", "..", "..." };
            int i = 0;

            while (true)
            {
                LoadingText.Text = "Loading" + dots[i];
                i = (i + 1) % dots.Length;
                await Task.Delay(400);
            }
        }

        private void FadeToProfileScreen()
        {
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.8));
            fadeOut.Completed += (s, e) =>
            {
                var profileWindow = new ProfileWindow();
                profileWindow.Show();
                this.Close();
            };

            this.BeginAnimation(Window.OpacityProperty, fadeOut);
        }
    }
}