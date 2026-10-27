using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;

namespace Origins.Launcher;

internal sealed class GuardPipeServer : IAsyncDisposable
{
    private const string ExpectedBuild = "origins-s6e3-phase9-1";
    private readonly string _pipeName;
    private readonly string _sessionId;
    private readonly byte[] _key;
    private readonly string _logPath;
    private readonly CancellationTokenSource _shutdown = new();
    private long _lastSequence;
    private long _lastValidHeartbeatUnixMilliseconds;
    private bool _disposed;

    public GuardPipeServer(string directory)
    {
        _sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(16));
        _key = RandomNumberGenerator.GetBytes(32);
        _pipeName = $"OriginsGuard-{_sessionId}";
        _logPath = Path.Combine(directory, "OriginsGuard.log");
    }

    public string SessionId => _sessionId;

    public string EncodedKey => Convert.ToBase64String(_key);

    public string PipeName => _pipeName;

    public bool HasReceivedHeartbeat => Interlocked.Read(ref _lastValidHeartbeatUnixMilliseconds) != 0;

    public DateTimeOffset LastValidHeartbeat
    {
        get
        {
            var milliseconds = Interlocked.Read(ref _lastValidHeartbeatUnixMilliseconds);
            return milliseconds == 0 ? DateTimeOffset.MinValue : DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
        }
    }

    public Task RunAsync() => Task.Run(AcceptLoopAsync);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        await _shutdown.CancelAsync();
        _shutdown.Dispose();
        CryptographicOperations.ZeroMemory(_key);
    }

    private async Task AcceptLoopAsync()
    {
        while (!_shutdown.IsCancellationRequested)
        {
            try
            {
                await using var pipe = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await pipe.WaitForConnectionAsync(_shutdown.Token);
                using var reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true);
                while (pipe.IsConnected && !_shutdown.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync(_shutdown.Token);
                    if (line is null) break;
                    Validate(line);
                }
            }
            catch (OperationCanceledException) when (_shutdown.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                Log($"heartbeat-server-error type={exception.GetType().Name}");
                await Task.Delay(1000, _shutdown.Token);
            }
        }
    }

    private void Validate(string line)
    {
        var fields = line.Split('|');
        if (fields.Length != 6 || fields[0] != _sessionId || !long.TryParse(fields[1], out var sequence))
        {
            Log("heartbeat-rejected reason=format");
            return;
        }

        if (fields[2] != ExpectedBuild || !int.TryParse(fields[3], out var risk) || risk is < 0 or > 100)
        {
            Log("heartbeat-rejected reason=build-or-risk");
            return;
        }

        byte[] supplied;
        try
        {
            supplied = Convert.FromHexString(fields[5]);
        }
        catch (FormatException)
        {
            Log("heartbeat-rejected reason=hmac-format");
            return;
        }

        var payload = string.Join('|', fields[..5]);
        var expected = HMACSHA256.HashData(_key, Encoding.UTF8.GetBytes(payload));
        if (!CryptographicOperations.FixedTimeEquals(supplied, expected))
        {
            Log("heartbeat-rejected reason=hmac");
            return;
        }

        if (sequence <= Interlocked.Read(ref _lastSequence))
        {
            Log($"heartbeat-rejected reason=sequence seq={sequence}");
            return;
        }

        Interlocked.Exchange(ref _lastSequence, sequence);
        Interlocked.Exchange(ref _lastValidHeartbeatUnixMilliseconds, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        Log($"heartbeat-ok seq={sequence} build={Safe(fields[2])} risk={Safe(fields[3])} codes={Safe(fields[4])}");
    }

    private static string Safe(string value) => new(value.Where(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-' or ',' or '.').Take(256).ToArray());

    private void Log(string message) => File.AppendAllText(_logPath, $"{DateTimeOffset.Now:u} {message}{Environment.NewLine}");
}
