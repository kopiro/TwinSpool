using System;
using XboxRemoteSync.Utilities;

namespace XboxRemoteSync.Services
{
    public static class SyncTransportFactory
    {
        public static ISyncTransport Create(string protocol)
        {
            switch (SyncProtocolHelpers.Normalize(protocol))
            {
                case SyncProtocolHelpers.Sftp:
                    return new SftpSyncTransport();
                case SyncProtocolHelpers.Smb:
                    return new SmbSyncTransport();
                default:
                    throw new InvalidOperationException($"Unsupported protocol '{protocol}'.");
            }
        }
    }
}
