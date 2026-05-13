using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using TwinSpool.Common;
using TwinSpool.Models;
using TwinSpool.Services;
using TwinSpool.Utilities;

namespace TwinSpool.ViewModels
{
    public sealed class ProfileEditorViewModel : BindableBase
    {
        private string _name;
        private string _server;
        private string _port;
        private string _share;
        private string _remoteRoot;
        private string _protocol;
        private string _username;
        private string _destinationDisplayName;
        private string _statusMessage;
        private string _saveButtonText;
        private string _testConnectionButtonText;
        private bool _canSave;
        private bool _canTestConnection;
        private bool _isBusy;
        private string _savedFingerprint;
        private string _validatedConnectionFingerprint;

        public string[] SupportedProtocols { get; } = { SyncProtocolHelpers.Smb, SyncProtocolHelpers.Sftp };

        public SyncProfile Profile { get; private set; }

        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string Server
        {
            get => _server;
            set
            {
                if (SetProperty(ref _server, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string Port
        {
            get => _port;
            set
            {
                if (SetProperty(ref _port, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string Share
        {
            get => _share;
            set
            {
                if (SetProperty(ref _share, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string RemoteRoot
        {
            get => _remoteRoot;
            set
            {
                if (SetProperty(ref _remoteRoot, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (SetProperty(ref _username, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string Protocol
        {
            get => _protocol;
            set
            {
                if (SetProperty(ref _protocol, value))
                {
                    RaisePropertyChanged(nameof(ShareLabel));
                    RaisePropertyChanged(nameof(SharePlaceholder));
                    RaisePropertyChanged(nameof(IsShareEnabled));
                    RaisePropertyChanged(nameof(ShareVisibility));
                    RaisePropertyChanged(nameof(RemoteRootLabel));
                    RaisePropertyChanged(nameof(RemoteRootPlaceholder));
                    RefreshActionState();
                }
            }
        }

        public string ShareLabel => SyncProtocolHelpers.RequiresShare(Protocol) ? "Share" : "Share (SMB only)";

        public string SharePlaceholder => SyncProtocolHelpers.RequiresShare(Protocol) ? "MediaShare" : "Unused for SFTP";

        public bool IsShareEnabled => SyncProtocolHelpers.RequiresShare(Protocol);

        public Visibility ShareVisibility => SyncProtocolHelpers.RequiresShare(Protocol) ? Visibility.Visible : Visibility.Collapsed;

        public string RemoteRootLabel => SyncProtocolHelpers.RequiresShare(Protocol) ? "Subdirectory" : "Remote Root";

        public string RemoteRootPlaceholder => SyncProtocolHelpers.RequiresShare(Protocol) ? "/library" : "/home/xboxsync/library";

        public string DestinationDisplayName
        {
            get => _destinationDisplayName;
            set
            {
                if (SetProperty(ref _destinationDisplayName, value))
                {
                    RefreshActionState();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            private set => SetProperty(ref _saveButtonText, value);
        }

        public string TestConnectionButtonText
        {
            get => _testConnectionButtonText;
            private set => SetProperty(ref _testConnectionButtonText, value);
        }

        public bool CanSave
        {
            get => _canSave;
            private set => SetProperty(ref _canSave, value);
        }

        public bool CanTestConnection
        {
            get => _canTestConnection;
            private set => SetProperty(ref _canTestConnection, value);
        }

        public async Task InitializeAsync(SyncProfile profile)
        {
            Profile = profile ?? new SyncProfile
            {
                CredentialKey = Guid.NewGuid().ToString("N")
            };

            Name = Profile.Name;
            Protocol = SyncProtocolHelpers.Normalize(Profile.Protocol);
            Server = Profile.Server;
            Port = Profile.Port?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
            Share = Profile.Share;
            RemoteRoot = Profile.RemoteRoot;
            Username = Profile.Username;
            DestinationDisplayName = string.IsNullOrWhiteSpace(Profile.DestinationDisplayName) ? "Choose destination" : Profile.DestinationDisplayName;
            _savedFingerprint = BuildFingerprint();
            _validatedConnectionFingerprint = null;
            StatusMessage = string.Empty;
            SaveButtonText = "Saved";
            TestConnectionButtonText = "Test Connection";
            RefreshActionState();
            await Task.CompletedTask;
        }

        public async Task SaveAsync(string password)
        {
            _isBusy = true;
            SaveButtonText = "Saving...";
            StatusMessage = string.Empty;
            RefreshActionState();
            try
            {
                Profile = BuildDraftProfile();
                if (!string.IsNullOrWhiteSpace(password))
                {
                    await AppServices.CredentialProtector.StoreAsync(Profile.CredentialKey, password);
                }

                var profiles = (await AppServices.ProfileRepository.LoadAsync()).ToList();
                var existingIndex = profiles.FindIndex(item => item.Id == Profile.Id);
                if (existingIndex >= 0)
                {
                    profiles[existingIndex] = Profile;
                }
                else
                {
                    profiles.Add(Profile);
                }

                await AppServices.ProfileRepository.SaveAsync(profiles);
                _savedFingerprint = BuildFingerprint();
                StatusMessage = $"Saved '{Profile.Name}'.";
                SaveButtonText = "Saved";
            }
            finally
            {
                _isBusy = false;
                RefreshActionState();
            }
        }

        public async Task<string> TestConnectionAsync(string password)
        {
            _isBusy = true;
            TestConnectionButtonText = "Testing...";
            StatusMessage = string.Empty;
            RefreshActionState();
            try
            {
                var draftProfile = BuildDraftProfile();
                var effectivePassword = password;
                if (string.IsNullOrWhiteSpace(effectivePassword) && !string.IsNullOrWhiteSpace(draftProfile.CredentialKey))
                {
                    effectivePassword = await AppServices.CredentialProtector.RetrieveAsync(draftProfile.CredentialKey);
                }

                using (var cancellationTokenSource = new CancellationTokenSource())
                {
                    var transport = SyncTransportFactory.Create(draftProfile.Protocol);
                    await transport.ConnectAsync(draftProfile, effectivePassword, cancellationTokenSource.Token);
                    await transport.DisconnectAsync();
                }

                _validatedConnectionFingerprint = BuildConnectionFingerprint();
                TestConnectionButtonText = "Connection Successful";
                StatusMessage = $"Connection to '{draftProfile.ConnectionDisplay}' succeeded.";
                return StatusMessage;
            }
            catch (Exception ex)
            {
                _validatedConnectionFingerprint = null;
                TestConnectionButtonText = "Failed";
                StatusMessage = ex.Message;
                return StatusMessage;
            }
            finally
            {
                _isBusy = false;
                RefreshActionState();
            }
        }

        public void NotifyPasswordChanged(bool hasPasswordText)
        {
            if (hasPasswordText)
            {
                SaveButtonText = "Save";
            }

            _validatedConnectionFingerprint = null;
            RefreshActionState();
        }

        public void NotifyTestConnectionFailed()
        {
            _validatedConnectionFingerprint = null;
            TestConnectionButtonText = "Failed";
            RefreshActionState();
        }

        public void SetStatusMessage(string message)
        {
            StatusMessage = message ?? string.Empty;
        }

        public Task SetDestinationAsync(StorageFolder folder)
        {
            if (folder == null)
            {
                return Task.CompletedTask;
            }

            Profile = BuildDraftProfile();
            Profile.DestinationToken = StorageApplicationPermissions.FutureAccessList.Add(folder);
            Profile.DestinationDisplayName = folder.Path;
            DestinationDisplayName = folder.Path;
            RefreshActionState();
            return Task.CompletedTask;
        }

        private SyncProfile BuildDraftProfile()
        {
            var profile = Profile ?? new SyncProfile
            {
                CredentialKey = Guid.NewGuid().ToString("N")
            };

            profile.Name = Name?.Trim();
            profile.Protocol = SyncProtocolHelpers.Normalize(Protocol);
            profile.Server = Server?.Trim();
            profile.Port = TryParsePort(out var port) ? port : (int?)null;
            profile.Share = Share?.Trim();
            profile.RemoteRoot = string.IsNullOrWhiteSpace(RemoteRoot) ? "/" : RemoteRoot.Trim();
            profile.Username = Username?.Trim();
            profile.DestinationToken = Profile?.DestinationToken;
            profile.DestinationDisplayName = DestinationDisplayName;
            profile.EnabledExtensions = ExtensionWhitelist.All.ToList();
            return profile;
        }

        private void RefreshActionState()
        {
            var hasRequiredFields =
                !string.IsNullOrWhiteSpace(Name) &&
                !string.IsNullOrWhiteSpace(Protocol) &&
                !string.IsNullOrWhiteSpace(Server) &&
                HasValidPort() &&
                (!SyncProtocolHelpers.RequiresShare(Protocol) || !string.IsNullOrWhiteSpace(Share)) &&
                (!string.Equals(SyncProtocolHelpers.Normalize(Protocol), SyncProtocolHelpers.Sftp, StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(Username));

            var isDirty = BuildFingerprint() != _savedFingerprint;
            var requiresConnectionTest = BuildConnectionFingerprint() != _validatedConnectionFingerprint;
            CanSave = !_isBusy && hasRequiredFields && isDirty;
            CanTestConnection = !_isBusy &&
                !string.IsNullOrWhiteSpace(Protocol) &&
                !string.IsNullOrWhiteSpace(Server) &&
                HasValidPort() &&
                (!SyncProtocolHelpers.RequiresShare(Protocol) || !string.IsNullOrWhiteSpace(Share)) &&
                (!string.Equals(SyncProtocolHelpers.Normalize(Protocol), SyncProtocolHelpers.Sftp, StringComparison.Ordinal) || !string.IsNullOrWhiteSpace(Username)) &&
                requiresConnectionTest;

            if (!_isBusy)
            {
                SaveButtonText = CanSave ? "Save" : "Saved";
                if (!CanTestConnection && !string.IsNullOrWhiteSpace(_validatedConnectionFingerprint))
                {
                    TestConnectionButtonText = "Connection Successful";
                }
                else if (BuildConnectionFingerprint() != _validatedConnectionFingerprint)
                {
                    TestConnectionButtonText = "Test Connection";
                }
            }
        }

        private string BuildFingerprint()
        {
            return string.Join("|",
                Name?.Trim() ?? string.Empty,
                Protocol?.Trim().ToUpperInvariant() ?? "SMB",
                Server?.Trim() ?? string.Empty,
                Port?.Trim() ?? string.Empty,
                Share?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(RemoteRoot) ? "/" : RemoteRoot.Trim(),
                Username?.Trim() ?? string.Empty,
                DestinationDisplayName ?? string.Empty);
        }

        private string BuildConnectionFingerprint()
        {
            return string.Join("|",
                Protocol?.Trim().ToUpperInvariant() ?? "SMB",
                Server?.Trim() ?? string.Empty,
                Port?.Trim() ?? string.Empty,
                Share?.Trim() ?? string.Empty,
                string.IsNullOrWhiteSpace(RemoteRoot) ? "/" : RemoteRoot.Trim(),
                Username?.Trim() ?? string.Empty);
        }

        private bool HasValidPort()
        {
            return string.IsNullOrWhiteSpace(Port) || TryParsePort(out _);
        }

        private bool TryParsePort(out int port)
        {
            if (string.IsNullOrWhiteSpace(Port))
            {
                port = 0;
                return false;
            }

            return int.TryParse(Port.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out port)
                && port > 0
                && port <= 65535;
        }
    }
}
