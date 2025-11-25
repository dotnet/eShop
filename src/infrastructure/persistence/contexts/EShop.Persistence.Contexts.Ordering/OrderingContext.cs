using DataArc.Abstractions;
using Microsoft.EntityFrameworkCore;

using EShop.Persistence.Models.Ordering;

namespace EShop.Persistence.Contexts.Ordering
{
    public class OrderingContext : DbContext, IPipelineContext<OrderingContext>
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<PaymentMethod> Payments { get; set; }
        public DbSet<Buyer> Buyers { get; set; }
        public DbSet<CardType> CardTypes { get; set; }
    }
}