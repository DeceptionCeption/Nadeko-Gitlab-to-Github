using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class rolerewardrm : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level",
                table: "XpRoleReward");

            migrationBuilder.AddColumn<int>(
                name: "Action",
                table: "XpRoleReward",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level_Action",
                table: "XpRoleReward",
                columns: new[] { "XpSettingsId", "Level", "Action" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level_Action",
                table: "XpRoleReward");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "XpRoleReward");

            migrationBuilder.CreateIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level",
                table: "XpRoleReward",
                columns: new[] { "XpSettingsId", "Level" },
                unique: true);
        }
    }
}
