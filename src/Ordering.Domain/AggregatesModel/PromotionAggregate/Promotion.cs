using eShop.Ordering.Domain.Exceptions;
using eShop.Ordering.Domain.SeedWork;

namespace eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

/// <summary>
/// Promotion aggregate root representing a promotional discount rule.
/// </summary>
public class Promotion : Entity, IAggregateRoot
{
    public string Name { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal DiscountValue { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public decimal? MinimumOrderAmount { get; private set; }
    public decimal? MaximumDiscount { get; private set; }
    public int? MinimumQuantity { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }

    private readonly List<string> _applicableCategories = new();
    public IReadOnlyCollection<string> ApplicableCategories => _applicableCategories.AsReadOnly();

    private readonly List<string> _excludedCategories = new();
    public IReadOnlyCollection<string> ExcludedCategories => _excludedCategories.AsReadOnly();

    public Promotion(string name, DiscountType discountType, decimal discountValue, DateTime startDate, DateTime endDate, int priority, 
        decimal? minimumOrderAmount = null, decimal? maximumDiscount = null, int? minimumQuantity = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrderingDomainException("Name cannot be null or empty.");
        }

        // Validate dates
        if (startDate >= endDate)
        {
            throw new OrderingDomainException("Start date must be before end date.");
        }

        // Validate discount value
        if (discountValue < 0)
        {
            throw new OrderingDomainException("Discount value cannot be negative.");
        }

        // Validate percentage discounts
        if (discountType == DiscountType.PercentageDiscount && (discountValue < 1 || discountValue > 99))
        {
            throw new OrderingDomainException("Percentage discount must be between 1 and 99.");
        }

        // Validate priority
        if (priority <= 0)
        {
            throw new OrderingDomainException("Priority must be greater than 0.");
        }

        // Validate minimum order amount
        if (minimumOrderAmount.HasValue && minimumOrderAmount.Value < 0)
        {
            throw new OrderingDomainException("MinimumOrderAmount cannot be negative.");
        }

        // Validate maximum discount
        if (maximumDiscount.HasValue && maximumDiscount.Value < 0)
        {
            throw new OrderingDomainException("MaximumDiscount cannot be negative.");
        }

        // Validate VolumeDiscount requirements
        if (discountType == DiscountType.VolumeDiscount && !minimumQuantity.HasValue)
        {
            throw new OrderingDomainException("VolumeDiscount requires MinimumQuantity.");
        }

        if (discountType == DiscountType.VolumeDiscount && minimumQuantity.HasValue && minimumQuantity.Value <= 0)
        {
            throw new OrderingDomainException("MinimumQuantity must be greater than zero for VolumeDiscount.");
        }

        Name = name;
        DiscountType = discountType;
        DiscountValue = discountValue;
        StartDate = startDate;
        EndDate = endDate;
        Priority = priority;
        MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscount = maximumDiscount;
        MinimumQuantity = minimumQuantity;
        IsActive = true;
    }

    /// <summary>
    /// Check if the promotion is active at a specific date/time.
    /// </summary>
    public bool IsActiveAt(DateTime dateTime)
    {
        return IsActive && dateTime >= StartDate && dateTime <= EndDate;
    }

    /// <summary>
    /// Deactivate the promotion.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Activate the promotion.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Add a category to the applicable categories list.
    /// </summary>
    public void AddApplicableCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !_applicableCategories.Contains(category))
        {
            _applicableCategories.Add(category);
        }
    }

    /// <summary>
    /// Add a category to the excluded categories list.
    /// </summary>
    public void AddExcludedCategory(string category)
    {
        if (!string.IsNullOrWhiteSpace(category) && !_excludedCategories.Contains(category))
        {
            _excludedCategories.Add(category);
        }
    }

    /// <summary>
    /// Update promotion details.
    /// </summary>
    public void Update(string name, decimal discountValue, DateTime startDate, DateTime endDate, int priority, 
        decimal? minimumOrderAmount = null, decimal? maximumDiscount = null, int? minimumQuantity = null)
    {
        // Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new OrderingDomainException("Name cannot be null or empty.");
        }

        // Validate dates
        if (startDate >= endDate)
        {
            throw new OrderingDomainException("Start date must be before end date.");
        }

        // Validate discount value
        if (discountValue < 0)
        {
            throw new OrderingDomainException("Discount value cannot be negative.");
        }

        // Validate percentage discounts
        if (DiscountType == DiscountType.PercentageDiscount && (discountValue < 1 || discountValue > 99))
        {
            throw new OrderingDomainException("Percentage discount must be between 1 and 99.");
        }

        // Validate priority
        if (priority <= 0)
        {
            throw new OrderingDomainException("Priority must be greater than 0.");
        }

        // Validate minimum order amount
        if (minimumOrderAmount.HasValue && minimumOrderAmount.Value < 0)
        {
            throw new OrderingDomainException("MinimumOrderAmount cannot be negative.");
        }

        // Validate maximum discount
        if (maximumDiscount.HasValue && maximumDiscount.Value < 0)
        {
            throw new OrderingDomainException("MaximumDiscount cannot be negative.");
        }

        // Validate VolumeDiscount requirements
        if (DiscountType == DiscountType.VolumeDiscount && !minimumQuantity.HasValue)
        {
            throw new OrderingDomainException("VolumeDiscount requires MinimumQuantity.");
        }

        if (DiscountType == DiscountType.VolumeDiscount && minimumQuantity.HasValue && minimumQuantity.Value <= 0)
        {
            throw new OrderingDomainException("MinimumQuantity must be greater than zero for VolumeDiscount.");
        }

        Name = name;
        DiscountValue = discountValue;
        StartDate = startDate;
        EndDate = endDate;
        Priority = priority;
        MinimumOrderAmount = minimumOrderAmount;
        MaximumDiscount = maximumDiscount;
        MinimumQuantity = minimumQuantity;
    }

    /// <summary>
    /// Update applicable and excluded categories.
    /// </summary>
    public void UpdateCategories(IEnumerable<string> applicableCategories, IEnumerable<string> excludedCategories)
    {
        _applicableCategories.Clear();
        _excludedCategories.Clear();

        if (applicableCategories != null)
        {
            foreach (var category in applicableCategories)
            {
                AddApplicableCategory(category);
            }
        }

        if (excludedCategories != null)
        {
            foreach (var category in excludedCategories)
            {
                AddExcludedCategory(category);
            }
        }
    }
}
