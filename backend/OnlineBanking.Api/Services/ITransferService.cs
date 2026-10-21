using OnlineBanking.Api.Dtos.Transfers;

namespace OnlineBanking.Api.Services;

public interface ITransferService
{
    Task<IReadOnlyList<PayeeDto>> GetPayeesAsync(
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<(TransferResultDto? Result, string? Error, int StatusCode)> CreateTransferAsync(
        Guid customerId,
        CreateTransferRequest request,
        CancellationToken cancellationToken = default);
}
