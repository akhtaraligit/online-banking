using System.ComponentModel.DataAnnotations;
using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Dtos.Transfers;

public class CreateTransferRequest
{
    [Required]
    public Guid SourceAccountId { get; set; }

    [Required]
    public TransferDestinationType DestinationType { get; set; }

    public Guid? DestinationAccountId { get; set; }

    public Guid? PayeeId { get; set; }

    [Range(0.01, 1_000_000)]
    public decimal Amount { get; set; }

    [MaxLength(200)]
    public string? Note { get; set; }

    [Required]
    [MaxLength(128)]
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class TransferResultDto
{
    public Guid TransferId { get; set; }
    public Guid TransferGroupId { get; set; }
    public bool AlreadyProcessed { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
    public TransferDestinationType DestinationType { get; set; }
    public AccountSnapshotDto SourceAccount { get; set; } = null!;
    public AccountSnapshotDto? DestinationAccount { get; set; }
    public PayeeDto? Payee { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class AccountSnapshotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
}

public class PayeeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
}
