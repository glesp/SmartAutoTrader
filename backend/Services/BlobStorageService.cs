/* <copyright file="BlobStorageService.cs" company="PlaceholderCompany">
 * Copyright (c) PlaceholderCompany. All rights reserved.
 * </copyright>
 *
<summary>
This file defines the BlobStorageService class, which provides methods for managing file uploads and deletions in Azure Blob Storage for the Smart Auto Trader application.
</summary>
<remarks>
The BlobStorageService class is responsible for interacting with Azure Blob Storage to handle file uploads and deletions. It uses the Azure.Storage.Blobs library to manage blob containers and blobs. The service ensures that the container exists before uploading files and provides public URLs for uploaded files. It also includes functionality to delete blobs from the storage. This service is typically used for managing vehicle images or other file assets in the application.
</remarks>
<dependencies>
- Azure.Storage.Blobs
- Azure.Storage.Blobs.Models
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.Logging
- System.IO
</dependencies>
 */

namespace SmartAutoTrader.API.Services
{
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;

    /// <summary>
    /// Service responsible for managing file uploads and storage in Azure Blob Storage.
    /// </summary>
    /// <remarks>
    /// This service provides methods for uploading files to Azure Blob Storage and deleting blobs. It ensures that the container exists before performing operations and logs errors if the configuration is invalid.
    /// </remarks>
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "vehicle-images";

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobStorageService"/> class.
        /// </summary>
        /// <param name="configuration">The application's configuration object, used to retrieve the Azure storage connection string.</param>
        /// <param name="logger">The logger instance for logging errors and information.</param>
        /// <exception cref="InvalidOperationException">Thrown if the Azure storage connection string is not configured.</exception>
        /// <remarks>
        /// The constructor initializes the BlobServiceClient using the connection string from the application's configuration. If the connection string is missing or invalid, an exception is thrown, and an error is logged.
        /// </remarks>
        public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
        {
            var connectionString = configuration.GetValue<string>("azurestorageconnectionstring");
            if (string.IsNullOrEmpty(connectionString))
            {
                logger.LogError("AzureStorageConnectionString is not configured.");
                throw new InvalidOperationException("AzureStorageConnectionString is not configured.");
            }

            this._blobServiceClient = new BlobServiceClient(connectionString);
        }

        /// <summary>
        /// Uploads a file to Azure Blob Storage.
        /// </summary>
        /// <param name="strFileName">The name to use for the blob in the storage container.</param>
        /// <param name="contentType">The MIME type of the file being uploaded.</param>
        /// <param name="fileStream">The stream containing the file data.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the public URI of the uploaded file.</returns>
        /// <exception cref="Exception">Thrown if the upload operation fails.</exception>
        /// <remarks>
        /// This method uploads a file to the specified blob container. If the container does not exist, it is created with public access. The method returns the public URI of the uploaded file.
        /// </remarks>
        /// <example>
        /// <code>
        /// using (var stream = File.OpenRead("path/to/file.jpg"))
        /// {
        ///     string uri = await blobStorageService.UploadFileToBlobAsync("file.jpg", "image/jpeg", stream);
        ///     Console.WriteLine($"File uploaded to: {uri}");
        /// }
        /// </code>
        /// </example>
        public async Task<string> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream)
        {
            var containerClient = this._blobServiceClient.GetBlobContainerClient(this._containerName);

            // Ensure the container exists
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(strFileName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders { ContentType = contentType });

            return blobClient.Uri.AbsoluteUri; // Public URL
        }

        /// <summary>
        /// Deletes a blob from Azure Blob Storage.
        /// </summary>
        /// <param name="blobName">The name of the blob to delete, including its path within the container.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /// <remarks>
        /// This method deletes a blob from the specified container. If the blob does not exist, the operation completes without throwing an exception.
        /// </remarks>
        /// <example>
        /// <code>
        /// await blobStorageService.DeleteBlobAsync("vehicles/image.jpg");
        /// Console.WriteLine("Blob deleted successfully.");
        /// </code>
        /// </example>
        public async Task DeleteBlobAsync(string blobName)
        {
            var containerClient = this._blobServiceClient.GetBlobContainerClient(this._containerName);
            var blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}