using GtKram.Application.UseCases.User.Models;
using GtKram.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GtKram.Infrastructure.Persistence;

internal sealed class RoleSeeder : IEntityTypeConfiguration<IdentityRoleGuid>
{
    private readonly Guid _adminId = Guid.Parse("D335DB90-9B76-45B6-A615-30AD94704A0B");
    private readonly Guid _managerId = Guid.Parse("FE670F84-4AC1-4C8C-B73F-BF6ADBD620A8");
    private readonly Guid _sellerId = Guid.Parse("A0026E94-2A0F-4E09-B81E-6EF6330C9E96");
    private readonly Guid _helperId = Guid.Parse("366295FD-E7A8-412D-81D0-4E423EDBC754");

    public void Configure(EntityTypeBuilder<IdentityRoleGuid> builder)
    {
        builder.HasData(
            new IdentityRoleGuid
            {
                Id = _adminId,
                Name = Roles.Admin,
                NormalizedName = Roles.Admin.ToUpperInvariant(),
                ConcurrencyStamp = "282BF017-140B-4E0F-A6FF-BE94953118B8"
            },
            new IdentityRoleGuid
            {
                Id = _managerId,
                Name = Roles.Manager,
                NormalizedName = Roles.Manager.ToUpperInvariant(),
                ConcurrencyStamp = "69979B19-80AD-4584-9BDD-E2F5F82F13A1"
            },
            new IdentityRoleGuid
            {
                Id = _sellerId,
                Name = Roles.Seller,
                NormalizedName = Roles.Seller.ToUpperInvariant(),
                ConcurrencyStamp = "3C1F9AB6-EF9F-4699-9937-5554BEA706B0"
            },
            new IdentityRoleGuid
            {
                Id = _helperId,
                Name = Roles.Billing,
                NormalizedName = Roles.Billing.ToUpperInvariant(),
                ConcurrencyStamp = "274A03E0-CB30-4324-9A27-90A5915E6C84"
            }
        );
    }
}
