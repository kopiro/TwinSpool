namespace TwinSpool.Models
{
    public sealed class SyncJobProgress
    {
        public SyncJobState State { get; set; }

        public string Message { get; set; }

        public string CurrentFile { get; set; }

        public long CurrentFileBytesTransferred { get; set; }

        public long CurrentFileTotalBytes { get; set; }

        public RunLogEntry LogEntry { get; set; }

        public int CompletedFiles { get; set; }

        public int TotalFiles { get; set; }

        public long BytesTransferred { get; set; }

        public long TotalBytes { get; set; }
    }
}
