using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace YoungManGomoku_WebServer.Migrations
{
    public partial class YoungManGomokuDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerProfileTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    AuthToken = table.Column<string>(nullable: true),
                    AuthLevel = table.Column<int>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    GameMoney = table.Column<int>(nullable: false),
                    CashMoney = table.Column<int>(nullable: false),
                    EquipProfile = table.Column<int>(nullable: false),
                    EquipStoneSkin = table.Column<int>(nullable: false),
                    EquipBoardSkin = table.Column<int>(nullable: false),
                    Rating = table.Column<float>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    ExperiencePoint = table.Column<int>(nullable: false),
                    MaxExperiencePoint = table.Column<int>(nullable: false),
                    RegisterDate = table.Column<DateTime>(nullable: false),
                    LastLoginDate = table.Column<DateTime>(nullable: false),
                    LastPlayDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfileTable", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGomokuRecordTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    WinCount = table.Column<long>(nullable: false),
                    DrawCount = table.Column<long>(nullable: false),
                    LoseCount = table.Column<long>(nullable: false),
                    DisconnectCount = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGomokuRecordTable", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PlayerGomokuRecordTable_PlayerProfileTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerProfileTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerProfileTable_AuthToken",
                table: "PlayerProfileTable",
                column: "AuthToken",
                unique: true,
                filter: "[AuthToken] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerGomokuRecordTable");

            migrationBuilder.DropTable(
                name: "PlayerProfileTable");
        }
    }
}
