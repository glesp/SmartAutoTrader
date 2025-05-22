// Services/IBlobStorageService.cs
namespace SmartAutoTrader.API.Services
{
    /// <summary>
    /// Interface for services that handle file storage operations.
    /// </summary>
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a file to blob storage.
        /// </summary>
        /// <param name="strFileName">Name to use for the blob.</param>
        /// <param name="contentType">MIME type of the file being uploaded.</param>
        /// <param name="fileStream">Stream containing the file data.</param>
        /// <returns>URI to the uploaded file.</returns>
        Task<string> UploadFileToBlobAsync(string strFileName, string contentType, Stream fileStream);

        /// <summary>
        /// Deletes a blob from storage.
        /// </summary>
        /// <param name="blobName">Name of the blob to delete.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        Task DeleteBlobAsync(string blobName);
    }
}