using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFaqSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "Question" },
                values: new object[,]
                {
                    { 1, "Fontana är ett familjeföretag med rötter i Grekland och Cypern. Våra produkter är noga utvalda från Medelhavsregionen.", "Allmänt", "Var kommer Fontanas produkter ifrån?" },
                    { 2, "Ja, vår extra virgin olivolja är kallpressad för att bevara smak och naturliga näringsvärden.", "Produkter", "Är Fontanas olivolja kallpressad?" },
                    { 3, "Våra produkter finns i de flesta svenska livsmedelsbutiker. Kontakta oss på fontana@support.com om du inte hittar dem nära dig.", "Inköp", "Var kan jag köpa Fontanas produkter?" },
                    { 4, "Förvara olivoljan svalt och mörkt, gärna i rumstemperatur och borta från direkt solljus. Undvik kylskåp då oljan kan stelna.", "Förvaring", "Hur ska jag förvara olivoljan?" },
                    { 5, "Vissa av våra produkter är ekologiskt certifierade. Se produktetiketten eller kontakta oss på fontana@support.com för mer information.", "Certifiering", "Är era produkter ekologiska?" },
                    { 6, "De flesta av våra produkter är glutenfria, men vi rekommenderar alltid att du kontrollerar ingrediensförteckningen på förpackningen för säkerhets skull.", "Allergener", "Innehåller era produkter gluten?" },
                    { 7, "Du når oss enklast via e-post på fontana@support.com. Vi svarar så snart vi kan.", "Kontakt", "Hur kontaktar jag Fontana?" },
                    { 8, "Vi rekommenderar att olivoljan används inom 3–6 månader efter öppning för bästa smak. Se alltid bäst-före-datum på förpackningen.", "Förvaring", "Hur länge håller olivoljan efter öppning?" }
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
        }
    }
}
