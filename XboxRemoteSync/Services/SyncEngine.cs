using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using XboxRemoteSync.Models;
using XboxRemoteSync.Utilities;

namespace XboxRemoteSync.Services
{
    public sealed class SyncEngine : ISyncEngine
    {
        private readonly IProfileRepository _profileRepository;
        private readonly IRunLogRepository _runLogRepository;
        private readonly CredentialProtector _credentialProtector;
        private readonly ISyncTransport _transport;

        public SyncEngine(
            IProfileRepository profileRepository,
            IRunLogRepository runLogRepository,
            CredentialProtector credentialProtector,
            ISyncTransport transport)
        {
            _profileRepository = profileRepository;
            _runLogRepository = runLogRepository;
            _credentialProtector = credentialProtector;
            _transport = transport;
        }

        public async Task<SyncPlan> BuildPlanAsync(SyncProfile profile, CancellationToken cancellationToken)
        {
            var password = await _credentialProtector.RetrieveAsync(profile.CredentialKey);
            var destination = await FutureAccessTokenValidator.TryGetFolderAsync(profile.DestinationToken);
            if (destination == null)
            {
                throw new InvalidOperationException("Destination folder access is no longer valid. Re-select the USB folder.");
            }

            await _transport.ConnectAsync(profile, password, cancellationToken);
            try
            {
                var syncIndex = await StorageHelpers.LoadSyncIndexAsync(destination);
                var remoteEntries = await _transport.EnumerateAsync(profile, cancellationToken);
                var plan = new SyncPlan();

                foreach (var entry in remoteEntries.OrderBy(item => item.RelativePath))
                {
                    if (!profile.EnabledExtensions.Contains(entry.Extension, StringComparer.OrdinalIgnoreCase))
                    {
                        plan.FilesToSkip.Add(new SyncPlanItem { Entry = entry, Reason = "skipped-extension" });
                        continue;
                    }

                    if (StorageHelpers.FileNeedsUpdate(syncIndex, entry))
                    {
                        plan.FilesToCopy.Add(new SyncPlanItem
                        {
                            Entry = entry,
                            Reason = "copy"
                        });
                    }
                    else
                    {
                        plan.FilesToSkip.Add(new SyncPlanItem { Entry = entry, Reason = "unchanged" });
                    }
                }

                return plan;
            }
            finally
            {
                await _transport.DisconnectAsync();
            }
        }

        public async Task<string> RunAsync(SyncProfile profile, IProgress<SyncJobProgress> progress, CancellationToken cancellationToken)
        {
            var logEntries = new List<RunLogEntry>();
            var destination = await FutureAccessTokenValidator.TryGetFolderAsync(profile.DestinationToken);
            if (destination == null)
            {
                throw new InvalidOperationException("Destination folder access is no longer valid. Re-select the USB folder.");
            }

            var password = await _credentialProtector.RetrieveAsync(profile.CredentialKey);
            var verboseLog = new Progress<RunLogEntry>(entry =>
            {
                if (entry == null)
                {
                    return;
                }

                entry.ProfileId = profile.Id;
                progress?.Report(new SyncJobProgress
                {
                    State = SyncJobState.Copying,
                    Message = entry.Detail,
                    LogEntry = entry,
                    CompletedFiles = 0,
                    TotalFiles = 0,
                    BytesTransferred = 0,
                    TotalBytes = 0
                });
            });

            progress?.Report(new SyncJobProgress { State = SyncJobState.Connecting, Message = "Connecting to SMB share..." });
            EmitLog(logEntries, progress, profile.Id, "sync-verbose", "Preparing sync job.");
            await _transport.ConnectAsync(profile, password, cancellationToken, verboseLog);

            try
            {
                progress?.Report(new SyncJobProgress { State = SyncJobState.ScanningRemote, Message = "Scanning remote content..." });
                var syncIndex = await StorageHelpers.LoadSyncIndexAsync(destination);
                EmitLog(logEntries, progress, profile.Id, "sync-verbose", "Loaded local sync index.");
                var plan = await BuildPlanWithoutReconnectAsync(profile, syncIndex, cancellationToken, verboseLog, progress);
                var totalBytes = plan.FilesToCopy.Sum(item => item.Entry.Size);
                var copiedBytes = 0L;
                var completedFiles = 0;

                foreach (var skipped in plan.FilesToSkip)
                {
                    EmitLog(logEntries, progress, profile.Id, skipped.Reason, skipped.Entry.Extension, skipped.Entry.RelativePath);
                }

                progress?.Report(new SyncJobProgress
                {
                    State = SyncJobState.Copying,
                    Message = $"Copying {plan.FilesToCopy.Count} file(s)...",
                    CurrentFile = plan.FilesToCopy.Count > 0 ? "Waiting to start first file..." : "No files need copying.",
                    CurrentFileBytesTransferred = 0,
                    CurrentFileTotalBytes = 0,
                    TotalFiles = plan.FilesToCopy.Count,
                    TotalBytes = totalBytes
                });

                foreach (var item in plan.FilesToCopy)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        EmitLog(logEntries, progress, profile.Id, "copy-verbose", $"Copying '{item.Entry.RelativePath}' ({item.Entry.Size} bytes).", item.Entry.RelativePath);
                        progress?.Report(new SyncJobProgress
                        {
                            State = SyncJobState.Copying,
                            Message = $"Copying {plan.FilesToCopy.Count} file(s)...",
                            CurrentFile = item.Entry.RelativePath,
                            CurrentFileBytesTransferred = 0,
                            CurrentFileTotalBytes = item.Entry.Size,
                            CompletedFiles = completedFiles,
                            TotalFiles = plan.FilesToCopy.Count,
                            BytesTransferred = copiedBytes,
                            TotalBytes = totalBytes
                        });
                        using (var remoteStream = await _transport.OpenReadAsync(profile, item.Entry, cancellationToken, verboseLog))
                        {
                            await StorageHelpers.CopyToFileAtomicallyAsync(
                                remoteStream,
                                destination,
                                item.Entry.RelativePath,
                                cancellationToken,
                                bytesCopiedForFile =>
                                {
                                    progress?.Report(new SyncJobProgress
                                    {
                                        State = SyncJobState.Copying,
                                        Message = $"Copying {plan.FilesToCopy.Count} file(s)...",
                                        CurrentFile = item.Entry.RelativePath,
                                        CurrentFileBytesTransferred = bytesCopiedForFile,
                                        CurrentFileTotalBytes = item.Entry.Size,
                                        CompletedFiles = completedFiles,
                                        TotalFiles = plan.FilesToCopy.Count,
                                        BytesTransferred = copiedBytes + bytesCopiedForFile,
                                        TotalBytes = totalBytes
                                    });

                                    return Task.CompletedTask;
                                });
                        }

                        syncIndex[item.Entry.RelativePath] = item.Entry;
                        copiedBytes += item.Entry.Size;
                        completedFiles++;

                        EmitLog(logEntries, progress, profile.Id, "copied", $"{item.Entry.Size} bytes", item.Entry.RelativePath);

                        progress?.Report(new SyncJobProgress
                        {
                            State = SyncJobState.Copying,
                            Message = item.Entry.RelativePath,
                            CurrentFile = item.Entry.RelativePath,
                            CurrentFileBytesTransferred = item.Entry.Size,
                            CurrentFileTotalBytes = item.Entry.Size,
                            CompletedFiles = completedFiles,
                            TotalFiles = plan.FilesToCopy.Count,
                            BytesTransferred = copiedBytes,
                            TotalBytes = totalBytes
                        });
                    }
                    catch (Exception ex)
                    {
                        EmitLog(logEntries, progress, profile.Id, "failed-write", ex.Message, item.Entry.RelativePath);
                        throw;
                    }
                }

