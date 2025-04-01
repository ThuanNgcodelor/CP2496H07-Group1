namespace CP2496H07Group1.Models;

public class Vip

{
    public required long Id { get; set; }

    public required int TypeVip { get; set; }

    public decimal Price { get; set; }

    public required bool NoPick { get; set; } = false;

    public int MoneyBack { get; set; }
    
    public List<Loans> Loans { get; set; } = new();

    

}