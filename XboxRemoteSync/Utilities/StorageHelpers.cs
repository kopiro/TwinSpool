using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using XboxRemoteSync.Models;

namespace XboxRemoteSync.Utilities
{
    public static class StorageHelpers
    {
        private const string SyncIndexFileName = ".xboxremotesync-index.json";

        public static async Task<StorageFolder> EnsureFolderPathAsync(StorageFolder root, string relativeDirectory)
        {
            var current = root;
            if (string.IsNullOrWhiteSpace(relativeDirectory))
            {
                return current;
            }

            var parts = relativeDirectory.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                current = await current.CreateFolderAsync(part, CreationCollisionOption.OpenIfExists);
            }

            return current;
        }

        public static async Task CopyToFileAtomicallyAsync(
            Stream source,
            StorageFolder destinationRoot,
            string relativePath,
            CancellationToken cancellationToken,
            Func<long, Task> progressCallback = null)
        {
            var directoryPath = Path.GetDirectoryName(relativePath) ?? string.Empty;
            var destinationFolder = await EnsureFolderPathAsync(destinationRoot, directoryPath);
            var fileName = Path.GetFileName(relativePath);
            var tempName = fileName + ".partial";
            var tempFile = await destinationFolder.CreateFileAsync(tempName, CreationCollisionOption.ReplaceExisting);

            using (var target = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
            using (var targetStream = target.AsStreamForWrite())
            {
                var buffer = new byte[81920];
                long bytesCopied = 0;
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    await targetStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    bytesCopied += bytesRead;
                    if (progressCallback != null)
                    {
                        await progressCallback(bytesCopied);
                    }
                }

                await targetStream.FlushAsync(cancellationToken);
            }

            var finalFile = await destinationFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await tempFile.MoveAndReplaceAsync(finalFile);
        }

        public static bool FileNeedsUpdate(IReadOnlyDictionary<string, SyncEntry> index, SyncEntry entry)
        {
            if (index == null || !index.TryGetValue(entry.RelativePath, out var existing))
            {
                return true;
            }

            return existing.Size != entry.Size || existing.ModifiedUtc != entry.ModifiedUtc;
        }

        public static async Task<Dictionary<string, SyncEntry>> LoadSyncIndexAsync(StorageFolder root)
        {
            try
            {
                var file = await root.CreateFileAsync(SyncIndexFileName, CreationCollisionOption.OpenIfExists);
                var json = await FileIO.ReadTextAsync(file);
                var entries = string.IsNullOrWhiteSpace(json)
                    ? new List<SyncEntry>()
                    : JsonConvert.DeserializeObject<List<SyncEntry>>(json) ?? new List<SyncEntry>();

                return entries.ToDictionary(item => item.RelativePath, StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, SyncEntry>(StringComparer.OrdinalIgnoreCase);
            }
        }

        public static async Task SaveSyncIndexAsync(StorageFolder root, IReadOnlyDictionary<string, SyncEntry> index)
        {
            var file = await root.CreateFileAsync(SyncIndexFileName, CreationCollisionOption.ReplaceExisting);
            var entries = index.Values.OrderBy(item => item.RelativePath).ToList();
            await FileIO.WriteTextAsync(file, JsonConvert.SerializeObject(entries, Formatting.Indented));
        }

        public static async Task<IReadOnlyList<string>> EnumerateDestinationFilesAsync(StorageFolder root)
        {
            var results = new List<string>();
            await EnumerateDestinationFilesCoreAsync(root, string.Empty, results);
            return results;
        }

        private static async Task EnumerateDestinationFilesCoreAsync(StorageFolder folder, string relativePrefix, ICollection<string> results)
        {
            foreach (var file in await folder.GetFilesAsync())
            {
                if (string.Equals(file.Name, SyncIndexFileName, StringComparison.OrdinalIgnoreCase) ||
                    file.Name.EndsWith(".partial", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                results.Add(string.IsNullOrWhiteSpace(relativePrefix)
                    ? file.Name
                    : relativePrefix + "/" + file.Name);
            }

            foreach (var childFolder in await folder.GetFoldersAsync())
            {
                var childPrefix = string.IsNullOrWhiteSpace(relativePrefix)
                    ? childFolder.Name
                    : relativePrefix + "/" + childFolder.Name;
                await EnumerateDestinationFilesCoreAsync(childFolder, childPrefix, results);
            }
        }
    }
}
