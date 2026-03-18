using MAEMS.MultiAgent.RAG.Configuration;
using MAEMS.MultiAgent.RAG.Interfaces;
using MAEMS.MultiAgent.RAG.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MAEMS.MultiAgent.RAG.Services;

/// <summary>
/// Service for retrieving relevant documents from the RAG system
/// </summary>
public class RagRetrievalService : IRagRetrievalService
{
    private readonly IRagEmbeddingService _embeddingService;
    private readonly IRagVectorStore _vectorStore;
    private readonly ILogger<RagRetrievalService> _logger;
    private readonly RagSettings _ragSettings;

    public RagRetrievalService(
        IRagEmbeddingService embeddingService,
        IRagVectorStore vectorStore,
        IConfiguration configuration,
        ILogger<RagRetrievalService> logger)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _logger = logger;

        _ragSettings = new RagSettings();
        configuration.GetSection(RagSettings.SectionName).Bind(_ragSettings);
    }

    public async Task<IEnumerable<RagDocument>> RetrieveAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query cannot be empty", nameof(query));
        }

        topK = topK <= 0 ? _ragSettings.DefaultTopK : topK;

        _logger.LogInformation($"Retrieving {topK} relevant documents for query: {query.Substring(0, Math.Min(100, query.Length))}...");

        try
        {
            // Generate embedding for the query
            var queryEmbedding = await _embeddingService.EmbedTextAsync(query, cancellationToken);

            // Search for similar documents
            var similarDocuments = await _vectorStore.SearchAsync(queryEmbedding, topK, cancellationToken);

            // Filter by minimum similarity score
            var relevantDocuments = similarDocuments
                .Where(d => d.Score >= _ragSettings.MinSimilarityScore)
                .OrderByDescending(d => d.Score)
                .Select(d => d.Document)
                .ToList();

            _logger.LogInformation($"Found {relevantDocuments.Count} relevant documents (minimum score: {_ragSettings.MinSimilarityScore})");

            return relevantDocuments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving documents");
            throw;
        }
    }

    public async Task<string> RetrieveAsContextAsync(string query, int topK = 5, CancellationToken cancellationToken = default)
    {
        var documents = await RetrieveAsync(query, topK, cancellationToken);

        if (!documents.Any())
        {
            _logger.LogWarning("No relevant documents found for query context");
            return "No relevant information found in the knowledge base.";
        }

        var contextBuilder = new System.Text.StringBuilder();
        contextBuilder.AppendLine("**RELEVANT INFORMATION FROM KNOWLEDGE BASE:**\n");

        foreach (var doc in documents)
        {
            contextBuilder.AppendLine($"**Source:** {doc.Source}");
            if (doc.Metadata != null && doc.Metadata.ContainsKey("type"))
            {
                contextBuilder.AppendLine($"**Type:** {doc.Metadata["type"]}");
            }
            contextBuilder.AppendLine(doc.Content);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    public async Task IndexDocumentsAsync(IEnumerable<RagDocument> documents, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting document indexing process");

        var documentList = documents.ToList();

        if (!documentList.Any())
        {
            _logger.LogWarning("No documents to index");
            return;
        }

        try
        {
            // Generate embeddings for all documents
            _logger.LogInformation($"Generating embeddings for {documentList.Count} documents");

            var contents = documentList.Select(d => d.Content).ToList();
            var embeddings = await _embeddingService.EmbedTextsAsync(contents, cancellationToken);

            // Pair documents with their embeddings
            var documentsWithEmbeddings = documentList
                .Zip(embeddings, (doc, emb) => new RagDocumentWithEmbedding(doc, emb))
                .ToList();

            // Index in vector store
            _logger.LogInformation($"Indexing {documentsWithEmbeddings.Count} documents into vector store");
            await _vectorStore.IndexDocumentsAsync(documentsWithEmbeddings, cancellationToken);

            _logger.LogInformation("Document indexing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing documents");
            throw;
        }
    }
}
