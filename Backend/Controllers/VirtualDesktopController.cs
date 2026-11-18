using System.Security.Claims;
using Backend.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VirtualDesktopController : ControllerBase
{
    private const int BasePort = 3100;

    private readonly DesktopSessionStore _sessions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VirtualDesktopController> _logger;

    public VirtualDesktopController(
        DesktopSessionStore sessions,
        IConfiguration configuration,
        ILogger<VirtualDesktopController> logger)
    {
        _sessions = sessions;
        _configuration = configuration;
        _logger = logger;
    }

    private string Image => _configuration["Desktop:Image"] ?? "xubuntu-kurs";
    private string Network => _configuration["Desktop:Network"] ?? "kursanci";
    private string PublicHost => _configuration["Desktop:PublicHost"] ?? "localhost";
    private int MaxContainers => _configuration.GetValue("Desktop:MaxContainers", 20);

    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        return _sessions.TryGetByUser(userId, out var session)
            ? Ok(ToResponse(session))
            : NoContent();
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartDesktop(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        // Użytkownik ma już pulpit - oddajemy istniejący zamiast stawiać kolejny.
        if (_sessions.TryGetByUser(userId, out var existing))
            return Ok(ToResponse(existing));

        await _sessions.StartLock.WaitAsync(cancellationToken);
        try
        {
            if (_sessions.TryGetByUser(userId, out existing))
                return Ok(ToResponse(existing));

            var slot = Enumerable.Range(1, MaxContainers).FirstOrDefault(s => !_sessions.IsSlotTaken(s));
            if (slot == 0)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    "Wszystkie środowiska są teraz zajęte. Spróbuj ponownie za kilka minut.");
            }

            var port = BasePort + slot;
            var containerName = $"lab-uczen-{slot}";

            // Pozostałość po poprzednim uruchomieniu o tej samej nazwie - usuwamy, żeby docker run nie wywalił się na konflikcie.
            await DockerCli.RemoveContainerAsync(containerName, cancellationToken);

            List<string> arguments =
            [
                "run", "-d", "--rm",
                "--name", containerName,
                "--hostname", "kurs",
                "--network", Network,
                "--label", "naukalinux.desktop=true",
                "--label", $"naukalinux.user={userId}",
                "--dns", "1.1.1.3", "--dns", "1.0.0.3",
                "-p", $"127.0.0.1:{port}:3000",
                "--memory=1g", "--memory-swap=1g", "--cpus=1", "--pids-limit=2000", "--shm-size=512m",
                "--cap-drop=NET_RAW", "--cap-drop=MKNOD",
                "--cap-drop=SYS_ADMIN", "--cap-drop=SYS_MODULE", "--cap-drop=SYS_BOOT", "--cap-drop=SYS_TIME",
                "-v", "/var/lib/lxcfs/proc/cpuinfo:/proc/cpuinfo:ro",
                "-v", "/var/lib/lxcfs/proc/diskstats:/proc/diskstats:ro",
                "-v", "/var/lib/lxcfs/proc/meminfo:/proc/meminfo:ro",
                "-v", "/var/lib/lxcfs/proc/stat:/proc/stat:ro",
                "-v", "/var/lib/lxcfs/proc/swaps:/proc/swaps:ro",
                "-v", "/var/lib/lxcfs/proc/uptime:/proc/uptime:ro",
                "-v", "/var/lib/lxcfs/proc/loadavg:/proc/loadavg:ro",
                "-v", "/var/lib/lxcfs/sys/devices/system/cpu/online:/sys/devices/system/cpu/online:ro",
                "-e", "PUID=1000", "-e", "PGID=1000", "-e", "TZ=Europe/Warsaw", "-e", "TITLE=NaukaLinux",
                Image
            ];

            var (exitCode, output, error) = await DockerCli.RunAsync(arguments, cancellationToken);
            if (exitCode != 0 || string.IsNullOrEmpty(output))
            {
                _logger.LogError("docker run zakończył się kodem {ExitCode}: {DockerError}", exitCode, error);
                return StatusCode(500, "Wystąpił błąd podczas uruchamiania kontenera.");
            }

            var session = new DesktopSession
            {
                ContainerId = output,
                ContainerName = containerName,
                UserId = userId,
                Slot = slot,
                Port = port,
                ConnectionUrl = $"http://{PublicHost}:{port}"
            };
            _sessions.Add(session);

            _logger.LogInformation("Uruchomiono pulpit {ContainerName} dla użytkownika {UserId}", containerName, userId);
            return Ok(ToResponse(session));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Nie udało się uruchomić pulpitu dla użytkownika {UserId}", userId);
            return StatusCode(500, "Nie udało się przygotować uruchomienia pulpitu.");
        }
        finally
        {
            _sessions.StartLock.Release();
        }
    }

    // ContainerId z query jest ignorowany - użytkownik może zatrzymać wyłącznie swój własny pulpit.
    [HttpPost("stop")]
    public async Task<IActionResult> StopDesktop(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!_sessions.TryRemoveByUser(userId, out var session))
            return Ok(new { Message = "Brak aktywnego kontenera." });

        var (exitCode, _, error) = await DockerCli.RemoveContainerAsync(session.ContainerId, cancellationToken);
        if (exitCode != 0)
            _logger.LogWarning("Nie udało się usunąć kontenera {ContainerName}: {DockerError}", session.ContainerName, error);

        return Ok(new { Message = "Kontener usunięty" });
    }

    [HttpPost("heartbeat")]
    public IActionResult Heartbeat()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        if (!_sessions.TryGetByUser(userId, out var session))
            return NotFound("Kontener nie istnieje lub już wygasł.");

        session.LastHeartbeat = DateTime.UtcNow;
        return Ok();
    }

    private static object ToResponse(DesktopSession session) => new
    {
        session.Port,
        session.ContainerId,
        session.ConnectionUrl
    };

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}