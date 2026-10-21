using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using OnlineBanking.Api.Tests.Support;

namespace OnlineBanking.Api.Tests;

public class TransferTests : IClassFixture<BankingWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public TransferTests(BankingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Transfer_BetweenOwnAccounts_UpdatesBalancesOnce_WhenRetriedWithSameKey()
    {
        var token = await LoginAsync("alice");
        var accounts = await GetAccountsAsync(token);
        var source = accounts.First(a => a.Name.Contains("Checking", StringComparison.OrdinalIgnoreCase));
        var destination = accounts.First(a => a.Name.Contains("Savings", StringComparison.OrdinalIgnoreCase));
        var key = Guid.NewGuid().ToString();

        var first = await PostTransferAsync(token, new
        {
            sourceAccountId = source.Id,
            destinationType = "OwnAccount",
            destinationAccountId = destination.Id,
            amount = 25.50m,
            note = "Idempotency check",
            idempotencyKey = key
        });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        var firstBody = await first.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        Assert.NotNull(firstBody);
        Assert.False(firstBody.AlreadyProcessed);
        Assert.Equal(source.Balance - 25.50m, firstBody.SourceAccount.Balance);
        Assert.Equal(destination.Balance + 25.50m, firstBody.DestinationAccount!.Balance);

        var second = await PostTransferAsync(token, new
        {
            sourceAccountId = source.Id,
            destinationType = "OwnAccount",
            destinationAccountId = destination.Id,
            amount = 25.50m,
            note = "Idempotency check",
            idempotencyKey = key
        });

        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var secondBody = await second.Content.ReadFromJsonAsync<TransferResponse>(JsonOptions);
        Assert.NotNull(secondBody);
        Assert.True(secondBody.AlreadyProcessed);
        Assert.Equal(firstBody.TransferId, secondBody.TransferId);
        Assert.Equal(firstBody.SourceAccount.Balance, secondBody.SourceAccount.Balance);
        Assert.Equal(firstBody.DestinationAccount!.Balance, secondBody.DestinationAccount!.Balance);
    }

    [Fact]
    public async Task Transfer_RejectsInsufficientFunds()
    {
        var token = await LoginAsync("bob");
        var accounts = await GetAccountsAsync(token);
        var source = accounts.OrderBy(a => a.Balance).First();
        var destination = accounts.First(a => a.Id != source.Id);

        var response = await PostTransferAsync(token, new
        {
            sourceAccountId = source.Id,
            destinationType = "OwnAccount",
            destinationAccountId = destination.Id,
            amount = source.Balance + 1m,
            idempotencyKey = Guid.NewGuid().ToString()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Insufficient funds.", payload?.Message);
    }

    [Fact]
    public async Task Transfer_RejectsSameSourceAndDestination()
    {
        var token = await LoginAsync("alice");
        var accounts = await GetAccountsAsync(token);
        var source = accounts.First();

        var response = await PostTransferAsync(token, new
        {
            sourceAccountId = source.Id,
            destinationType = "OwnAccount",
            destinationAccountId = source.Id,
            amount = 10m,
            idempotencyKey = Guid.NewGuid().ToString()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ErrorResponse>(JsonOptions);
        Assert.Equal("Source and destination accounts must be different.", payload?.Message);
    }

    [Fact]
    public async Task Accounts_AreScopedToSignedInCustomer()
    {
        var aliceToken = await LoginAsync("alice");
        var bobToken = await LoginAsync("bob");

        var aliceAccounts = await GetAccountsAsync(aliceToken);
        var bobAccounts = await GetAccountsAsync(bobToken);

        Assert.All(aliceAccounts, a => Assert.StartsWith("100", a.AccountNumber));
        Assert.All(bobAccounts, a => Assert.StartsWith("200", a.AccountNumber));
        Assert.Empty(aliceAccounts.Select(a => a.Id).Intersect(bobAccounts.Select(a => a.Id)));
    }

    private async Task<string> LoginAsync(string username)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "Password123!"
        });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return body!.Token;
    }

    private async Task<List<AccountResponse>> GetAccountsAsync(string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/accounts");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var accounts = await response.Content.ReadFromJsonAsync<List<AccountResponse>>(JsonOptions);
        return accounts ?? [];
    }

    private async Task<HttpResponseMessage> PostTransferAsync(string token, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/transfers");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body);
        return await _client.SendAsync(request);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
    }

    private sealed class AccountResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
    }

    private sealed class TransferResponse
    {
        public Guid TransferId { get; set; }
        public bool AlreadyProcessed { get; set; }
        public AccountResponse SourceAccount { get; set; } = null!;
        public AccountResponse? DestinationAccount { get; set; }
    }

    private sealed class ErrorResponse
    {
        public string Message { get; set; } = string.Empty;
    }
}
