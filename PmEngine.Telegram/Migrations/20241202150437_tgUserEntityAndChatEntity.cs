using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PmEngine.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class tgUserEntityAndChatEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramUserEntity",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "ChatId",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "ForChatId",
                table: "MessageQueueEntity");

            migrationBuilder.AlterColumn<long>(
                name: "TGID",
                table: "TelegramUserEntity",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "Comment",
                table: "TelegramUserEntity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "TelegramUserEntity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "TelegramUserEntity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Login",
                table: "TelegramUserEntity",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ForChatTgId",
                table: "MessageQueueEntity",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ForUserTgId",
                table: "MessageQueueEntity",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramUserEntity",
                table: "TelegramUserEntity",
                column: "TGID");

            migrationBuilder.CreateTable(
                name: "TelegramChatEntity",
                columns: table => new
                {
                    ChatId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ChatTitle = table.Column<string>(type: "text", nullable: true),
                    ChannelLogin = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramChatEntity", x => x.ChatId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramChatEntity");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TelegramUserEntity",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "Comment",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "Login",
                table: "TelegramUserEntity");

            migrationBuilder.DropColumn(
                name: "ForChatTgId",
                table: "MessageQueueEntity");

            migrationBuilder.DropColumn(
                name: "ForUserTgId",
                table: "MessageQueueEntity");

            migrationBuilder.AlterColumn<long>(
                name: "TGID",
                table: "TelegramUserEntity",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .OldAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<long>(
                name: "ChatId",
                table: "TelegramUserEntity",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ForChatId",
                table: "MessageQueueEntity",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TelegramUserEntity",
                table: "TelegramUserEntity",
                columns: new[] { "TGID", "ChatId" });
        }
    }
}
