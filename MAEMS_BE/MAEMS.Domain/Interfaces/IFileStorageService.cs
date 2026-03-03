namespace MAEMS.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "documents");
    Task<bool> DeleteFileAsync(string filePath);
    Task<string> GetDownloadUrlAsync(string filePath);
}