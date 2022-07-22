using System.Diagnostics;

namespace DalleApi;

/// <summary>
/// Dalle running as a python program with the lifetime controlled by this background service class
/// </summary>
public class DalleBackgroundService : BackgroundService
{
    private readonly ILogger<DalleBackgroundService> _logger;

    // use of dependency injection ILogger and IHostApplicationLifetime
    public DalleBackgroundService(ILogger<DalleBackgroundService> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;

        // register to lifetime callbacks
        applicationLifetime.ApplicationStarted.Register(OnStarted);
        applicationLifetime.ApplicationStopped.Register(OnStopped);
        applicationLifetime.ApplicationStopping.Register(OnStopping);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var proc = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "/bin/python",
                Arguments = "/home/ralcaraz/RiderProjects/min-dalle/redis_stream.py --redis-host ''",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        proc.Start();

        while (!stoppingToken.IsCancellationRequested)
        {
            while (await proc.StandardOutput.ReadLineAsync() is { } output)
            {
                _logger.LogInformation("{Out}", output);
            }
            
            _logger.LogInformation("Doing very important stuff...");

            await Task.Delay(TimeSpan.FromMilliseconds(100), stoppingToken);
        }

        try
        {
            _logger.LogInformation("Cancellation was requested, killing python program");
            var timeout = new CancellationTokenSource(100);
            timeout.Token.ThrowIfCancellationRequested();
            await proc.WaitForExitAsync(timeout.Token);
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("Python app failed to stop in time, killing forcefully");
            proc.Kill();
        }
    }

    private void OnStarted()
    {
        _logger.LogInformation($"Executing: {nameof(OnStarted)}, I should get ready to work!");
    }

    private void OnStopping()
    {
        _logger.LogInformation($"Executing: {nameof(OnStopping)}, I should block stopping and clean up!");
    }

    private void OnStopped()
    {
        _logger.LogInformation($"Executing: {nameof(OnStopped)}, I should stop!");
    }
}