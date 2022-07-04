using System.Threading.Channels;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace DalleApi;

public class FileWatchBackgroundService : BackgroundService
{
    private readonly ILogger<FileWatchBackgroundService> _logger;
    private readonly string _fileSaveLocation;
    private TimeSpan _delayTime;
    private readonly Channel<CreatedFileEvent> _channel;

    public FileWatchBackgroundService(ILogger<FileWatchBackgroundService> logger, IOptionsMonitor<FileWatcherOptions> optionsMonitor)
    {
        _logger = logger;
        _fileSaveLocation = optionsMonitor.CurrentValue.ImageSaveLocation.AbsolutePath;
        _delayTime = TimeSpan.FromSeconds(optionsMonitor.CurrentValue.FailedLoopDelayInSec);
        _channel = Channel.CreateUnbounded<CreatedFileEvent>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Starting File Watcher Service");
                using var fileSystemWatcher = new FileSystemWatcher(_fileSaveLocation);

                await foreach (var fileCreatedEvent in _channel.Reader.ReadAllAsync(stoppingToken).WithCancellation(stoppingToken))
                {
                    _logger.LogInformation("Processing {File}", fileCreatedEvent);
                    await using (var file = File.Open(fileCreatedEvent.FilePath.AbsolutePath, FileMode.Open))
                    {
                        //save file into image api
                        var image = await Image.LoadAsync(file, stoppingToken);
                        var imageId = Guid.Empty;
                        var dalleEventCreated = new DalleApiImageCreated(fileCreatedEvent.RequestId, imageId, new Uri($"https://image-api/images/{imageId}"));
                        //publish dalle response created event based on file data
                    }
                    
                    _logger.LogInformation("Deleting {File} after processing", fileCreatedEvent.FilePath.AbsolutePath);
                    File.Delete(fileCreatedEvent.FilePath.AbsolutePath);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cancellation requested. Shutting down gracefully");
            }
            catch (Exception ex)
            {
                _delayTime = TimeSpan.FromSeconds(Math.Min(101, _delayTime.TotalSeconds * 2));
                _logger.LogError(ex, "Failure while processing new files. Restarting loop after {Sec}s delay", _delayTime);
                await Task.Delay(_delayTime, stoppingToken);
            }
        }
    }

    private void PublishEventOnFileCreate(object sender, FileSystemEventArgs e)
    {
        var requestId = Guid.Parse(e.FullPath.Split('.')[0]);
        var createdFileEvent = new CreatedFileEvent(requestId, new Uri(e.FullPath));
        if (_channel.Writer.TryWrite(createdFileEvent))
        {
            _logger.LogDebug("Successful file event write");
        }
    }
}

public record FileWatcherOptions(int FailedLoopDelayInSec, int MaxLoopDelay, Uri ImageSaveLocation);

public record DalleApiOptions(Uri ImageApiGrpcSocket);
public record CreatedFileEvent(Guid RequestId, Uri FilePath);

public record DalleApiImageCreated(Guid RequestId,  Guid ImageId, Uri FileLocation);