using System.Text.Json;
using HireIQ.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace HireIQ.Infrastructure.Cache;

public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RedisSettings _settings;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(
        IConnectionMultiplexer redis,
        IOptions<RedisSettings> settings,
        ILogger<RedisCacheService> logger)
    {
        _redis = redis;
        _settings = settings.Value;
        _logger = logger;
    }

    private IDatabase Db => _redis.GetDatabase();
    private string Key(string k) => $"{_settings.InstanceName}{k}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        try
        {
            var value = await Db.StringGetAsync(Key(key));
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!, JsonOpts);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET failed for {Key} — falling back to source", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOpts);
            await Db.StringSetAsync(
                Key(key),
                json,
                ttl ?? TimeSpan.FromSeconds(_settings.DefaultTtlSeconds));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET failed for {Key}", key);
        }
    }

    public Task RemoveAsync(string key, CancellationToken ct = default) =>
        Db.KeyDeleteAsync(Key(key));

    public async Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        await Db.KeyExistsAsync(Key(key));
}
