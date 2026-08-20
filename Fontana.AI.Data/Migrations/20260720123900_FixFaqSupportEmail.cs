using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFaqSupportEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 3,
                column: "Answer",
                value: "Våra produkter finns i de flesta svenska livsmedelsbutiker. Kontakta oss på info@fontanafood.se om du inte hittar dem nära dig.");

            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 5,
                column: "Answer",
                value: "Vissa av våra produkter är ekologiskt certifierade. Se produktetiketten eller kontakta oss på info@fontanafood.se för mer information.");

            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 7,
                column: "Answer",
                value: "Du når oss enklast via e-post på info@fontanafood.se. Vi svarar så snart vi kan.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 3,
                column: "Answer",
                value: "Våra produkter finns i de flesta svenska livsmedelsbutiker. Kontakta oss på fontana@support.com om du inte hittar dem nära dig.");

            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 5,
                column: "Answer",
                value: "Vissa av våra produkter är ekologiskt certifierade. Se produktetiketten eller kontakta oss på fontana@support.com för mer information.");

            migrationBuilder.UpdateData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 7,
                column: "Answer",
                value: "Du når oss enklast via e-post på fontana@support.com. Vi svarar så snart vi kan.");
        }
    }
}
