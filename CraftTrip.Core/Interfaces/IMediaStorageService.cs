namespace CraftTrip.Core.Interfaces;

public interface IMediaStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string originalFileName, string contentType, string folder = "general");
    Task<bool> DeleteFileAsync(string fileUrl);
}
