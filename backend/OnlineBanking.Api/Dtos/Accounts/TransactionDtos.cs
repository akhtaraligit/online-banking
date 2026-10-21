using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Dtos.Accounts;

public class TransactionDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public decimal? RunningBalance { get; set; }
}

public class PagedTransactionsResponse
{
    public IReadOnlyList<TransactionDto> Items { get; set; } = Array.Empty<TransactionDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class TransactionQuery
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public TransactionType? Type { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
