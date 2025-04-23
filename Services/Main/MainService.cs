using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Configs.Redis;
using CP2496H07Group1.Dtos;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using X.PagedList;
using X.PagedList.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace CP2496H07Group1.Services.Main;

public class MainService : IMainService
{
    private readonly AppDataContext _context;
    private readonly RedisService _redis;

    public MainService(AppDataContext context, RedisService redisService)
    {
        _context = context;
        _redis = redisService;
    }

    public IPagedList<Admin> GetAllAdmins(int page, int pageSize, string? keyword)
    {
        try
        {
            string cacheKey = $"Admin:Page:{page}:size:{pageSize}:keyword:{keyword ?? ""}";
            var cacheResult = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheResult))
            {
                var admins = JsonSerializer.Deserialize<List<Admin>>(cacheResult,
                    new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    });
                if (admins != null)
                    return new StaticPagedList<Admin>(
                        admins.Skip((page - 1) * pageSize).Take(pageSize),
                        page,
                        pageSize,
                        admins.Count);
            }

            var query = _context.Admins
                .Include(r => r.Roles)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.Username.Contains(keyword) || p.Email.Contains(keyword));
            }

            var allAdmin = query.OrderByDescending(i => i.Id).ToList();
            var result = new StaticPagedList<Admin>(
                allAdmin.Skip((page - 1) * pageSize).Take(pageSize),
                page,
                pageSize,
                allAdmin.Count);

            string json = JsonSerializer.Serialize(allAdmin, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
            _redis.Set(cacheKey, json, TimeSpan.FromDays(30));
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving admins: " + ex.Message);
        }
    }

    public async Task<Admin?> FindAdminById(long id)
    {
        try
        {
            var cacheKey = $"Admin_{id}";
            var cacheAdmin = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheAdmin))
            {
                return JsonConvert.DeserializeObject<Admin>(cacheAdmin);
            }

            var admin = await _context.Admins
                .Include(r => r.Roles)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (admin != null)
            {
                var adminJson = JsonConvert.SerializeObject(admin, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, adminJson, TimeSpan.FromDays(30));
            }

            return admin;
        }
        catch (Exception ex)
        {
            throw new Exception("Error finding admin: " + ex.Message);
        }
    }

    public async Task<Admin?> UpdateAdmin(Admin model, long id, string? newPassword)
    {
        try
        {
            var existingAdmin = await _context.Admins.Include(a => a.Roles).FirstOrDefaultAsync(a => a.Id == id);
            if (existingAdmin == null)
                throw new Exception("Admin not found");

            existingAdmin.Username = model.Username;
            existingAdmin.Email = model.Email;
            existingAdmin.Description = model.Description;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existingAdmin.Password = HashPassword(newPassword);
            }

            if (model.Roles != null && model.Roles.Count > 0)
            {
                existingAdmin.Roles = model.Roles;
            }

            await _context.SaveChangesAsync();

            var cacheKey = $"Admin_{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Admin:Page:*");
            var adminJson = JsonConvert.SerializeObject(existingAdmin, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, adminJson, TimeSpan.FromDays(30));

            return existingAdmin;
        }
        catch (Exception ex)
        {
            throw new Exception("Failed to update admin: " + ex.Message);
        }
    }

    public async Task<Admin?> AddAdmin(Admin model)
    {
        try
        {
            model.Password = HashPassword(model.Password);

            await _context.Admins.AddAsync(model);
            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Admin");
            if (userRole == null)
            {
                userRole = new Role
                {
                    RoleName = "Admin",
                    Description = "Admin",
                    Status = true
                };
                _context.Roles.Add(userRole);
                await _context.SaveChangesAsync();
            }

            model.Roles ??= new List<Role>();
            model.Roles.Add(userRole);
            await _context.SaveChangesAsync();

            await _redis.RemoveByPatternAsync("Admin:Page:*");
            var cacheKey = $"Admin_{model.Id}";
            var adminJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, adminJson, TimeSpan.FromDays(30));

            return model;
        }
        catch (Exception ex)
        {
            throw new Exception("Error adding admin: " + ex.Message);
        }
    }

    public async Task DeleteAdmin(long id)
    {
        try
        {
            var admin = await _context.Admins.FindAsync(id);
            if (admin == null) throw new Exception("Admin not found");

            _context.Admins.Remove(admin);
            await _context.SaveChangesAsync();

            var cacheKey = $"Admin_{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Admin:Page:*");
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting admin: " + ex.Message);
        }
    }

    public IPagedList<User> GetAllUsers(int page, int pageSize, string? keyword)
    {
        try
        {
            string cacheKey = $"User:Page:{page}:size:{pageSize}:keyword:{keyword ?? ""}";
            var cacheResult = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheResult))
            {
                var users = JsonSerializer.Deserialize<List<User>>(cacheResult,
                    new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    });
                if (users != null)
                    return new StaticPagedList<User>(
                        users.Skip((page - 1) * pageSize).Take(pageSize),
                        page,
                        pageSize,
                        users.Count);
            }

            var query = _context.Users
                .Include(r => r.Roles)
                .AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p =>
                    p.FirstName.Contains(keyword) || p.Email.Contains(keyword) || p.PhoneNumber.Contains(keyword) ||
                    p.LastName.Contains(keyword));
            }

            var allUsers = query.OrderByDescending(i => i.Id).ToList();
            var result = new StaticPagedList<User>(
                allUsers.Skip((page - 1) * pageSize).Take(pageSize),
                page,
                pageSize,
                allUsers.Count);

            string json = JsonSerializer.Serialize(allUsers, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
            _redis.Set(cacheKey, json, TimeSpan.FromDays(30));
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving users: " + ex.Message);
        }
    }

    public async Task<User?> FindUserById(long id)
    {
        try
        {
            var cacheKey = $"User_{id}";
            var cached = _redis.Get(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return JsonConvert.DeserializeObject<User>(cached);

            var user = await _context.Users
                .Include(r => r.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user != null)
            {
                var userJson = JsonConvert.SerializeObject(user, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, userJson, TimeSpan.FromDays(30));
            }

            return user;
        }
        catch (Exception ex)
        {
            throw new Exception("Error finding user: " + ex.Message);
        }
    }

    public async Task<User?> AddUser(User model)
    {
        try
        {
            if (model == null || string.IsNullOrWhiteSpace(model.PasswordHash))
            {
                throw new ArgumentException("User or password cannot be null or empty.");
            }

            model.PasswordHash = HashPassword(model.PasswordHash);

            var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Client");
            if (userRole == null)
            {
                userRole = new Role
                {
                    RoleName = "Client",
                    Description = "Client",
                    Status = true
                };
                _context.Roles.Add(userRole);
            }

            model.Roles ??= new List<Role>();
            if (model.Roles.All(r => r.RoleName != "Client"))
            {
                model.Roles.Add(userRole);
            }

            model.IsConfirm = true;

            await _context.Users.AddAsync(model);
            await _context.SaveChangesAsync();

            await _redis.RemoveByPatternAsync("User:Page:*");
            var cacheKey = $"User_{model.Id}";
            var userJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, userJson, TimeSpan.FromDays(30));

            return model;
        }
        catch (Exception ex)
        {
            throw new Exception("Error adding user: " + ex.Message);
        }
    }

    public async Task<User?> UpdateUser(User model, long id)
    {
        try
        {
            var user = await _context.Users.Include(r => r.Roles).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) throw new Exception("User not found");

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Identity = model.Identity;
            user.Address = model.Address;
            user.Status = model.Status;
            user.IsConfirm = model.IsConfirm;
            user.Birthday = model.Birthday;

            if (!string.IsNullOrEmpty(model.PasswordHash))
            {
                user.PasswordHash = HashPassword(model.PasswordHash);
            }

            await _context.SaveChangesAsync();

            var cacheKey = $"User_{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("User:Page:*");
            var userJson = JsonConvert.SerializeObject(user, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, userJson, TimeSpan.FromDays(30));

            return user;
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating user: " + ex.Message);
        }
    }

    public async Task DeleteUser(long id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) throw new Exception("User not found");

            user.IsConfirm = false;
            user.Status = "Off";
            await _context.SaveChangesAsync();

            var cacheKey = $"User_{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("User:Page:*");
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting user: " + ex.Message);
        }
    }


    public IPagedList<Models.Account> GetAllAccounts(int page, int pageSize, string? keyword)
    {
        try
        {
            string cacheKey = $"Card:Page:{page}:size:{pageSize}:keyword:{keyword ?? ""}";
            var cacheResult = _redis.Get(cacheKey);

            if (!string.IsNullOrEmpty(cacheResult))
            {
                var accounts = JsonSerializer.Deserialize<List<Models.Account>>(cacheResult,
                    new JsonSerializerOptions
                    {
                        ReferenceHandler = ReferenceHandler.IgnoreCycles
                    });
                if (accounts != null)
                    return new StaticPagedList<Models.Account>(
                        accounts.Skip((page - 1) * pageSize).Take(pageSize),
                        page,
                        pageSize,
                        accounts.Count);
            }

            var query = _context.Accounts
                .Include(a => a.User)
                .Include(v => v.Vip)
                .Include(c => c.CreditCard)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(p => p.AccountNumber.Contains(keyword));
            }

            var allAccounts = query.OrderByDescending(i => i!.Id).ToList();
            var result = new StaticPagedList<Models.Account>(
                allAccounts.Skip((page - 1) * pageSize).Take(pageSize)!,
                page,
                pageSize,
                allAccounts.Count);

            string json = JsonSerializer.Serialize(allAccounts, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles
            });
            _redis.Set(cacheKey, json, TimeSpan.FromDays(30));
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving accounts: " + ex.Message);
        }
    }

    public async Task<Models.Account?> FindAccountById(long id)
    {
        try
        {
            var cacheKey = $"Card{id}";
            var cached = _redis.Get(cacheKey);
            if (!string.IsNullOrEmpty(cached))
                return JsonConvert.DeserializeObject<Models.Account>(cached);

            var acc = await _context.Accounts
                .Include(a => a.User)
                .Include(v => v.Vip)
                .Include(c => c.CreditCard)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (acc != null)
            {
                var accJson = JsonConvert.SerializeObject(acc, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, accJson, TimeSpan.FromDays(30));
            }

            return acc;
        }
        catch (Exception ex)
        {
            throw new Exception("Error finding account: " + ex.Message);
        }
    }

    public async Task<CreditCardDto?> FindCreditCardById(long id)
    {
        try
        {
            var creditCard = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.AccountId == id);

            if (creditCard == null)
            {
                return null;
            }

            return new CreditCardDto
            {
                Id = creditCard.Id,
                AccountId = creditCard.AccountId,
                CardNumber = creditCard.CardNumber,
                CreditLimit = creditCard.CreditLimit,
                CurrentDebt = creditCard.CurrentDebt,
                InterestRate = creditCard.InterestRate,
                StatementDate = creditCard.StatementDate,
                DueDate = creditCard.DueDate,
                IsActive = creditCard.IsActive
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error finding credit card: " + ex.Message);
        }
    }

    public async Task<CreditCardDto?> LockCreditCardById(long id)
    {
        try
        {
            var creditCard = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.AccountId == id);

            if (creditCard == null)
            {
                return null;
            }

            creditCard.IsActive = false;
            await _context.SaveChangesAsync();

            var cacheKey = $"Card{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Card:Page:*");
            var account = await _context.Accounts
                .Include(a => a.User)
                .Include(v => v.Vip)
                .Include(c => c.CreditCard)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (account != null)
            {
                var accountJson = JsonConvert.SerializeObject(account, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, accountJson, TimeSpan.FromDays(30));
            }

            return new CreditCardDto
            {
                Id = creditCard.Id,
                AccountId = creditCard.AccountId,
                CardNumber = creditCard.CardNumber,
                CreditLimit = creditCard.CreditLimit,
                CurrentDebt = creditCard.CurrentDebt,
                InterestRate = creditCard.InterestRate,
                StatementDate = creditCard.StatementDate,
                DueDate = creditCard.DueDate,
                IsActive = creditCard.IsActive
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error locking credit card: " + ex.Message);
        }
    }

    public async Task<CreditCardDto?> UnlockCreditCardById(long id)
    {
        try
        {
            var creditCard = await _context.CreditCards
                .FirstOrDefaultAsync(c => c.AccountId == id);

            if (creditCard == null)
            {
                return null;
            }

            creditCard.IsActive = true;
            await _context.SaveChangesAsync();

            var cacheKey = $"Card{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Card:Page:*");
            var account = await _context.Accounts
                .Include(a => a.User)
                .Include(v => v.Vip)
                .Include(c => c.CreditCard)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (account != null)
            {
                var accountJson = JsonConvert.SerializeObject(account, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, accountJson, TimeSpan.FromDays(30));
            }

            return new CreditCardDto
            {
                Id = creditCard.Id,
                AccountId = creditCard.AccountId,
                CardNumber = creditCard.CardNumber,
                CreditLimit = creditCard.CreditLimit,
                CurrentDebt = creditCard.CurrentDebt,
                InterestRate = creditCard.InterestRate,
                StatementDate = creditCard.StatementDate,
                DueDate = creditCard.DueDate,
                IsActive = creditCard.IsActive
            };
        }
        catch (Exception ex)
        {
            throw new Exception("Error unlocking credit card: " + ex.Message);
        }
    }

    public async Task<Models.Account?> AddAccount(Models.Account model)
    {
        try
        {
            await _context.Accounts.AddAsync(model);
            await _context.SaveChangesAsync();

            await _redis.RemoveByPatternAsync("Card:Page:*");
            var cacheKey = $"Card{model.Id}";
            var accountJson = JsonConvert.SerializeObject(model, new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore
            });
            _redis.Set(cacheKey, accountJson, TimeSpan.FromDays(30));

            return model;
        }
        catch (Exception ex)
        {
            throw new Exception("Error adding account: " + ex.Message);
        }
    }

    public async Task<Models.Account?> UpdateAccount(Models.Account model, long id)
    {
        try
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                throw new Exception("Account not found");

            account.AccountNumber = model.AccountNumber;
            account.Balance = model.Balance;
            account.Pin = model.Pin;
            account.Status = model.Status;

            await _context.SaveChangesAsync();

            var cacheKey = $"Card{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Card:Page:*");
            var updatedAccount = await _context.Accounts
                .Include(a => a.User)
                .Include(v => v.Vip)
                .Include(c => c.CreditCard)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (updatedAccount != null)
            {
                var accountJson = JsonConvert.SerializeObject(updatedAccount, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                _redis.Set(cacheKey, accountJson, TimeSpan.FromDays(30));
            }

            return account;
        }
        catch (Exception ex)
        {
            throw new Exception("Error updating account: " + ex.Message);
        }
    }

    public async Task DeleteAccount(long id)
    {
        try
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null) throw new Exception("Account not found");

            account.Status = "Deleted";
            await _context.SaveChangesAsync();

            var cacheKey = $"Card{id}";
            _redis.Remove(cacheKey);
            await _redis.RemoveByPatternAsync("Card:Page:*");
        }
        catch (Exception ex)
        {
            throw new Exception("Error deleting account: " + ex.Message);
        }
    }

    public IPagedList<UserInsurance> GetAllUserSec(int page, int pageSize, string? keyword, string? status)
    {
        try
        {
            var query = _context.UserInsurances
                .Include(p => p.Package)
                .Include(u => u.User)
                .ThenInclude(a => a.Accounts)
                .Include(t => t.Transaction)
                .Where(u => u.Transaction.Status == "Sec" &&
                            (u.Status == "Pending" || u.Status == "Cancel" || u.Status == "Active" ||
                             u.Status == "Inactive"))
                .OrderByDescending(u => u.Id)
                .AsQueryable();

            // Lọc theo keyword
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(u =>
                    u.User.Email.Contains(keyword) ||
                    u.Package.Name.Contains(keyword) ||
                    u.Status.Contains(keyword) ||
                    u.InsuranceNumber.Contains(keyword));
            }

            // Lọc theo status
            if (!string.IsNullOrWhiteSpace(status) && status != "")
            {
                query = query.Where(u => u.Status == status);
            }

            var result = query.ToPagedList(page, pageSize);
            return result;
        }
        catch (Exception ex)
        {
            throw new Exception("Error retrieving user insurances: " + ex.Message);
        }
    }

    public async Task<UserInsurance?> UpdateStatusSec(long id, string status)
    {
        var query = await _context.UserInsurances
            .Include(a => a.User)
            .ThenInclude(u => u.Accounts)
            .ThenInclude(acc => acc.CreditCard)
            .Include(p => p.Package).Include(userInsurance => userInsurance.Transaction)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (query == null)
            throw new Exception("User not found");

        query.Status = status;

        if (status == "Active")
        {
            var account = query.User.Accounts.FirstOrDefault(id => id.Id == query.Transaction.FromAccountId);
            if (account == null)
                throw new Exception("Account not found");

            if (account.AccountType == "Credit Card")
            {
                if (account.CreditCard == null)
                    throw new Exception("Credit card not found");

                if (account.CreditCard.CurrentDebt + query.Package.Price > account.CreditCard.CreditLimit)
                    throw new Exception("Insufficient credit limit.");

                account.CreditCard.CurrentDebt += query.Package.Price;
            }
            else if (account.AccountType == "Normal Card")
            {
                if (account.Balance < query.Package.Price)
                    throw new Exception("Insufficient balance.");

                account.Balance -= query.Package.Price;
            }
            else
            {
                throw new Exception("Invalid account type.");
            }
        }

        await _context.SaveChangesAsync();
        return query;
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string inputPassword, string storedHash)
    {
        return HashPassword(inputPassword) == storedHash;
    }
}