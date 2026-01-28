namespace eShop.Ordering.Domain.AggregatesModel.PromotionAggregate;

public interface IPromotionRepository : IRepository<Promotion>
{
    Task<Promotion> GetByIdAsync(int id);
    Task<IEnumerable<Promotion>> GetActivePromotionsAsync();
    Promotion Add(Promotion promotion);
    void Update(Promotion promotion);
    void Delete(Promotion promotion);
}
