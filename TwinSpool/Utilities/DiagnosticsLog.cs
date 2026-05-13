using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace TwinSpool.Utilities
{
    public static class DiagnosticsLog
    {
        private const string FileName = "diagnostics.log";

        public static async Task AppendAsync(string context, Exception exception)
        {
            if (exception == null)
            {
                return;
            }

            try
            {
                var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(FileName, CreationCollisionOption.OpenIfExists);
                var entry =
                    $"[{DateTimeOffset.UtcNow:O}] {context}{Environment.NewLine}" +
                    $"{exception}{Environment.NewLine}{Environment.NewLine}";
                await FileIO.AppendTextAsync(file, entry);
            }
            catch
            {
            }
        }
    }
}
