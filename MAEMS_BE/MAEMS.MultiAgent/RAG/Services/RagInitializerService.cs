using MAEMS.MultiAgent.RAG.Configuration;
using MAEMS.MultiAgent.RAG.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Background service for RAG system initialization and periodic re-indexing
/// </summary>
public class RagInitializerService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RagInitializerService> _logger;
    private readonly RagSettings _ragSettings;
    private Timer? _indexingTimer;

    public RagInitializerService(
        IServiceScopeFactory serviceScopeFactory,
        IConfiguration configuration,
        ILogger<RagInitializerService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _configuration = configuration;
        _logger = logger;

        _ragSettings = new RagSettings();
        _configuration.GetSection(RagSettings.SectionName).Bind(_ragSettings);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("RAG Initializer Service starting");

        try
        {
            // Initial indexing on startup if enabled
            if (_ragSettings.EnableAutoIndexing)
            {
                _logger.LogInformation("Performing initial document indexing");
                await IndexDocumentsAsync(stoppingToken);
            }

            // Setup periodic re-indexing
            var indexingInterval = TimeSpan.FromMinutes(_ragSettings.IndexingIntervalMinutes);
            _indexingTimer = new Timer(
                async _ => await IndexDocumentsAsync(stoppingToken),
                null,
                indexingInterval,
                indexingInterval);

            _logger.LogInformation($"RAG indexing scheduled every {_ragSettings.IndexingIntervalMinutes} minutes");

            // Keep the service running
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("RAG Initializer Service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RAG Initializer Service");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("RAG Initializer Service stopping");
        _indexingTimer?.Dispose();
        await base.StopAsync(cancellationToken);
    }

    private async Task IndexDocumentsAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting RAG document indexing");

            // Use scope to get scoped services
            using var scope = _serviceScopeFactory.CreateScope();
            var documentLoader = scope.ServiceProvider.GetRequiredService<IRagDocumentLoader>();
            var retrievalService = scope.ServiceProvider.GetRequiredService<IRagRetrievalService>();

            // Load documents from database and files
            var documents = await documentLoader.LoadDocumentsAsync(cancellationToken);
            var documentList = documents.ToList();

            if (!documentList.Any())
            {
                _logger.LogWarning("No documents loaded for indexing");
                return;
            }

            _logger.LogInformation($"Loaded {documentList.Count} documents, starting embedding and indexing");

            // Index documents (this generates embeddings and stores in vector DB)
            await retrievalService.IndexDocumentsAsync(documentList, cancellationToken);

            _logger.LogInformation("RAG document indexing completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Indexing operation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during RAG document indexing");
            // Don't rethrow - let the service continue running even if indexing fails
        }
    }
}
