using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XboxRemoteSync.Common;
using XboxRemoteSync.Models;
using XboxRemoteSync.Services;
using XboxRemoteSync.Utilities;

namespace XboxRemoteSync.ViewModels
{
    public sealed class SyncRunViewModel : BindableBase
    {
        private SyncProfile _selectedProfile;
        private SyncProfile _profile;
        private string _status;
        private int _completedFiles;
        private int _totalFiles;
        private long _bytesTransferred;
        private long _totalBytes;
        private string _currentFile;
        private string _totalProgressText;
        private double _totalProgressPercent;
        private string _previewSummary;
        private string _recentLogText;
        private bool _canStartSync;
        private bool _canRefreshPreview;
        private string _refreshPreviewButtonText;
        private string _startSyncButtonText;
        private string _sourceRootDisplay;
        private string _destinationRootDisplay;
        private bool _isRunning;
        private CancellationTokenSource _cancellationTokenSource;

        public ObservableCollection<SyncProfile> Profiles { get; } = new ObservableCollection<SyncProfile>();
        public ObservableCollection<RunLogEntry> RecentEntries { get; } = new ObservableCollection<RunLogEntry>();
        public ObservableCollection<SyncPreviewItem> PlannedFiles { get; } = new ObservableCollection<SyncPreviewItem>();
        public ObservableCollection<SyncPreviewItem> DestinationFiles { get; } = new ObservableCollection<SyncPreviewItem>();

        public SyncProfile SelectedProfile
        {
            get => _selectedProfile;
            set => SetProperty(ref _selectedProfile, value);
        }

        public SyncProfile Profile
        {
            get => _profile;
            private set => SetProperty(ref _profile, value);
        }

        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        public int CompletedFiles
        {
            get => _completedFiles;
            set => SetProperty(ref _completedFiles, value);
        }

        public int TotalFiles
        {
            get => _totalFiles;
            set => SetProperty(ref _totalFiles, value);
        }

        public long BytesTransferred
        {
            get => _bytesTransferred;
            set => SetProperty(ref _bytesTransferred, value);
        }

        public long TotalBytes
        {
            get => _totalBytes;
            set => SetProperty(ref _totalBytes, value);
        }

        public string CurrentFile
        {
            get => _currentFile;
            set => SetProperty(ref _currentFile, value);
        }

        public string TotalProgressText
        {
            get => _totalProgressText;
            set => SetProperty(ref _totalProgressText, value);
        }

        public double TotalProgressPercent
        {
            get => _totalProgressPercent;
            set => SetProperty(ref _totalProgressPercent, value);
        }

        public string PreviewSummary
        {
            get => _previewSummary;
            set => SetProperty(ref _previewSummary, value);
        }

        public string RecentLogText
        {
            get => _recentLogText;
            set => SetProperty(ref _recentLogText, value);
        }

        public bool CanStartSync
        {
            get => _canStartSync;
            set => SetProperty(ref _canStartSync, value);
        }

        public bool CanRefreshPreview
        {
            get => _canRefreshPreview;
            set => SetProperty(ref _canRefreshPreview, value);
        }

        public string RefreshPreviewButtonText
        {
            get => _refreshPreviewButtonText;
            set => SetProperty(ref _refreshPreviewButtonText, value);
        }

        public string StartSyncButtonText
        {
            get => _startSyncButtonText;
            set => SetProperty(ref _startSyncButtonText, value);
        }

        public string SourceRootDisplay
        {
            get => _sourceRootDisplay;
            set => SetProperty(ref _sourceRootDisplay, value);
        }

        public string DestinationRootDisplay
        {
            get => _destinationRootDisplay;
            set => SetProperty(ref _destinationRootDisplay, value);
        }

        public bool IsRunning
        {
            get => _isRunning;
            private set => SetProperty(ref _isRunning, value);
        }

        public async Task InitializeAsync(SyncProfile profile)
        {
            Profiles.Clear();
            var profiles = (await AppServices.ProfileRepository.LoadAsync()).OrderBy(item => item.Name).ToList();
            foreach (var item in profiles)
            {
                Profiles.Add(item);
            }

            SelectedProfile = profile != null
                ? Profiles.FirstOrDefault(item => item.Id == profile.Id) ?? Profiles.FirstOrDefault()
                : Profiles.FirstOrDefault();

            ApplySelectedProfile(SelectedProfile);
            UpdateProgressText(0, 0);
            RefreshPreviewButtonText = "Plan";
            StartSyncButtonText = "Start Sync";
            await RefreshLogAsync();
        }

        public async Task SelectProfileAsync(SyncProfile profile)
        {
            if (IsRunning)
            {
                return;
            }

            SelectedProfile = profile;
            ApplySelectedProfile(profile);
            await RefreshLogAsync();
        }

