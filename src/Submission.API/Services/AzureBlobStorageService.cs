using Azure.Storage.Blobs;

namespace Inked.Submission.API.Services;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobContainerClient _containerClient;

    public AzureBlobStorageService(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("AzureBlobStorage");
        var containerName = configuration["AzureBlobContainerName"];
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    public async Task<string> SaveFileAsync(IFormFile file, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await using var stream = file.OpenReadStream();
        await blobClient.UploadAsync(stream, true);
        return fileName;
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<Stream> GetFileAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }
}
