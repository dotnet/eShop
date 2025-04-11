namespace Inked.Submission.API.Services;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string fileName);
    Task DeleteFileAsync(string fileName);
    Task<Stream> GetFileAsync(string fileName);
}
