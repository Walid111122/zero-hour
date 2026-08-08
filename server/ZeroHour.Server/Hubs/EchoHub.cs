using Microsoft.AspNetCore.SignalR;

namespace ZeroHour.Server.Hubs;

/// <summary>
/// Proves the realtime path end to end: WebSocket upgrade, MessagePack framing, hub
/// dispatch, and the return trip. It carries no game logic and must not grow any.
/// </summary>
/// <remarks>
/// This exists so that when the first real hub misbehaves in Phase 1, the transport
/// itself is already known-good and the search starts at the gameplay code instead.
/// Standing this up after the gameplay hub would mean debugging both at once.
/// </remarks>
public sealed class EchoHub : Hub
{
    private readonly ILogger<EchoHub> _logger;

    public EchoHub(ILogger<EchoHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns the caller's message with a server timestamp, so a client can measure
    /// round-trip time without a second endpoint.
    /// </summary>
    public EchoReply Echo(string message)
    {
        // Connection ids only. The message body is caller-supplied and could contain
        // anything, so it is deliberately not logged (`24 §6` — no PII in logs).
        _logger.LogInformation(
            "Echo from {ConnectionId} ({Length} chars)",
            Context.ConnectionId,
            message?.Length ?? 0);

        return new EchoReply(
            message ?? string.Empty,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Context.ConnectionId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Hub connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        // A disconnect with an exception is the interesting case — mobile clients drop
        // constantly, and a clean close is unremarkable enough to log at a lower level.
        if (exception is null)
        {
            _logger.LogInformation("Hub disconnected: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogWarning(
                exception,
                "Hub disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}

/// <param name="Message">Echoed back verbatim.</param>
/// <param name="ServerUnixMs">Server clock, for round-trip measurement.</param>
/// <param name="ConnectionId">Lets a client confirm which connection answered.</param>
public sealed record EchoReply(string Message, long ServerUnixMs, string ConnectionId);
