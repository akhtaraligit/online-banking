using Microsoft.EntityFrameworkCore;
using OnlineBanking.Api.Data;
using OnlineBanking.Api.Dtos.Transfers;
using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Services;

public class TransferService(AppDbContext db) : ITransferService
{
    public async Task<IReadOnlyList<PayeeDto>> GetPayeesAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await db.Payees
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId)
            .OrderBy(p => p.Name)
            .Select(p => new PayeeDto
            {
                Id = p.Id,
                Name = p.Name,
                AccountNumber = p.AccountNumber,
                BankName = p.BankName
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<(TransferResultDto? Result, string? Error, int StatusCode)> CreateTransferAsync(
        Guid customerId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var idempotencyKey = request.IdempotencyKey.Trim();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return (null, "Idempotency key is required.", StatusCodes.Status400BadRequest);
        }

        if (request.Amount <= 0)
        {
            return (null, "Amount must be greater than zero.", StatusCodes.Status400BadRequest);
        }

        var amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount != request.Amount)
        {
            return (null, "Amount can have at most two decimal places.", StatusCodes.Status400BadRequest);
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var existing = await db.Transfers
            .Include(t => t.SourceAccount)
            .Include(t => t.DestinationAccount)
            .Include(t => t.Payee)
            .FirstOrDefaultAsync(
                t => t.CustomerId == customerId && t.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is not null)
        {
            await tx.CommitAsync(cancellationToken);
            return (MapResult(existing, alreadyProcessed: true), null, StatusCodes.Status200OK);
        }

        var source = await db.Accounts
            .FirstOrDefaultAsync(
                a => a.Id == request.SourceAccountId && a.CustomerId == customerId,
                cancellationToken);

        if (source is null)
        {
            return (null, "Source account not found.", StatusCodes.Status404NotFound);
        }

        Account? destination = null;
        Payee? payee = null;
        string creditDescription;
        string debitDescription;

        if (request.DestinationType == TransferDestinationType.OwnAccount)
        {
            if (!request.DestinationAccountId.HasValue)
            {
                return (null, "Destination account is required.", StatusCodes.Status400BadRequest);
            }

            if (request.DestinationAccountId.Value == request.SourceAccountId)
            {
                return (null, "Source and destination accounts must be different.", StatusCodes.Status400BadRequest);
            }

            destination = await db.Accounts
                .FirstOrDefaultAsync(
                    a => a.Id == request.DestinationAccountId.Value && a.CustomerId == customerId,
                    cancellationToken);

            if (destination is null)
            {
                return (null, "Destination account not found.", StatusCodes.Status404NotFound);
            }

            debitDescription = BuildDescription($"Transfer to {destination.Name}", request.Note);
            creditDescription = BuildDescription($"Transfer from {source.Name}", request.Note);
        }
        else if (request.DestinationType == TransferDestinationType.Payee)
        {
            if (!request.PayeeId.HasValue)
            {
                return (null, "Payee is required.", StatusCodes.Status400BadRequest);
            }

            payee = await db.Payees
                .FirstOrDefaultAsync(
                    p => p.Id == request.PayeeId.Value && p.CustomerId == customerId,
                    cancellationToken);

            if (payee is null)
            {
                return (null, "Payee not found.", StatusCodes.Status404NotFound);
            }

            debitDescription = BuildDescription($"Payment to {payee.Name}", request.Note);
            creditDescription = string.Empty;
        }
        else
        {
            return (null, "Unsupported destination type.", StatusCodes.Status400BadRequest);
        }

        if (source.Balance < amount)
        {
            return (null, "Insufficient funds.", StatusCodes.Status400BadRequest);
        }

        var now = DateTime.UtcNow;
        var transferGroupId = Guid.NewGuid();
        var transferId = Guid.NewGuid();

        source.Balance -= amount;

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            AccountId = source.Id,
            OccurredAt = now,
            Description = debitDescription,
            Amount = amount,
            Type = TransactionType.Debit,
            TransferGroupId = transferGroupId,
            IdempotencyKey = $"{idempotencyKey}:debit"
        });

        if (destination is not null)
        {
            destination.Balance += amount;
            db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                AccountId = destination.Id,
                OccurredAt = now,
                Description = creditDescription,
                Amount = amount,
                Type = TransactionType.Credit,
                TransferGroupId = transferGroupId,
                IdempotencyKey = $"{idempotencyKey}:credit"
            });
        }

        var transfer = new Transfer
        {
            Id = transferId,
            CustomerId = customerId,
            IdempotencyKey = idempotencyKey,
            DestinationType = request.DestinationType,
            SourceAccountId = source.Id,
            DestinationAccountId = destination?.Id,
            PayeeId = payee?.Id,
            Amount = amount,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            TransferGroupId = transferGroupId,
            CreatedAt = now,
            SourceAccount = source,
            DestinationAccount = destination,
            Payee = payee
        };

        db.Transfers.Add(transfer);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync(cancellationToken);

            var raced = await db.Transfers
                .AsNoTracking()
                .Include(t => t.SourceAccount)
                .Include(t => t.DestinationAccount)
                .Include(t => t.Payee)
                .FirstOrDefaultAsync(
                    t => t.CustomerId == customerId && t.IdempotencyKey == idempotencyKey,
                    cancellationToken);

            if (raced is not null)
            {
                return (MapResult(raced, alreadyProcessed: true), null, StatusCodes.Status200OK);
            }

            throw;
        }

        return (MapResult(transfer, alreadyProcessed: false), null, StatusCodes.Status201Created);
    }

    private static string BuildDescription(string baseText, string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return baseText;
        }

        var trimmed = note.Trim();
        var combined = $"{baseText} — {trimmed}";
        return combined.Length <= 256 ? combined : combined[..256];
    }

    private static TransferResultDto MapResult(Transfer transfer, bool alreadyProcessed)
    {
        return new TransferResultDto
        {
            TransferId = transfer.Id,
            TransferGroupId = transfer.TransferGroupId,
            AlreadyProcessed = alreadyProcessed,
            Amount = transfer.Amount,
            Note = transfer.Note,
            CreatedAt = transfer.CreatedAt,
            DestinationType = transfer.DestinationType,
            SourceAccount = new AccountSnapshotDto
            {
                Id = transfer.SourceAccount.Id,
                Name = transfer.SourceAccount.Name,
                AccountNumber = transfer.SourceAccount.AccountNumber,
                Balance = transfer.SourceAccount.Balance,
                Currency = transfer.SourceAccount.Currency
            },
            DestinationAccount = transfer.DestinationAccount is null
                ? null
                : new AccountSnapshotDto
                {
                    Id = transfer.DestinationAccount.Id,
                    Name = transfer.DestinationAccount.Name,
                    AccountNumber = transfer.DestinationAccount.AccountNumber,
                    Balance = transfer.DestinationAccount.Balance,
                    Currency = transfer.DestinationAccount.Currency
                },
            Payee = transfer.Payee is null
                ? null
                : new PayeeDto
                {
                    Id = transfer.Payee.Id,
                    Name = transfer.Payee.Name,
                    AccountNumber = transfer.Payee.AccountNumber,
                    BankName = transfer.Payee.BankName
                },
            Message = alreadyProcessed
                ? "This transfer was already processed. No additional money was moved."
                : "Transfer completed successfully."
        };
    }
}
