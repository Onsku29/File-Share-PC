using System;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System.Diagnostics;

namespace File_Share
{
    public partial class App : Application
    {
        public static App Instance => (App)Current;
        public MainWindow mainWindow;
        public App()
        {
            Debug.WriteLine("App constructor called");
            InitializeComponent();

            var appInstance = AppInstance.GetCurrent();

            var args = Environment.GetCommandLineArgs();
            bool startInBackground = args.Any(a => string.Equals(a, "--background", StringComparison.OrdinalIgnoreCase));

            mainWindow = new MainWindow();

            if (!startInBackground)
            {
                mainWindow.Activate();
            }
            else
            {
                Debug.WriteLine("Starting in background");
            }
        }
    }
}
