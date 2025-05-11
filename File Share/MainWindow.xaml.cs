using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUIEx;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;
using FileShare.Scripts;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI;
using Windows.UI.ViewManagement;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using System.Text.Json;
using FileShare.Networking;
using System.Collections.Generic;
using FileShare.Storing;
using System.Linq;

namespace File_Share
{
    public sealed partial class MainWindow : WindowEx
    {
        private readonly PairingServer _server;
        private readonly PairingService _service;
        private DeviceManager _deviceManager;
        private readonly List<string> _pairedDevices = new();

        public MainWindow()
        {
            InitializeComponent();
            this.AppWindow.Closing += AppWindow_Closing;

            Debug.WriteLine("Window created");

            _server = new PairingServer();
            _server.DevicePaired += OnDevicePaired;
            _service = new PairingService(_server);
            _deviceManager = new DeviceManager();

            var pairedDevicesJson = _deviceManager.GetAllPairedDevices();

            foreach (var device in pairedDevicesJson)
            {
                _pairedDevices.Add(device.DeviceName);
            }

            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = _pairedDevices;

            var uiSettings = new UISettings();
            Color accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            if (IsColorTooLight(accentColor))
            {
                addDevice.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                addDevice.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            this.Hide();
        }


        [RelayCommand]
        public void ShowHideWindow()
        {
            var app = (App)Application.Current;
            var window = app.mainWindow;
            if (window == null)
            {
                return;
            }

            if (window.Visible)
            {
                window.Hide();
            }
            else
            {
                window.Show();
            }
        }

        [RelayCommand]
        public void ExitApplication()
        {
            var app = (App)Application.Current;
            var window = app.mainWindow;
            if (window != null)
            {
                window.Close();
            }
            TrayIcon.Dispose();
        }

        private void MainWindow_Closed(Object sender, WindowEventArgs args)
        {
            Debug.WriteLine("Window hidden");
        }

        private bool IsColorTooLight(Color color)
        {
            double luminance = (0.2126 * color.R / 255.0) +
                               (0.7152 * color.G / 255.0) +
                               (0.0722 * color.B / 255.0);
            return luminance > 0.25;
        }

        private async void addDevice_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            LoadingRing.Visibility = Visibility.Visible;
            LoadingRing.IsActive = true;
            QRCodeImage.Source = null;

            var fadeInStoryboard = (Storyboard)Application.Current.Resources["FadeInOverlay"];
            fadeInStoryboard?.Stop();
            Storyboard.SetTarget(fadeInStoryboard.Children[0], Overlay);
            fadeInStoryboard.Begin();

            Stream qrStream = await Task.Run(() => _service.GenerateQrCodeStream());

            var image = new BitmapImage();
            await image.SetSourceAsync(qrStream.AsRandomAccessStream());

            QRCodeImage.Source = image;
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }

        private async void OnDevicePaired(string deviceName)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                Overlay.Visibility = Visibility.Collapsed;
                QRCodeImage.Source = null;
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;

                _pairedDevices.Add(deviceName);
                DeviceList.ItemsSource = null;
                DeviceList.ItemsSource = _pairedDevices;
            });
        }
    }
}
