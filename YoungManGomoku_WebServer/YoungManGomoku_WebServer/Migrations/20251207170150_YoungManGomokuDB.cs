using Microsoft.EntityFrameworkCore.Migrations;

namespace YoungManGomoku_WebServer.Migrations
{
    public partial class YoungManGomokuDB : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerGomokuRecordTable",
                columns: table => new
                {
                    UID = table.Column<decimal>(nullable: false),
                    WinCount = table.Column<long>(nullable: false),
                    DrawCount = table.Column<long>(nullable: false),
                    LoseCount = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerGomokuRecordTable", x => x.UID);
                });

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
                    ExperiencePoint = table.Column<int>(nullable: false),
                    MaxExperiencePoint = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerProfileTable", x => x.UID);
                });
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
