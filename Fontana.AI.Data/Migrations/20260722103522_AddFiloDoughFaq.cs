using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiloDoughFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "Question" },
                values: new object[] { 11, "Börja med att tina paketet oöppnat i kylskåp över natten. Låt det sedan ligga i rumstemperatur i 20–30 minuter innan användning, fortfarande oöppnat. När paketet väl är öppnat, ta ett eller några ark i taget och lägg en fuktig kökshandduk över de övriga arken — filodeg torkar och smular lätt sönder annars, både färsk och fryst.", "Tillagning & Hantering", "Hur tinar jag er frysta filodeg utan att den går sönder?" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 11);
        }
    }
}
