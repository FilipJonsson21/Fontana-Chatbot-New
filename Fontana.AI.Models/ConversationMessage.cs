namespace Fontana.AI.Models
{
    // Representerar ett meddelande i en konversationshistorik
    public class ConversationMessage
    {
        // "user" eller "assistant"
        public required string Role { get; set; }
        public required string Content { get; set; }
    }
}
