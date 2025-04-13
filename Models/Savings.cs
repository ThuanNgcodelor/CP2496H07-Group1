namespace CP2496H07Group1.Models;

public class Savings
{
    public  long Id { get; set; }
    public long? AccountId { get; set; } // Có thể null nếu chưa liên kết với tài khoản nào
    public DateTime DateStart { get; set; }
    public DateTime DateEnd { get; set; }
    public required string TypeTk { get; set; }
    public string Status { get; set; } = "chưa được rút";
    public string Pay { get; set; } = "chưa rút";

    public required long SavingCategoryId { get; set; } // Liên kết đến SavingCategory
    public required SavingCategory SavingCategory { get; set; }

    public Account? Account { get; set; } // Liên kết với Account, có thể null nếu chưa gắn với tài khoản
}
