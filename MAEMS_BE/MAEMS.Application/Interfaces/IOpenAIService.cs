using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MAEMS.Application.Interfaces
{
    /// <summary>
    /// OpenAI service interface - CHAT ONLY
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
            CancellationToken cancellationToken = default);
    }
}
