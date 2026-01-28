using eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;
using eShop.Ordering.Domain.Seedwork;
using Microsoft.EntityFrameworkCore;

namespace eShop.Ordering.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly OrderingContext _context;

    public IUnitOfWork UnitOfWork => _context;

    public PromotionRepository(OrderingContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Promotion Add(Promotion promotion)
    {
        return _context.Promotions.Add(promotion).Entity;
    }

    public void Delete(Promotion promotion)
    {
        _context.Promotions.Remove(promotion);
    }

    public async Task<IEnumerable<Promotion>> GetActivePromotionsAsync()
    {
        return await _context.Promotions
            .Where(p => p.IsActive 
                && p.StartDate <= DateTime.UtcNow 
                && p.EndDate >= DateTime.UtcNow)
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    public async Task<Promotion> GetByIdAsync(int id)
    {
        return await _context.Promotions
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public void Update(Promotion promotion)
    {
        _context.Entry(promotion).State = EntityState.Modified;
    }
}
