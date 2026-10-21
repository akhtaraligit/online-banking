namespace OnlineBanking.Api.Models;

public enum TransferDestinationType
{
    OwnAccount = 0,
    Payee = 1
}

public class Transfer
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public TransferDestinationType DestinationType { get; set; }
    public Guid SourceAccountId { get; set; }
    public Guid? DestinationAccountId { get; set; }
    public Guid? PayeeId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public Guid TransferGroupId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Customer Customer { get; set; } = null!;
    public Account SourceAccount { get; set; } = null!;
    public Account? DestinationAccount { get; set; }
    public Payee? Payee { get; set; }
}
