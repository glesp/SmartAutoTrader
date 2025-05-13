// Services/IBlobStorageService.cs
namespace SmartAutoTrader.API.Services // Adjust namespace if needed
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream);

        Task DeleteBlobAsync(string blobName);
    }
}