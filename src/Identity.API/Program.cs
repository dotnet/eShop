var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddControllersWithViews();

builder.AddNpgsqlDbContext<ApplicationDbContext>("identitydb");

// Apply database migration automatically. Note that this approach is not
// recommended for production scenarios. Consider generating SQL scripts from
// migrations instead.
builder.Services.AddMigration<ApplicationDbContext, UsersSeed>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
    {
        //options.IssuerUri = "null";
        options.Authentication.CookieLifetime = TimeSpan.FromHours(2);

        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseInformationEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;

        // TODO: Remove this line in production.
        options.KeyManagement.Enabled = false;
    })
    .AddInMemoryIdentityResources(Config.GetResources())
    .AddInMemoryApiScopes(Config.GetApiScopes())
    .AddInMemoryApiResources(Config.GetApis())
    .AddInMemoryClients(Config.GetClients(builder.Configuration))
    .AddAspNetIdentity<ApplicationUser>()
// TODO: Not recommended for production - you need to store your key material somewhere secure
    .AddDeveloperSigningCredential();

builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            throw new InvalidOperationException(
                "Google Client ID and Client Secret must be set in environment variables.");
        }

        options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;
        options.ClientId = clientId;
        options.ClientSecret = clientSecret;
    });

builder.Services.AddTransient<IProfileService, ProfileService>();
builder.Services.AddTransient<ILoginService<ApplicationUser>, EFLoginService>();
builder.Services.AddTransient<IRedirectService, RedirectService>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseStaticFiles();

// This cookie policy fixes login issues with Chrome 80+ using HTTP
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapDefaultControllerRoute();

app.Run();
