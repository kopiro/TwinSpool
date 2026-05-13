using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using TwinSpool.Models;
using TwinSpool.Services;

namespace TwinSpool.Views
{
    public sealed partial class MainPage : Page
    {
        public static Frame AppFrame { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            AppFrame = ContentFrame;
            Loaded += MainPage_Loaded;
            Unloaded += MainPage_Unloaded;
            AppFrame.Navigated += AppFrame_Navigated;
            SyncUiState.SyncRunningChanged += SyncUiState_SyncRunningChanged;
            UpdateNavigationState();
        }

        private void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (AppFrame.Content == null)
            {
                AppFrame.Navigate(typeof(ProfilesPage));
            }
        }

        private void MainPage_Unloaded(object sender, RoutedEventArgs e)
        {
            AppFrame.Navigated -= AppFrame_Navigated;
            SyncUiState.SyncRunningChanged -= SyncUiState_SyncRunningChanged;
        }

        private async void SyncUiState_SyncRunningChanged(object sender, bool isRunning)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, UpdateNavigationState);
        }

        private void UpdateNavigationState()
        {
            var isEnabled = !SyncUiState.IsSyncRunning;
            ProfilesButton.IsEnabled = isEnabled;
            RunSyncButton.IsEnabled = isEnabled;
            LogsButton.IsEnabled = isEnabled;
            UpdateSelectedNavigation();
        }

        private void AppFrame_Navigated(object sender, Windows.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            UpdateSelectedNavigation();
        }

        private void UpdateSelectedNavigation()
        {
            var currentPageType = AppFrame?.CurrentSourcePageType;
            SetSelectedState(ProfilesButton, currentPageType == typeof(ProfilesPage) || currentPageType == typeof(ProfileEditorPage));
            SetSelectedState(RunSyncButton, currentPageType == typeof(SyncRunPage));
            SetSelectedState(LogsButton, currentPageType == typeof(LogsPage));
        }

        private void SetSelectedState(Button button, bool isSelected)
        {
            button.Background = isSelected
                ? (Brush)Application.Current.Resources["AppSidebarSelectedBrush"]
                : new SolidColorBrush(Windows.UI.Colors.Transparent);
            button.BorderBrush = isSelected
                ? (Brush)Application.Current.Resources["AppAccentBrush"]
                : new SolidColorBrush(Windows.UI.Colors.Transparent);
            button.BorderThickness = isSelected ? new Thickness(2) : new Thickness(0);
        }

        private void Profiles_Click(object sender, RoutedEventArgs e)
        {
            if (SyncUiState.IsSyncRunning)
            {
                return;
            }

            if (AppFrame.CurrentSourcePageType == typeof(ProfilesPage) || AppFrame.CurrentSourcePageType == typeof(ProfileEditorPage))
            {
                return;
            }

            AppFrame.Navigate(typeof(ProfilesPage));
        }

        private void RunSync_Click(object sender, RoutedEventArgs e)
        {
            if (SyncUiState.IsSyncRunning)
            {
                return;
            }

            if (AppFrame.CurrentSourcePageType == typeof(SyncRunPage))
            {
                return;
            }

            AppFrame.Navigate(typeof(SyncRunPage));
        }

        private void Logs_Click(object sender, RoutedEventArgs e)
        {
            if (SyncUiState.IsSyncRunning)
            {
                return;
            }

            if (AppFrame.CurrentSourcePageType == typeof(LogsPage))
            {
                return;
            }

            AppFrame.Navigate(typeof(LogsPage));
        }
    }
}
