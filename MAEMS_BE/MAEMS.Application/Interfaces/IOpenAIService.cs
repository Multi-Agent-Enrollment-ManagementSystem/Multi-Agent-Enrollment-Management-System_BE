using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MAEMS.Application.Interfaces
{
    /// <summary>
    /// OpenAI service interface - CHAT + VISION
    /// Note: Embeddings still use Gemini (gemini-embedding-001, 3072 dims) to avoid re-indexing
    /// </summary>
    public interface IOpenAIService
    {
        /// <summary>
        /// Get chat completion from OpenAI GPT-4 with conversation history
        /// </summary>
        Task<string> GetChatCompletionAsync(
            string systemPrompt,
            string userMessage,
            List<(string role, string content)> conversationHistory = null,
            int? maxTokens = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get vision completion from OpenAI GPT-4o-mini with images
        /// </summary>
        Task<string> GetVisionCompletionAsync(
            string systemPrompt,
            string userMessage,
            List<string> base64Images,
            int? maxTokens = null,
            CancellationToken cancellationToken = default);
    }
}
