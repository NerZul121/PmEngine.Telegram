using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PmEngine.Telegram.Migrations
{
    /// <inheritdoc />
    public partial class tgEntitits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramUserEntity",
                columns: table => new
                {
                    TGID = table.Column<long>(type: "bigint", nullable: false),
                    ChatId = table.Column<long>(type: "bigint", nullable: false),
                    OwnerId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUserEntity", x => new { x.TGID, x.ChatId });
                    table.ForeignKey(
                        name: "FK_TelegramUserEntity_UserEntity_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "UserEntity",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramUserEntity_OwnerId",
                table: "TelegramUserEntity",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramUserEntity");
        }
    }
}
