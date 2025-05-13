// Services/BlobStorageService.cs
namespace SmartAutoTrader.API.Services // Adjust namespace if needed
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "vehicle-images";

        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger) // Added ILogger
        {
            var connectionString = configuration.GetValue<string>("azurestorageconnectionstring");
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("AzureStorageConnectionString is not configured.");
                throw new InvalidOperationException("AzureStorageConnectionString is not configured.");
            }

            this._blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
        {
            var containerClient = this._blobServiceClient.GetBlobContainerClient(this._containerName);

            // In production, you might not want to create it on every upload
            // but ensure it's created during app startup or deployment.
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(strFileName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

            return blobClient.Uri.AbsoluteUri; // Public URL
        }

        public async Task DeleteBlobAsync(string blobName) // blobName here should be the path within the container e.g., "vehicles/image.jpg"
        {
            var containerClient = this._blobServiceClient.GetBlobContainerClient(this._containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}