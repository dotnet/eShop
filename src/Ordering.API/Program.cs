using eShop.Ordering.API.Apis;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddApplicationServices();
builder.Services.AddProblemDetails();

var withApiVersioning = builder.Services.AddApiVersioning();

builder.AddDefaultOpenApi(withApiVersioning);

var app = builder.Build();

app.MapDefaultEndpoints();

var orders = app.NewVersionedApi("Orders");

orders.MapOrdersApiV1()
      .RequireAuthorization();

var promotions = app.NewVersionedApi("Promotions");

promotions.MapPromotionsApiV1()
          .RequireAuthorization();

app.UseDefaultOpenApi();
app.Run();