        public async Task RunAsync()
        {
            if (Profile == null || IsRunning)
            {
                return;
            }

            var wasCanceled = false;
            _cancellationTokenSource = new CancellationTokenSource();
            IsRunning = true;
            CanStartSync = true;
            CanRefreshPreview = false;
            StartSyncButtonText = "Cancel";
            SyncUiState.SetSyncRunning(true);
            var progress = new Progress<SyncJobProgress>(item =>
            {
                if (!string.IsNullOrWhiteSpace(item.Message))
                {
                    Status = item.Message;
                }

                if (!string.IsNullOrWhiteSpace(item.CurrentFile))
                {
                    CurrentFile = item.CurrentFile;
                }

                if (item.CurrentFileTotalBytes > 0 || item.CurrentFileBytesTransferred > 0)
                {
                    UpdatePlannedFileProgress(item.CurrentFile, item.CurrentFileBytesTransferred, item.CurrentFileTotalBytes);
                }

                if (item.LogEntry != null)
                {
                    RecentEntries.Insert(0, item.LogEntry);
                    while (RecentEntries.Count > 40)
                    {
                        RecentEntries.RemoveAt(RecentEntries.Count - 1);
                    }

                    RebuildRecentLogText();
                }

                if (item.TotalFiles > 0 || item.CompletedFiles > 0)
                {
                    CompletedFiles = item.CompletedFiles;
                    TotalFiles = item.TotalFiles;
                }

                if (item.TotalBytes > 0 || item.BytesTransferred > 0)
                {
                    BytesTransferred = item.BytesTransferred;
                    TotalBytes = item.TotalBytes;
                }

                UpdateProgressText(item.CurrentFileBytesTransferred, item.CurrentFileTotalBytes);
            });

            try
            {
                await AppServices.SyncEngine.RunAsync(Profile, progress, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                wasCanceled = true;
                throw;
            }
            finally
            {
                IsRunning = false;
                StartSyncButtonText = PlannedFiles.Count == 0 ? "No Changes" : "Start Sync";
                RefreshPreviewButtonText = PlannedFiles.Count == 0 ? "Plan" : "Planned";
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                if (wasCanceled)
                {
                    ResetRunState();
                }
                SyncUiState.SetSyncRunning(false);
                await RefreshLogAsync();
            }
        }

        public void Cancel()
        {
            if (_cancellationTokenSource == null)
            {
                return;
            }

            Status = string.Empty;
            _cancellationTokenSource.Cancel();
        }

        public async Task RefreshLogAsync()
        {
            RecentEntries.Clear();
            if (Profile == null)
            {
                return;
            }

            var entries = await AppServices.RunLogRepository.LoadAsync(Profile.Id);
            foreach (var entry in entries.Take(40))
            {
                RecentEntries.Add(entry);
            }

            RebuildRecentLogText();
        }

        public async Task RefreshPreviewAsync()
        {
            PlannedFiles.Clear();
            DestinationFiles.Clear();
            if (Profile == null)
            {
                PreviewSummary = string.Empty;
                TotalFiles = 0;
                TotalBytes = 0;
                BytesTransferred = 0;
                UpdateProgressText(0, 0);
                CanStartSync = false;
                CanRefreshPreview = false;
                RefreshPreviewButtonText = "Plan";
                return;
            }

            if (IsRunning)
            {
                return;
            }

            try
            {
                RefreshPreviewButtonText = "Planning...";
                CanRefreshPreview = false;
                Status = "Building sync plan...";
                var plan = await AppServices.SyncEngine.BuildPlanAsync(Profile, CancellationToken.None);
                foreach (var item in plan.FilesToCopy
                    .Select(entry => new { Entry = entry.Entry, IsPlannedCopy = true })
                    .Concat(plan.FilesToSkip
                        .Where(entry => string.Equals(entry.Reason, "unchanged", StringComparison.OrdinalIgnoreCase))
                        .Select(entry => new { Entry = entry.Entry, IsPlannedCopy = false }))
                    .OrderBy(entry => entry.Entry.RelativePath))
                {
                    PlannedFiles.Add(new SyncPreviewItem
                    {
                        RelativePath = item.Entry.RelativePath,
                        SizeBytes = item.Entry.Size,
                        SizeDisplay = FormatSize(item.Entry.Size),
                        ProgressText = $"0 B / {FormatSize(item.Entry.Size)}",
                        ProgressPercent = 0,
                        IsActive = false,
                        ItemOpacity = item.IsPlannedCopy ? 1 : 0.6,
                        ActionIconOpacity = item.IsPlannedCopy ? 1 : 0
                    });
                }

                var destinationFolder = await FutureAccessTokenValidator.TryGetFolderAsync(Profile.DestinationToken);
                var existingDestinationFiles = destinationFolder == null
                    ? Array.Empty<string>()
                    : await StorageHelpers.EnumerateDestinationFilesAsync(destinationFolder);
                var filesToCopy = new HashSet<string>(
                    plan.FilesToCopy.Select(item => item.Entry.RelativePath),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var relativePath in existingDestinationFiles
                    .Concat(filesToCopy)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
                {
                    var isPlannedCopy = filesToCopy.Contains(relativePath);
                    DestinationFiles.Add(new SyncPreviewItem
                    {
                        RelativePath = relativePath,
                        ItemOpacity = isPlannedCopy ? 1 : 0.6,
                        ActionIconOpacity = isPlannedCopy ? 1 : 0
                    });
                }

                var totalBytes = plan.FilesToCopy.Sum(item => item.Entry.Size);
                TotalFiles = PlannedFiles.Count;
                TotalBytes = totalBytes;
                BytesTransferred = 0;
                PreviewSummary = string.Empty;
                UpdateProgressText(0, 0);
                Status = PlannedFiles.Count == 0 ? "No new or changed files to copy." : string.Empty;
                CanStartSync = PlannedFiles.Count > 0;
                StartSyncButtonText = PlannedFiles.Count == 0 ? "No Changes" : "Start Sync";
                RefreshPreviewButtonText = "Planned";
            }
            catch (Exception ex)
            {
                PreviewSummary = string.Empty;
                Status = ex.Message;
                CanStartSync = false;
                StartSyncButtonText = "Start Sync";
                RefreshPreviewButtonText = "Plan Failed";
            }
            finally
            {
                if (!IsRunning)
                {
                    CanRefreshPreview = true;
                }
            }
        }

        private void UpdateProgressText(long currentFileBytesTransferred, long currentFileTotalBytes)
        {
            TotalProgressText = $"{FormatTransferredSize(BytesTransferred)} / {FormatSize(TotalBytes)} ({TotalFiles} file{(TotalFiles == 1 ? string.Empty : "s")})";
            TotalProgressPercent = CalculatePercent(BytesTransferred, TotalBytes);
        }

        private void UpdatePlannedFileProgress(string relativePath, long bytesTransferred, long totalBytes)
        {
            foreach (var plannedFile in PlannedFiles)
            {
                plannedFile.IsActive = false;
            }

            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return;
            }

            var item = PlannedFiles.FirstOrDefault(entry => string.Equals(entry.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));
            if (item == null)
            {
                return;
            }

            item.IsActive = true;
            item.ProgressPercent = CalculatePercent(bytesTransferred, totalBytes);
            item.ProgressText = $"{FormatTransferredSize(bytesTransferred)} / {FormatSize(totalBytes)}";
        }

        private void ResetPlannedFileProgress()
        {
            foreach (var item in PlannedFiles)
            {
                item.IsActive = false;
                item.ProgressPercent = 0;
                item.ProgressText = $"0 B / {item.SizeDisplay}";
            }
        }

        private static double CalculatePercent(long completed, long total)
        {
            if (total <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, (completed * 100d) / total));
        }

