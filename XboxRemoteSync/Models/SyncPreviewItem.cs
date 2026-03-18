using XboxRemoteSync.Common;

namespace XboxRemoteSync.Models
{
    public sealed class SyncPreviewItem : BindableBase
    {
        private string _relativePath;
        private long _sizeBytes;
        private string _sizeDisplay;
        private double _progressPercent;
        private string _progressText;
        private bool _isActive;
        private double _itemOpacity = 1;
        private double _actionIconOpacity = 1;

        public string RelativePath
        {
            get => _relativePath;
            set => SetProperty(ref _relativePath, value);
        }

        public long SizeBytes
        {
            get => _sizeBytes;
            set => SetProperty(ref _sizeBytes, value);
        }

        public string SizeDisplay
        {
            get => _sizeDisplay;
            set => SetProperty(ref _sizeDisplay, value);
        }

        public double ProgressPercent
        {
            get => _progressPercent;
            set => SetProperty(ref _progressPercent, value);
        }

        public string ProgressText
        {
            get => _progressText;
            set => SetProperty(ref _progressText, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public double ItemOpacity
        {
            get => _itemOpacity;
            set => SetProperty(ref _itemOpacity, value);
        }

        public double ActionIconOpacity
        {
            get => _actionIconOpacity;
            set => SetProperty(ref _actionIconOpacity, value);
        }
    }
}
