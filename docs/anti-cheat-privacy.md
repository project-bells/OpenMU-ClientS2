# Origins Client Anti-Cheat Privacy Disclosure

Origins Anti-Cheat is a user-mode component embedded in `Main.exe`. It protects game integrity without installing a kernel driver.

It checks the SHA-256 identity of the game executable, the in-memory executable code section, modules loaded into the game process, basic Windows debugger state, game-clock consistency, attack timing, client build identity, and authenticated heartbeat sequence. It does not inspect documents, browser data, messages, keystrokes, or arbitrary files. It does not enumerate or transmit the complete process list. Module evidence sent by heartbeat contains only a stable violation code, never a path or Windows user name.

The launcher and client exchange a random per-launch session key. Heartbeats contain a session identifier, increasing sequence number, client build, risk score, and violation codes authenticated with HMAC-SHA-256. The local guard rejects invalid or replayed heartbeats. Session keys are temporary and are not used as a hardware identifier.

Initial enforcement is limited to logging and disconnecting a client session at high risk. The client never deletes files, terminates other applications, or automatically bans an account. Server administrators must review corroborating server-side evidence before a ban. A hardware identifier is not collected and must never be used as the sole basis for enforcement.

Allowlisted Windows, game, overlay, and graphics modules reduce false positives. Antivirus and supported Windows releases must be tested before each public build. Code signing the launcher and `Main.exe` is strongly recommended.
