using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Api.Dtos.Accounts;
using OnlineBanking.Api.Extensions;
using OnlineBanking.Api.Models;
using OnlineBanking.Api.Services;

namespace OnlineBanking.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountsController(IAccountService accountService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountSummaryDto>>> GetAccounts(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCustomerId(out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var accounts = await accountService.GetAccountsAsync(customerId, cancellationToken);
        return Ok(accounts);
    }

    [HttpGet("{accountId:guid}")]
    public async Task<ActionResult<AccountDetailDto>> GetAccount(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCustomerId(out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var account = await accountService.GetAccountAsync(customerId, accountId, cancellationToken);
        if (account is null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(account);
    }

    [HttpGet("{accountId:guid}/transactions")]
    public async Task<ActionResult<PagedTransactionsResponse>> GetTransactions(
        Guid accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] TransactionType? type,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (!User.TryGetCustomerId(out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var result = await accountService.GetTransactionsAsync(
            customerId,
            accountId,
            new TransactionQuery
            {
                From = from,
                To = to,
                Type = type,
                Search = search,
                Page = page,
                PageSize = pageSize
            },
            cancellationToken);

        if (result is null)
        {
            return NotFound(new { message = "Account not found." });
        }

        return Ok(result);
    }
}
