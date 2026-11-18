namespace Backend.Service;

/// <summary>
/// Sprząta pulpity kursantów:
///  - przy starcie usuwa wszystkie kontenery z poprzedniego uruchomienia backendu (nikt już nie ma do nich ID),
///  - co 30 s usuwa kontenery, które przestały wysyłać heartbeat.
/// </summary>
public class ContainerHeartBeatCleanUpService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly DesktopSessionStore _sessions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ContainerHeartBeatCleanUpService> _logger;

    public ContainerHeartBeatCleanUpService(
        DesktopSessionStore sessions,
        IConfiguration configuration,
        ILogger<ContainerHeartBeatCleanUpService> logger)
    {
        _sessions = sessions;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RemoveOrphanedContainersAsync(stoppingToken);

        var timeout = TimeSpan.FromMinutes(_configuration.GetValue("Desktop:IdleTimeoutMinutes", 2));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CheckInterval, stoppingToken);

                var threshold = DateTime.UtcNow - timeout;
                foreach (var session in _sessions.All.Where(session => session.LastHeartbeat < threshold))
                {
                    if (!_sessions.TryRemove(session)) continue;

                    await DockerCli.RemoveContainerAsync(session.ContainerId, stoppingToken);
                    _logger.LogInformation(
                        "Usunięto nieaktywny pulpit {ContainerName} użytkownika {UserId}",
                        session.ContainerName, session.UserId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Wyjątek w BackgroundService zatrzymałby całą aplikację - logujemy i próbujemy dalej.
                _logger.LogError(exception, "Błąd podczas sprzątania pulpitów.");
            }
        }
    }

    private async Task RemoveOrphanedContainersAsync(CancellationToken cancellationToken)
    {
        try
        {
            var (exitCode, output, error) = await DockerCli.RunAsync(
                ["ps", "-aq", "--filter", "label=naukalinux.desktop=true"], cancellationToken);

            // Kontenery uruchomione starszą wersją backendu nie mają etykiety
            var (legacyExitCode, legacyOutput, _) = await DockerCli.RunAsync(
                ["ps", "-aq", "--filter", "name=^lab-uczen-"], cancellationToken);

            if (exitCode != 0)
            {
                _logger.LogWarning("Nie udało się pobrać listy kontenerów: {DockerError}", error);
                return;
            }

            var ids = output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Concat(legacyExitCode == 0
                    ? legacyOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    : Array.Empty<string>())
                .Distinct()
                .ToList();

            if (ids.Count == 0) return;

            await DockerCli.RunAsync(["rm", "-f", .. ids], cancellationToken);
            _logger.LogInformation("Usunięto {Count} osieroconych pulpitów z poprzedniego uruchomienia.", ids.Count);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Nie udało się posprzątać starych pulpitów (czy docker jest dostępny?).");
        }
    }
}