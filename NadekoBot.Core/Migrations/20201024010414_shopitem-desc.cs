using Microsoft.EntityFrameworkCore.Migrations;

namespace NadekoBot.Migrations
{
    public partial class shopitemdesc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ShopEntry",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "ShopEntry");
        }
    }
}
