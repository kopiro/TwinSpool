using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using XboxRemoteSync.Models;
using XboxRemoteSync.Utilities;

namespace XboxRemoteSync.Services
{
    public sealed class SftpSyncTransport : ISyncTransport
    {
        private SftpClient _client;

        public Task ConnectAsync(SyncProfile profile, string password, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            return ExecuteAsync(() =>
            {
                if (string.IsNullOrWhiteSpace(profile?.Username))
                {
                    throw new TransportException("SFTP requires a username.");
                }

                var port = profile.Port ?? SyncProtocolHelpers.GetDefaultPort(profile.Protocol);
                ReportVerbose(verboseLog, "connect-verbose", $"Connecting to SFTP host '{profile.Server}' on port {port}.");
                _client = new SftpClient(profile.Server, port, profile.Username, password ?? string.Empty);
                _client.Connect();

                if (!_client.IsConnected)
                {
                    throw new TransportException("Unable to connect to SFTP host.");
                }

                ReportVerbose(verboseLog, "connect-verbose", $"Connected to SFTP host '{profile.Server}'.");
            }, cancellationToken, ex =>
            {
                if (ex is TransportException transportException)
                {
                    return transportException;
                }

                return new TransportException($"SFTP connection failed: {ex.Message}", ex);
            });
        }

        public Task<IReadOnlyList<SyncEntry>> EnumerateAsync(SyncProfile profile, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            if (_client == null || !_client.IsConnected)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            return ExecuteAsync(() =>
            {
                var results = new List<SyncEntry>();
                var normalizedRoot = NormalizeRemotePath(profile.RemoteRoot);
                ReportVerbose(verboseLog, "scan-verbose", $"Starting recursive scan from '{normalizedRoot}'.");
                EnumerateDirectory(normalizedRoot, string.Empty, results, cancellationToken, verboseLog);
                ReportVerbose(verboseLog, "scan-verbose", $"Recursive scan found {results.Count} file(s).");
                return (IReadOnlyList<SyncEntry>)results;
            }, cancellationToken, ex =>
            {
                if (ex is TransportException transportException)
                {
                    return transportException;
                }

                return new TransportException($"SFTP enumeration failed: {ex.Message}", ex);
            });
        }

        public Task<Stream> OpenReadAsync(SyncProfile profile, SyncEntry entry, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            if (_client == null || !_client.IsConnected)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var remotePath = CombineRemotePath(profile.RemoteRoot, entry.RelativePath);
            ReportVerbose(verboseLog, "read-verbose", $"Opening remote file '{entry.RelativePath}' for read.");
            return ExecuteAsync(() => (Stream)_client.OpenRead(remotePath), cancellationToken, ex =>
            {
                if (ex is TransportException transportException)
                {
                    return transportException;
                }

                return new TransportException($"Failed to open remote file '{entry.RelativePath}': {ex.Message}", ex);
            });
        }

        public Task DisconnectAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    if (_client?.IsConnected == true)
                    {
                        _client.Disconnect();
                    }
                }
                catch
                {
                }
                finally
                {
                    _client?.Dispose();
                    _client = null;
                }
            });
        }

        private void EnumerateDirectory(string remotePath, string relativeBase, List<SyncEntry> results, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ReportVerbose(verboseLog, "scan-verbose", $"Scanning directory '{remotePath}'.");

            IEnumerable<ISftpFile> items;
            try
            {
                items = _client.ListDirectory(remotePath);
            }
            catch (Exception ex)
            {
                throw new TransportException($"Failed to enumerate '{remotePath}': {ex.Message}", ex);
            }

            foreach (var item in items ?? Enumerable.Empty<ISftpFile>())
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item == null || item.Name == "." || item.Name == "..")
                {
                    continue;
                }

                var relativePath = string.IsNullOrEmpty(relativeBase)
                    ? item.Name
                    : relativeBase + "/" + item.Name;

                if (item.IsDirectory)
                {
                    EnumerateDirectory(item.FullName, relativePath, results, cancellationToken, verboseLog);
                    continue;
                }

                var extension = Path.GetExtension(item.Name) ?? string.Empty;
                results.Add(new SyncEntry
                {
                    RelativePath = relativePath.Replace('\\', '/'),
                    Size = item.Length,
                    ModifiedUtc = item.LastWriteTimeUtc,
                    Extension = extension.ToLowerInvariant()
                });
            }
        }

        private static string NormalizeRemotePath(string remoteRoot)
        {
            if (string.IsNullOrWhiteSpace(remoteRoot) || remoteRoot == "/")
            {
                return "/";
            }

            return "/" + remoteRoot.Trim().Trim('/').Replace('\\', '/');
        }

        private static string CombineRemotePath(string root, string child)
        {
            var left = NormalizeRemotePath(root);
            var right = string.IsNullOrWhiteSpace(child) ? string.Empty : child.Replace('\\', '/').Trim('/');
            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left == "/" ? "/" + right : left + "/" + right;
        }

        private static void ReportVerbose(IProgress<RunLogEntry> verboseLog, string resultCode, string detail)
        {
            verboseLog?.Report(new RunLogEntry
            {
                ResultCode = resultCode,
                Detail = detail
            });
        }

        private static async Task ExecuteAsync(Action action, CancellationToken cancellationToken, Func<Exception, TransportException> mapException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TransportException error = null;
            await Task.Run(() =>
            {
                try
                {
                    action();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error = mapException(ex);
                }
            }, cancellationToken);

            if (error != null)
            {
                throw error;
            }
        }

        private static async Task<T> ExecuteAsync<T>(Func<T> action, CancellationToken cancellationToken, Func<Exception, TransportException> mapException)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TransportException error = null;
            T result = default(T);
            await Task.Run(() =>
            {
                try
                {
                    result = action();
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    error = mapException(ex);
                }
            }, cancellationToken);

            if (error != null)
            {
                throw error;
            }

            return result;
        }
    }
}
