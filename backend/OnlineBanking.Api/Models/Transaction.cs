namespace OnlineBanking.Api.Models;

public enum TransactionType
{
    Debit = 0,
    Credit = 1
}

public class Transaction
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public DateTime OccurredAt { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public Guid? TransferGroupId { get; set; }
    public string? IdempotencyKey { get; set; }

    public Account Account { get; set; } = null!;
}
