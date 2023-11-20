using Redis.OM.Contracts;
using Redis.OM;
using StackExchange.Redis;
using StackExchange.Redis.Extensions.Core.Configuration;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(
        options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Get the "redis" object from appsettings.json
var redisConfiguration = builder.Configuration.GetSection("redis").Get<RedisConfiguration>()!;

var connectionMultiplexer = ConnectionMultiplexer.Connect(redisConfiguration.ConfigurationOptions);

builder.Services.AddSingleton<IConnectionMultiplexer>(connectionMultiplexer);

builder.Services.AddSingleton<IRedisConnectionProvider>(new RedisConnectionProvider(connectionMultiplexer));

builder.Services.AddSingleton(_ =>
{
    return connectionMultiplexer.GetDatabase(redisConfiguration.Database);
});

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
