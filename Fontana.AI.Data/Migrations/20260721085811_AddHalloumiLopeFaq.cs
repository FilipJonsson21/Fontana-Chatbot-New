using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHalloumiLopeFaq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Faqs",
                columns: new[] { "Id", "Answer", "Category", "Question" },
                values: new object[] { 10, "Vår halloumi tillverkas med mikrobiell löpe (ystenzym) som framställs med hjälp av mikroorganismen Rhizomucor miehei — inte animalisk löpe (som annars utvinns ur magen hos unga kalvar, lamm eller killingar). Båda typerna av löpe har samma funktion i osttillverkningen: att få mjölken att koagulera så att ost kan bildas. Skillnaden ligger i hur enzymet framställs, och det påverkar normalt inte halloumins smak eller användningsområde på ett märkbart sätt. Eftersom vår halloumi använder mikrobiell löpe är den helt vegetarisk och passar därför fler konsumenter.", "Ingredienser", "Använder ni animalisk eller mikrobiell löpe i er halloumi?" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Faqs",
                keyColumn: "Id",
                keyValue: 10);
        }
    }
}
