using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SMBLibrary;
using SMBLibrary.Client;
using TwinSpool.Models;
using TwinSpool.Utilities;

namespace TwinSpool.Services
{
    public sealed class SmbSyncTransport : ISyncTransport
    {
        private SMB2Client _client;
        private ISMBFileStore _fileStore;

        public Task ConnectAsync(SyncProfile profile, string password, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _client = new SMB2Client();
                ReportVerbose(verboseLog, "connect-verbose", $"Resolving SMB host '{profile.Server}'.");
                var ipAddress = ResolveAddress(profile.Server);
                ReportVerbose(verboseLog, "connect-verbose", $"Connecting to {ipAddress} over Direct TCP.");
                if (!_client.Connect(ipAddress, SMBTransportType.DirectTCPTransport))
                {
                    throw new TransportException("Unable to connect to SMB host.");
                }

                ReportVerbose(verboseLog, "connect-verbose", $"Authenticating as '{profile.Username ?? string.Empty}'.");
                var status = _client.Login(string.Empty, profile.Username ?? string.Empty, password ?? string.Empty);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new TransportException($"SMB login failed: {status}");
                }

                ReportVerbose(verboseLog, "connect-verbose", $"Connecting to share '{profile.Share}'.");
                _fileStore = _client.TreeConnect(profile.Share, out status);
                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new TransportException($"Unable to connect to share '{profile.Share}': {status}");
                }

