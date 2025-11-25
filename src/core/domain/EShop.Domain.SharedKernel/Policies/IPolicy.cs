namespace EShop.Domain.SharedKernel.Policies
{
    public interface IPolicy<TContext>
    {
        PolicyResult Apply(TContext context);
    }
}