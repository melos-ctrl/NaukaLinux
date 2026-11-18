using System.Diagnostics;

namespace Backend.Service;

public static class DockerCli
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        IEnumerable<string> arguments,
        CancellationToken cancellationToken = default)
    {
        var processInfo = new ProcessStartInfo
        {
            FileName = "docker",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            processInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException("Nie udało się uruchomić procesu docker.");

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);

        return (process.ExitCode, (await outputTask).Trim(), (await errorTask).Trim());
    }

    public static Task<(int ExitCode, string Output, string Error)> RemoveContainerAsync(
        string containerId,
        CancellationToken cancellationToken = default) =>
        RunAsync(["rm", "-f", containerId], cancellationToken);
}