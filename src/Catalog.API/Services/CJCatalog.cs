using System.Text.Json;
using Catalog.API.Model;
using Microsoft.Extensions.Logging;

namespace eShop.Catalog.API.Services
{

    public class CJCatalog
    {
        public async Task<ItemWithVariants> GetCatalogItemAsync(HttpClient httpClient, string url, ILogger logger, string token)
        {

            httpClient.DefaultRequestHeaders.Add("CJ-Access-Token", token);

            using HttpResponseMessage response = await httpClient.GetAsync(url);
            CatalogItem result = null;
            List<CatalogItemVariant> itemVariants = null;
            string data = "";
            string variants = "";
            List<OriginalImages> images = new();
            try
            {
                response.EnsureSuccessStatusCode();
                var jsonResponse = await response.Content.ReadAsStringAsync();
                logger.LogInformation($"Response from api {jsonResponse}");

                using (JsonDocument doc = JsonDocument.Parse(jsonResponse))
                {
                    JsonElement root = doc.RootElement;

                    // Extract the "data" field
                    if (root.TryGetProperty("data", out JsonElement dataElement))
                    {
                        data = dataElement.GetRawText();
                        dataElement.TryGetProperty("variants", out JsonElement variantsElement);

                        dataElement.TryGetProperty("productImageSet", out JsonElement imagesElement);

                        logger.LogInformation($"Images element - {imagesElement}");
                        foreach (JsonElement element in imagesElement.EnumerateArray())
                        {
                            images.Add(new OriginalImages() { Src = element.ToString() });
                        }
                        variants = variantsElement.GetRawText();

                        //foreach (JsonElement element in variants.EnumerateArray())
                        //{
                        //    string variantImage = element.GetProperty("variantImage").GetString();

                        //    Console.WriteLine($"Name: {variantImage}"); 
                        //}
                        //Console.WriteLine($"Product variants: {variants}");
                        result = JsonSerializer.Deserialize<CatalogItem>(data);
                        itemVariants = JsonSerializer.Deserialize<List<CatalogItemVariant>>(variants);
                    }
                    else
                    {
                        Console.WriteLine("Data field not found.");
                    }
                }



            }
            catch (HttpRequestException)
            {

            }

            return new ItemWithVariants { Item = result, Variants = itemVariants, Images = images };

        }
    }

    public class ItemWithVariants
    {
        public CatalogItem Item;

        public List<CatalogItemVariant> Variants;

        public List<OriginalImages> Images;
    }
}
