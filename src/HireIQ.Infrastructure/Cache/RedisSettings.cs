namespace HireIQ.Infrastructure.Cache;

public sealed class RedisSettings
{
    public const string SectionName = "Redis";
    public string ConnectionString { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "hireiq:";
    public int DefaultTtlSeconds { get; set; } = 300;
}
