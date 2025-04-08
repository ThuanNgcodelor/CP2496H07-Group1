using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;

namespace CP2496H07Group1.Services.Package;

public class UserInsurance :IUserInsurance
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;

    public UserInsurance(AppDataContext context, RedisService redis)
    {
        _context = context;
        _redis = redis;
    }
}