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
        var proc = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "/bin/python",
                Arguments = "",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Doing very important stuff...");

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }

        _logger.LogInformation("Oops, cancellation was requested!");
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