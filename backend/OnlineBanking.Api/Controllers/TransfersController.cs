using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBanking.Api.Dtos.Transfers;
using OnlineBanking.Api.Extensions;
using OnlineBanking.Api.Services;

namespace OnlineBanking.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TransfersController(ITransferService transferService) : ControllerBase
{
    [HttpGet("~/api/payees")]
    public async Task<ActionResult<IReadOnlyList<PayeeDto>>> GetPayees(CancellationToken cancellationToken)
    {
        if (!User.TryGetCustomerId(out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var payees = await transferService.GetPayeesAsync(customerId, cancellationToken);
        return Ok(payees);
    }

    [HttpPost]
    public async Task<ActionResult<TransferResultDto>> CreateTransfer(
        [FromBody] CreateTransferRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetCustomerId(out var customerId))
        {
            return Unauthorized(new { message = "Invalid token subject." });
        }

        var (result, error, statusCode) = await transferService.CreateTransferAsync(
            customerId,
            request,
            cancellationToken);

        if (error is not null)
        {
            return StatusCode(statusCode, new { message = error });
        }

        if (statusCode == StatusCodes.Status201Created)
        {
            return Created($"/api/transfers/{result!.TransferId}", result);
        }

        return Ok(result);
    }
}
