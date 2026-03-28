using Fontana.AI.Models;

namespace Fontana.AI.Services
{
    public interface IChatService
    {
        Task<ChatResponse> GetAiResponseAsync(string userMessage, IList<ConversationMessage>? history = null);
    }
}
