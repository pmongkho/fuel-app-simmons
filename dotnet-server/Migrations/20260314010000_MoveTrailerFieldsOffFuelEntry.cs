using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class MoveTrailerFieldsOffFuelEntry : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsFull",
                table: "Trailers",
                newName: "IsTankFull");

            migrationBuilder.DropColumn(
                name: "EndGaugeLevel",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "StartGaugeLevel",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "TrailerNumber",
                table: "FuelEntries");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTankFull",
                table: "Trailers",
                newName: "IsFull");

            migrationBuilder.AddColumn<string>(
                name: "EndGaugeLevel",
                table: "FuelEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FuelEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartGaugeLevel",
                table: "FuelEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TrailerNumber",
                table: "FuelEntries",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
