namespace CP2496H07Group1.Models;

public  class UserInsurance
{
    public  long Id { get; set; }
    public required long UserId { get; set; }
    public required long PackageId { get; set; }
    public required long TransactionId { get; set; }
    public required DateTime StartDate { get; set; } 
    public required DateTime EndDate { get; set; }
    public required string Status { get; set; } //(Active, Expired)
    
    public required User User { get; set; }
    public required InsurancePackage Package { get; set; }
    public required Transaction Transaction { get; set; }
}