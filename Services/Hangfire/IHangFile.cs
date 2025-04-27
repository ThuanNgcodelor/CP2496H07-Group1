namespace CP2496H07Group1.Services.Hangfire;

public interface IHangFile
{
    Task AutoPayCreditCardDebts();
    Task ProcessMonthlyPayments();
    Task AutoBlockCreditCard();
}