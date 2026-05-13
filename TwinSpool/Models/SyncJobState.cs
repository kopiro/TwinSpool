namespace TwinSpool.Models
{
    public enum SyncJobState
    {
        Idle,
        Connecting,
        ScanningRemote,
        ScanningLocal,
        Copying,
        Completed,
        Failed,
        Canceled
    }
}
