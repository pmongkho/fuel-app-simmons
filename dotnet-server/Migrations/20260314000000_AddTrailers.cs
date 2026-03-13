using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class AddTrailers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trailers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TrailerNumber = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<string>(type: "text", nullable: false),
                    IsFull = table.Column<bool>(type: "boolean", nullable: false),
                    HasMechanicalIssues = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trailers", x => x.Id);
                });

            migrationBuilder.AddColumn<int>(
                name: "TrailerId",
                table: "FuelEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelEntries_TrailerId",
                table: "FuelEntries",
                column: "TrailerId");

            migrationBuilder.CreateIndex(
                name: "IX_Trailers_TrailerNumber",
                table: "Trailers",
                column: "TrailerNumber",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelEntries_Trailers_TrailerId",
                table: "FuelEntries",
                column: "TrailerId",
                principalTable: "Trailers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelEntries_Trailers_TrailerId",
                table: "FuelEntries");

            migrationBuilder.DropTable(
                name: "Trailers");

            migrationBuilder.DropIndex(
                name: "IX_FuelEntries_TrailerId",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "TrailerId",
                table: "FuelEntries");
        }
    }
}
