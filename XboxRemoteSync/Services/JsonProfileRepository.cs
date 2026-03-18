using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.FileProperties;
using Newtonsoft.Json;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Services
{
    public sealed class JsonProfileRepository : IProfileRepository
    {
        private const string FileName = "profiles.json";

        public async Task<IReadOnlyList<SyncProfile>> LoadAsync()
        {
            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
                var json = await FileIO.ReadTextAsync(file);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new List<SyncProfile>();
                }

                return JsonConvert.DeserializeObject<List<SyncProfile>>(json) ?? new List<SyncProfile>();
            }
            catch
            {
                return new List<SyncProfile>();
            }
        }

        public async Task SaveAsync(IReadOnlyList<SyncProfile> profiles)
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonConvert.SerializeObject(profiles.ToList(), Formatting.Indented);
            await FileIO.WriteTextAsync(file, json);
        }
    }
}
