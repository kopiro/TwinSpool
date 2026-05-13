using System.Collections.Generic;
using System.Threading.Tasks;
using TwinSpool.Models;

namespace TwinSpool.Services
{
    public interface IRunLogRepository
    {
        Task AppendAsync(IReadOnlyList<RunLogEntry> entries);

        Task<IReadOnlyList<RunLogEntry>> LoadAsync(string profileId);

        Task<IReadOnlyList<RunLogEntry>> LoadAllAsync();
    }
}
