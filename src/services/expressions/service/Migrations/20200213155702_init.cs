using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ExpressionsService.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "expressions");

            migrationBuilder.CreateTable(
                name: "expressions",
                schema: "expressions",
                columns: table => new
                {
                    id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    dateadded = table.Column<DateTime>(nullable: false, defaultValueSql: "timezone('utc', now())"),
                    guildid = table.Column<decimal>(nullable: false),
                    authorid = table.Column<decimal>(nullable: false),
                    authorname = table.Column<string>(nullable: true),
                    trigger = table.Column<string>(nullable: true),
                    response = table.Column<string>(nullable: true),
                    autodelete = table.Column<bool>(nullable: false),
                    directmessage = table.Column<bool>(nullable: false),
                    isquote = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_expressions", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "expressions",
                table: "expressions",
                columns: new[] { "id", "authorid", "authorname", "autodelete", "dateadded", "directmessage", "guildid", "isquote", "response", "trigger" },
                values: new object[,]
                {
                    { -1, 105635576866156544m, "Kwoth#2452", false, new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(369), false, 0m, false, @"{
                  ""title"": ""How to donate"",
                  ""description"": ""%user.fullname%, If you want to support NadekoBot, you can do so through these links"",
                                ""color"": 53380,
                                ""fields"": [
                    {
                                ""name"": ""Patreon"",
                      ""value"": "" https://patreon.com/nadekobot"",
                      ""inline"": true
                    },
                    {
                                ""name"": ""Paypal"",
                      ""value"": "" https://paypal.me/Kwoth"",
                      ""inline"": true
                    }
                  ]
                }", ".donate" },
                    { -2, 105635576866156544m, "Kwoth#2452", false, new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(2133), false, 0m, false, @"{
                                ""color"": 53380,
                                ""fields"": [
                                {
                                    ""name"": ""List of commands"",
                                    ""value"": ""https://nadeko.bot/commands"",
                                    ""inline"": false
                                },
                                {
                                    ""name"": ""Hosting guides and docs"",
                                    ""value"": ""http://nadekobot.readthedocs.io/en/latest/"",
                                    ""inline"": false
                                }]
                }", ".guide" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_expressions_guildid",
                schema: "expressions",
                table: "expressions",
                column: "guildid");

            migrationBuilder.CreateIndex(
                name: "IX_expressions_trigger",
                schema: "expressions",
                table: "expressions",
                column: "trigger");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "expressions",
                schema: "expressions");
        }
    }
}
