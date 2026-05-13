using System;

namespace TwinSpool.Models
{
    public sealed class RunLogEntry
    {
        public string ProfileId { get; set; }

        public string RelativePath { get; set; }

        public string ResultCode { get; set; }

        public string Detail { get; set; }

        public DateTimeOffset TimestampUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
