namespace eShop.Catalog.API.Model;

/// <summary>
/// Stock taken out of <see cref="CatalogItem.AvailableStock"/> on behalf of an order.
/// The stock is held from the moment the order is validated, and is either kept for good
/// once the order is paid or given back if the order is cancelled.
/// </summary>
public class StockReservation
{
    public int OrderId { get; set; }

    public StockReservationStatus Status { get; set; }

    public List<StockReservationItem> Items { get; set; } = [];
}

public class StockReservationItem
{
    public int ProductId { get; set; }

    public int Units { get; set; }
}

public enum StockReservationStatus
{
    /// <summary>
    /// Stock is held for the order while it awaits payment
    /// </summary>
    Reserved,

    /// <summary>
    /// The order was paid, the stock is gone for good
    /// </summary>
    Committed,

    /// <summary>
    /// The order was cancelled, any held stock was returned
    /// </summary>
    Released
}