                ReportVerbose(verboseLog, "connect-verbose", $"Connected to '{profile.Share}' on '{profile.Server}'.");
                return Task.CompletedTask;
            }
            catch (Exception ex) when (!(ex is TransportException))
            {
                throw new TransportException("SMB connection failed.", ex);
            }
        }

        public Task<IReadOnlyList<SyncEntry>> EnumerateAsync(SyncProfile profile, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            if (_fileStore == null)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            return Task.Run(() =>
            {
                var results = new List<SyncEntry>();
                var normalizedRoot = NormalizeRemotePath(profile.RemoteRoot);
                ReportVerbose(verboseLog, "scan-verbose", $"Starting recursive scan from '{(string.IsNullOrEmpty(normalizedRoot) ? "\\" : normalizedRoot)}'.");
                EnumerateDirectory(normalizedRoot, string.Empty, results, cancellationToken, verboseLog);
                ReportVerbose(verboseLog, "scan-verbose", $"Recursive scan found {results.Count} file(s).");
                return (IReadOnlyList<SyncEntry>)results;
            }, cancellationToken);
        }

        public Task<Stream> OpenReadAsync(SyncProfile profile, SyncEntry entry, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog = null)
        {
            if (_fileStore == null)
            {
                throw new InvalidOperationException("Transport is not connected.");
            }

            cancellationToken.ThrowIfCancellationRequested();
            var remotePath = CombineRemotePath(profile.RemoteRoot, entry.RelativePath);
            ReportVerbose(verboseLog, "read-verbose", $"Opening remote file '{entry.RelativePath}' for read.");
            var fileHandle = default(object);
            FileStatus fileStatus;
            var status = _fileStore.CreateFile(
                out fileHandle,
                out fileStatus,
                remotePath,
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
                SMBLibrary.FileAttributes.Normal,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new TransportException($"Failed to open remote file '{entry.RelativePath}': {status}");
            }

            return Task.FromResult<Stream>(new SmbReadStream(_fileStore, (int)_client.MaxReadSize, fileHandle));
        }

        public Task DisconnectAsync()
        {
            try
            {
                _fileStore?.Disconnect();
            }
            catch
            {
            }

            try
            {
                _client?.Logoff();
                _client?.Disconnect();
            }
            catch
            {
            }

            _fileStore = null;
            _client = null;
            return Task.CompletedTask;
        }

        private void EnumerateDirectory(string remotePath, string relativeBase, List<SyncEntry> results, CancellationToken cancellationToken, IProgress<RunLogEntry> verboseLog)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var displayPath = string.IsNullOrEmpty(remotePath) ? "\\" : remotePath;
            ReportVerbose(verboseLog, "scan-verbose", $"Scanning directory '{displayPath}'.");

            object directoryHandle;
            FileStatus fileStatus;
            var status = _fileStore.CreateFile(
                out directoryHandle,
                out fileStatus,
                remotePath,
                AccessMask.GENERIC_READ,
                SMBLibrary.FileAttributes.Directory,
                ShareAccess.Read | ShareAccess.Write,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                throw new TransportException($"Failed to enumerate '{remotePath}': {status}");
            }

            try
            {
                List<QueryDirectoryFileInformation> items;
                status = _fileStore.QueryDirectory(out items, directoryHandle, "*", FileInformationClass.FileDirectoryInformation);
                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_NO_MORE_FILES)
                {
                    throw new TransportException($"Failed to query directory '{remotePath}': {status}");
                }

                foreach (var item in items?.OfType<FileDirectoryInformation>() ?? Enumerable.Empty<FileDirectoryInformation>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fileName = item.FileName;
                    if (fileName == "." || fileName == "..")
                    {
                        continue;
                    }

                    var relativePath = string.IsNullOrEmpty(relativeBase)
                        ? fileName
                        : relativeBase + "/" + fileName;

                    if ((item.FileAttributes & SMBLibrary.FileAttributes.Directory) == SMBLibrary.FileAttributes.Directory)
                    {
                        EnumerateDirectory(CombineRemotePath(remotePath, fileName), relativePath, results, cancellationToken, verboseLog);
                        continue;
                    }

                    var extension = Path.GetExtension(fileName) ?? string.Empty;
                    results.Add(new SyncEntry
                    {
                        RelativePath = relativePath.Replace('\\', '/'),
                        Size = (long)item.EndOfFile,
                        ModifiedUtc = item.LastWriteTime.ToUniversalTime(),
                        Extension = extension.ToLowerInvariant()
                    });
                }
            }
            finally
            {
                _fileStore.CloseFile(directoryHandle);
            }
        }

        private static IPAddress ResolveAddress(string host)
        {
            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return ipAddress;
            }

            var addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0)
            {
                throw new TransportException($"Unable to resolve host '{host}'.");
            }

            return addresses[0];
        }

        private static string NormalizeRemotePath(string remoteRoot)
        {
            var normalized = string.IsNullOrWhiteSpace(remoteRoot) ? string.Empty : remoteRoot.Replace('/', '\\').Trim('\\');
            return normalized;
        }

        private static string CombineRemotePath(string root, string child)
        {
            var left = NormalizeRemotePath(root);
            var right = string.IsNullOrWhiteSpace(child) ? string.Empty : child.Replace('/', '\\').Trim('\\');
            if (string.IsNullOrEmpty(left))
            {
                return right;
            }

            if (string.IsNullOrEmpty(right))
            {
                return left;
            }

            return left + "\\" + right;
        }

        private static void ReportVerbose(IProgress<RunLogEntry> verboseLog, string resultCode, string detail)
        {
            verboseLog?.Report(new RunLogEntry
            {
                ResultCode = resultCode,
                Detail = detail
            });
        }

        private sealed class SmbReadStream : Stream
        {
            private readonly ISMBFileStore _fileStore;
            private readonly int _maxReadSize;
            private readonly object _handle;
            private long _position;
            private bool _disposed;

            public SmbReadStream(ISMBFileStore fileStore, int maxReadSize, object handle)
            {
                _fileStore = fileStore;
                _maxReadSize = maxReadSize;
                _handle = handle;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => _position;
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(nameof(SmbReadStream));
                }

                byte[] data;
                var status = _fileStore.ReadFile(out data, _handle, _position, Math.Min(count, _maxReadSize));
                if (status == NTStatus.STATUS_END_OF_FILE || data == null || data.Length == 0)
                {
                    return 0;
                }

                if (status != NTStatus.STATUS_SUCCESS)
                {
                    throw new TransportException($"Remote read failed: {status}");
                }

                Array.Copy(data, 0, buffer, offset, data.Length);
                _position += data.Length;
                return data.Length;
            }

            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

            public override void SetLength(long value) => throw new NotSupportedException();

            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

            protected override void Dispose(bool disposing)
            {
                if (!_disposed)
                {
                    _fileStore.CloseFile(_handle);
                    _disposed = true;
                }

                base.Dispose(disposing);
            }
        }
    }
}
