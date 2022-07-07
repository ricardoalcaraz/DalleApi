using StackExchange.Redis;

namespace DalleApi;

public class RedisStreamBackgroundService : BackgroundService
{
    private readonly ILogger<RedisStreamBackgroundService> _logger;

    public RedisStreamBackgroundService(ILogger<RedisStreamBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var connection = await ConnectionMultiplexer.ConnectAsync(options);
        var db = connection.GetDatabase();
        var pong = await db.PingAsync();
        await db.PublishAsync(new RedisChannel(publishName, RedisChannel.PatternMode.Auto), new RedisValue("test"),CommandFlags.FireAndForget);

    }
}