using Microsoft.EntityFrameworkCore;
using OnlineBanking.Api.Models;

namespace OnlineBanking.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Payee> Payees => Set<Payee>();
    public DbSet<Transfer> Transfers => Set<Transfer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(c => c.Username).IsUnique();
            entity.Property(c => c.Username).HasMaxLength(64);
            entity.Property(c => c.FullName).HasMaxLength(128);
            entity.Property(c => c.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(a => a.AccountNumber).IsUnique();
            entity.Property(a => a.Balance).HasPrecision(18, 2);
            entity.Property(a => a.Name).HasMaxLength(64);
            entity.Property(a => a.AccountNumber).HasMaxLength(32);
            entity.Property(a => a.Currency).HasMaxLength(3);
            entity.HasOne(a => a.Customer)
                .WithMany(c => c.Accounts)
                .HasForeignKey(a => a.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.Description).HasMaxLength(256);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(128);
            entity.HasIndex(t => t.IdempotencyKey);
            entity.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Payee>(entity =>
        {
            entity.Property(p => p.Name).HasMaxLength(128);
            entity.Property(p => p.AccountNumber).HasMaxLength(32);
            entity.Property(p => p.BankName).HasMaxLength(128);
            entity.HasOne(p => p.Customer)
                .WithMany(c => c.Payees)
                .HasForeignKey(p => p.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Transfer>(entity =>
        {
            entity.Property(t => t.Amount).HasPrecision(18, 2);
            entity.Property(t => t.IdempotencyKey).HasMaxLength(128);
            entity.Property(t => t.Note).HasMaxLength(200);
            entity.HasIndex(t => new { t.CustomerId, t.IdempotencyKey }).IsUnique();

            entity.HasOne(t => t.Customer)
                .WithMany()
                .HasForeignKey(t => t.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.SourceAccount)
                .WithMany()
                .HasForeignKey(t => t.SourceAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.DestinationAccount)
                .WithMany()
                .HasForeignKey(t => t.DestinationAccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.Payee)
                .WithMany()
                .HasForeignKey(t => t.PayeeId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
