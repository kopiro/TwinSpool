namespace XboxRemoteSync.Services
{
    public static class AppServices
    {
        public static IProfileRepository ProfileRepository { get; private set; }

        public static IRunLogRepository RunLogRepository { get; private set; }

        public static CredentialProtector CredentialProtector { get; private set; }

        public static ISyncTransport SyncTransport { get; private set; }

        public static ISyncEngine SyncEngine { get; private set; }

        public static void Initialize()
        {
            CredentialProtector = new CredentialProtector();
            ProfileRepository = new JsonProfileRepository();
            RunLogRepository = new JsonRunLogRepository();
            SyncTransport = new SmbSyncTransport();
            SyncEngine = new SyncEngine(ProfileRepository, RunLogRepository, CredentialProtector, SyncTransport);
        }
    }
}
