using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using XboxRemoteSync.Common;
using XboxRemoteSync.Models;
using XboxRemoteSync.Services;

namespace XboxRemoteSync.ViewModels
{
    public sealed class LogsViewModel : BindableBase
    {
        private SyncProfile _selectedProfile;
        private string _logText;

        public ObservableCollection<SyncProfile> Profiles { get; } = new ObservableCollection<SyncProfile>();

        public SyncProfile SelectedProfile
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value);
        }

        public string LogText
        {
            get => _logText;
            set => SetProperty(ref _logText, value);
        }

        public async Task InitializeAsync()
        {
            Profiles.Clear();
            Profiles.Add(new SyncProfile { Id = string.Empty, Name = "All Profiles" });

            var profiles = (await AppServices.ProfileRepository.LoadAsync()).OrderBy(item => item.Name).ToList();
            foreach (var profile in profiles)
            {
                Profiles.Add(profile);
            }

            SelectedProfile = Profiles.FirstOrDefault();
            await RefreshAsync();
        }

        public async Task RefreshAsync()
        {
            var profiles = (await AppServices.ProfileRepository.LoadAsync()).ToDictionary(item => item.Id, item => item.Name);
            IReadOnlyList<RunLogEntry> entries;

            if (SelectedProfile == null || string.IsNullOrWhiteSpace(SelectedProfile.Id))
            {
                entries = await AppServices.RunLogRepository.LoadAllAsync();
            }
            else
            {
                entries = await AppServices.RunLogRepository.LoadAsync(SelectedProfile.Id);
            }

            LogText = string.Join(Environment.NewLine, entries.Select(entry =>
            {
                var profileName = profiles.TryGetValue(entry.ProfileId ?? string.Empty, out var name) ? name : "Unknown Profile";
                return $"[{entry.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss}] [{profileName}] {entry.ResultCode} {entry.RelativePath} {entry.Detail}".Trim();
            }));
        }
    }
}
