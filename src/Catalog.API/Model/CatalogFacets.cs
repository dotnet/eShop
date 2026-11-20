namespace eShop.Catalog.API.Model;

public record CatalogFacetCount(int Id, int Count);

public record CatalogFacets(
    IReadOnlyList<CatalogFacetCount> BrandCounts,
    IReadOnlyList<CatalogFacetCount> TypeCounts,
    int BrandTotal,
    int TypeTotal);
