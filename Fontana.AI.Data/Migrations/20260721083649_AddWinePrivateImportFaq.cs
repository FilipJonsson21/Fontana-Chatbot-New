using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWinePrivateImportFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "Question" },
                values: new object[] { 9, "Nej, vi säljer alkoholhaltiga drycker endast till restauranger och foodservice-kunder — inte direkt till privatpersoner, det är inte tillåtet enligt svensk lag. Vill du som privatperson få tag på ett specifikt vin eller sprit ur vårt sortiment kan du kontakta Systembolagets avdelning för speciella viner och sprit och begära en så kallad privatimport. Systembolaget skickar då en förfrågan vidare till oss, och vi lämnar i så fall ett pris till dem — inte till dig direkt. Det går inte att köpa varan via en restaurang istället.", "Vin & Alkohol", "Kan jag som privatperson köpa vin eller sprit direkt av Fontana?" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 9);
        }
    }
}
