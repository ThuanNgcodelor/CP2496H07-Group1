namespace CP2496H07Group1.Models;

public class SavingCategory
{
    public required long Id { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public decimal Money { get; set; }
    public required string TypeTk { get; set; }

    public List<Savings> Savings { get; set; } = new(); // Liên kết 1-n với Savings
}
