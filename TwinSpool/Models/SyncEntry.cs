using System;

namespace TwinSpool.Models
{
    public sealed class SyncEntry
    {
        public string RelativePath { get; set; }

        public long Size { get; set; }

        public DateTimeOffset ModifiedUtc { get; set; }

        public string Extension { get; set; }
    }
}
