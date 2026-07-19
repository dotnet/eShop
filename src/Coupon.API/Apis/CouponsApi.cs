namespace eShop.Coupon.API.Apis;

using System.ComponentModel;
using eShop.Coupon.Application.Dtos;
using eShop.Coupon.Application.Services;
using eShop.Coupon.Domain.Exceptions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

public static class CouponsApi
{
    public static IEndpointRouteBuilder MapCouponsApi(this IEndpointRouteBuilder app)
    {
        var vApi = app.NewVersionedApi("Coupons");
        var api = vApi.MapGroup("api/v{version:apiVersion}/coupons")
            .HasApiVersion(1, 0);

        // POST: Create a new coupon
        api.MapPost("", CreateCoupon)
            .WithName("CreateCoupon")
            .WithSummary("Create a new coupon")
            .WithDescription("Create a new coupon with the specified details")
            .WithTags("Coupons")
            .Produces<CouponDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        // GET: Get coupon by ID
        api.MapGet("{id:guid}", GetCouponById)
            .WithName("GetCoupon")
            .WithSummary("Get a coupon by ID")
            .WithDescription("Retrieve a coupon by its unique identifier")
            .WithTags("Coupons")
            .Produces<CouponDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // GET: Get coupon by code
        api.MapGet("code/{code}", GetCouponByCode)
            .WithName("GetCouponByCode")
            .WithSummary("Get a coupon by code")
            .WithDescription("Retrieve a coupon by its code")
            .WithTags("Coupons")
            .Produces<CouponDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // PUT: Update a coupon
        api.MapPut("{id:guid}", UpdateCoupon)
            .WithName("UpdateCoupon")
            .WithSummary("Update a coupon")
            .WithDescription("Update coupon properties")
            .WithTags("Coupons")
            .Produces<CouponDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // DELETE: Delete a coupon
        api.MapDelete("{id:guid}", DeleteCoupon)
            .WithName("DeleteCoupon")
            .WithSummary("Delete a coupon")
            .WithDescription("Delete a coupon (soft delete)")
            .WithTags("Coupons")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<Created<CouponDto>> CreateCoupon(
        CreateCouponRequest request,
        [FromServices] CouponService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var couponDto = await service.CreateCouponAsync(request, cancellationToken);
            return TypedResults.Created($"/api/v1/coupons/{couponDto.Id}", couponDto);
        }
        catch (CouponException ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }

    private static async Task<Ok<CouponDto>> GetCouponById(
        Guid id,
        [FromServices] CouponService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var couponDto = await service.GetCouponByIdAsync(id, cancellationToken);
            return TypedResults.Ok(couponDto);
        }
        catch (CouponException)
        {
            throw new InvalidOperationException($"Coupon with ID '{id}' not found.");
        }
    }

    private static async Task<Ok<CouponDto>> GetCouponByCode(
        string code,
        [FromServices] CouponService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var couponDto = await service.GetCouponByCodeAsync(code, cancellationToken);
            return TypedResults.Ok(couponDto);
        }
        catch (CouponException)
        {
            throw new InvalidOperationException($"Coupon with code '{code}' not found.");
        }
    }

    private static async Task<Ok<CouponDto>> UpdateCoupon(
        Guid id,
        UpdateCouponRequest request,
        [FromServices] CouponService service,
        CancellationToken cancellationToken)
    {
        try
        {
            var couponDto = await service.UpdateCouponAsync(id, request, cancellationToken);
            return TypedResults.Ok(couponDto);
        }
        catch (CouponException ex)
        {
            throw new InvalidOperationException(ex.Message);
        }
    }

    private static async Task<NoContent> DeleteCoupon(
        Guid id,
        [FromServices] CouponService service,
        CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteCouponAsync(id, cancellationToken);
            return TypedResults.NoContent();
        }
        catch (CouponException)
        {
            throw new InvalidOperationException($"Coupon with ID '{id}' not found.");
        }
    }
}
