using System;

namespace TwinSpool.Utilities
{
    public static class SyncProtocolHelpers
    {
        public const string Smb = "SMB";
        public const string Sftp = "SFTP";

        public static string Normalize(string protocol)
        {
            var normalized = protocol?.Trim().ToUpperInvariant();
            return normalized == Sftp ? Sftp : Smb;
        }

        public static bool RequiresShare(string protocol)
        {
            return string.Equals(Normalize(protocol), Smb, StringComparison.Ordinal);
        }

        public static int GetDefaultPort(string protocol)
        {
            return string.Equals(Normalize(protocol), Sftp, StringComparison.Ordinal) ? 22 : 445;
        }
    }
}
