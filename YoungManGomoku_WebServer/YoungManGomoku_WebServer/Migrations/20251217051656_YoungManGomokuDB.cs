using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace YoungManGomoku_WebServer.Migrations
{
    public partial class YoungManGomokuDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerAccountTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    AuthToken = table.Column<string>(nullable: true),
                    AuthLevel = table.Column<int>(nullable: false),
                    Nickname = table.Column<string>(nullable: true),
                    RegisterDate = table.Column<DateTime>(nullable: false),
                    LastLoginDate = table.Column<DateTime>(nullable: false),
                    LastPlayDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerAccountTable", x => x.UID);
                });

            migrationBuilder.CreateTable(
                name: "PlayerEquipItemStateTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    EquipProfile = table.Column<long>(nullable: false),
                    EquipStoneSkin = table.Column<long>(nullable: false),
                    EquipBoardSkin = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerEquipItemStateTable", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PlayerEquipItemStateTable_PlayerAccountTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerAccountTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerGomokuBattleRecordTable",
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
                    table.PrimaryKey("PK_PlayerGomokuBattleRecordTable", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PlayerGomokuBattleRecordTable_PlayerAccountTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerAccountTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerInventoryTable",
                columns: table => new
                {
                    InventoryId = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UID = table.Column<decimal>(nullable: false),
                    ItemType = table.Column<long>(nullable: false),
                    ItemId = table.Column<long>(nullable: false),
                    AcquiredDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerInventoryTable", x => x.InventoryId);
                    table.ForeignKey(
                        name: "FK_PlayerInventoryTable_PlayerAccountTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerAccountTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerMoneyTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    GameMoney = table.Column<int>(nullable: false),
                    CashMoney = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMoneyTable", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PlayerMoneyTable_PlayerAccountTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerAccountTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStatusTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    Rating = table.Column<float>(nullable: false),
                    Level = table.Column<int>(nullable: false),
                    ExperiencePoint = table.Column<int>(nullable: false),
                    MaxExperiencePoint = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStatusTable", x => x.UID);
                    table.ForeignKey(
                        name: "FK_PlayerStatusTable_PlayerAccountTable_UID",
                        column: x => x.UID,
                        principalTable: "PlayerAccountTable",
                        principalColumn: "UID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerAccountTable_AuthToken",
                table: "PlayerAccountTable",
                column: "AuthToken",
                unique: true,
                filter: "[AuthToken] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerInventoryTable_UID_ItemType_ItemId",
                table: "PlayerInventoryTable",
                columns: new[] { "UID", "ItemType", "ItemId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerEquipItemStateTable");

            migrationBuilder.DropTable(
                name: "PlayerGomokuBattleRecordTable");

            migrationBuilder.DropTable(
                name: "PlayerInventoryTable");

            migrationBuilder.DropTable(
                name: "PlayerMoneyTable");

            migrationBuilder.DropTable(
                name: "PlayerStatusTable");

            migrationBuilder.DropTable(
                name: "PlayerAccountTable");
        }
    }
}