        private static string FormatSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return unitIndex == 0 ? $"{bytes} B" : $"{size:0.##} {units[unitIndex]}";
        }

        private static string FormatTransferredSize(long bytes)
        {
            if (bytes <= 0)
            {
                return "0 B";
            }

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = bytes;
            var unitIndex = 0;
            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            if (unitIndex == 0)
            {
                return $"{bytes} B";
            }

            return $"{Math.Round(size, MidpointRounding.AwayFromZero):0} {units[unitIndex]}";
        }

        private void ResetRunState()
        {
            Status = string.Empty;
            CurrentFile = string.Empty;
            CompletedFiles = 0;
            BytesTransferred = 0;
            UpdateProgressText(0, 0);
            ResetPlannedFileProgress();
        }

        private void ApplySelectedProfile(SyncProfile profile)
        {
            Profile = profile;
            PlannedFiles.Clear();
            DestinationFiles.Clear();
            SourceRootDisplay = profile?.ConnectionDisplay ?? "No source selected";
            DestinationRootDisplay = profile?.DestinationSummary ?? "No destination selected";
            PreviewSummary = string.Empty;
            Status = profile?.LastSummary ?? string.Empty;
            CanStartSync = false;
            CanRefreshPreview = profile != null;
            RefreshPreviewButtonText = "Plan";
            StartSyncButtonText = "Start Sync";
            RecentLogText = string.Empty;
            TotalFiles = 0;
            TotalBytes = 0;
            ResetRunState();
            Status = profile?.LastSummary ?? string.Empty;
        }

        private void RebuildRecentLogText()
        {
            var profileName = string.IsNullOrWhiteSpace(Profile?.Name) ? "No Profile" : Profile.Name;
            RecentLogText = string.Join(Environment.NewLine, RecentEntries
                .Take(40)
                .Select(entry => $"[{entry.TimestampUtc.ToLocalTime():HH:mm:ss}] [{profileName}] {entry.ResultCode} {entry.RelativePath} {entry.Detail}".Trim()));
        }
    }
}
