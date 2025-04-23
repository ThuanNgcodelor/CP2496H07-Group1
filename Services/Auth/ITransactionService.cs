using CP2496H07Group1.Models;
using X.PagedList;

namespace CP2496H07Group1.Services.Auth;

public interface ITransactionService
{
    IPagedList<Transaction> GetTransactions(long userId,DateTime? fromDate, DateTime? toDate,List<string> types, int page, int pageSize);
}
