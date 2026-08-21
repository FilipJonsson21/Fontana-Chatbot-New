using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWineAssortmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AssortmentType",
                table: "Wines",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AssortmentType",
                table: "Wines");
        }
    }
}
