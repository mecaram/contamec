using ContaMec.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ContaMec.Api.Infrastructure.Data;

public class ContaMecDbContext(DbContextOptions<ContaMecDbContext> options) : DbContext(options)
{
    public DbSet<Closure> Closures => Set<Closure>();
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    public DbSet<ClosureBalance> ClosureBalances => Set<ClosureBalance>();
    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Income> Incomes => Set<Income>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Closure>(entity =>
        {
            entity.ToTable("Cierres");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.OpenDate).HasColumnName("OpenDate").HasColumnType("datetime");
            entity.Property(e => e.CloseDate).HasColumnName("CloseDate").HasColumnType("datetime");
            entity.Property(e => e.IsClosed).HasColumnName("IsClosed");
            entity.Property(e => e.Result).HasColumnName("Result").HasColumnType("money");

            entity.HasMany(e => e.Balances)
                .WithOne(b => b.Closure)
                .HasForeignKey(b => b.ClosureId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClosureBalance>(entity =>
        {
            entity.ToTable("ClosureBalances");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.PaymentAccountId).HasColumnName("PaymentAccountId");
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("money");

            entity.HasOne(e => e.PaymentAccount)
                .WithMany(t => t.ClosureBalances)
                .HasForeignKey(e => e.PaymentAccountId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentAccount>(entity =>
        {
            entity.ToTable("PaymentAccounts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Type).HasColumnName("Type").HasMaxLength(50).IsUnicode(false);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.EmissionDate).HasColumnName("EmissionDate").HasColumnType("datetime");
            entity.Property(e => e.AccountId).HasColumnName("AccountId");
            entity.Property(e => e.Detail).HasColumnName("Detail").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("money");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime");

            entity.HasOne(e => e.Closure)
                .WithMany(c => c.Expenses)
                .HasForeignKey(e => e.ClosureId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.ExpensesCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Income>(entity =>
        {
            entity.ToTable("Incomes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.EmissionDate).HasColumnName("EmissionDate").HasColumnType("datetime");
            entity.Property(e => e.AccountId).HasColumnName("AccountId");
            entity.Property(e => e.Detail).HasColumnName("Detail").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("money");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("datetime");

            entity.HasOne(e => e.Closure)
                .WithMany(c => c.Incomes)
                .HasForeignKey(e => e.ClosureId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.Account)
                .WithMany(a => a.Incomes)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.IncomesCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(256).IsUnicode(false);
            entity.Property(e => e.IsActive).HasColumnName("IsActive");
            entity.Property(e => e.UserRoleId).HasColumnName("UserRoleId");

            entity.HasIndex(e => e.Name).IsUnique();

            entity.HasOne(e => e.UserRole)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.UserRoleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
        });
    }
}
