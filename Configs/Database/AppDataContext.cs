using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;

namespace CP2496H07Group1.Configs.Database;

public class AppDataContext : DbContext
{
    public AppDataContext(DbContextOptions<AppDataContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }             
    public DbSet<Account> Accounts { get; set; }        
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<Request> Requests { get; set; }     
    public DbSet<InsurancePackage> InsurancePackages { get; set; } 
    public DbSet<UserInsurance> UserInsurances { get; set; }    
    public DbSet<Category> Categories { get; set; }  
    public DbSet<News> News { get; set; }  
    public DbSet<Comment> Comments { get; set; }
    public DbSet<AccountDiscounts> AccountDiscounts { get; set; }
    public DbSet<DiscountCode> DiscountCodes { get; set; }
    public DbSet<SavingCategory> SavingCategories { get; set; }
    public DbSet<Savings> Savings { get; set; }
    public DbSet<Vip> Vips { get; set; }
    public DbSet<Loans> Loans { get; set; }
    public DbSet<LoanOption> LoanOptions { get; set; }
    public DbSet<Slider> Sliders { get; set; }
    public DbSet<Faq> Fqas { get; set; }
    public DbSet<Role> Roles { get; set; }


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        
        //Gpt cmt cho cac ban cung doc nhe !!
        
        // Quan hệ nhiều-nhiều giữa User và Role (bảng trung gian UserRole)
        // Một User có nhiều Role, một Role có thể thuộc về nhiều User
        modelBuilder.Entity<User>()
            .HasMany(u => u.Roles)
            .WithMany(r => r.Users)
            .UsingEntity(j => j.ToTable("UserRole"));

        // Quan hệ 1-n giữa User và Account
        // Một User có nhiều Account, một Account thuộc về một User
        modelBuilder.Entity<Account>()
            .HasOne(a => a.User)             // Account có một User
            .WithMany(u => u.Accounts)       // User có nhiều Account
            .HasForeignKey(a => a.UserId);   // Khóa ngoại là UserId trong Account

