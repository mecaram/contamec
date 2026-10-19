using ContaMec.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.NameTranslation;

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
    public DbSet<SubAccount> SubAccounts => Set<SubAccount>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    // Calificado: "Application" choca con el namespace ContaMec.Api.Application.
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // En Supabase los Id/FK son bigint; en C# se mantienen como int.
        configurationBuilder.Properties<int>().HaveConversion<long>();
        configurationBuilder.Properties<DateTime>().HaveConversion<WallClockDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<AccountType>("public", "AccountTypes", new NpgsqlNullNameTranslator());
        modelBuilder.HasPostgresEnum<ApplicationType>("public", "ApplicationTypes", new NpgsqlNullNameTranslator());
        modelBuilder.HasPostgresEnum<BehaviorType>("public", "BehaviorTypes", new NpgsqlNullNameTranslator());
        modelBuilder.HasPostgresEnum<OccurrenceType>("public", "OccurrenceTypes", new NpgsqlNullNameTranslator());
        modelBuilder.HasPostgresEnum<RateType>("public", "RateTypes", new NpgsqlNullNameTranslator());

        modelBuilder.Entity<Domain.Entities.Application>(entity =>
        {
            entity.ToTable("Applications");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").IsRequired();
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.Active).HasColumnName("Active");
            entity.Property(e => e.WhatsApp).HasColumnName("WhatsApp");
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.ApplicationTypeDate).HasColumnName("ApplicationTypeDate").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.Email).HasColumnName("Email");
            entity.Property(e => e.Address).HasColumnName("Address");
            entity.Property(e => e.Locality).HasColumnName("Locality");
            entity.Property(e => e.Guid).HasColumnName("Guid");
        });

        modelBuilder.Entity<Closure>(entity =>
        {
            entity.ToTable("Closures");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.OpenDate).HasColumnName("OpenDate").HasColumnType("timestamp with time zone");
            entity.Property(e => e.CloseDate).HasColumnName("CloseDate").HasColumnType("timestamp with time zone");
            entity.Property(e => e.IsClosed).HasColumnName("isClosed");
            entity.Property(e => e.Result).HasColumnName("Result").HasColumnType("numeric");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

            entity.HasMany(e => e.Balances)
                .WithOne(b => b.Closure)
                .HasForeignKey(b => b.ClosureId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Closures)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ClosureBalance>(entity =>
        {
            entity.ToTable("ClosureBalances");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.PaymentAccountId).HasColumnName("PaymentAccountId");
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("numeric");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

            entity.HasOne(e => e.PaymentAccount)
                .WithMany(t => t.ClosureBalances)
                .HasForeignKey(e => e.PaymentAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.ClosureBalances)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<PaymentAccount>(entity =>
        {
            entity.ToTable("PaymentAccounts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.PaymentAccounts)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Account>(entity =>
        {
            entity.ToTable("Accounts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.Type).HasColumnName("Type");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.SubAccountRequires).HasColumnName("SubAccountRequires");
            entity.Property(e => e.Active).HasColumnName("Active");
            entity.Property(e => e.BehaviorType).HasColumnName("BehaviorType");

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Accounts)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<SubAccount>(entity =>
        {
            entity.ToTable("SubAccounts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.AccountId).HasColumnName("AccountId");
            entity.Property(e => e.Active).HasColumnName("Active");
            entity.Property(e => e.BehaviorType).HasColumnName("BehaviorType");

            entity.HasOne(e => e.Account)
                .WithMany(a => a.SubAccounts)
                .HasForeignKey(e => e.AccountId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<ExchangeRate>(entity =>
        {
            entity.ToTable("ExchangeRates");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.CreatedAt).HasColumnName("Created_at").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.RateDate).HasColumnName("RateDate").HasColumnType("timestamp with time zone").HasDefaultValueSql("now()");
            entity.Property(e => e.RateType).HasColumnName("RateType");
            entity.Property(e => e.BuyRate).HasColumnName("BuyRate").HasColumnType("numeric");
            entity.Property(e => e.SellRate).HasColumnName("SellRate").HasColumnType("numeric");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId");

            entity.HasOne(e => e.CreatedByUser)
                .WithMany(u => u.ExchangeRatesCreated)
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Expense>(entity =>
        {
            entity.ToTable("Expenses");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.EmissionDate).HasColumnName("EmissionDate").HasColumnType("timestamp with time zone");
            entity.Property(e => e.AccountId).HasColumnName("AccountId");
            entity.Property(e => e.Detail).HasColumnName("Detail").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("numeric(12,2)");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone");

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

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Columna ya existente en Supabase; los egresos anteriores pueden no tenerla cargada.
            entity.Property(e => e.PaymentAccountId).HasColumnName("PaymentAccountId");

            entity.HasOne(e => e.PaymentAccount)
                .WithMany(a => a.Expenses)
                .HasForeignKey(e => e.PaymentAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.SubAccountId).HasColumnName("SubAccountId");
            entity.Property(e => e.OccurrenceType).HasColumnName("OccurrenceType");
            entity.Property(e => e.ExchangeRateId).HasColumnName("ExchangeRateId");

            entity.HasOne(e => e.SubAccount)
                .WithMany(s => s.Expenses)
                .HasForeignKey(e => e.SubAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ExchangeRate)
                .WithMany(r => r.Expenses)
                .HasForeignKey(e => e.ExchangeRateId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Income>(entity =>
        {
            entity.ToTable("Incomes");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.ClosureId).HasColumnName("ClosureId");
            entity.Property(e => e.EmissionDate).HasColumnName("EmissionDate").HasColumnType("timestamp with time zone");
            entity.Property(e => e.AccountId).HasColumnName("AccountId");
            entity.Property(e => e.Detail).HasColumnName("Detail").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.Amount).HasColumnName("Amount").HasColumnType("numeric(12,2)");
            entity.Property(e => e.CreatedByUserId).HasColumnName("CreatedByUserId");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone");

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

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Incomes)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);

            // Columna ya existente en Supabase; los ingresos anteriores pueden no tenerla cargada.
            entity.Property(e => e.PaymentAccountId).HasColumnName("PaymentAccountId");

            entity.HasOne(e => e.PaymentAccount)
                .WithMany(a => a.Incomes)
                .HasForeignKey(e => e.PaymentAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.SubAccountId).HasColumnName("SubAccountId");
            entity.Property(e => e.OccurrenceType).HasColumnName("OccurrenceType");
            entity.Property(e => e.ExchangeRateId).HasColumnName("ExchangeRateId");

            entity.HasOne(e => e.SubAccount)
                .WithMany(s => s.Incomes)
                .HasForeignKey(e => e.SubAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ExchangeRate)
                .WithMany(r => r.Incomes)
                .HasForeignKey(e => e.ExchangeRateId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(80).IsUnicode(false);
            entity.Property(e => e.PasswordHash).HasColumnName("PasswordHash").HasMaxLength(256).IsUnicode(false);
            entity.Property(e => e.IsActive).HasColumnName("Active");
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone");
            entity.Property(e => e.LastLoginAt).HasColumnName("LastLoginAt").HasColumnType("timestamp with time zone");
            entity.Property(e => e.UserRoleId).HasColumnName("UserRoleId");

            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.UserRole)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.UserRoleId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.Property(e => e.ApplicationId).HasColumnName("ApplicationId");

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Users)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Name).HasColumnName("Name").HasMaxLength(50).IsUnicode(false);
            entity.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamp with time zone");
        });
    }
}
