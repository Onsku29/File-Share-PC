using System;
using Microsoft.UI.Xaml;
using WinUIEx;
using System.Threading.Tasks;
using FileShare.Scripts;
using Microsoft.UI.Xaml.Media.Animation;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.IO;
using FileShare.Networking;
using System.Collections.Generic;
using WinRT.Interop;
using FileShare.Storing;

namespace File_Share
{
    public sealed partial class MainWindow : WindowEx
    {
        private readonly PairingServer _server;
        private readonly PairingService _service;
        private readonly DeviceManager _deviceManager;
        private readonly List<PairedDevice> _pairedDevices = new();

        public MainWindow()
        {
            InitializeComponent();
            this.AppWindow.Closing += AppWindow_Closing;

            Debug.WriteLine("Window created");

            _deviceManager = new DeviceManager();
            _server = new PairingServer(_deviceManager);
            _service = new PairingService(_server);
            _deviceManager.DevicePaired += OnDevicePaired;

            var pairedDevicesJson = _deviceManager.GetAllPairedDevices();

            foreach (var device in pairedDevicesJson)
            {
                _pairedDevices.Add(device);
            }

            DeviceList.ItemsSource = null;
            DeviceList.ItemsSource = _pairedDevices;
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

        private async void addDevice_Click(object sender, RoutedEventArgs e)
        {
            Overlay.Visibility = Visibility.Visible;
            LoadingRing.Visibility = Visibility.Visible;
            cancelAdd.Visibility = Visibility.Visible;
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

        private void cancelAdd_Click(object sender, RoutedEventArgs e)
        {
            //KORJAA ANIMAATIO
            var fadeOutStoryboard = (Storyboard)Application.Current.Resources["FadeOutOverlay"];
            fadeOutStoryboard?.Stop();
            Storyboard.SetTarget(fadeOutStoryboard.Children[0], Overlay);
            fadeOutStoryboard.Begin();
            Overlay.Visibility = Visibility.Collapsed;
            QRCodeImage.Source = null;
            LoadingRing.IsActive = false;
            LoadingRing.Visibility = Visibility.Collapsed;
        }

        private void removeDevice_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement menuItem && menuItem.DataContext is PairedDevice device)
            {
                _deviceManager.DeleteDevice(device);
                _pairedDevices.Remove(device);
                DeviceList.ItemsSource = null;
                DeviceList.ItemsSource = _pairedDevices;
            }
            else
            {
                Debug.WriteLine("Could not determine which device to remove.");
            }
        }

        private async void OnDevicePaired(PairedDevice device)
        {
            try
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    //KORJAA ANIMAATIO
                    var fadeOutStoryboard = (Storyboard)Application.Current.Resources["FadeOutOverlay"];
                    fadeOutStoryboard?.Stop();
                    Storyboard.SetTarget(fadeOutStoryboard.Children[0], Overlay);
                    fadeOutStoryboard.Begin();
                    Overlay.Visibility = Visibility.Collapsed;
                    QRCodeImage.Source = null;
                    LoadingRing.IsActive = false;
                    LoadingRing.Visibility = Visibility.Collapsed;

                    _pairedDevices.Add(device);
                    DeviceList.ItemsSource = null;
                    DeviceList.ItemsSource = _pairedDevices;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in OnDevicePaired: {ex.Message}");
            }
        }

        private async void OnPairingFailed()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                //KORJAA ANIMAATIO
                var fadeOutStoryboard = (Storyboard)Application.Current.Resources["FadeOutOverlay"];
                fadeOutStoryboard?.Stop();
                Storyboard.SetTarget(fadeOutStoryboard.Children[0], Overlay);
                fadeOutStoryboard.Begin();
                Overlay.Visibility = Visibility.Collapsed;
                QRCodeImage.Source = null;
                LoadingRing.IsActive = false;
                LoadingRing.Visibility = Visibility.Collapsed;
            });
        }

        public async Task<string?> RequestSavePathAsync(string suggestedFileName)
        {
            BringToForeground();
            var picker = new Windows.Storage.Pickers.FileSavePicker();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
            picker.SuggestedFileName = suggestedFileName;
            picker.FileTypeChoices.Add("All files", new List<string>() { "." });

            var file = await picker.PickSaveFileAsync();
            return file?.Path;
        }

        public void BringToForeground()
        {
            if (this.AppWindow != null)
            {
                this.AppWindow.Show();
                this.Activate();
            }
            else
            {
                Debug.WriteLine("AppWindow is null, cannot bring to foreground.");
            }
        }
    }
}
