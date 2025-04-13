namespace CP2496H07Group1.Models;

public class Request
{
    public  long RequestId { get; set; }         
    public required long UserId { get; set; }          
    public required string RequestType { get; set; }      // ChequeBook, ChangeAddress, StopPayment
    public required string Details { get; set; }          // Chi tiết yêu cầu
    public required string Status { get; set; }           // (Pending, Approved, Rejected)
    public DateTime RequestDate { get; set; }  = DateTime.Now;  // Time request 

    public required User User { get; set; }               // n-1 with User
}