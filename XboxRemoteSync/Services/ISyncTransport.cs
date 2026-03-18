using System.Collections.Generic;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Services
{
    public interface ISyncTransport
    {
        Task ConnectAsync(SyncProfile profile, string password, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null);

        Task<IReadOnlyList<SyncEntry>> EnumerateAsync(SyncProfile profile, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null);

        Task<Stream> OpenReadAsync(SyncProfile profile, SyncEntry entry, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null);

        Task DisconnectAsync();
    }
}