                await StorageHelpers.SaveSyncIndexAsync(destination, syncIndex);
                var summary = $"Copied {completedFiles} file(s), skipped {plan.FilesToSkip.Count}, total {plan.FilesToCopy.Count + plan.FilesToSkip.Count}.";
                EmitLog(logEntries, progress, profile.Id, "sync-verbose", "Saved local sync index.");
                profile.LastRunUtc = DateTimeOffset.UtcNow;
                profile.LastSummary = summary;
                await PersistProfileSummaryAsync(profile);
                await _runLogRepository.AppendAsync(logEntries);
                progress?.Report(new SyncJobProgress
                {
                    State = SyncJobState.Completed,
                    Message = summary,
                    CurrentFile = "No file in progress.",
                    CurrentFileBytesTransferred = 0,
                    CurrentFileTotalBytes = 0,
                    CompletedFiles = completedFiles,
                    TotalFiles = plan.FilesToCopy.Count,
                    BytesTransferred = copiedBytes,
                    TotalBytes = totalBytes
                });

                return summary;
            }
            catch (OperationCanceledException)
            {
                EmitLog(logEntries, progress, profile.Id, "canceled", "The sync operation was canceled.");
                await _runLogRepository.AppendAsync(logEntries);
                throw;
            }
            finally
            {
                await _transport.DisconnectAsync();
            }
        }

        private async Task<SyncPlan> BuildPlanWithoutReconnectAsync(
            SyncProfile profile,
            IReadOnlyDictionary<string, SyncEntry> syncIndex,
            CancellationToken cancellationToken,
            IProgress<RunLogEntry> verboseLog,
            IProgress<SyncJobProgress> progress)
        {
            var remoteEntries = await _transport.EnumerateAsync(profile, cancellationToken, verboseLog);
            EmitLog(null, progress, profile.Id, "sync-verbose", $"Planning {remoteEntries.Count} remote file(s).");
            var plan = new SyncPlan();

            foreach (var entry in remoteEntries.OrderBy(item => item.RelativePath))
            {
                if (!profile.EnabledExtensions.Contains(entry.Extension, StringComparer.OrdinalIgnoreCase))
                {
                    plan.FilesToSkip.Add(new SyncPlanItem { Entry = entry, Reason = "skipped-extension" });
                    continue;
                }

                if (StorageHelpers.FileNeedsUpdate(syncIndex, entry))
                {
                    plan.FilesToCopy.Add(new SyncPlanItem { Entry = entry, Reason = "copy" });
                }
                else
                {
                    plan.FilesToSkip.Add(new SyncPlanItem { Entry = entry, Reason = "unchanged" });
                }
            }

            return plan;
        }

        private static void EmitLog(
            List<RunLogEntry> logEntries,
            IProgress<SyncJobProgress> progress,
            string profileId,
            string resultCode,
            string detail,
            string relativePath = "")
        {
            var entry = new RunLogEntry
            {
                ProfileId = profileId,
                RelativePath = relativePath,
                ResultCode = resultCode,
                Detail = detail
            };

            logEntries?.Add(entry);
            progress?.Report(new SyncJobProgress
            {
                LogEntry = entry
            });
        }

        private async Task PersistProfileSummaryAsync(SyncProfile profile)
        {
            var profiles = (await _profileRepository.LoadAsync()).ToList();
            var existing = profiles.FirstOrDefault(item => item.Id == profile.Id);
            if (existing == null)
            {
                return;
            }

            existing.LastRunUtc = profile.LastRunUtc;
            existing.LastSummary = profile.LastSummary;
            await _profileRepository.SaveAsync(profiles);
        }
    }
}
