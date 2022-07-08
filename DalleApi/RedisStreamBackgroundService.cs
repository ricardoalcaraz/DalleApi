using System.Threading.Channels;
using Dalle;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DalleApi;

public class RedisStreamBackgroundService : BackgroundService
{
    private readonly ILogger<RedisStreamBackgroundService> _logger;
    private readonly RedisOptions _redisOptions;
    private CancellationTokenSource _threadCancellationToken;
    private readonly Channel<ImageResponse> _imageChannel;
    
    public RedisStreamBackgroundService(ILogger<RedisStreamBackgroundService> logger, IOptions<RedisOptions> options)
    {
        _logger = logger;
        _imageChannel = Channel.CreateUnbounded<ImageResponse>();
        _threadCancellationToken = new CancellationTokenSource();
        _redisOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _threadCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
        
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var thread = new Thread(ReadImageStream);
                thread.Start();
                await foreach (var image in _imageChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    
                    _logger.LogDebug("Received the following: {Image}", image);
                }
            }
            catch (Exception ex)
            {
                var waitTime = TimeSpan.FromSeconds(5);
                _logger.LogError(ex, "Exception while reading redis stream. Restarting in {Time}s", waitTime.TotalSeconds);
                await Task.Delay(waitTime, stoppingToken);
            }
        }
    }

    private void ReadImageStream()
    {
        try
        {
            while (!_threadCancellationToken.IsCancellationRequested)
            {
                using var multiplexer = ConnectionMultiplexer.Connect(_redisOptions.ConnectionString!);
                var db = multiplexer.GetDatabase();
                var isCreated = db.StreamCreateConsumerGroup(_redisOptions.StreamName, _redisOptions.ConsumerGroupName, StreamPosition.NewMessages);
                var messages = db.StreamReadGroup(_redisOptions.StreamName, _redisOptions.TextPromptStream, "huxley",
                    StreamPosition.NewMessages, count: 1);
           
                if (messages.FirstOrDefault() is { IsNull: false } message)
                {
                    var messageBuffer = message.Values.First(v => v.Name == "Test");
                    //TextRequest.Parser.ParseFrom(image.);
                    _logger.LogInformation("Read image for {Id}", message);
                    _imageChannel.Writer.TryWrite(new(){});
                }
                else
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100), _threadCancellationToken.Token).RunSynchronously();
                }
            }
        }
        catch (TaskCanceledException)
        {
            
        }
        
        
    }

    private async Task GetStreamInfo(IDatabase db)
    {
        var pong = await db.PingAsync();
        _logger.LogInformation("Connected to redis with {Time}ms of latency", pong.TotalMilliseconds);
        //var message = db.StreamAdd(redisOptions.StreamName, redisOptions.TestStream, "This is a test"); 
        var info = db.StreamInfo(_redisOptions.DalleStream);
        _logger.LogInformation("FirstId: {FirstId}, LastId: {LastId}, Length: {Length}", info.FirstEntry.Id, info.LastEntry.Id, info.Length);
    }
}

internal class ImageStub
{
}