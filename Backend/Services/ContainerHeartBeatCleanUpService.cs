using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Backend.Controllers;

namespace Backend.Service;

public class ContainerHeartBeatCleanUpService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            
            var threshold = DateTime.UtcNow.AddMinutes(-2);

            foreach (var kvp in VirtualDesktopController.ActiveContainers)
            {
                if (kvp.Value < threshold)
                {
                    string containerId = kvp.Key;
                    
                    var processInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = $"rm -f {containerId}",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(processInfo);
                    process?.WaitForExit();

                    VirtualDesktopController.ActiveContainers.TryRemove(containerId, out _);
                }
            }
        }
    }
}