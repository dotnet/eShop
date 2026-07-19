namespace eShop.Coupon.API.Extensions;

using eShop.Coupon.API.Persistence;
using eShop.Coupon.Application.Interfaces;
using eShop.Coupon.Application.Mappings;
using eShop.Coupon.Infrastructure.Persistence;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Avoid loading full database config and migrations if startup
        // is being invoked from build-time OpenAPI generation
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<CouponContext>();
            return;
        }

        builder.AddNpgsqlDbContext<CouponContext>("coupondb", configureDbContextOptions: dbContextOptionsBuilder =>
        {
            dbContextOptionsBuilder.UseNpgsql(builder =>
            {
                // Add any custom NpgsqlDbContext options here
            });
        });

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<CouponContext, CouponContextSeed>();

        // Add the application services
        builder.Services.AddScoped<CouponService>();
        builder.Services.AddScoped<ICouponDataAccess, CouponDataAccess>();

        // Add AutoMapper
        builder.Services.AddAutoMapper(typeof(CouponProfile));

        // Add health checks
        builder.Services.AddHealthChecks()
            .AddDbContextCheck<CouponContext>();
    }
}
