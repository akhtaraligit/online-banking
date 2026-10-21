using OnlineBanking.Api.Dtos.Accounts;

namespace OnlineBanking.Api.Services;

public interface IAccountService
{
    Task<IReadOnlyList<AccountSummaryDto>> GetAccountsAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<AccountDetailDto?> GetAccountAsync(
        Guid customerId,
        Guid accountId,
        CancellationToken cancellationToken = default);

    Task<PagedTransactionsResponse?> GetTransactionsAsync(
        Guid customerId,
        Guid accountId,
        TransactionQuery query,
        CancellationToken cancellationToken = default);
}
