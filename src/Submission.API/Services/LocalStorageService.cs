namespace Inked.Submission.API.Services;

public class LocalStorageService : IStorageService
{
    private readonly string _storagePath;

    public LocalStorageService(IWebHostEnvironment env)
    {
        _storagePath = Path.Combine(env.ContentRootPath, "Pics");
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    public async Task<string> SaveFileAsync(IFormFile file, string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        await using var stream = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(stream);
        return fileName;
    }

    public Task DeleteFileAsync(string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        return Task.CompletedTask;
    }

    public Task<Stream> GetFileAsync(string fileName)
    {
        var filePath = Path.Combine(_storagePath, fileName);
        var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        return Task.FromResult<Stream>(stream);
    }
}
