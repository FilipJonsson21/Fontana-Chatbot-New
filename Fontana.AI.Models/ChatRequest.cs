using System.ComponentModel.DataAnnotations;

namespace Fontana.AI.Models
{
    public class ChatRequest
    {
        [Required(ErrorMessage = "Frågan får inte vara tom.")]
        [MaxLength(2000, ErrorMessage = "Frågan får vara max 2000 tecken.")]
        public required string Message { get; set; }

        // Valfri konversationshistorik — klienten ansvarar för att spara och skicka tillbaka den
        [MaxLength(50, ErrorMessage = "Historiken får innehålla max 50 meddelanden.")]
        public IList<ConversationMessage>? History { get; set; }
    }
}
