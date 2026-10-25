# Origins Launcher and Guard

Players start `OriginsLauncher.exe`; starting `main.exe` directly is rejected. The launcher verifies every file listed by the HTTPS manifest with SHA-256, starts the game, then remains hidden as Origins Guard. It checks the configured process-name denylist every 60 seconds. A match closes only the game process launched by this guard. Closing the guard also closes the game.

Publish with `dotnet publish launcher/OriginsLauncher.csproj -c Release`. Put `OriginsLauncher.exe`, `launcher.settings.json`, and `launcher-assets/origins-guard.jpe` in the client directory. Host `manifest.json` and client files below `https://muorigins.online/client/`; never include player-writable configuration or logs in the manifest.
