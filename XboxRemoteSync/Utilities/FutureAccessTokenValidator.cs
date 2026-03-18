using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace XboxRemoteSync.Utilities
{
    public static class FutureAccessTokenValidator
    {
        public static async Task<StorageFolder> TryGetFolderAsync(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            try
            {
                return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
            }
            catch
            {
                return null;
            }
        }
    }
}
