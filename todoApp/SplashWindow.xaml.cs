using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using todoApp;

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
            await Task.Delay(2000);
            FadeToProfileScreen();
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