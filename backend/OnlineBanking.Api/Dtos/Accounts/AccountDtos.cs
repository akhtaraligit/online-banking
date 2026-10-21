using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Dtos.Accounts;

public class AccountSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AccountType Type { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
}

public class AccountDetailDto : AccountSummaryDto
{
    public DateTime CreatedAt { get; set; }
}
