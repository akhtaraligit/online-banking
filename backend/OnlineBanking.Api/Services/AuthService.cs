using Microsoft.EntityFrameworkCore;
using OnlineBanking.Api.Data;
using OnlineBanking.Api.Dtos.Auth;

namespace OnlineBanking.Api.Services;

public class AuthService(AppDbContext db, ITokenService tokenService) : IAuthService
{
    public async Task<AuthResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var customer = await db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Username == username, cancellationToken);

        if (customer is null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
        {
            return null;
        }

        var (token, expiresAt) = tokenService.CreateToken(customer);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Customer = new CustomerDto
            {
                Id = customer.Id,
                Username = customer.Username,
                FullName = customer.FullName,
                Email = customer.Email
            }
        };
    }

    public async Task<CustomerDto?> GetCurrentCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        return await db.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new CustomerDto
            {
                Id = c.Id,
                Username = c.Username,
                FullName = c.FullName,
                Email = c.Email
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
