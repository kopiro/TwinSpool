using System.Collections.Generic;
using System.Threading.Tasks;
using TwinSpool.Models;

namespace TwinSpool.Services
{
    public interface IProfileRepository
    {
        Task<IReadOnlyList<SyncProfile>> LoadAsync();

        Task SaveAsync(IReadOnlyList<SyncProfile> profiles);
    }
}
