using OnlineBanking.Api.Dtos.Auth;

namespace OnlineBanking.Api.Services;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetCurrentCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);
}
