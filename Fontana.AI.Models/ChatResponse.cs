namespace Fontana.AI.Models
{
    // Returtyp från ChatService — innehåller svaret och ID:t för loggposten
    public record ChatResponse(string Answer, int LogId);
}
