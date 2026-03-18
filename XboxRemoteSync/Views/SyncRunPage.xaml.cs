using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using XboxRemoteSync.Models;
using XboxRemoteSync.ViewModels;

namespace XboxRemoteSync.Views
{
    public sealed partial class SyncRunPage : Page
    {
        public SyncRunViewModel ViewModel { get; } = new SyncRunViewModel();

        public SyncRunPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.InitializeAsync(e.Parameter as SyncProfile);
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.IsRunning)
                {
                    ViewModel.Cancel();
                    return;
                }

                await ViewModel.RunAsync();
            }
            catch (OperationCanceledException)
            {
                ViewModel.Status = string.Empty;
            }
            catch (Exception ex)
            {
                ViewModel.Status = ex.Message;
            }
        }

        private async void RefreshPreview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.RefreshPreviewAsync();
            }
            catch (Exception ex)
            {
                ViewModel.Status = ex.Message;
            }
        }

        private async void ProfileSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                await ViewModel.SelectProfileAsync(ViewModel.SelectedProfile);
            }
            catch (Exception ex)
            {
                ViewModel.Status = ex.Message;
            }
        }
    }
}
