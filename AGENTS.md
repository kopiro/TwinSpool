# AGENTS.md

TwinSpool is a UWP/XAML C# app for Xbox Dev Mode.

- Solution: `TwinSpool.sln`
- Project: `TwinSpool/TwinSpool.csproj`
- App manifest: `TwinSpool/Package.appxmanifest`
- Namespace: `TwinSpool`
- Sync transports: SMB and SFTP under `TwinSpool/Services`
- Build check: MSBuild `TwinSpool.sln` with `Configuration=Release` and `Platform=x64`
- Release workflow: tags `v*` build Xbox Dev Mode ZIPs and require `TWIN_SPOOL_PFX_BASE64` plus `TWIN_SPOOL_PFX_PASSWORD` secrets.
- Generated outputs (`bin`, `obj`, `AppPackages`, `.vs`) are ignored and should not be committed.
