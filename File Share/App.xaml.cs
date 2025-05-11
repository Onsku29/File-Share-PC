using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Diagnostics;
using WinUIEx;

namespace File_Share
{
    public partial class App : Application
    {
        public MainWindow mainWindow;

        public App()
        {
            Debug.WriteLine("App constructor called");
            InitializeComponent();

            var appInstance = AppInstance.GetCurrent();
            mainWindow = new MainWindow();
            mainWindow.Activate();
        }
    }
}
