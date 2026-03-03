using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using MAEMS.Application.Interfaces; // Đảm bảo dùng đúng namespace
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.Infrastructure.Services;

public class FirebaseStorageService : IFileStorageService
{
    private readonly StorageClient _storageClient;
    private readonly string _bucketName;
    private readonly ILogger<FirebaseStorageService> _logger;

    public FirebaseStorageService(IConfiguration configuration, ILogger<FirebaseStorageService> logger)
    {
        _logger = logger;
        _bucketName = configuration["Firebase:StorageBucket"] ?? "capstone-ddc58.appspot.com";
        
        try
        {
            // Initialize Firebase Admin SDK if not already initialized
            if (FirebaseApp.DefaultInstance == null)
            {
                var credential = GoogleCredential.FromFile("firebase-adminsdk.json");
                FirebaseApp.Create(new AppOptions()
                {
                    Credential = credential,
                    ProjectId = "capstone-ddc58"
                });
            }

            _storageClient = StorageClient.Create(GoogleCredential.FromFile("firebase-adminsdk.json"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Storage Service");
            throw;
        }
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string folder = "documents")
    {
        try
        {
            var objectName = $"{folder}/{DateTime.UtcNow:yyyy/MM/dd}/{Guid.NewGuid()}_{fileName}";
            
            await _storageClient.UploadObjectAsync(
                bucket: _bucketName,
                objectName: objectName,
                contentType: GetContentType(fileName),
                source: fileStream
            );
            
            _logger.LogInformation("File uploaded successfully: {ObjectName}", objectName);
            return objectName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            await _storageClient.DeleteObjectAsync(_bucketName, filePath);
            _logger.LogInformation("File deleted successfully: {FilePath}", filePath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return false;
        }
    }

    public async Task<string> GetDownloadUrlAsync(string filePath)
    {
        try
        {
            var obj = await _storageClient.GetObjectAsync(_bucketName, filePath);
            return $"https://storage.googleapis.com/{_bucketName}/{filePath}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get download URL for file: {FilePath}", filePath);
            throw;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}