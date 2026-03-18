using System.Collections.Generic;
using System.Threading.Tasks;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Services
{
    public interface IRunLogRepository
    {
        Task AppendAsync(IReadOnlyList<RunLogEntry> entries);

        Task<IReadOnlyList<RunLogEntry>> LoadAsync(string profileId);

        Task<IReadOnlyList<RunLogEntry>> LoadAllAsync();
    }
}
