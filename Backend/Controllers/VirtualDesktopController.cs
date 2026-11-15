using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Collections.Concurrent;
using System;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VirtualDesktopController : ControllerBase
{
    private static int _currentPort = 3100;
    private readonly ILogger<VirtualDesktopController> _logger;

    public VirtualDesktopController(ILogger<VirtualDesktopController> logger)
    {
        _logger = logger;
    }
    
    public static ConcurrentDictionary<string, DateTime> ActiveContainers { get; } = new();

    private async Task<int> GetNextPortAsync()
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        processInfo.ArgumentList.Add("ps");
        processInfo.ArgumentList.Add("-a");
        processInfo.ArgumentList.Add("--format");
        processInfo.ArgumentList.Add("{{.Names}}");

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            throw new InvalidOperationException("Nie udało się sprawdzić istniejących kontenerów Docker.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string output = await outputTask;
        string error = (await errorTask).Trim();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Nie udało się sprawdzić kontenerów Docker: {error}");
        }

        int highestExistingPort = 3100;
        foreach (string containerName in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            Match match = Regex.Match(containerName.Trim(), "^lab-uczen-(\\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int containerNumber))
            {
                highestExistingPort = Math.Max(highestExistingPort, 3100 + containerNumber);
            }
        }

        while (true)
        {
            int currentPort = Volatile.Read(ref _currentPort);
            int nextPort = Math.Max(currentPort + 1, highestExistingPort + 1);
            if (Interlocked.CompareExchange(ref _currentPort, nextPort, currentPort) == currentPort)
            {
                return nextPort;
            }
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartDesktop()
    {
        int assignedPort;
        try
        {
            assignedPort = await GetNextPortAsync();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Nie udało się przydzielić portu dla pulpitu Docker.");
            return StatusCode(500, "Nie udało się przygotować uruchomienia pulpitu.");
        }

        string containerName = $"lab-uczen-{assignedPort - 3100}";
        const string desktopBindAddress = "127.0.0.1";
        const string desktopUrlHost = "localhost";

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run -d --name {containerName} --hostname kurs --network kursanci " +
                        $"--dns 1.1.1.3 --dns 1.0.0.3 -p {desktopBindAddress}:{assignedPort}:3000 " +
                        "--memory=1g --memory-swap=1g --cpus=1 --pids-limit=2000 --shm-size=512m " +
                        "--cap-drop=NET_RAW --cap-drop=MKNOD " +
                        "--cap-drop=SYS_ADMIN --cap-drop=SYS_MODULE --cap-drop=SYS_BOOT --cap-drop=SYS_TIME " +
                        "-v /var/lib/lxcfs/proc/cpuinfo:/proc/cpuinfo:ro " +
                        "-v /var/lib/lxcfs/proc/diskstats:/proc/diskstats:ro " +
                        "-v /var/lib/lxcfs/proc/meminfo:/proc/meminfo:ro " +
                        "-v /var/lib/lxcfs/proc/stat:/proc/stat:ro " +
                        "-v /var/lib/lxcfs/proc/swaps:/proc/swaps:ro " +
                        "-v /var/lib/lxcfs/proc/uptime:/proc/uptime:ro " +
                        "-v /var/lib/lxcfs/proc/loadavg:/proc/loadavg:ro " +
                        "-v /var/lib/lxcfs/sys/devices/system/cpu/online:/sys/devices/system/cpu/online:ro " +
                        "-e PUID=1000 -e PGID=1000 -e TZ=Europe/Warsaw -e TITLE=\"NaukaLinux\" " +
                        "--rm xubuntu-kurs",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process is null)
        {
            return StatusCode(500, "Nie udało się uruchomić procesu Docker.");
        }

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        string containerId = (await outputTask).Trim();
        string dockerError = (await errorTask).Trim();

        if (process.ExitCode != 0 || string.IsNullOrEmpty(containerId))
        {
            _logger.LogError("docker run failed with exit code {ExitCode}: {DockerError}", process.ExitCode, dockerError);
            return StatusCode(500, "Wystąpił błąd podczas uruchamiania kontenera.");
        }

        ActiveContainers[containerId] = DateTime.UtcNow;

        return Ok(new
        {
            Port = assignedPort,
            ContainerId = containerId,
            ConnectionUrl = $"http://{desktopUrlHost}:{assignedPort}"
        });
    }

    [HttpPost("stop")]
    public IActionResult StopDesktop([FromQuery] string ContainerId)
    {
        if (string.IsNullOrEmpty(ContainerId)) 
            return BadRequest("Brak ID kontenera.");

        if (!Regex.IsMatch(ContainerId, "^[a-fA-F0-9]+$"))
            return BadRequest("Nieprawidłowy format ID kontenera.");

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"rm -f {ContainerId}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(processInfo);
        process?.WaitForExit();
        
        ActiveContainers.TryRemove(ContainerId, out _);
        
        return Ok(new { Message = "Kontener usunięty" });
    }

    [HttpPost("heartbeat")]
    public IActionResult Heartbeat([FromQuery] string ContainerId)
    {
        if (string.IsNullOrEmpty(ContainerId)) 
            return BadRequest();

        if (ActiveContainers.ContainsKey(ContainerId))
        {
            ActiveContainers[ContainerId] = DateTime.UtcNow;
            return Ok();
        }

        return NotFound("Kontener nie istnieje lub już wygasł.");
    }
}