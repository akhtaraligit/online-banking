using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using OnlineBanking.Api.Tests.Support;

namespace OnlineBanking.Api.Tests;

public class AuthTests : IClassFixture<BankingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(BankingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "alice",
            password = "Password123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body.Token));
        Assert.Equal("alice", body.Customer.Username);
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "alice",
            password = "wrong-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithToken_ReturnsCurrentCustomer()
    {
        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "bob",
            password = "Password123!"
        });
        var auth = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(auth);

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var me = await response.Content.ReadFromJsonAsync<CustomerResponse>();
        Assert.NotNull(me);
        Assert.Equal("bob", me.Username);
    }

    private sealed class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public CustomerResponse Customer { get; set; } = null!;
    }

    private sealed class CustomerResponse
    {
        public string Username { get; set; } = string.Empty;
    }
}
