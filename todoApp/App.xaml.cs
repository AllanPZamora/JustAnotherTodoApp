using System.Configuration;
using System.Data;
using System.Windows;

namespace todoApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ShutdownMode = ShutdownMode.OnLastWindowClose;

            var splash = new SplashWindow();
            splash.Show();
        }
    }
}
