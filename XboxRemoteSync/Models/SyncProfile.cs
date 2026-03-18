using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using XboxRemoteSync.Utilities;

namespace XboxRemoteSync.Models
{
    public sealed class SyncProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        public string Name { get; set; } = "New Profile";

        public string Protocol { get; set; } = "SMB";

        public string Server { get; set; }

        public string Share { get; set; }

        public string RemoteRoot { get; set; } = "/";

        public string Username { get; set; }

        public string CredentialKey { get; set; }

        public string DestinationToken { get; set; }

        public string DestinationDisplayName { get; set; }

        public List<string> EnabledExtensions { get; set; } = new List<string>(ExtensionWhitelist.All);

        public DateTimeOffset? LastRunUtc { get; set; }

        public string LastSummary { get; set; }

        [JsonIgnore]
        public string ConnectionDisplay
        {
            get
            {
                var authority = string.IsNullOrWhiteSpace(Username)
                    ? Server ?? string.Empty
                    : $"{Username}:***@{Server}";

                var shareSegment = string.IsNullOrWhiteSpace(Share) ? string.Empty : $"/{Share.Trim('/')}";
                var rootSegment = NormalizeRemoteRoot(RemoteRoot);
                return $"{(Protocol ?? "SMB").ToLowerInvariant()}://{authority}{shareSegment}{rootSegment}";
            }
        }

        [JsonIgnore]
        public string DestinationSummary => string.IsNullOrWhiteSpace(DestinationDisplayName)
            ? "No destination selected"
            : DestinationDisplayName;

        private static string NormalizeRemoteRoot(string remoteRoot)
        {
            if (string.IsNullOrWhiteSpace(remoteRoot) || remoteRoot == "/")
            {
                return string.Empty;
            }

            return "/" + remoteRoot.Trim().Trim('/');
        }
    }
}
