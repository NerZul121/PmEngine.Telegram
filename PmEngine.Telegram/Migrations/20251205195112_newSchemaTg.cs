using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PmEngine.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class newSchemaTg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TgChats",
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
                    table.PrimaryKey("PK_TgChats", x => x.ChatId);
                });

            migrationBuilder.CreateTable(
                name: "TgMessagesQueue",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ForUserTgId = table.Column<long>(type: "bigint", nullable: true),
                    ForChatTgId = table.Column<long>(type: "bigint", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    Actions = table.Column<string>(type: "text", nullable: true),
                    Media = table.Column<string>(type: "text", nullable: true),
                    Arguments = table.Column<string>(type: "text", nullable: true),
                    SendedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MessageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgMessagesQueue", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TgUserData",
                columns: table => new
                {
                    TGID = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: true),
                    FirstName = table.Column<string>(type: "text", nullable: true),
                    LastName = table.Column<string>(type: "text", nullable: true),
                    Comment = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TgUserData", x => x.TGID);
                    table.ForeignKey(
                        name: "FK_TgUserData_Users_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TgUserData_OwnerId",
                table: "TgUserData",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TgChats");

            migrationBuilder.DropTable(
                name: "TgMessagesQueue");

            migrationBuilder.DropTable(
                name: "TgUserData");
        }
    }
}
