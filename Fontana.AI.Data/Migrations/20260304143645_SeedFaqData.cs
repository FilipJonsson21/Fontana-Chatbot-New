using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedFaqData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "Question" },
                values: new object[,]
                {
                    { 1, "Fontana är ett familjeföretag med rötter i Grekland och Cypern som importerar och säljer autentiska medelhavsdelikatesser till den svenska marknaden. Vi brinner för äkta smaker och hög kvalitet.", "Om oss", "Vad är Fontana?" },
                    { 2, "Fontanas produkter finns i välsorterade livsmedelsbutiker runt om i Sverige. Kontakta din lokala butik för att höra om de har Fontanas sortiment.", "Om oss", "Var kan jag köpa Fontanas produkter?" },
                    { 3, "Du är välkommen att kontakta oss på fontana@support.com så hjälper vi dig så snart vi kan!", "Om oss", "Hur kontaktar jag Fontana?" },
                    { 4, "Halloumi innehåller mjölk (get- och/eller fårmjölk). Produkten är inte lämplig för personer med mjölkallergi eller laktosintolerans.", "Allergener", "Vilka allergener finns i halloumi?" },
                    { 5, "Traditionell halloumi är gjord på pastöriserad get- och/eller fårmjölk, salt och mynta. Inga onödiga tillsatser.", "Ingredienser", "Vad innehåller halloumi för ingredienser?" },
                    { 6, "Halloumi är en traditionell ost med ursprung på Cypern och är en skyddad ursprungsbeteckning (PDO) inom EU sedan 2021.", "Ursprung", "Var kommer halloumin ifrån?" },
                    { 7, "Ja! Halloumi är perfekt för grillning. Den smälter inte utan får en fin stekyta och krämig insida. Grilla på hög värme i 2–3 minuter per sida.", "Tillagning", "Kan man grilla halloumi?" },
                    { 8, "Ja, halloumi är vegetarisk. Den innehåller inga kött- eller fiskprodukter.", "Kost", "Är halloumi vegetarisk?" },
                    { 9, "Fetaost innehåller mjölk (fårmjölk, ibland upp till 30% getmjölk). Inte lämplig för personer med mjölkallergi.", "Allergener", "Vilka allergener finns i fetaost?" },
                    { 10, "Äkta fetaost är en skyddad ursprungsbeteckning (PDO) och tillverkas uteslutande i Grekland. Den görs på fårmjölk eller en blandning av får- och getmjölk.", "Ursprung", "Var kommer fetaosten ifrån?" },
                    { 11, "Fetaost innehåller pastöriserad fårmjölk (ibland upp till 30% getmjölk) och salt. Lagras i saltlake för sin karakteristiska smak.", "Ingredienser", "Vad innehåller fetaost?" },
                    { 12, "Ja, fetaost är vegetarisk och innehåller inga kött- eller fiskprodukter.", "Kost", "Är fetaost vegetarisk?" },
                    { 13, "Fontana erbjuder extra virgin olivolja av hög kvalitet med ursprung från Medelhavsregionen. Extra virgin innebär att oljan är kallpressad och av högsta kvalitetsklass.", "Produktinfo", "Vilken olivolja säljer Fontana?" },
                    { 14, "Ren olivolja innehåller inga kända allergener. Den är naturligt fri från gluten, mjölk, nötter och soja.", "Allergener", "Vilka allergener finns i olivolja?" },
                    { 15, "Många av Fontanas produkter som ostar och olivolja är naturligt glutenfria. Kontrollera alltid förpackningens innehållsförteckning för den specifika produkten eller kontakta oss på fontana@support.com.", "Kost", "Är Fontanas produkter glutenfria?" },
                    { 16, "Fontana strävar efter att erbjuda naturliga och högkvalitativa produkter. För information om specifika ekologiska produkter, kontakta oss på fontana@support.com.", "Produktinfo", "Säljer Fontana ekologiska produkter?" },
                    { 17, "Halloumi och fetaost förvaras bäst i kylskåp. Öppnad förpackning bör användas inom några dagar. Fetaost kan förvaras i saltlake för längre hållbarhet.", "Förvaring", "Hur förvarar jag halloumi och fetaost?" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 17);
        }
    }
}
