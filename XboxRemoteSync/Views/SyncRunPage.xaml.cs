using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using XboxRemoteSync.Models;
using XboxRemoteSync.Utilities;
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
            try
            {
                await ViewModel.InitializeAsync(e.Parameter as SyncProfile);
            }
            catch (Exception ex)
            {
                await DiagnosticsLog.AppendAsync("SyncRunPage.OnNavigatedTo", ex);
                ViewModel.Status = "Unable to load the sync page.";
            }
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
                await DiagnosticsLog.AppendAsync("SyncRunPage.Start_Click", ex);
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
                await DiagnosticsLog.AppendAsync("SyncRunPage.RefreshPreview_Click", ex);
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
                await DiagnosticsLog.AppendAsync("SyncRunPage.ProfileSelection_Changed", ex);
                ViewModel.Status = ex.Message;
            }
        }
    }
}
