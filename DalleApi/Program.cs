using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ImportantService>();

var app = builder.Build();

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

public class ImportantService : BackgroundService
{
    private readonly ILogger _logger;

    // use of dependency injection ILogger and IHostApplicationLifetime
    public ImportantService(ILogger<ImportantService> logger,
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
                FileName = "/bin/bash",
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