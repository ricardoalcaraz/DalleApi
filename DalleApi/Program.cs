using System.Diagnostics;
using DalleApi;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<DalleBackgroundService>();
builder.Services
    .AddOptions<RedisOptions>()
    .BindConfiguration("Redis")
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(sp =>
{
    var redisOptions = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var connection = ConnectionMultiplexer.Connect(redisOptions.ConnectionString!);
    return connection;
});
builder.Services.AddTransient(sp => sp.GetRequiredService<ConnectionMultiplexer>().GetDatabase());

var app = builder.Build();
var redisOptions = app.Services.GetRequiredService<IOptions<RedisOptions>>().Value;

var db = app.Services.GetRequiredService<IDatabase>();

var message = db.StreamAdd(redisOptions.StreamName, redisOptions.TestStream, "This is a test"); 
var info = db.StreamInfo(redisOptions.TextPromptStream);
app.Logger.LogInformation("FirstId: {FirstId}, LastId: {LastId}, Length: {Length}", info.FirstEntry.Id, info.LastEntry.Id, info.Length);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();