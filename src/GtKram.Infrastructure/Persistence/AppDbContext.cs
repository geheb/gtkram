using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace GtKram.Infrastructure.Persistence;

internal sealed class AppDbContext
    : IdentityDbContext<IdentityUserGuid, IdentityRoleGuid, Guid, IdentityUserClaimGuid, IdentityUserRoleGuid, IdentityUserLoginGuid, IdentityRoleClaimGuid, IdentityUserTokenGuid>
    , IDataProtectionKeyContext
{
    const string KeyType = "binary(16)";

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet(CharSet.Utf8Mb4, DelegationModes.ApplyToDatabases);

        CreateDataProtection(modelBuilder);
        CreateIdentity(modelBuilder);
        CreateEmailQueue(modelBuilder);
        CreateBazaar(modelBuilder);
    }

    private void CreateBazaar(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BazaarEvent>(eb =>
        {
            eb.ToTable("bazaar_events");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.Name).IsRequired().HasMaxLength(128);
            eb.Property(e => e.Description).HasMaxLength(1024);
            eb.Property(e => e.Address).HasMaxLength(256);
            eb.Property(e => e.StartDate).IsRequired();
            eb.Property(e => e.EndDate).IsRequired();
            eb.Property(e => e.MaxSellers).IsRequired().HasDefaultValue(100);
            eb.Property(e => e.Commission).IsRequired().HasDefaultValue(20);
            eb.Property(e => e.RegisterStartDate).IsRequired();
            eb.Property(e => e.RegisterEndDate).IsRequired();

            eb.HasMany(e => e.SellerRegistrations)
                .WithOne(e => e.BazaarEvent)
                .HasForeignKey(e => e.BazaarEventId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasMany(e => e.BazaarSellers)
                .WithOne(e => e.BazaarEvent)
                .HasForeignKey(e => e.BazaarEventId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        });

        modelBuilder.Entity<BazaarSellerRegistration>(eb =>
        {
            eb.ToTable("bazaar_seller_registrations");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.Email).IsRequired().HasMaxLength(256);
            eb.Property(e => e.Name).IsRequired().HasMaxLength(64);
            eb.Property(e => e.Phone).IsRequired().HasMaxLength(32);
            eb.Property(e => e.Clothing).HasMaxLength(1024);
            eb.Property(e => e.PreferredType).IsRequired().HasDefaultValue(0);
            eb.Property(e => e.BazaarEventId).HasColumnType(KeyType);
            eb.Property(e => e.BazaarSellerId).HasColumnType(KeyType);

            eb.HasOne(e => e.BazaarEvent)
                .WithMany(e => e.SellerRegistrations)
                .HasForeignKey(e => e.BazaarEventId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasOne(e => e.BazaarSeller)
                .WithOne(e => e.BazaarSellerRegistration)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasIndex(e => new { e.BazaarEventId, e.Email }).IsUnique();
        });

        modelBuilder.Entity<BazaarSeller>(eb =>
        {
            eb.ToTable("bazaar_sellers");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.BazaarEventId).HasColumnType(KeyType);
            eb.Property(e => e.UserId).HasColumnType(KeyType);
            eb.Property(e => e.SellerNumber).IsRequired();
            eb.Property(e => e.Role).IsRequired();
            eb.Property(e => e.CanCreateBillings).IsRequired().HasDefaultValue(false);

            eb.HasOne(e => e.BazaarEvent)
                .WithMany(e => e.BazaarSellers)
                .HasForeignKey(e => e.BazaarEventId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasOne(e => e.User)
                .WithMany(e => e.BazaarSellers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasIndex(e => new { e.BazaarEventId, e.UserId }).IsUnique();
        });

        modelBuilder.Entity<BazaarSellerArticle>(eb =>
        {
            eb.ToTable("bazaar_seller_articles");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.BazaarSellerId).HasColumnType(KeyType);
            eb.Property(e => e.LabelNumber).IsRequired();
            eb.Property(e => e.Name).HasMaxLength(256).IsRequired();
            eb.Property(e => e.Size).HasMaxLength(16);
            eb.Property(e => e.Price).HasColumnType("decimal(6,2)").IsRequired();

            eb.HasOne(e => e.BazaarSeller)
                .WithMany(e => e.BazaarSellerArticles)
                .HasForeignKey(e => e.BazaarSellerId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        });

        modelBuilder.Entity<BazaarBilling>(eb =>
        {
            eb.ToTable("bazaar_billings");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.Status).IsRequired();
            eb.Property(e => e.BazaarEventId).HasColumnType(KeyType);
            eb.Property(e => e.UserId).HasColumnType(KeyType);

            eb.HasOne(e => e.BazaarEvent)
                .WithMany(e => e.BazaarBillings)
                .HasForeignKey(e => e.BazaarEventId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasOne(e => e.User)
                .WithMany(e => e.BazaarBillings)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        });

        modelBuilder.Entity<BazaarBillingArticle>(eb =>
        {
            eb.ToTable("bazaar_billing_articles");
            eb.Property(e => e.Id).HasColumnType(KeyType).ValueGeneratedNever();
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.BazaarBillingId).HasColumnType(KeyType);
            eb.Property(e => e.BazaarSellerArticleId).HasColumnType(KeyType);

            eb.HasOne(e => e.BazaarBilling)
                .WithMany(e => e.BazaarBillingArticles)
                .HasForeignKey(e => e.BazaarBillingId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            eb.HasOne(e => e.BazaarSellerArticle)
                .WithOne(e => e.BazaarBillingArticle)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
        });
    }

    private void CreateEmailQueue(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EmailQueue>(eb =>
        {
            eb.ToTable("email_queue");

            eb.Property(e => e.Id).HasColumnType(KeyType);
            eb.Property(e => e.CreatedOn).IsRequired();
            eb.Property(e => e.Recipient).IsRequired();
            eb.Property(e => e.Subject).IsRequired();
            eb.Property(e => e.Body).IsRequired();

            eb.HasIndex(e => e.CreatedOn);
        });
    }

    private void CreateDataProtection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataProtectionKey>(eb =>
        {
            eb.Property(e => e.Id).UseMySqlIdentityColumn();
            eb.ToTable("data_protection_keys");
        });
    }

    private void CreateIdentity(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new RoleSeeder());

        modelBuilder.Entity<IdentityUserGuid>(eb =>
        {
            eb.Property(e => e.Id).HasColumnType(KeyType);
            eb.Property(e => e.Name).HasMaxLength(256);

            eb.HasMany(e => e.UserRoles)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .IsRequired();

            eb.ToTable("users");
        });

        modelBuilder.Entity<IdentityRoleGuid>(eb =>
        {
            eb.Property(e => e.Id).HasColumnType(KeyType);

            eb.HasMany(e => e.UserRoles)
                .WithOne(e => e.Role)
                .HasForeignKey(e => e.RoleId)
                .IsRequired();

            eb.ToTable("roles");
        });

        modelBuilder.Entity<IdentityUserRoleGuid>(eb =>
        {
            eb.Property(e => e.UserId).HasColumnType(KeyType);
            eb.Property(e => e.RoleId).HasColumnType(KeyType);
            eb.ToTable("user_roles");
        });

        modelBuilder.Entity<IdentityUserLoginGuid>(eb =>
        {
            eb.Property(e => e.UserId).HasColumnType(KeyType);
            eb.ToTable("user_logins");
        });


        modelBuilder.Entity<IdentityUserTokenGuid>(eb =>
        {
            eb.Property(e => e.UserId).HasColumnType(KeyType);
            eb.ToTable("user_tokens");
        });


        modelBuilder.Entity<IdentityUserClaimGuid>(eb =>
        {
            eb.Property(e => e.Id).UseMySqlIdentityColumn();
            eb.Property(e => e.UserId).HasColumnType(KeyType);
            eb.ToTable("user_claims");
        });


        modelBuilder.Entity<IdentityRoleClaimGuid>(eb =>
        {
            eb.Property(e => e.Id).UseMySqlIdentityColumn();
            eb.Property(e => e.RoleId).HasColumnType(KeyType);
            eb.ToTable("role_claims");
        });
    }
}
