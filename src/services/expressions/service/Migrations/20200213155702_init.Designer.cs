﻿// <auto-generated />
using System;
using ExpressionsService.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ExpressionsService.Migrations
{
    [DbContext(typeof(ExpressionsContext))]
    [Migration("20200213155702_init")]
    partial class init
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("expressions")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("ExpressionsService.Database.Models.Expression", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("id")
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<decimal>("AuthorId")
                        .HasColumnName("authorid")
                        .HasColumnType("numeric(20,0)");

                    b.Property<string>("AuthorName")
                        .HasColumnName("authorname")
                        .HasColumnType("text");

                    b.Property<bool>("AutoDelete")
                        .HasColumnName("autodelete")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("DateAdded")
                        .ValueGeneratedOnAdd()
                        .HasColumnName("dateadded")
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("timezone('utc', now())");

                    b.Property<bool>("DirectMessage")
                        .HasColumnName("directmessage")
                        .HasColumnType("boolean");

                    b.Property<decimal>("GuildId")
                        .HasColumnName("guildid")
                        .HasColumnType("numeric(20,0)");

                    b.Property<bool>("IsQuote")
                        .HasColumnName("isquote")
                        .HasColumnType("boolean");

                    b.Property<string>("Response")
                        .HasColumnName("response")
                        .HasColumnType("text");

                    b.Property<string>("Trigger")
                        .HasColumnName("trigger")
                        .HasColumnType("text");

                    b.HasKey("Id")
                        .HasName("pk_expressions");

                    b.HasIndex("GuildId");

                    b.HasIndex("Trigger");

                    b.ToTable("expressions");

                    b.HasData(
                        new
                        {
                            Id = -1,
                            AuthorId = 105635576866156544m,
                            AuthorName = "Kwoth#2452",
                            AutoDelete = false,
                            DateAdded = new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(369),
                            DirectMessage = false,
                            GuildId = 0m,
                            IsQuote = false,
                            Response = @"{
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
}",
                            Trigger = ".donate"
                        },
                        new
                        {
                            Id = -2,
                            AuthorId = 105635576866156544m,
                            AuthorName = "Kwoth#2452",
                            AutoDelete = false,
                            DateAdded = new DateTime(2020, 2, 13, 15, 57, 2, 411, DateTimeKind.Utc).AddTicks(2133),
                            DirectMessage = false,
                            GuildId = 0m,
                            IsQuote = false,
                            Response = @"{
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
}",
                            Trigger = ".guide"
                        });
                });
#pragma warning restore 612, 618
        }
    }
}
