using DataArc.Abstractions;

using Eshop.Persistence.Models.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EShop.Persistence.Contexts.Identity
{
    public class EShopIdentityContext : IdentityDbContext<EShopUser, IdentityRole<int>, int>, IPipelineContext<EShopIdentityContext>
    {
        public EShopIdentityContext(DbContextOptions<EShopIdentityContext> options)
            : base(options)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<EShopUser>(user =>
            {
                user.Property(u => u.Id)
                    .ValueGeneratedOnAdd()
                    .UseIdentityColumn();
            });
        }
    }
}