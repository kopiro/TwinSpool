using System;
using System.Threading;
using System.Threading.Tasks;
using TwinSpool.Models;

namespace TwinSpool.Services
{
    public interface ISyncEngine
    {
        Task<SyncPlan> BuildPlanAsync(SyncProfile profile, CancellationToken cancellationToken);

        Task<string> RunAsync(SyncProfile profile, IProgress<SyncJobProgress> progress, CancellationToken cancellationToken);
    }
}
