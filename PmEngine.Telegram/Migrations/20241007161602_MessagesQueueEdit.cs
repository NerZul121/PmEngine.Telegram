using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PmEngine.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class MessagesQueueEdit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ForUserId",
                table: "MessageQueueEntity",
                newName: "ForChatId");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "MessageQueueEntity",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ForChatId",
                table: "MessageQueueEntity",
                newName: "ForUserId");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "MessageQueueEntity",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);
        }
    }
}
