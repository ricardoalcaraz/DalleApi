using System.Diagnostics;
using System.Net.Sockets;
using Dalle;
using DalleProxy;
using DalleProxy.Services;
using Grpc.Net.Client;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Additional configuration is required to successfully run gRPC on macOS.
// For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();


var udsEndPoint = new UnixDomainSocketEndPoint(app.Configuration["DalleGrpcAddress"]);
var connectionFactory = new UnixDomainSocketConnectionFactory(udsEndPoint);
var socketsHttpHandler = new SocketsHttpHandler
{
    ConnectCallback = connectionFactory.ConnectAsync
};
var channel = GrpcChannel.ForAddress("http://localhost", new GrpcChannelOptions
{
    HttpHandler = socketsHttpHandler
});
var stopWatch = new Stopwatch();
stopWatch.Start();

var client = new DalleImageGenerator.DalleImageGeneratorClient(channel);
var publishName = app.Configuration["PublishChannelName"];
var options = ConfigurationOptions.Parse("localhost:6379");
var connection = await ConnectionMultiplexer.ConnectAsync(options);
var db = connection.GetDatabase();
var pong = await db.PingAsync();
await db.PublishAsync(new RedisChannel(publishName, RedisChannel.PatternMode.Auto), new RedisValue("test"),CommandFlags.FireAndForget);


var imageResponse = await client.GetImageAsync(new TextRequest{Prompt = "Dali painting of WALL·E", Num = Random.Shared.Next()});

// Configure the HTTP request pipeline.
app.MapGrpcService<GreeterService>();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
app.Run();

public class UnixDomainSocketConnectionFactory
{
    private readonly System.Net.EndPoint _endPoint;

    public UnixDomainSocketConnectionFactory(System.Net.EndPoint endPoint)
    {
        _endPoint = endPoint;
    }

    public async ValueTask<Stream> ConnectAsync(SocketsHttpConnectionContext _,
        CancellationToken cancellationToken = default)
    {
        var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

        try
        {
            await socket.ConnectAsync(_endPoint, cancellationToken).ConfigureAwait(false);
            return new NetworkStream(socket, true);
        }
        catch
        {
            socket.Dispose();
            throw;
        }
    }
}