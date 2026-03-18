using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Newtonsoft.Json;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Services
{
    public sealed class JsonRunLogRepository : IRunLogRepository
    {
        private const string FileName = "runlog.json";
        private const int MaxEntries = 500;

        public async Task AppendAsync(IReadOnlyList<RunLogEntry> entries)
        {
            var allEntries = (await LoadAllAsync()).ToList();
            allEntries.AddRange(entries);
            allEntries = allEntries
                .OrderByDescending(entry => entry.TimestampUtc)
                .Take(MaxEntries)
                .OrderBy(entry => entry.TimestampUtc)
                .ToList();

            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonConvert.SerializeObject(allEntries, Formatting.Indented);
            await FileIO.WriteTextAsync(file, json);
        }

        public async Task<IReadOnlyList<RunLogEntry>> LoadAsync(string profileId)
        {
            var entries = await LoadAllCoreAsync();
            return entries.Where(entry => entry.ProfileId == profileId).OrderByDescending(entry => entry.TimestampUtc).ToList();
        }

        public async Task<IReadOnlyList<RunLogEntry>> LoadAllAsync()
        {
            var entries = await LoadAllCoreAsync();
            return entries.OrderByDescending(entry => entry.TimestampUtc).ToList();
        }

        private static async Task<IReadOnlyList<RunLogEntry>> LoadAllCoreAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
                var json = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<RunLogEntry>();
                }

                return JsonConvert.DeserializeObject<List<RunLogEntry>>(json) ?? new List<RunLogEntry>();
            }
            catch
            {
                return new List<RunLogEntry>();
            }
        }
    }
}
