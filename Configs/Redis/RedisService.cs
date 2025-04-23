using StackExchange.Redis;

namespace CP2496H07Group1.Configs.Redis;

public class RedisService
{
    private readonly IConfiguration _configuration;
    private ConnectionMultiplexer _connection;
    private IDatabase _database;

    public RedisService(IConfiguration configuration)
    {
        _configuration = configuration;
        Initialize();
    }
    private void Initialize()       
    {
        var redisConnectionString = _configuration.GetValue<string>("Redis:ConnectionStrings");
        if (redisConnectionString != null) _connection = ConnectionMultiplexer.Connect(redisConnectionString);
        _database = _connection.GetDatabase();
    }

    public IDatabase GetDatabase()
    {
        return _database;
    }

    public void Set(string key, string value, TimeSpan timeSpan)
    {
        _database.StringSet(key, value);
    }

    public string Get(string key)
    {
        return _database.StringGet(key);
    }

    public void Remove(string key)
    {
        _database.KeyDelete(key);
    }

    public void Update(string key, string value)
    {
        _database.StringSet(key, value);
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        var server = _connection.GetServer(_connection.GetEndPoints().First());
        var keys = new List<RedisKey>();
        var cursor = 0L;

        do
        {
            var result = server.Keys(_database.Database, pattern: $"{pattern}");
            foreach (var key in result)
            {
                keys.Add(key);
            }
            cursor = 0;
        }
        while (cursor != 0);

        foreach (var key in keys)
        {
            await _database.KeyDeleteAsync(key);
        }
    }
    
    public void Clear()
    {
        var server =  _connection.GetServer(_connection.GetEndPoints().First());
        server.FlushDatabase();
    }
}