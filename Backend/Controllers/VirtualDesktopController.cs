using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Collections.Concurrent;
using System;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VirtualDesktopController : ControllerBase
{
    private static int _currentPort = 8000;
    
    public static ConcurrentDictionary<string, DateTime> ActiveContainers { get; } = new();

    [HttpPost("start")]
    public IActionResult StartDesktop()
    {
        int assignedPort = Interlocked.Increment(ref _currentPort);
        string containerName = $"webtop_lab_{assignedPort}";

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run -d --name {containerName} -p {assignedPort}:3000 --network students_network " +
                        "--shm-size=\"1gb\" --memory=\"1g\" --cpus=\"1.0\" --pids-limit=200 " +
                        "--cap-drop=SYS_ADMIN --cap-drop=SYS_MODULE --cap-drop=SYS_BOOT --cap-drop=SYS_TIME " +
                        "-e PUID=1000 -e PGID=1000 -e TZ=Europe/Warsaw -e TITLE=\"NaukaLinux\" " +
                        "naukalinux-xfce",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        process?.WaitForExit();

        string containerId = process?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(containerId))
        {
            return StatusCode(500, "Wystąpił błąd podczas uruchamiania kontenera.");
        }

        ActiveContainers[containerId] = DateTime.UtcNow;

        return Ok(new
        {
            Port = assignedPort,
            ContainerId = containerId,
            ConnectionUrl = $"http://localhost:{assignedPort}"
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