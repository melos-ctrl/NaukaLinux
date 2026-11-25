using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    private const string DesktopCookieName = "nl_desktop";

    private readonly DesktopSessionStore _sessions;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VirtualDesktopController> _logger;
    private readonly IWebHostEnvironment _env;

    public VirtualDesktopController(
        DesktopSessionStore sessions,
        IConfiguration configuration,
        ILogger<VirtualDesktopController> logger,
        IWebHostEnvironment env)
    {
        _sessions = sessions;
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    private string Image => _configuration["Desktop:Image"] ?? "xubuntu-kurs";
    private string Network => _configuration["Desktop:Network"] ?? "kursanci";
    private int MaxContainers => _configuration.GetValue("Desktop:MaxContainers", 5);
    private int MaxSessionMinutes => _configuration.GetValue("Desktop:MaxSessionMinutes", 60);
    private string ConnectionUrlTemplate => _configuration["Desktop:ConnectionUrlTemplate"] ?? "/desktop/{port}/";

    [HttpGet("current")]
    public IActionResult GetCurrent()
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        return _sessions.TryGetByUser(userId, out var session) && !session.IsExpired
            ? Ok(ToResponse(session))
            : NoContent();
    }

[HttpPost("start")]
public async Task<IActionResult> StartDesktop(CancellationToken cancellationToken)
{
    if (!TryGetUserId(out var userId)) return Unauthorized();

    // Użytkownik ma już pulpit, który nie wygasł - zwracamy go zamiast uruchamiać nowy. 
    if (_sessions.TryGetByUser(userId, out var existing) && !existing.IsExpired)
        return Ok(ToResponse(existing));

    await _sessions.StartLock.WaitAsync(cancellationToken);
    try
    {
        if (_sessions.TryGetByUser(userId, out existing))
        {
            if (!existing.IsExpired)
                return Ok(ToResponse(existing));

            // Sesja minęła, ale sprzątacz jeszcze jej nie usunął
            if (_sessions.TryRemove(existing))
                await DockerCli.RemoveContainerAsync(existing.ContainerId, cancellationToken);
        }

        var slot = Enumerable.Range(1, MaxContainers).FirstOrDefault(s => !_sessions.IsSlotTaken(s));
        if (slot == 0)
        {
            // Najbliższy moment, w którym zwolni się maszyna (najpóźniej po upływie limitu czasu sesji).
            var minutesToFree = _sessions.All.Count == 0
                ? 0
                : Math.Max(1, (int)Math.Ceiling(_sessions.All.Min(s => s.ExpiresAt - DateTime.UtcNow).TotalMinutes));

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                message = $"Witaj! Wszystkie maszyny wirtualne są teraz zajęte :( ({MaxContainers}/{MaxContainers}). " +
                          $"Maszyna może się zwolnić za {minutesToFree} min - spróbuj wtedy ponownie.",
                minutesToFree
            });
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
                "--memory=1g", "--memory-swap=1g", "--cpus=1", "--pids-limit=2000", "--shm-size=512m","--storage-opt", "size=5G",
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
                // Pulpit działa pod ścieżką /desktop/{port}/ - przez nią wystawia go nginx.
                "-e", $"SUBFOLDER=/desktop/{port}/",
                Image
            ];

            var (exitCode, output, error) = await DockerCli.RunAsync(arguments, cancellationToken);
            if (exitCode != 0 || string.IsNullOrEmpty(output))
            {
                _logger.LogError("docker run zakończył się kodem {ExitCode}: {DockerError}", exitCode, error);
                return StatusCode(500, "Wystąpił błąd podczas uruchamiania kontenera.");
            }

            var now = DateTime.UtcNow;
            var session = new DesktopSession
            {
                ContainerId = output,
                ContainerName = containerName,
                UserId = userId,
                Slot = slot,
                Port = port,
                // Losowy token do ciasteczka - nginx pyta /authorize, czy przepuścić ruch do /desktop/{port}/
                AccessToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
                ConnectionUrl = $"/desktop/{port}/",
                StartedAt = now,
                ExpiresAt = now.AddMinutes(MaxSessionMinutes)
            };
            _sessions.Add(session);

            _logger.LogInformation("Uruchomiono pulpit {ContainerName} dla użytkownika {UserId} (do {ExpiresAt:HH:mm} UTC)",
            containerName, userId, session.ExpiresAt);
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

        // 410 Gone = sesja skończyła się, bo minął limit czasu (frontend pokaże osobny komunikat).
        if (session.IsExpired)
            return StatusCode(StatusCodes.Status410Gone,
                $"Minął limit {MaxSessionMinutes} min na sesję. Środowisko zostało wyłączone – możesz uruchomić je ponownie.");

            session.LastHeartbeat = DateTime.UtcNow;
            return Ok(new { session.ExpiresAt });
    }

    [AllowAnonymous]
    [HttpGet("authorize")]
    public IActionResult AuthorizeDesktop()
    {
        var uri = Request.Headers["X-Forwarded-Uri"].ToString();
        var match = Regex.Match(uri, @"^/desktop/(\d+)/");
        if (!match.Success || !int.TryParse(match.Groups[1].Value, out var port))
            return Unauthorized();

        var session = _sessions.FindByPort(port);
        var cookie = Request.Cookies[DesktopCookieName];
        if (session is null || string.IsNullOrEmpty(cookie))
            return Unauthorized();

        return CryptographicOperations.FixedTimeEquals(
                   Encoding.UTF8.GetBytes(cookie),
                   Encoding.UTF8.GetBytes(session.AccessToken))
            ? Ok()
            : Unauthorized();
    }

    private object ToResponse(DesktopSession session)
    {
        Response.Cookies.Append(DesktopCookieName, session.AccessToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = $"/desktop/{session.Port}/",
            MaxAge = TimeSpan.FromHours(4)
        });

        return new
        {
            session.Port,
            session.ContainerId,
            session.ConnectionUrl,
            session.ExpiresAt
        };
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);
}