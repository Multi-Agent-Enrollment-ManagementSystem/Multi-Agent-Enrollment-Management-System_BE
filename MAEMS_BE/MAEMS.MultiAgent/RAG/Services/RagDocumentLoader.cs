using MAEMS.Infrastructure.Models;
using MAEMS.MultiAgent.RAG.Interfaces;
using MAEMS.MultiAgent.RAG.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Service for loading admission-related documents from database and static files
/// </summary>
public class RagDocumentLoader : IRagDocumentLoader
{
    private readonly postgresContext _context;
    private readonly ILogger<RagDocumentLoader> _logger;
    private readonly string _staticFilesPath;

    public RagDocumentLoader(postgresContext context, ILogger<RagDocumentLoader> logger)
    {
        _context = context;
        _logger = logger;
        // Static files would be stored in a RAG folder
        _staticFilesPath = Path.Combine(AppContext.BaseDirectory, "RAG", "StaticDocuments");
    }

    public async Task<IEnumerable<RagDocument>> LoadDocumentsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting document loading from database and static files");

        var documents = new List<RagDocument>();

        try
        {
            // Load from database
            var dbDocuments = await LoadFromDatabaseAsync(cancellationToken);
            documents.AddRange(dbDocuments);

            // Load from static files
            var fileDocuments = await LoadFromFilesAsync(cancellationToken);
            documents.AddRange(fileDocuments);

            _logger.LogInformation($"Successfully loaded {documents.Count} documents ({dbDocuments.Count()} from DB, {fileDocuments.Count()} from files)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading documents");
            throw;
        }

        return documents;
    }

    public async Task<IEnumerable<RagDocument>> LoadFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Loading documents from database");

        var documents = new List<RagDocument>();

        try
        {
            // Load Programs with their descriptions
            var programs = await _context.Programs
                .Where(p => p.IsActive == true)
                .ToListAsync(cancellationToken);

            foreach (var program in programs)
            {
                var content = $@"Program: {program.ProgramName}
Duration: {program.Duration ?? "Not specified"}
Description: {program.Description ?? "No description available"}
Career Prospects: {program.CareerProspects ?? "No career information available"}";

                documents.Add(new RagDocument
                {
                    Id = $"program_{program.ProgramId}",
                    Content = content,
                    Source = "database",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "program" },
                        { "program_id", program.ProgramId.ToString() },
                        { "program_name", program.ProgramName },
                        { "major_id", program.MajorId?.ToString() ?? "N/A" }
                    },
                    CreatedAt = DateTime.Now
                });
            }

            _logger.LogInformation($"Loaded {programs.Count} programs from database");

            // Load Majors with their descriptions
            var majors = await _context.Majors
                .Where(m => m.IsActive == true)
                .ToListAsync(cancellationToken);

            foreach (var major in majors)
            {
                var content = $@"Major: {major.MajorName}
Code: {major.MajorCode}
Description: {major.Description ?? "No description available"}";

                documents.Add(new RagDocument
                {
                    Id = $"major_{major.MajorId}",
                    Content = content,
                    Source = "database",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "major" },
                        { "major_id", major.MajorId.ToString() },
                        { "major_name", major.MajorName },
                        { "major_code", major.MajorCode }
                    },
                    CreatedAt = DateTime.Now
                });
            }

            _logger.LogInformation($"Loaded {majors.Count} majors from database");

            // Load Admission Types
            var admissionTypes = await _context.AdmissionTypes
                .Where(a => a.IsActive == true)
                .ToListAsync(cancellationToken);

            foreach (var admissionType in admissionTypes)
            {
                var content = $@"Admission Type: {admissionType.AdmissionTypeName}
Type: {admissionType.Type ?? "Standard"}";

                if (admissionType.RequiredDocumentList != null)
                {
                    content += $"\nRequired Documents: {admissionType.RequiredDocumentList}";
                }

                documents.Add(new RagDocument
                {
                    Id = $"admission_type_{admissionType.AdmissionTypeId}",
                    Content = content,
                    Source = "database",
                    Metadata = new Dictionary<string, string>
                    {
                        { "type", "admission_type" },
                        { "admission_type_id", admissionType.AdmissionTypeId.ToString() },
                        { "admission_type_name", admissionType.AdmissionTypeName },
                        { "admission_type_code", admissionType.Type ?? "N/A" }
                    },
                    CreatedAt = DateTime.Now
                });
            }

            _logger.LogInformation($"Loaded {admissionTypes.Count} admission types from database");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading documents from database");
            throw;
        }

        return documents;
    }

    public async Task<IEnumerable<RagDocument>> LoadFromFilesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation($"Loading documents from static files at {_staticFilesPath}");

        var documents = new List<RagDocument>();

        try
        {
            if (!Directory.Exists(_staticFilesPath))
            {
                _logger.LogWarning($"Static files directory not found at {_staticFilesPath}");
                return documents;
            }

            // Load all .txt files from the static folder
            var textFiles = Directory.GetFiles(_staticFilesPath, "*.txt", SearchOption.AllDirectories);

            foreach (var filePath in textFiles)
            {
                try
                {
                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    var content = await System.IO.File.ReadAllTextAsync(filePath, cancellationToken);

                    if (string.IsNullOrWhiteSpace(content))
                    {
                        _logger.LogWarning($"Static file {fileName} is empty, skipping");
                        continue;
                    }

                    documents.Add(new RagDocument
                    {
                        Id = $"static_{fileName}_{Guid.NewGuid().ToString().Substring(0, 8)}",
                        Content = content,
                        Source = "file",
                        Metadata = new Dictionary<string, string>
                        {
                            { "type", "static" },
                            { "file_name", fileName },
                            { "file_path", filePath }
                        },
                        CreatedAt = DateTime.Now
                    });

                    _logger.LogDebug($"Loaded static file: {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading static file {filePath}");
                }
            }

            _logger.LogInformation($"Loaded {textFiles.Length} static files from disk");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading documents from static files");
            throw;
        }

        return documents;
    }
}
