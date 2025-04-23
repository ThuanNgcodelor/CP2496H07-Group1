using CP2496H07Group1.Configs.Database;
using CP2496H07Group1.Models;
using Microsoft.EntityFrameworkCore;
using X.PagedList;
using X.PagedList.Extensions;

namespace CP2496H07Group1.Services.Auth;

public class TransactionService : ITransactionService
{
    private readonly AppDataContext _context;

    public TransactionService(AppDataContext context)
    {
        _context = context;
    }

    public IPagedList<Transaction> GetTransactions(long userId, DateTime? fromDate, DateTime? toDate,
        List<string> types, int page, int pageSize)
    {
        var userAccountIds = _context.Accounts
            .Where(a => a.UserId == userId)
            .Select(a => a.Id);

        var query = _context.Transactions
            .Where(t =>
                (userAccountIds.Contains(t.FromAccountId) || userAccountIds.Contains(t.ToAccountId ?? 0)) &&
                (!fromDate.HasValue || t.TransactionDate >= fromDate.Value) &&
                (!toDate.HasValue || t.TransactionDate <= toDate.Value) &&
                (types == null || types.Count == 0 || types.Contains(t.TransactionType))
            )
            .Include(t => t.FromAccount).ThenInclude(a => a.User)
            .Include(t => t.ToAccount).ThenInclude(a => a.User)
            .OrderByDescending(t => t.TransactionDate);

        return query.ToPagedList(page, pageSize);
    }
}