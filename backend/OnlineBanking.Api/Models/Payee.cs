namespace OnlineBanking.Api.Models;

public class Payee
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;

    public Customer Customer { get; set; } = null!;
}
