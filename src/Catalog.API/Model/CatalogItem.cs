using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Catalog.API.Model;

using Pgvector;

namespace eShop.Catalog.API.Model;

public class CatalogItem
{
    public int Id { get; set; }


    public string Name { get; set; }

    [JsonPropertyName("productNameEn")]
    [MaxLength(250)]
    public string OriginName { get; set; }

    [MaxLength(200)]
    public string NameEN { get; set; }

    [MaxLength(200)]
    public string NameDE { get; set; }

    [JsonPropertyName("description")]

    [MaxLength(5000)]

    public string Description { get; set; }

    [MaxLength(5000)]
    public string DescriptionDE { get; set; }

    [MaxLength(5000)]
    public string DescriptionEN { get; set; }



    [JsonPropertyName("productWeight")]
    public string ProductWeight { get; set; }

    [JsonPropertyName("productType")]
    public string ProducctType { get; set; }



    [JsonPropertyName("categoryId")]
    public string CategoryId { get; set; }

    [MaxLength(200)]
    [JsonPropertyName("categoryName")]
    public string CategoryNameEN { get; set; }

    [MaxLength(200)]
    public string CategoryNameDE { get; set; }


    [JsonPropertyName("productSku")]
    public string ProductSKU { get; set; }


    [JsonPropertyName("productKeyEn")]
    public string ProductKeyEN { get; set; }

    public string ProductKenDE { get; set; }



    public decimal Price { get; set; }

    [JsonPropertyName("sellPrice")]

    public string OriginPrice { get; set; }


    [JsonPropertyName("suggestSellPrice")]
    public string SuggestSellPrice { get; set; }

    public int PriceAligmentAlgorithm()
    {
        return 0;
    }


    [JsonPropertyName("ListedNum")]
    public int ListedNum { get; set; }

    public string PictureFileName { get; set; }


    [JsonPropertyName("productImageSet")]
    public ICollection<OriginalImages> OriginalImages { get; } = new List<OriginalImages>();

    public ICollection<EnchancedImages> EnhancedImages { get; } = new List<EnchancedImages>();

    public List<CatalogFeature> CatalogFeatures { get; set; } 

    public List<CatalogKit> CatalogKits { get; set; }

    public ICollection<CatalogItemVariant> CatalogItemVariants { get; } = new List<CatalogItemVariant>();


    [JsonPropertyName("packingWeight")]
    public string PackingWeight { get; set; }


    [JsonPropertyName("packingNameEn")]
    public string PackingNameEN { get; set; }

    public string PackingNameDE { get; set; }


    public string PackingNameSetEN { get; set; }

    public string PackingNameSetDE { get; set; }

    public int CatalogTypeId { get; set; }

    //public CatalogType CatalogType { get; set; }

    public int CatalogBrandId { get; set; }

    //public CatalogBrand CatalogBrand { get; set; }

    // Quantity in stock
    public int AvailableStock { get; set; }

    // Available stock at which we should reorder
    public int RestockThreshold { get; set; }


    // Maximum number of units that can be in-stock at any time (due to physicial/logistical constraints in warehouses)
    public int MaxStockThreshold { get; set; }

    /// <summary>Optional embedding for the catalog item's description.</summary>
    [JsonIgnore]
    public Vector Embedding { get; set; }

    /// <summary>
    /// True if item is on reorder
    /// </summary>
    public bool OnReorder { get; set; }

    public CatalogItem() { }


    /// <summary>
    /// Decrements the quantity of a particular item in inventory and ensures the restockThreshold hasn't
    /// been breached. If so, a RestockRequest is generated in CheckThreshold. 
    /// 
    /// If there is sufficient stock of an item, then the integer returned at the end of this call should be the same as quantityDesired. 
    /// In the event that there is not sufficient stock available, the method will remove whatever stock is available and return that quantity to the client.
    /// In this case, it is the responsibility of the client to determine if the amount that is returned is the same as quantityDesired.
    /// It is invalid to pass in a negative number. 
    /// </summary>
    /// <param name="quantityDesired"></param>
    /// <returns>int: Returns the number actually removed from stock. </returns>
    /// 
    public int RemoveStock(int quantityDesired)
    {
        if (AvailableStock == 0)
        {
            throw new CatalogDomainException($"Empty stock, product item {Name} is sold out");
        }

        if (quantityDesired <= 0)
        {
            throw new CatalogDomainException($"Item units desired should be greater than zero");
        }

        int removed = Math.Min(quantityDesired, this.AvailableStock);

        this.AvailableStock -= removed;

        return removed;
    }

    /// <summary>
    /// Increments the quantity of a particular item in inventory.
    /// <param name="quantity"></param>
    /// <returns>int: Returns the quantity that has been added to stock</returns>
    /// </summary>
    public int AddStock(int quantity)
    {
        int original = this.AvailableStock;

        // The quantity that the client is trying to add to stock is greater than what can be physically accommodated in the Warehouse
        if ((this.AvailableStock + quantity) > this.MaxStockThreshold)
        {
            // For now, this method only adds new units up maximum stock threshold. In an expanded version of this application, we
            //could include tracking for the remaining units and store information about overstock elsewhere. 
            this.AvailableStock += (this.MaxStockThreshold - this.AvailableStock);
        }
        else
        {
            this.AvailableStock += quantity;
        }

        this.OnReorder = false;

        return this.AvailableStock - original;
    }
}
