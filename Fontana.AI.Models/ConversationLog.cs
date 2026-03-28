namespace Fontana.AI.Models
{
    public class ConversationLog
    {
        public int Id { get; set; }
        public required string UserMessage { get; set; }
        public required string BotResponse { get; set; }
        public DateTime Timestamp { get; set; }
        public int? TokensUsed { get; set; }
        public long? ResponseTimeMs { get; set; }

        // true = 👍, false = 👎, null = ingen feedback
        public bool? Feedback { get; set; }
    }
}
