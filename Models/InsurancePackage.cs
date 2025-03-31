namespace CP2496H07Group1.Models;

public class InsurancePackage
{
    public required long Id { get; set; }       
    public required string Name { get; set; }             
    public required string Description { get; set; }      
    public required decimal Price { get; set; }         
    public required int DurationDays { get; set; }      //Thoi han
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    public required List<UserInsurance> UserInsurances { get; set; } = new List<UserInsurance>(); // 1 n with UserInsurance
}