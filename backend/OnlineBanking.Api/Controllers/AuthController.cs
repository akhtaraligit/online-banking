using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Api.Dtos.Auth;
using OnlineBanking.Api.Services;

namespace OnlineBanking.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        // JWT is stateless; client discards the token. Endpoint exists for a clean UX contract.
        return Ok(new { message = "Signed out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CustomerDto>> Me(CancellationToken cancellationToken)
    {
        var customerIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(customerIdValue, out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var customer = await authService.GetCurrentCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return Unauthorized(new { message = "Customer not found." });
        }

        return Ok(customer);
    }
}
