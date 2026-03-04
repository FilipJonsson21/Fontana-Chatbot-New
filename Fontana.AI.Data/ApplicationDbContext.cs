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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FaqItem>().HasData(

                // --- Om Fontana ---
                new FaqItem { Id = 1, Question = "Vad är Fontana?", Answer = "Fontana är ett familjeföretag med rötter i Grekland och Cypern som importerar och säljer autentiska medelhavsdelikatesser till den svenska marknaden. Vi brinner för äkta smaker och hög kvalitet.", Category = "Om oss" },
                new FaqItem { Id = 2, Question = "Var kan jag köpa Fontanas produkter?", Answer = "Fontanas produkter finns i välsorterade livsmedelsbutiker runt om i Sverige. Kontakta din lokala butik för att höra om de har Fontanas sortiment.", Category = "Om oss" },
                new FaqItem { Id = 3, Question = "Hur kontaktar jag Fontana?", Answer = "Du är välkommen att kontakta oss på fontana@support.com så hjälper vi dig så snart vi kan!", Category = "Om oss" },

                // --- Halloumi ---
                new FaqItem { Id = 4, Question = "Vilka allergener finns i halloumi?", Answer = "Halloumi innehåller mjölk (get- och/eller fårmjölk). Produkten är inte lämplig för personer med mjölkallergi eller laktosintolerans.", Category = "Allergener" },
                new FaqItem { Id = 5, Question = "Vad innehåller halloumi för ingredienser?", Answer = "Traditionell halloumi är gjord på pastöriserad get- och/eller fårmjölk, salt och mynta. Inga onödiga tillsatser.", Category = "Ingredienser" },
                new FaqItem { Id = 6, Question = "Var kommer halloumin ifrån?", Answer = "Halloumi är en traditionell ost med ursprung på Cypern och är en skyddad ursprungsbeteckning (PDO) inom EU sedan 2021.", Category = "Ursprung" },
                new FaqItem { Id = 7, Question = "Kan man grilla halloumi?", Answer = "Ja! Halloumi är perfekt för grillning. Den smälter inte utan får en fin stekyta och krämig insida. Grilla på hög värme i 2–3 minuter per sida.", Category = "Tillagning" },
                new FaqItem { Id = 8, Question = "Är halloumi vegetarisk?", Answer = "Ja, halloumi är vegetarisk. Den innehåller inga kött- eller fiskprodukter.", Category = "Kost" },

                // --- Feta ---
                new FaqItem { Id = 9, Question = "Vilka allergener finns i fetaost?", Answer = "Fetaost innehåller mjölk (fårmjölk, ibland upp till 30% getmjölk). Inte lämplig för personer med mjölkallergi.", Category = "Allergener" },
                new FaqItem { Id = 10, Question = "Var kommer fetaosten ifrån?", Answer = "Äkta fetaost är en skyddad ursprungsbeteckning (PDO) och tillverkas uteslutande i Grekland. Den görs på fårmjölk eller en blandning av får- och getmjölk.", Category = "Ursprung" },
                new FaqItem { Id = 11, Question = "Vad innehåller fetaost?", Answer = "Fetaost innehåller pastöriserad fårmjölk (ibland upp till 30% getmjölk) och salt. Lagras i saltlake för sin karakteristiska smak.", Category = "Ingredienser" },
                new FaqItem { Id = 12, Question = "Är fetaost vegetarisk?", Answer = "Ja, fetaost är vegetarisk och innehåller inga kött- eller fiskprodukter.", Category = "Kost" },

                // --- Olivolja ---
                new FaqItem { Id = 13, Question = "Vilken olivolja säljer Fontana?", Answer = "Fontana erbjuder extra virgin olivolja av hög kvalitet med ursprung från Medelhavsregionen. Extra virgin innebär att oljan är kallpressad och av högsta kvalitetsklass.", Category = "Produktinfo" },
                new FaqItem { Id = 14, Question = "Vilka allergener finns i olivolja?", Answer = "Ren olivolja innehåller inga kända allergener. Den är naturligt fri från gluten, mjölk, nötter och soja.", Category = "Allergener" },

                // --- Allmänt om produkter ---
                new FaqItem { Id = 15, Question = "Är Fontanas produkter glutenfria?", Answer = "Många av Fontanas produkter som ostar och olivolja är naturligt glutenfria. Kontrollera alltid förpackningens innehållsförteckning för den specifika produkten eller kontakta oss på fontana@support.com.", Category = "Kost" },
                new FaqItem { Id = 16, Question = "Säljer Fontana ekologiska produkter?", Answer = "Fontana strävar efter att erbjuda naturliga och högkvalitativa produkter. För information om specifika ekologiska produkter, kontakta oss på fontana@support.com.", Category = "Produktinfo" },
                new FaqItem { Id = 17, Question = "Hur förvarar jag halloumi och fetaost?", Answer = "Halloumi och fetaost förvaras bäst i kylskåp. Öppnad förpackning bör användas inom några dagar. Fetaost kan förvaras i saltlake för längre hållbarhet.", Category = "Förvaring" }
            );
        }
    }
}
