using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixFaqProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "question",
                table: "Faqs",
                newName: "Question");

            migrationBuilder.RenameColumn(
                name: "answer",
                table: "Faqs",
                newName: "Answer");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Faqs",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Question",
                table: "Faqs",
                newName: "question");

            migrationBuilder.RenameColumn(
                name: "Answer",
                table: "Faqs",
                newName: "answer");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Faqs",
                newName: "id");
        }
    }
}
