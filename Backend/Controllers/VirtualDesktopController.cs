using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Controllers;

[ApiController]
[Route("api/[controller]")]

public class VirtualDesktopController : ControllerBase
{
    private static int _currentPort = 8000;

    [HttpPost("start")]
    public IActionResult startDesktop()
    {
        int assignedPort = _currentPort++;
        string containerName = $"webtop_lab_{assignedPort}";

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments =
                $"run -d --name {containerName} -p {assignedPort}:3000 --shm-size=\"1gb\" -e PUID=1000 -e PGID=1000 -e TZ=Europe/Warsaw lscr.io/linuxserver/webtop:ubuntu-mate",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        process.WaitForExit();

        string containerId = process?.StandardOutput.ReadToEnd().Trim() ?? string.Empty;

        return Ok(new
        {
            Message = "KasmVNC is ready",
            Port = assignedPort,
            ContainerId = containerId,
            ConnectionUrl = $"http://localhost:{assignedPort}"
        });
    }


    [HttpPost("stop")]
    public IActionResult stopDesktop(string ContainerId)
    {
        if (string.IsNullOrEmpty(ContainerId)) return BadRequest("No matching ID container ");

        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"rm -f {ContainerId}",
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        using var process = Process.Start(processInfo);
        process?.WaitForExit();
        return Ok(new
        {
            Message = "Container is deleted sucesfull"
        });

        
    }
}