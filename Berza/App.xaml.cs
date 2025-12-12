using Berza;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows;

namespace Berza
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppRegistration.RegistrationCheck();

            ToastNotificationManagerCompat.OnActivated += Toast_OnActivated;
        }

        private void Toast_OnActivated(ToastNotificationActivatedEventArgsCompat e)
        {
            Current.Dispatcher.Invoke(() =>
            {
                var main = Current.MainWindow;

                if (main == null)
                {
                    main = new MainWindow();
                    Current.MainWindow = main;
                }

                if (main.Visibility != Visibility.Visible)
                    main.Show();

                main.WindowState = WindowState.Maximized;
                main.Activate();
                main.Topmost = true;
                main.Topmost = false;
                main.Focus();
            });
        }
    }
}