# Origins Launcher and Guard

Players start `OriginsLauncher.exe`; starting `Main.exe` directly is rejected. The launcher verifies every file listed by the HTTPS manifest with SHA-256 and then waits for the player to press **START GAME**. **SETTINGS** edits and saves the manifest URL. After launch it remains hidden as Origins Guard and displays a shield icon in the notification area. It checks the configured process-name denylist every 60 seconds. A match closes only the game process launched by this guard. Closing the guard also closes the game. Guard start, detection, and stop events are written to `OriginsGuard.log`.

If the manifest endpoint returns HTTP 404 while the update service is not deployed yet, the launcher uses the existing local client and continues to the guard. Other HTTP failures remain visible errors so a broken update cannot silently install partial files.

Publish with `dotnet publish launcher/OriginsLauncher.csproj -c Release`. The splash image is embedded in `OriginsLauncher.exe`; put the executable next to `Main.exe`. The launcher creates `launcher.settings.json` when settings are saved. Host `manifest.json` and client files below `https://muorigins.online/client/`; never include player-writable configuration or logs in the manifest.
