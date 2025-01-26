// <auto-generated />
using System;
using GtKram.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace GtKram.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250120223906_EmailQueue")]
    partial class EmailQueue
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.11")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.HasCharSet(modelBuilder, "utf8mb4", DelegationModes.ApplyToDatabases);
            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarBilling", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("BazaarEventId")
                        .HasColumnType("binary(16)");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<decimal>("Total")
                        .HasColumnType("decimal(8,2)");

                    b.Property<DateTimeOffset?>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<byte[]>("UserId")
                        .HasColumnType("binary(16)");

                    b.HasKey("Id");

                    b.HasIndex("BazaarEventId");

                    b.HasIndex("UserId");

                    b.ToTable("bazaar_billings", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarBillingArticle", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<DateTimeOffset>("AddedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<byte[]>("BazaarBillingId")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("BazaarSellerArticleId")
                        .HasColumnType("binary(16)");

                    b.HasKey("Id");

                    b.HasIndex("BazaarBillingId");

                    b.HasIndex("BazaarSellerArticleId")
                        .IsUnique();

                    b.ToTable("bazaar_billing_articles", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarEvent", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<string>("Address")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<int>("Commission")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(20);

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Description")
                        .HasMaxLength(1024)
                        .HasColumnType("varchar(1024)");

                    b.Property<DateTimeOffset?>("EditArticleEndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("EndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("IsRegistrationsLocked")
                        .HasColumnType("tinyint(1)");

                    b.Property<int>("MaxSellers")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(100);

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<DateTimeOffset?>("PickUpLabelsEndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset?>("PickUpLabelsStartDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("RegisterEndDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("RegisterStartDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset>("StartDate")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset?>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("bazaar_events", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSeller", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("BazaarEventId")
                        .HasColumnType("binary(16)");

                    b.Property<bool>("CanCreateBillings")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("tinyint(1)")
                        .HasDefaultValue(false);

                    b.Property<int>("MaxArticleCount")
                        .HasColumnType("int");

                    b.Property<int>("Role")
                        .HasColumnType("int");

                    b.Property<int>("SellerNumber")
                        .HasColumnType("int");

                    b.Property<byte[]>("UserId")
                        .HasColumnType("binary(16)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.HasIndex("BazaarEventId", "UserId")
                        .IsUnique();

                    b.ToTable("bazaar_sellers", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSellerArticle", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("BazaarSellerId")
                        .HasColumnType("binary(16)");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("LabelNumber")
                        .HasColumnType("int");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<decimal>("Price")
                        .HasColumnType("decimal(6,2)");

                    b.Property<string>("Size")
                        .HasMaxLength(16)
                        .HasColumnType("varchar(16)");

                    b.Property<int>("Status")
                        .HasColumnType("int");

                    b.Property<DateTimeOffset?>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("BazaarSellerId");

                    b.ToTable("bazaar_seller_articles", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSellerRegistration", b =>
                {
                    b.Property<byte[]>("Id")
                        .HasColumnType("binary(16)");

                    b.Property<bool?>("Accepted")
                        .HasColumnType("tinyint(1)");

                    b.Property<byte[]>("BazaarEventId")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("BazaarSellerId")
                        .HasColumnType("binary(16)");

                    b.Property<string>("Clothing")
                        .HasMaxLength(1024)
                        .HasColumnType("varchar(1024)");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Phone")
                        .IsRequired()
                        .HasMaxLength(32)
                        .HasColumnType("varchar(32)");

                    b.Property<int>("PreferredType")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasDefaultValue(0);

                    b.Property<DateTimeOffset?>("UpdatedOn")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("BazaarSellerId")
                        .IsUnique();

                    b.HasIndex("BazaarEventId", "Email")
                        .IsUnique();

                    b.ToTable("bazaar_seller_registrations", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.EmailQueue", b =>
                {
                    b.Property<byte[]>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("AttachmentBlob")
                        .HasColumnType("longblob");

                    b.Property<string>("AttachmentMimeType")
                        .HasColumnType("longtext");

                    b.Property<string>("AttachmentName")
                        .HasColumnType("longtext");

                    b.Property<string>("Body")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset>("CreatedOn")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTimeOffset?>("SentOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Recipient")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Subject")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("CreatedOn");

                    b.ToTable("email_queue", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityRoleClaimGuid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("longtext");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("RoleId")
                        .IsRequired()
                        .HasColumnType("binary(16)");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("role_claims", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityRoleGuid", b =>
                {
                    b.Property<byte[]>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("binary(16)");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("longtext");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("roles", (string)null);

                    b.HasData(
                        new
                        {
                            Id = new byte[] { 144, 219, 53, 211, 118, 155, 182, 69, 166, 21, 48, 173, 148, 112, 74, 11 },
                            ConcurrencyStamp = "282BF017-140B-4E0F-A6FF-BE94953118B8",
                            Name = "admin",
                            NormalizedName = "ADMIN"
                        },
                        new
                        {
                            Id = new byte[] { 132, 15, 103, 254, 193, 74, 140, 76, 183, 63, 191, 106, 219, 214, 32, 168 },
                            ConcurrencyStamp = "69979B19-80AD-4584-9BDD-E2F5F82F13A1",
                            Name = "manager",
                            NormalizedName = "MANAGER"
                        },
                        new
                        {
                            Id = new byte[] { 148, 110, 2, 160, 15, 42, 9, 78, 184, 30, 110, 246, 51, 12, 158, 150 },
                            ConcurrencyStamp = "3C1F9AB6-EF9F-4699-9937-5554BEA706B0",
                            Name = "seller",
                            NormalizedName = "SELLER"
                        },
                        new
                        {
                            Id = new byte[] { 253, 149, 98, 54, 168, 231, 45, 65, 129, 208, 78, 66, 62, 219, 199, 84 },
                            ConcurrencyStamp = "274A03E0-CB30-4324-9A27-90A5915E6C84",
                            Name = "billing",
                            NormalizedName = "BILLING"
                        });
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserClaimGuid", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("ClaimType")
                        .HasColumnType("longtext");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("UserId")
                        .IsRequired()
                        .HasColumnType("binary(16)");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("user_claims", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", b =>
                {
                    b.Property<byte[]>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("binary(16)");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("int");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("longtext");

                    b.Property<DateTimeOffset?>("DisabledOn")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset?>("LastLogin")
                        .HasColumnType("datetime(6)");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("longtext");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("longtext");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("longtext");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserLoginGuid", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("longtext");

                    b.Property<byte[]>("UserId")
                        .IsRequired()
                        .HasColumnType("binary(16)");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("user_logins", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserRoleGuid", b =>
                {
                    b.Property<byte[]>("UserId")
                        .HasColumnType("binary(16)");

                    b.Property<byte[]>("RoleId")
                        .HasColumnType("binary(16)");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("user_roles", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserTokenGuid", b =>
                {
                    b.Property<byte[]>("UserId")
                        .HasColumnType("binary(16)");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Name")
                        .HasColumnType("varchar(255)");

                    b.Property<string>("Value")
                        .HasColumnType("longtext");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("user_tokens", (string)null);
                });

            modelBuilder.Entity("Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("FriendlyName")
                        .HasColumnType("longtext");

                    b.Property<string>("Xml")
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("data_protection_keys", (string)null);
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarBilling", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarEvent", "BazaarEvent")
                        .WithMany("BazaarBillings")
                        .HasForeignKey("BazaarEventId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", "User")
                        .WithMany("BazaarBillings")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("BazaarEvent");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarBillingArticle", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarBilling", "BazaarBilling")
                        .WithMany("BazaarBillingArticles")
                        .HasForeignKey("BazaarBillingId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarSellerArticle", "BazaarSellerArticle")
                        .WithOne("BazaarBillingArticle")
                        .HasForeignKey("GtKram.Infrastructure.Persistence.Entities.BazaarBillingArticle", "BazaarSellerArticleId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("BazaarBilling");

                    b.Navigation("BazaarSellerArticle");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSeller", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarEvent", "BazaarEvent")
                        .WithMany("BazaarSellers")
                        .HasForeignKey("BazaarEventId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", "User")
                        .WithMany("BazaarSellers")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("BazaarEvent");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSellerArticle", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarSeller", "BazaarSeller")
                        .WithMany("BazaarSellerArticles")
                        .HasForeignKey("BazaarSellerId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("BazaarSeller");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSellerRegistration", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarEvent", "BazaarEvent")
                        .WithMany("SellerRegistrations")
                        .HasForeignKey("BazaarEventId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.BazaarSeller", "BazaarSeller")
                        .WithOne("BazaarSellerRegistration")
                        .HasForeignKey("GtKram.Infrastructure.Persistence.Entities.BazaarSellerRegistration", "BazaarSellerId")
                        .OnDelete(DeleteBehavior.NoAction);

                    b.Navigation("BazaarEvent");

                    b.Navigation("BazaarSeller");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityRoleClaimGuid", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityRoleGuid", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserClaimGuid", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserLoginGuid", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserRoleGuid", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityRoleGuid", "Role")
                        .WithMany("UserRoles")
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", "User")
                        .WithMany("UserRoles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Role");

                    b.Navigation("User");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserTokenGuid", b =>
                {
                    b.HasOne("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarBilling", b =>
                {
                    b.Navigation("BazaarBillingArticles");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarEvent", b =>
                {
                    b.Navigation("BazaarBillings");

                    b.Navigation("BazaarSellers");

                    b.Navigation("SellerRegistrations");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSeller", b =>
                {
                    b.Navigation("BazaarSellerArticles");

                    b.Navigation("BazaarSellerRegistration");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.BazaarSellerArticle", b =>
                {
                    b.Navigation("BazaarBillingArticle");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityRoleGuid", b =>
                {
                    b.Navigation("UserRoles");
                });

            modelBuilder.Entity("GtKram.Infrastructure.Persistence.Entities.IdentityUserGuid", b =>
                {
                    b.Navigation("BazaarBillings");

                    b.Navigation("BazaarSellers");

                    b.Navigation("UserRoles");
                });
#pragma warning restore 612, 618
        }
    }
}
