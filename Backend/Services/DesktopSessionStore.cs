using System.Collections.Concurrent;

namespace Backend.Service;

public sealed class DesktopSession
{
    public required string ContainerId { get; init; }
    public required string ContainerName { get; init; }
    public required int UserId { get; init; }
    public required int Slot { get; init; }
    public required int Port { get; init; }
    public required string ConnectionUrl { get; init; }
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}

public sealed class DesktopSessionStore
{
    private readonly ConcurrentDictionary<int, DesktopSession> _sessionsByUser = new();

    /// <summary>Blokada na czas uruchamiania kontenera, żeby dwa żądania nie zajęły tego samego slotu.</summary>
    public SemaphoreSlim StartLock { get; } = new(1, 1);

    public int Count => _sessionsByUser.Count;

    public IReadOnlyCollection<DesktopSession> All => _sessionsByUser.Values.ToList();

    public bool TryGetByUser(int userId, out DesktopSession session) =>
        _sessionsByUser.TryGetValue(userId, out session!);

    public void Add(DesktopSession session) => _sessionsByUser[session.UserId] = session;

    public bool TryRemoveByUser(int userId, out DesktopSession session) =>
        _sessionsByUser.TryRemove(userId, out session!);

    /// <summary>Usuwa wpis tylko wtedy, gdy to nadal ta sama sesja (a nie nowa, uruchomiona w międzyczasie).</summary>
    public bool TryRemove(DesktopSession session) =>
        _sessionsByUser.TryRemove(new KeyValuePair<int, DesktopSession>(session.UserId, session));

    public bool IsSlotTaken(int slot) => _sessionsByUser.Values.Any(session => session.Slot == slot);
}