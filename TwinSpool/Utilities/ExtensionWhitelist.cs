using System;
using System.Collections.Generic;
using System.Linq;

namespace TwinSpool.Utilities
{
    public static class ExtensionWhitelist
    {
        public static readonly IReadOnlyList<string> All = new[]
        {
            ".7z", ".aac", ".aif", ".aiff", ".ape", ".avi", ".bin", ".bmp", ".bz2",
            ".c", ".cb7", ".cbr", ".cbz", ".ccd", ".cfg", ".chd", ".cia", ".cpp", ".cs",
            ".cso", ".csv", ".cue", ".doc", ".docx", ".epub", ".f4v", ".fds", ".flac",
            ".flv", ".gba", ".gb", ".gbc", ".gcm", ".gdi", ".gen", ".gg", ".gif", ".gz",
            ".h", ".heic", ".htm", ".html", ".img", ".ini", ".iso", ".jar", ".java", ".jpeg",
            ".jpg", ".js", ".json", ".log", ".lua", ".m3u", ".m3u8", ".m4a", ".m4v", ".max",
            ".md", ".mdf", ".mds", ".mkv", ".mov", ".mp3", ".mp4", ".mpeg", ".mpg", ".n64",
            ".nds", ".nes", ".nfo", ".nrg", ".odp", ".ods", ".odt", ".ogg", ".oga", ".ogv",
            ".opus", ".pdf", ".pce", ".png", ".ppt", ".pptx", ".ps1", ".py", ".rar", ".rtf",
            ".sfc", ".sh", ".smd", ".smc", ".sms", ".sql", ".sub", ".svg", ".tar", ".tgz",
            ".tif", ".tiff", ".torrent", ".ts", ".txt", ".v64", ".wav", ".webm", ".webp", ".wma",
            ".wmv", ".xhtml", ".xls", ".xlsx", ".xiso", ".xml", ".xz", ".yaml", ".yml", ".z64", ".zip",
            ".zso"
        };

        public static bool IsSupported(string extension)
        {
            return !string.IsNullOrWhiteSpace(extension) &&
                All.Contains(extension.StartsWith(".") ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase);
        }
    }
}
