using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using MAEMS.Application.Interfaces;
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
        _bucketName = configuration["Firebase:StorageBucket"]
            ?? throw new InvalidOperationException("Firebase:StorageBucket is not configured");

        try
        {
            var credentialPath = configuration["Firebase:StorageCredentialPath"]
                ?? throw new InvalidOperationException("Firebase:StorageCredentialPath is not configured");

            // StorageClient dùng credential riêng của bucket testing-4532d,
            // không liên quan tới FirebaseApp.DefaultInstance của FirebaseAuthService
            var credential = GoogleCredential.FromFile(credentialPath)
                .CreateScoped("https://www.googleapis.com/auth/cloud-platform");

            _storageClient = StorageClient.Create(credential);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Firebase Storage Service");
            throw;
        }
    }

    /// <summary>
    /// Upload file lên Firebase Storage và trả về public download URL để lưu vào Document.FilePath
    /// </summary>
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

            // Trả về public download URL dạng Firebase Storage REST API
            // URL này có thể dùng trực tiếp để tải file
            var encodedObjectName = Uri.EscapeDataString(objectName);
            var downloadUrl = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedObjectName}?alt=media";

            _logger.LogInformation("File uploaded successfully: {ObjectName}", objectName);
            return downloadUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file: {FileName}", fileName);
            throw;
        }
    }

    /// <summary>
    /// Xóa file. Chấp nhận cả public download URL (lưu trong DB) lẫn object path thuần
    /// </summary>
    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            var objectName = ExtractObjectName(filePath);
            await _storageClient.DeleteObjectAsync(_bucketName, objectName);
            _logger.LogInformation("File deleted successfully: {ObjectName}", objectName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FilePath}", filePath);
            return false;
        }
    }

    /// <summary>
    /// Lấy download URL — nếu filePath đã là URL thì trả về nguyên, ngược lại build từ object path
    /// </summary>
    public Task<string> GetDownloadUrlAsync(string filePath)
    {
        // Nếu filePath đã là public download URL (lưu từ UploadFileAsync), trả về ngay
        if (filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(filePath);
        }

        // Fallback: build URL từ object path
        var encodedObjectName = Uri.EscapeDataString(filePath);
        var url = $"https://firebasestorage.googleapis.com/v0/b/{_bucketName}/o/{encodedObjectName}?alt=media";
        return Task.FromResult(url);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Extract object name từ public download URL hoặc trả về nguyên nếu đã là object path
    /// VD: https://firebasestorage.googleapis.com/v0/b/bucket/o/folder%2Ffile.pdf?alt=media
    ///     → folder/file.pdf
    /// </summary>
    private static string ExtractObjectName(string filePath)
    {
        if (!filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return filePath;

        try
        {
            var uri = new Uri(filePath);
            // path: /v0/b/{bucket}/o/{encodedObjectName}
            var segments = uri.AbsolutePath.Split('/');
            // index 0="" 1="v0" 2="b" 3="{bucket}" 4="o" 5="{encodedObjectName}"
            if (segments.Length >= 6)
                return Uri.UnescapeDataString(segments[5]);
        }
        catch { /* fall through */ }

        return filePath;
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf"  => "application/pdf",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".png"  => "image/png",
            ".doc"  => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _       => "application/octet-stream"
        };
    }
}