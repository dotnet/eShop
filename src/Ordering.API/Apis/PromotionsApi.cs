using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Infrastructure;

namespace eShop.Ordering.API.Apis;

public static class PromotionsApi
{
    public static RouteGroupBuilder MapPromotionsApiV1(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("api/promotions").HasApiVersion(1.0);

        api.MapGet("/", GetPromotionsAsync);
        api.MapGet("{id:int}", GetPromotionByIdAsync).WithName("GetPromotionById");
        api.MapPost("/", CreatePromotionAsync);
        api.MapPut("{id:int}", UpdatePromotionAsync);
        api.MapDelete("{id:int}", DeletePromotionAsync);

        return api;
    }

    public static async Task<Results<Ok<IEnumerable<PromotionDTO>>, BadRequest<string>>> GetPromotionsAsync(
        IPromotionRepository repository)
    {
        var promotions = await repository.GetActivePromotionsAsync();
        var dtos = promotions.Select(MapToDTO);
        return TypedResults.Ok(dtos);
    }

    public static async Task<Results<Ok<PromotionDTO>, NotFound>> GetPromotionByIdAsync(
        int id,
        IPromotionRepository repository)
    {
        var promotion = await repository.GetByIdAsync(id);
        if (promotion is null)
        {
            return TypedResults.NotFound();
        }
        return TypedResults.Ok(MapToDTO(promotion));
    }

    public static async Task<Results<Created, BadRequest<string>>> CreatePromotionAsync(
        PromotionDTO promotionDto,
        IPromotionRepository repository,
        OrderingContext dbContext)
    {
        try
        {
            // Parse discount type
            if (!Enum.TryParse<DiscountType>(promotionDto.DiscountType, out var discountType))
            {
                return TypedResults.BadRequest($"Invalid discount type: {promotionDto.DiscountType}");
            }

            // Create promotion entity
            var promotion = new Promotion(
                promotionDto.Name,
                discountType,
                promotionDto.DiscountValue,
                promotionDto.StartDate,
                promotionDto.EndDate,
                promotionDto.Priority,
                promotionDto.MinimumOrderAmount,
                promotionDto.MaximumDiscount,
                promotionDto.MinimumQuantity);

            // Set IsActive if provided (default is true from constructor)
            if (!promotionDto.IsActive)
            {
                promotion.Deactivate();
            }

            // Add to repository
            repository.Add(promotion);
            await dbContext.SaveChangesAsync();

            // Return 201 Created with location
            return TypedResults.Created($"/api/promotions/{promotion.Id}");
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<NoContent, NotFound, BadRequest<string>>> UpdatePromotionAsync(
        int id,
        PromotionDTO promotionDto,
        IPromotionRepository repository,
        OrderingContext dbContext)
    {
        try
        {
            var promotion = await repository.GetByIdAsync(id);
            if (promotion is null)
            {
                return TypedResults.NotFound();
            }

            // Update promotion details
            promotion.Update(
                promotionDto.Name,
                promotionDto.DiscountValue,
                promotionDto.StartDate,
                promotionDto.EndDate,
                promotionDto.Priority,
                promotionDto.MinimumOrderAmount,
                promotionDto.MaximumDiscount,
                promotionDto.MinimumQuantity);

            // Update categories
            promotion.UpdateCategories(
                promotionDto.ApplicableCategories ?? Enumerable.Empty<string>(),
                promotionDto.ExcludedCategories ?? Enumerable.Empty<string>());

            // Handle IsActive status changes
            if (promotionDto.IsActive && !promotion.IsActive)
            {
                promotion.Activate();
            }
            else if (!promotionDto.IsActive && promotion.IsActive)
            {
                promotion.Deactivate();
            }

            // Save changes
            repository.Update(promotion);
            await dbContext.SaveChangesAsync();

            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }
    }

    public static async Task<Results<NoContent, NotFound>> DeletePromotionAsync(
        int id,
        IPromotionRepository repository,
        OrderingContext dbContext)
    {
        var promotion = await repository.GetByIdAsync(id);
        if (promotion is null)
        {
            return TypedResults.NotFound();
        }

        repository.Delete(promotion);
        await dbContext.SaveChangesAsync();

        return TypedResults.NoContent();
    }

    private static PromotionDTO MapToDTO(Promotion promotion)
    {
        return new PromotionDTO(
            promotion.Id,
            promotion.Name,
            promotion.DiscountType.ToString(),
            promotion.DiscountValue,
            promotion.StartDate,
            promotion.EndDate,
            promotion.MinimumOrderAmount,
            promotion.MaximumDiscount,
            promotion.MinimumQuantity,
            promotion.IsActive,
            promotion.Priority,
            promotion.ApplicableCategories,
            promotion.ExcludedCategories);
    }
}

public record PromotionDTO(
    int Id,
    string Name,
    string DiscountType,
    decimal DiscountValue,
    DateTime StartDate,
    DateTime EndDate,
    decimal? MinimumOrderAmount,
    decimal? MaximumDiscount,
    int? MinimumQuantity,
    bool IsActive,
    int Priority,
    IEnumerable<string> ApplicableCategories = null,
    IEnumerable<string> ExcludedCategories = null);
