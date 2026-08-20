using Microsoft.EntityFrameworkCore;
using Fontana.AI.Models;

namespace Fontana.AI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<FaqItem> Faqs { get; set; }
        public DbSet<DabasProduct> DabasProducts { get; set; }
        public DbSet<ConversationLog> ConversationLogs { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Wine> Wines { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Initiala FAQ-poster för Fontana-chatboten
            modelBuilder.Entity<FaqItem>().HasData(
                new FaqItem { Id = 1, Question = "Var kommer Fontanas produkter ifrån?", Answer = "Fontana är ett familjeföretag med rötter i Grekland och Cypern. Våra produkter är noga utvalda från Medelhavsregionen.", Category = "Allmänt" },
                new FaqItem { Id = 2, Question = "Är Fontanas olivolja kallpressad?", Answer = "Ja, vår extra virgin olivolja är kallpressad för att bevara smak och naturliga näringsvärden.", Category = "Produkter" },
                new FaqItem { Id = 3, Question = "Var kan jag köpa Fontanas produkter?", Answer = "Våra produkter finns i de flesta svenska livsmedelsbutiker. Kontakta oss på info@fontanafood.se om du inte hittar dem nära dig.", Category = "Inköp" },
                new FaqItem { Id = 4, Question = "Hur ska jag förvara olivoljan?", Answer = "Förvara olivoljan svalt och mörkt, gärna i rumstemperatur och borta från direkt solljus. Undvik kylskåp då oljan kan stelna.", Category = "Förvaring" },
                new FaqItem { Id = 5, Question = "Är era produkter ekologiska?", Answer = "Vissa av våra produkter är ekologiskt certifierade. Se produktetiketten eller kontakta oss på info@fontanafood.se för mer information.", Category = "Certifiering" },
                new FaqItem { Id = 6, Question = "Innehåller era produkter gluten?", Answer = "De flesta av våra produkter är glutenfria, men vi rekommenderar alltid att du kontrollerar ingrediensförteckningen på förpackningen för säkerhets skull.", Category = "Allergener" },
                new FaqItem { Id = 7, Question = "Hur kontaktar jag Fontana?", Answer = "Du når oss enklast via e-post på info@fontanafood.se. Vi svarar så snart vi kan.", Category = "Kontakt" },
                new FaqItem { Id = 8, Question = "Hur länge håller olivoljan efter öppning?", Answer = "Vi rekommenderar att olivoljan används inom 3–6 månader efter öppning för bästa smak. Se alltid bäst-före-datum på förpackningen.", Category = "Förvaring" },
                new FaqItem { Id = 9, Question = "Kan jag som privatperson köpa vin eller sprit direkt av Fontana?", Answer = "Nej, vi säljer alkoholhaltiga drycker endast till restauranger och foodservice-kunder — inte direkt till privatpersoner, det är inte tillåtet enligt svensk lag. Vill du som privatperson få tag på ett specifikt vin eller sprit ur vårt sortiment kan du kontakta Systembolagets avdelning för speciella viner och sprit och begära en så kallad privatimport. Systembolaget skickar då en förfrågan vidare till oss, och vi lämnar i så fall ett pris till dem — inte till dig direkt. Det går inte att köpa varan via en restaurang istället.", Category = "Vin & Alkohol" },
                new FaqItem { Id = 10, Question = "Använder ni animalisk eller mikrobiell löpe i er halloumi?", Answer = "Vår halloumi tillverkas med mikrobiell löpe (ystenzym) som framställs med hjälp av mikroorganismen Rhizomucor miehei — inte animalisk löpe (som annars utvinns ur magen hos unga kalvar, lamm eller killingar). Båda typerna av löpe har samma funktion i osttillverkningen: att få mjölken att koagulera så att ost kan bildas. Skillnaden ligger i hur enzymet framställs, och det påverkar normalt inte halloumins smak eller användningsområde på ett märkbart sätt. Eftersom vår halloumi använder mikrobiell löpe är den helt vegetarisk och passar därför fler konsumenter.", Category = "Ingredienser" },
                new FaqItem { Id = 11, Question = "Hur tinar jag er frysta filodeg utan att den går sönder?", Answer = "Börja med att tina paketet oöppnat i kylskåp över natten. Låt det sedan ligga i rumstemperatur i 20–30 minuter innan användning, fortfarande oöppnat. När paketet väl är öppnat, ta ett eller några ark i taget och lägg en fuktig kökshandduk över de övriga arken — filodeg torkar och smular lätt sönder annars, både färsk och fryst.", Category = "Tillagning & Hantering" }
            );
        }
    }
}
