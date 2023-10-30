using Microsoft.AspNetCore.Http.HttpResults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddAuthenticationServices();
builder.AddApplicationServices();

builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

app.MapDefaultEndpoints();

// Fix samesite issue when running eShop from docker-compose locally as by default http protocol is being used
// Refer to https://github.com/dotnet-architecture/eShopOnContainers/issues/1391
app.UseCookiePolicy(new CookiePolicyOptions { MinimumSameSitePolicy = SameSiteMode.Lax });

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.MapDefaultControllerRoute();
app.MapRazorPages();

bool.TryParse(builder.Configuration["ValidateToken"], out var validateToken);
var tokenToValidate = builder.Configuration["Token"];

app.MapMethods("/check", [HttpMethods.Options], Results<Ok, BadRequest<string>> ([FromHeader(Name = HeaderNames.WebHookCheckHeader)] string value, HttpResponse response) =>
{
    if (!validateToken || value == tokenToValidate)
    {
        if (!string.IsNullOrWhiteSpace(tokenToValidate))
        {
            response.Headers.Append(HeaderNames.WebHookCheckHeader, tokenToValidate);
        }

        return TypedResults.Ok();
    }

    return TypedResults.BadRequest("Invalid token");
});

await app.RunAsync();
