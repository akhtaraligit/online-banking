using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace OnlineBanking.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static bool TryGetCustomerId(this ClaimsPrincipal user, out Guid customerId)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(value, out customerId);
    }
}
