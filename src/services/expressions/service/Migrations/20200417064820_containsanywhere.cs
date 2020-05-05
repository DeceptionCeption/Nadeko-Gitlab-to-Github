using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ExpressionsService.Migrations
{
    public partial class containsanywhere : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "containsanywhere",
                schema: "expressions",
                table: "expressions",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                schema: "expressions",
                table: "expressions",
                keyColumn: "id",
                keyValue: -2,
                column: "dateadded",
                value: new DateTime(2020, 4, 17, 6, 48, 19, 703, DateTimeKind.Utc).AddTicks(4456));

            migrationBuilder.UpdateData(
                schema: "expressions",
                table: "expressions",
                keyColumn: "id",
                keyValue: -1,
                column: "dateadded",
                value: new DateTime(2020, 4, 17, 6, 48, 19, 703, DateTimeKind.Utc).AddTicks(2687));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "containsanywhere",
                schema: "expressions",
                table: "expressions");

            migrationBuilder.UpdateData(
                schema: "expressions",
                table: "expressions",
                keyColumn: "id",
                keyValue: -2,
                column: "dateadded",
                value: new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(2133));

            migrationBuilder.UpdateData(
                schema: "expressions",
                table: "expressions",
                keyColumn: "id",
                keyValue: -1,
                column: "dateadded",
                value: new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(369));
        }
    }
}
