using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using XboxRemoteSync.Common;
using XboxRemoteSync.Models;
using XboxRemoteSync.Services;

namespace XboxRemoteSync.ViewModels
{
    public sealed class ProfilesViewModel : BindableBase
    {
        private SyncProfile _selectedProfile;

        public ObservableCollection<SyncProfile> Profiles { get; } = new ObservableCollection<SyncProfile>();

        public SyncProfile SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetProperty(ref _selectedProfile, value))
                {
                    RaisePropertyChanged(nameof(HasSelectedProfile));
                }
            }
        }

        public bool HasSelectedProfile => SelectedProfile != null;

        public async Task LoadAsync()
        {
            Profiles.Clear();
            var profiles = await AppServices.ProfileRepository.LoadAsync();
            foreach (var profile in profiles.OrderBy(item => item.Name))
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault();
        }

        public async Task DeleteSelectedAsync()
        {
            if (SelectedProfile == null)
            {
                return;
            }

            await AppServices.CredentialProtector.RemoveAsync(SelectedProfile.CredentialKey);
            var remaining = Profiles.Where(item => item.Id != SelectedProfile.Id).ToList();
            await AppServices.ProfileRepository.SaveAsync(remaining);
            await LoadAsync();
        }
    }
}
