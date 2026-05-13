using System;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using XboxRemoteSync.Models;
using XboxRemoteSync.Utilities;
using XboxRemoteSync.ViewModels;

namespace XboxRemoteSync.Views
{
    public sealed partial class ProfileEditorPage : Page
    {
        public ProfileEditorViewModel ViewModel { get; } = new ProfileEditorViewModel();

        public ProfileEditorPage()
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
                await DiagnosticsLog.AppendAsync("ProfileEditorPage.OnNavigatedTo", ex);
                ViewModel.SetStatusMessage("Unable to load the profile editor.");
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.SaveAsync(PasswordBox.Password);
            }
            catch (Exception ex)
            {
                await DiagnosticsLog.AppendAsync("ProfileEditorPage.Save_Click", ex);
                ViewModel.SetStatusMessage(ex.Message);
            }
        }

        private async void TestConnection_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await ViewModel.TestConnectionAsync(PasswordBox.Password);
            }
            catch (Exception ex)
            {
                await DiagnosticsLog.AppendAsync("ProfileEditorPage.TestConnection_Click", ex);
                ViewModel.NotifyTestConnectionFailed();
                ViewModel.SetStatusMessage(ex.Message);
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ViewModel.NotifyPasswordChanged(!string.IsNullOrEmpty(PasswordBox.Password));
        }

        private async void Destination_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder
            };
            picker.FileTypeFilter.Add("*");

            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                await ViewModel.SetDestinationAsync(folder);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            MainPage.AppFrame.Navigate(typeof(ProfilesPage));
        }
    }
}
