using Microsoft.EntityFrameworkCore;
using OnlineBanking.Api.Data;
using OnlineBanking.Api.Dtos.Accounts;
using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Services;

public class AccountService(AppDbContext db) : IAccountService
{
    public async Task<IReadOnlyList<AccountSummaryDto>> GetAccountsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .OrderBy(a => a.Type)
            .ThenBy(a => a.Name)
            .Select(a => new AccountSummaryDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                Currency = a.Currency
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AccountDetailDto?> GetAccountAsync(
        Guid customerId,
        Guid accountId,
        CancellationToken cancellationToken = default)
    {
        return await db.Accounts
            .AsNoTracking()
            .Where(a => a.CustomerId == customerId && a.Id == accountId)
            .Select(a => new AccountDetailDto
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                Currency = a.Currency,
                CreatedAt = a.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedTransactionsResponse?> GetTransactionsAsync(
        Guid customerId,
        Guid accountId,
        TransactionQuery query,
        CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.CustomerId == customerId && a.Id == accountId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 50 ? 10 : query.PageSize;

        var filtered = db.Transactions
            .AsNoTracking()
            .Where(t => t.AccountId == accountId);

        if (query.From.HasValue)
        {
            var from = DateTime.SpecifyKind(query.From.Value.Date, DateTimeKind.Utc);
            filtered = filtered.Where(t => t.OccurredAt >= from);
        }

        if (query.To.HasValue)
        {
            var toExclusive = DateTime.SpecifyKind(query.To.Value.Date.AddDays(1), DateTimeKind.Utc);
            filtered = filtered.Where(t => t.OccurredAt < toExclusive);
        }

        if (query.Type.HasValue)
        {
            filtered = filtered.Where(t => t.Type == query.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLower();
            filtered = filtered.Where(t => t.Description.ToLower().Contains(term));
        }

        var totalCount = await filtered.CountAsync(cancellationToken);

        var pageItems = await filtered
            .OrderByDescending(t => t.OccurredAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                OccurredAt = t.OccurredAt,
                Description = t.Description,
                Amount = t.Amount,
                Type = t.Type
            })
            .ToListAsync(cancellationToken);

        // Running balance for the returned page (newest-first): start from current balance,
        // then walk older transactions on/after the newest item shown.
        if (pageItems.Count > 0)
        {
            var newestOnPage = pageItems[0].OccurredAt;
            var newerCredits = await db.Transactions
                .AsNoTracking()
                .Where(t => t.AccountId == accountId && t.OccurredAt > newestOnPage)
                .SumAsync(t => t.Type == TransactionType.Credit ? t.Amount : -t.Amount, cancellationToken);

            var balanceCursor = account.Balance - newerCredits;

            foreach (var item in pageItems)
            {
                item.RunningBalance = balanceCursor;
                balanceCursor += item.Type == TransactionType.Credit ? -item.Amount : item.Amount;
            }
        }

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedTransactionsResponse
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}
