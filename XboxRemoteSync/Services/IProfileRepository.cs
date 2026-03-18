using System.Collections.Generic;
using System.Threading.Tasks;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Services
{
    public interface IProfileRepository
    {
        Task<IReadOnlyList<SyncProfile>> LoadAsync();

        Task SaveAsync(IReadOnlyList<SyncProfile> profiles);
    }
}
