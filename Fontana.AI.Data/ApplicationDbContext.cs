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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Initiala FAQ-poster för Fontana-chatboten
            modelBuilder.Entity<FaqItem>().HasData(
                new FaqItem { Id = 1, Question = "Var kommer Fontanas produkter ifrån?", Answer = "Fontana är ett familjeföretag med rötter i Grekland och Cypern. Våra produkter är noga utvalda från Medelhavsregionen.", Category = "Allmänt" },
                new FaqItem { Id = 2, Question = "Är Fontanas olivolja kallpressad?", Answer = "Ja, vår extra virgin olivolja är kallpressad för att bevara smak och naturliga näringsvärden.", Category = "Produkter" },
                new FaqItem { Id = 3, Question = "Var kan jag köpa Fontanas produkter?", Answer = "Våra produkter finns i de flesta svenska livsmedelsbutiker. Kontakta oss på fontana@support.com om du inte hittar dem nära dig.", Category = "Inköp" },
                new FaqItem { Id = 4, Question = "Hur ska jag förvara olivoljan?", Answer = "Förvara olivoljan svalt och mörkt, gärna i rumstemperatur och borta från direkt solljus. Undvik kylskåp då oljan kan stelna.", Category = "Förvaring" },
                new FaqItem { Id = 5, Question = "Är era produkter ekologiska?", Answer = "Vissa av våra produkter är ekologiskt certifierade. Se produktetiketten eller kontakta oss på fontana@support.com för mer information.", Category = "Certifiering" },
                new FaqItem { Id = 6, Question = "Innehåller era produkter gluten?", Answer = "De flesta av våra produkter är glutenfria, men vi rekommenderar alltid att du kontrollerar ingrediensförteckningen på förpackningen för säkerhets skull.", Category = "Allergener" },
                new FaqItem { Id = 7, Question = "Hur kontaktar jag Fontana?", Answer = "Du når oss enklast via e-post på fontana@support.com. Vi svarar så snart vi kan.", Category = "Kontakt" },
                new FaqItem { Id = 8, Question = "Hur länge håller olivoljan efter öppning?", Answer = "Vi rekommenderar att olivoljan används inom 3–6 månader efter öppning för bästa smak. Se alltid bäst-före-datum på förpackningen.", Category = "Förvaring" }
            );
        }
    }
}
