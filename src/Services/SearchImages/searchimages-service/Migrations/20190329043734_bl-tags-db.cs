using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace SearchImagesService.Migrations
{
    public partial class bltagsdb : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "searchimages");

            migrationBuilder.CreateTable(
                name: "blacklistedtags",
                schema: "searchimages",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.SerialColumn),
                    dateadded = table.Column<DateTime>(nullable: false, defaultValueSql: "timezone('utc', now())"),
                    guildid = table.Column<decimal>(nullable: false),
                    tags = table.Column<string[]>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blacklistedtags", x => x.id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "blacklistedtags",
                schema: "searchimages");
        }
    }
}
