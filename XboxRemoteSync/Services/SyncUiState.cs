using System;

namespace XboxRemoteSync.Services
{
    public static class SyncUiState
    {
        private static bool _isSyncRunning;

        public static event EventHandler<bool> SyncRunningChanged;

        public static bool IsSyncRunning => _isSyncRunning;

        public static void SetSyncRunning(bool isRunning)
        {
            if (_isSyncRunning == isRunning)
            {
                return;
            }

            _isSyncRunning = isRunning;
            SyncRunningChanged?.Invoke(null, _isSyncRunning);
        }
    }
}
