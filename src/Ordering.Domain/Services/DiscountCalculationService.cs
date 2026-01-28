namespace eShop.Ordering.Domain.Services;

using eShop.Ordering.Domain.AggregatesModel.OrderAggregate;
using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public class DiscountCalculationService : IDiscountCalculationService
{
    private const decimal GlobalDiscountCapPercentage = 0.50m; // 50% cap
    private readonly IDiscountStrategyFactory _strategyFactory;

    public DiscountCalculationService(IDiscountStrategyFactory strategyFactory)
    {
        _strategyFactory = strategyFactory ?? throw new ArgumentNullException(nameof(strategyFactory));
    }

    public DiscountCalculationResult Calculate(Order order, IEnumerable<Promotion> promotions, DiscountContext context)
    {
        if (order == null)
        {
            throw new ArgumentNullException(nameof(order));
        }

        // Step 1: Calculate order subtotal
        var orderSubtotal = order.GetTotal();
        var appliedDiscounts = new List<AppliedDiscount>();
        var skippedPromotions = new List<string>();

        // Step 2: Handle edge cases
        if (promotions == null || !promotions.Any())
        {
            return new DiscountCalculationResult(orderSubtotal, appliedDiscounts, skippedPromotions);
        }

        // Step 3: Filter active promotions
        var activePromotions = FilterActivePromotions(promotions, orderSubtotal, DateTime.UtcNow, skippedPromotions);

        // Step 4: Sort by priority (ascending = higher priority)
        var sortedPromotions = activePromotions.OrderBy(p => p.Priority).ToList();

        // Step 5: Apply discounts
        decimal totalDiscountApplied = 0m;
        decimal globalDiscountCap = orderSubtotal * GlobalDiscountCapPercentage;
        var customerSpecificTypesApplied = new HashSet<DiscountType>();

        foreach (var promotion in sortedPromotions)
        {
            // Rule 2: Prevent stacking multiple customer-specific discounts of the same type
            if (promotion.DiscountType == DiscountType.FirstTimeCustomerDiscount)
            {
                if (customerSpecificTypesApplied.Contains(DiscountType.FirstTimeCustomerDiscount))
                {
                    // Skip - already applied a FirstTimeCustomerDiscount
                    continue;
                }
                customerSpecificTypesApplied.Add(DiscountType.FirstTimeCustomerDiscount);
            }

            // Get strategy and calculate discount
            var strategy = _strategyFactory.CreateStrategy(promotion.DiscountType);
            var calculatedDiscount = strategy.CalculateDiscount(promotion, context);

            // Apply per-promotion maximum discount cap
            if (promotion.MaximumDiscount.HasValue && calculatedDiscount > promotion.MaximumDiscount.Value)
            {
                calculatedDiscount = promotion.MaximumDiscount.Value;
            }

            // Check global 50% cap
            var remainingCap = globalDiscountCap - totalDiscountApplied;
            var discountToApply = Math.Min(calculatedDiscount, remainingCap);

            // Apply the discount (even if partially reduced)
            if (discountToApply > 0)
            {
                var appliedDiscount = new AppliedDiscount(
                    promotion.Id.ToString(),
                    promotion.Name,
                    discountToApply,
                    DateTime.UtcNow,
                    context.Items.Count()
                );

                appliedDiscounts.Add(appliedDiscount);
                totalDiscountApplied += discountToApply;

                // If we've hit the cap, no need to process more promotions
                if (totalDiscountApplied >= globalDiscountCap)
                {
                    break;
                }
            }
        }

        return new DiscountCalculationResult(orderSubtotal, appliedDiscounts, skippedPromotions);
    }

    private IEnumerable<Promotion> FilterActivePromotions(
        IEnumerable<Promotion> promotions,
        decimal orderSubtotal,
        DateTime currentDateTime,
        List<string> skippedPromotions)
    {
        var activePromotions = new List<Promotion>();

        foreach (var promotion in promotions)
        {
            // Check if active flag is set
            if (!promotion.IsActive)
            {
                skippedPromotions.Add(promotion.Name);
                continue;
            }

            // Check if within time range
            if (!promotion.IsActiveAt(currentDateTime))
            {
                skippedPromotions.Add(promotion.Name);
                continue;
            }

            // Check minimum order amount
            if (promotion.MinimumOrderAmount.HasValue && orderSubtotal < promotion.MinimumOrderAmount.Value)
            {
                skippedPromotions.Add(promotion.Name);
                continue;
            }

            activePromotions.Add(promotion);
        }

        return activePromotions;
    }
}
