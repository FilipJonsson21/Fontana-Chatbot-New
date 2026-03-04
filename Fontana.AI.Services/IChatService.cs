using Fontana.AI.Models;

namespace Fontana.AI.Services
{
    public interface IChatService
    {
        Task<string> GetAiResponseAsync(string userMessage, IList<ConversationMessage>? history = null);
    }
}
