using System.Diagnostics;
using DalleApi;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<DalleBackgroundService>();
builder.Services
    .AddOptions<ConfigurationOptions>()
    .Configure(opt =>
    {
        var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
        ConfigurationOptions.Parse(redisConnectionString);

    })
    .BindConfiguration("ConnectionStrings:Redis");

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