
using System.Windows;
using System.Threading;

namespace TaskbarGrouper
{
    public partial class App : Application
    {
        private static Mutex? mutex = null;
        private static Views.MainWindow? singleMainWindow = null;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Ensure only one instance can run
            const string appName = "TaskbarGrouperSingleInstance";
            bool createdNew;

            mutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                // Another instance is already running
                Current.Shutdown();
                return;
            }

            base.OnStartup(e);

            // Ensure only one MainWindow is created
            if (singleMainWindow == null)
            {
                singleMainWindow = new Views.MainWindow();
                singleMainWindow.Closed += (s, args) => singleMainWindow = null;
                singleMainWindow.Show();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            mutex?.ReleaseMutex();
            base.OnExit(e);
        }
    }
}