using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace YoungManGomoku_WebServer.Migrations
{
    public partial class InitMySqlSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerAccountTable",
                columns: table => new
                {
                    UID = table.Column<ulong>(nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AuthToken = table.Column<string>(maxLength: 190, nullable: true),
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
                    UID = table.Column<ulong>(nullable: false),
                    EquipProfile = table.Column<uint>(nullable: false),
                    EquipStoneSkin = table.Column<uint>(nullable: false),
                    EquipBoardSkin = table.Column<uint>(nullable: false)
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
                    UID = table.Column<ulong>(nullable: false),
                    WinCount = table.Column<uint>(nullable: false),
                    DrawCount = table.Column<uint>(nullable: false),
                    LoseCount = table.Column<uint>(nullable: false),
                    DisconnectCount = table.Column<uint>(nullable: false)
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
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UID = table.Column<ulong>(nullable: false),
                    ItemType = table.Column<uint>(nullable: false),
                    ItemId = table.Column<uint>(nullable: false),
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
                    UID = table.Column<ulong>(nullable: false),
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
                    UID = table.Column<ulong>(nullable: false),
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
                unique: true);

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
