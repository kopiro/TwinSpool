using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using XboxRemoteSync.Models;
using XboxRemoteSync.ViewModels;

namespace XboxRemoteSync.Views
{
    public sealed partial class ProfilesPage : Page
    {
        public ProfilesViewModel ViewModel { get; } = new ProfilesViewModel();

        public ProfilesPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
            Loaded += ProfilesPage_Loaded;
        }

        private async void ProfilesPage_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadAsync();
        }

        private void ProfilesList_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.SelectedProfile = e.ClickedItem as SyncProfile;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            MainPage.AppFrame.Navigate(typeof(ProfileEditorPage), null);
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedProfile == null)
            {
                return;
            }

            MainPage.AppFrame.Navigate(typeof(ProfileEditorPage), ViewModel.SelectedProfile);
        }

        private async void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.SelectedProfile == null)
            {
                return;
            }

            var dialog = new MessageDialog($"Delete profile '{ViewModel.SelectedProfile.Name}'?");
            dialog.Commands.Add(new UICommand("Delete"));
            dialog.Commands.Add(new UICommand("Cancel"));
            dialog.DefaultCommandIndex = 1;
            var result = await dialog.ShowAsync();
            if (result.Label == "Delete")
            {
                await ViewModel.DeleteSelectedAsync();
            }
        }
    }
}
