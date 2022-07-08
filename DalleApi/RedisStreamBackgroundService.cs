using System.Threading.Channels;
using Dalle;
using Google.Protobuf;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace DalleApi;

public class RedisStreamBackgroundService : BackgroundService
{
    private readonly ILogger<RedisStreamBackgroundService> _logger;
    private readonly IDatabase _db;
    private readonly RedisOptions _redisOptions;
    private CancellationTokenSource _threadCancellationToken;
    private readonly Channel<ImageResponse> _imageChannel;
    
    public RedisStreamBackgroundService(ILogger<RedisStreamBackgroundService> logger, IOptions<RedisOptions> options, IDatabase db)
    {
        _logger = logger;
        _db = db;
        _imageChannel = Channel.CreateUnbounded<ImageResponse>();
        _threadCancellationToken = new CancellationTokenSource();
        _redisOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var isCreated = _db.StreamCreateConsumerGroup(_redisOptions.StreamName, _redisOptions.ConsumerGroupName, StreamPosition.NewMessages);
                var messages = await _db.StreamReadGroupAsync(_redisOptions.StreamName, _redisOptions.TextPromptStreamField, "huxley",
                    StreamPosition.NewMessages, count: 1);
            
                if (messages.FirstOrDefault() is { IsNull: false } message && message[_redisOptions.DalleStream].HasValue)
                {
                    byte[] imageResponseBytes = message[_redisOptions.DalleStream]!;
                    var imageResponse = ImageResponse.Parser.ParseFrom(imageResponseBytes);

                    var textPrompt = new TextRequest
                    {
                        Prompt = "",
                        ReferenceId = Guid.NewGuid().ToString(),
                        Seed = 1
                    };
                    await _db.StreamAddAsync(_redisOptions.DalleStream, _redisOptions.TextPromptStreamField, textPrompt.ToByteArray());
                    _logger.LogInformation("Read image for {Id}", message);
                    await _imageChannel.Writer.WriteAsync(new(){});
                }
                else
                {
                    Task.Delay(TimeSpan.FromMilliseconds(100), _threadCancellationToken.Token).RunSynchronously();
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

    private async Task ReadImageStream()
    {
 
        
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