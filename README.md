# TwinSpool

![TwinSpool app icon](AppIcon.png)

TwinSpool copies files from a remote SMB or SFTP location to a folder you choose on your Xbox console, including USB storage or any other accessible destination.

It is meant for Xbox Dev Mode and keeps repeated syncs fast by copying only files that are new or changed since the last run.

## What it does

- Save reusable sync profiles.
- Connect to SMB or SFTP sources.
- Pick a destination folder on the console or attached storage.
- Preview and run one-way incremental syncs.
- Keep a local history of recent sync runs.

## Install on Xbox Dev Mode

1. Download the latest `TwinSpool-*-x64-devmode.zip` from GitHub Releases.
2. Extract the ZIP on your computer.
3. Boot your Xbox into Dev Mode.
4. Open Xbox Device Portal from your browser.
5. Go to the app install page.
6. Upload the `.msix` file as the main package.
7. Upload the `Dependencies/x64/*.appx` files as dependencies if Device Portal asks for them.
8. Start the install, then launch TwinSpool from Dev Home.