        // Quan hệ 1-n giữa Account và Transaction (tài khoản nguồn)
        // Một Account có nhiều Transaction xuất phát từ nó
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.FromAccount)      // Transaction có một tài khoản nguồn
            .WithMany(a => a.TransactionsFrom) // Account có nhiều Transaction từ nó
            .HasForeignKey(t => t.FromAccountId) // Khóa ngoại là FromAccountId
            .OnDelete(DeleteBehavior.Restrict); // Không xóa Account nếu có Transaction liên quan

        // Quan hệ 1-n giữa Account và Transaction (tài khoản đích)
        // Một Account có nhiều Transaction đến nó (có thể null)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.ToAccount)        // Transaction có một tài khoản đích
            .WithMany(a => a.TransactionsTo) // Account có nhiều Transaction đến nó
            .HasForeignKey(t => t.ToAccountId) // Khóa ngoại là ToAccountId
            .OnDelete(DeleteBehavior.Restrict) // Không xóa Account nếu có Transaction liên quan
            .IsRequired(false);              // ToAccountId có thể null

        // Quan hệ 1-n giữa User và Request
        // Một User có nhiều Request, một Request thuộc về một User
        modelBuilder.Entity<Request>()
            .HasOne(r => r.User)             // Request có một User
            .WithMany(u => u.Requests)       // User có nhiều Request
            .HasForeignKey(r => r.UserId)   // Khóa ngoại là UserId trong Request
            .OnDelete(DeleteBehavior.Restrict);
        // Quan hệ 1-n giữa User và UserInsurance
        // Một User có nhiều UserInsurance, một UserInsurance thuộc về một User
        modelBuilder.Entity<UserInsurance>()
            .HasOne(ui => ui.User)           // UserInsurance có một User
            .WithMany(u => u.UserInsurances) // User có nhiều UserInsurance
            .HasForeignKey(ui => ui.UserId) // Khóa ngoại là UserId trong UserInsurance
            .OnDelete(DeleteBehavior.Restrict);
        // Quan hệ 1-n giữa InsurancePackage và UserInsurance
        // Một InsurancePackage có nhiều UserInsurance, một UserInsurance thuộc về một Package
        modelBuilder.Entity<UserInsurance>()
            .HasOne(ui => ui.Package)        // UserInsurance có một InsurancePackage
            .WithMany(ip => ip.UserInsurances) // InsurancePackage có nhiều UserInsurance
            .HasForeignKey(ui => ui.PackageId) // Khóa ngoại là PackageId trong UserInsurance
            .OnDelete(DeleteBehavior.Restrict);
        // Quan hệ 1-1 hoặc 1-n giữa UserInsurance và Transaction
        // Một UserInsurance liên kết với một Transaction (giao dịch mua bảo hiểm)
        modelBuilder.Entity<UserInsurance>()
            .HasOne(ui => ui.Transaction)    // UserInsurance có một Transaction
            .WithMany()                      // Transaction không cần danh sách UserInsurance ngược lại
            .HasForeignKey(ui => ui.TransactionId) // Khóa ngoại là TransactionId trong UserInsurance
            .OnDelete(DeleteBehavior.Restrict);
        
        // Quan hệ 1-N giữa Category và News
        modelBuilder.Entity<News>()
            .HasOne(n => n.Category)
            .WithMany(c => c.News)
            .HasForeignKey(n => n.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Quan hệ 1-N giữa News và Comment
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.News)
            .WithMany(n => n.Comments)
            .HasForeignKey(c => c.NewsId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Quan hệ 1-N giữa User và Comment
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Quan hệ 1-N giữa Comment và Comment (replies)
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.NoAction);
        
        // Quan hệ 1-N giữa DiscountCode và AccountDiscounts
        modelBuilder.Entity<AccountDiscounts>()
            .HasOne(ad => ad.DiscountCode)
            .WithMany(dc => dc.AccountDiscounts)
            .HasForeignKey(ad => ad.DiscountId)
            .OnDelete(DeleteBehavior.Restrict); 

        // Quan hệ 1-N giữa Account và AccountDiscounts
        modelBuilder.Entity<AccountDiscounts>()
            .HasOne(ad => ad.Account)
            .WithMany(a => a.AccountDiscounts)
            .HasForeignKey(ad => ad.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
        
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.DiscountCode)
            .WithMany() // Nếu một DiscountCode có thể áp dụng cho nhiều giao dịch, dùng WithMany()
            .HasForeignKey(t => t.DiscountCodeId)
            .OnDelete(DeleteBehavior.SetNull); // Nếu DiscountCode bị xóa, Transaction vẫn giữ nguyên
        
        modelBuilder.Entity<Savings>()
            .HasOne(s => s.SavingCategory)  
            .WithMany(sc => sc.Savings)  // Một SavingCategory có nhiều Savings
            .HasForeignKey(s => s.SavingCategoryId) 
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Savings>()
            .HasOne(s => s.Account)  
            .WithMany()  // Một Account có thể có nhiều Savings, nhưng không cần truy vấn ngược
            .HasForeignKey(s => s.AccountId)
            .OnDelete(DeleteBehavior.SetNull); // Nếu Account bị xóa, Savings vẫn tồn tại
        
        modelBuilder.Entity<Account>()
            .HasOne(a => a.Vip)
            .WithOne()
            .HasForeignKey<Account>(a => a.VipId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Quan hệ 1-n giữa Account và Loan (một Account có thể có nhiều Loan)
        modelBuilder.Entity<Loans>()
            .HasOne(l => l.Account)
            .WithMany(a => a.Loans)
            .HasForeignKey(l => l.AccountId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa tài khoản thì xóa luôn các khoản vay

        // Quan hệ 1-n giữa LoanOption và Loan (một LoanOption có thể được chọn bởi nhiều Loan)
        modelBuilder.Entity<Loans>()
            .HasOne(l => l.LoanOption)
            .WithMany(lo => lo.Loans)
            .HasForeignKey(l => l.LoanOptionId)
            .OnDelete(DeleteBehavior.Restrict); // Không cho xóa nếu còn khoản vay liên kết
        
        modelBuilder.Entity<Loans>()
            .HasOne(l => l.Vip)
            .WithMany(v => v.Loans)
            .HasForeignKey(l => l.VipId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}