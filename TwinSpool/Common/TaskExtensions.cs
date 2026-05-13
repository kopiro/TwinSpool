using System;
using System.Threading.Tasks;

namespace TwinSpool.Common
{
    public static class TaskExtensions
    {
        public static async void FireAndForget(this Task task, Action<Exception> onError = null)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex);
            }
        }
    }
}
