# Xbox Remote Sync Utility

Sideloaded Xbox UWP utility for one-way incremental sync from SMB or SFTP sources to a user-selected USB folder.

## What's included

- UWP/XAML app shell with `Windows.Universal` and `Windows.Xbox` targeting
- Manifest-declared removable storage access and a broad fixed extension whitelist for common archives, media, documents, disk images, and ROM/content files
- Controller-first flow for profiles, editing SMB or SFTP settings, choosing a USB destination, and running syncs
- Sync core built around `ISyncTransport`, `SmbSyncTransport`, `SftpSyncTransport`, `SyncProfile`, `SyncEntry`, `SyncPlan`, and `SyncJobState`
- Local JSON persistence for profiles and bounded run logs
- Password storage through `PasswordVault` with encrypted app-local fallback

## Incremental sync note

UWP removable storage APIs on Xbox do not provide a clean way to preserve remote last-write timestamps during copy. To keep v1 incremental sync reliable without unsupported filesystem tricks, the app writes a small `.xboxremotesync-index.json` file into the selected destination root and compares remote entries against that stored size/timestamp index on later runs.

## Build

Open [XboxRemoteSync.sln](C:\Users\deste\Documents\XBOXRemoteSync\XboxRemoteSync.sln) in Visual Studio 2022 with the UWP workload installed, restore NuGet packages, and deploy in Xbox Dev Mode.

## Known follow-up items

- Validate the exact `SMBLibrary` API surface against the chosen package version in Visual Studio and adjust if needed
- Replace placeholder asset images in `Assets/`
- Run on hardware to verify folder picker behavior and Xbox controller focus
