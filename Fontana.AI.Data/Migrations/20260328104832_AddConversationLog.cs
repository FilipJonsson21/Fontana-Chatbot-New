using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fontana.AI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConversationLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BotResponse = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokensUsed = table.Column<int>(type: "int", nullable: true),
                    ResponseTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    Feedback = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationLogs", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConversationLogs");
        }
    }
}
