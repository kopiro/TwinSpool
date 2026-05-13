using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using TwinSpool.ViewModels;

namespace TwinSpool.Views
{
    public sealed partial class LogsPage : Page
    {
        public LogsViewModel ViewModel { get; } = new LogsViewModel();

        public LogsPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.InitializeAsync();
        }

        private async void ProfileSelection_Changed(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                await ViewModel.RefreshAsync();
            }
            catch (Exception ex)
            {
                ViewModel.LogText = ex.Message;
            }
        }
    }
}
