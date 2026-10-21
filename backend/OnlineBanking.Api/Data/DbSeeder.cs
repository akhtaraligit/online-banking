using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.EnsureCreatedAsync();

        if (db.Customers.Any())
        {
            return;
        }

        var aliceId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var bobId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var aliceCheckingId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var aliceSavingsId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var bobCheckingId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var bobSavingsId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        var alice = new Customer
        {
            Id = aliceId,
            Username = "alice",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "Alice Johnson",
            Email = "alice@demo.bank"
        };

        var bob = new Customer
        {
            Id = bobId,
            Username = "bob",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123!"),
            FullName = "Bob Smith",
            Email = "bob@demo.bank"
        };

        var accounts = new List<Account>
        {
            new()
            {
                Id = aliceCheckingId,
                CustomerId = aliceId,
                Name = "Everyday Checking",
                Type = AccountType.Checking,
                AccountNumber = "1001001001",
                Balance = 2450.75m
            },
            new()
            {
                Id = aliceSavingsId,
                CustomerId = aliceId,
                Name = "Rainy Day Savings",
                Type = AccountType.Savings,
                AccountNumber = "1001001002",
                Balance = 10250.00m
            },
            new()
            {
                Id = bobCheckingId,
                CustomerId = bobId,
                Name = "Primary Checking",
                Type = AccountType.Checking,
                AccountNumber = "2002002001",
                Balance = 980.40m
            },
            new()
            {
                Id = bobSavingsId,
                CustomerId = bobId,
                Name = "Vacation Savings",
                Type = AccountType.Savings,
                AccountNumber = "2002002002",
                Balance = 3200.00m
            }
        };

        var now = DateTime.UtcNow;
        var transactions = new List<Transaction>
        {
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-12),
                Description = "Payroll deposit",
                Amount = 3200.00m,
                Type = TransactionType.Credit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-10),
                Description = "Grocery Store",
                Amount = 86.45m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-7),
                Description = "Electric bill",
                Amount = 124.10m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-3),
                Description = "Coffee shop",
                Amount = 6.50m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-15),
                Description = "Rent payment",
                Amount = 1200.00m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-14),
                Description = "Pharmacy",
                Amount = 28.90m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-9),
                Description = "Online shopping",
                Amount = 64.99m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-6),
                Description = "Restaurant",
                Amount = 42.75m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-4),
                Description = "Transit card top-up",
                Amount = 25.00m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceCheckingId,
                OccurredAt = now.AddDays(-1),
                Description = "ATM withdrawal",
                Amount = 100.00m,
                Type = TransactionType.Debit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceSavingsId,
                OccurredAt = now.AddDays(-20),
                Description = "Interest credit",
                Amount = 12.35m,
                Type = TransactionType.Credit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = aliceSavingsId,
                OccurredAt = now.AddDays(-5),
                Description = "Transfer from checking",
                Amount = 500.00m,
                Type = TransactionType.Credit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = bobCheckingId,
                OccurredAt = now.AddDays(-8),
                Description = "Freelance payment",
                Amount = 750.00m,
                Type = TransactionType.Credit
            },
            new()
            {
                Id = Guid.NewGuid(),
                AccountId = bobCheckingId,
                OccurredAt = now.AddDays(-2),
                Description = "Gas station",
                Amount = 45.20m,
                Type = TransactionType.Debit
            }
        };

        var payees = new List<Payee>
        {
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = aliceId,
                Name = "City Power Co",
                AccountNumber = "9009009001",
                BankName = "Utility Clearing Bank"
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = aliceId,
                Name = "Landlord LLC",
                AccountNumber = "9009009002",
                BankName = "Metro Trust"
            },
            new()
            {
                Id = Guid.NewGuid(),
                CustomerId = bobId,
                Name = "Internet Provider",
                AccountNumber = "9009009003",
                BankName = "Utility Clearing Bank"
            }
        };

        db.Customers.AddRange(alice, bob);
        db.Accounts.AddRange(accounts);
        db.Transactions.AddRange(transactions);
        db.Payees.AddRange(payees);
        await db.SaveChangesAsync();
    }
}
