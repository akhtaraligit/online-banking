using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(Customer customer);
}
