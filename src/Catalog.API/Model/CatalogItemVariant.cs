using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Catalog.API.Model
{
    public class CatalogItemVariant
    {
        public int Id { get; set; }

        public int ProductId { get; set; }


        [JsonPropertyName("pid")]
        public string ProudctIdString { get; set; }


        [JsonPropertyName("vid")]
        public string VariantId { get; set; }


        [JsonPropertyName("variantImage")]
        [MaxLength(200)]
        public string VariantImageOrigin { get; set; }

        [MaxLength(200)]
        public string VarianImageEnhanced { get; set; }


        [JsonPropertyName("variantSku")]
        public string VariantSKU { get; set; }

        [JsonPropertyName("variantKey")]
        public string VariantKeyEN { get; set; }

        public string VariantKeyDE { get; set; }

        [JsonPropertyName("variantLength")]
        public decimal VariantLength { get; set; }

        [JsonPropertyName("variantHeight")]
        public decimal VariantHeigt { get; set; }

        [JsonPropertyName("variantWidth")]
        public decimal VariantWith { get; set; }

        [JsonPropertyName("variantVolume")]
        public decimal VariatVolume { get; set; }

        [JsonPropertyName("variantSellPrice")]
        public decimal VariantPrice { get; set; }

        [JsonPropertyName("variantSugSellPrice")]
        public decimal VariantSellPrice { get; set; }


        [JsonPropertyName("variantStandard")]
        public string VariantStandart { get; set; }

        public int CatalogItemId { get; set; } // Required foreign key property
        public CatalogItem CatalogItem { get; set; } = null!;

        public string VarianKeySizeAjusments()
        {
            return "";
        }

    }
}
