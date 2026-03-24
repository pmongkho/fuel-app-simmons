using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class MoveGaugeToReportAndAddSupervisorSignoffs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FuelingTankLevelEnd",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "FuelingTankLevelStart",
                table: "FuelEntries");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndGaugeSignedAtUtc",
                table: "FuelReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EndGaugeSignedBySupervisorId",
                table: "FuelReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EndGaugeSupervisorSignatureName",
                table: "FuelReports",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FuelingTankLevelEnd",
                table: "FuelReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FuelingTankLevelStart",
                table: "FuelReports",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartGaugeSignedAtUtc",
                table: "FuelReports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StartGaugeSignedBySupervisorId",
                table: "FuelReports",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StartGaugeSupervisorSignatureName",
                table: "FuelReports",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FuelReports_EndGaugeSignedBySupervisorId",
                table: "FuelReports",
                column: "EndGaugeSignedBySupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelReports_StartGaugeSignedBySupervisorId",
                table: "FuelReports",
                column: "StartGaugeSignedBySupervisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_FuelReports_AspNetUsers_EndGaugeSignedBySupervisorId",
                table: "FuelReports",
                column: "EndGaugeSignedBySupervisorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelReports_AspNetUsers_StartGaugeSignedBySupervisorId",
                table: "FuelReports",
                column: "StartGaugeSignedBySupervisorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelReports_AspNetUsers_EndGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelReports_AspNetUsers_StartGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropIndex(
                name: "IX_FuelReports_EndGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropIndex(
                name: "IX_FuelReports_StartGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "EndGaugeSignedAtUtc",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "EndGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "EndGaugeSupervisorSignatureName",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "FuelingTankLevelEnd",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "FuelingTankLevelStart",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "StartGaugeSignedAtUtc",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "StartGaugeSignedBySupervisorId",
                table: "FuelReports");

            migrationBuilder.DropColumn(
                name: "StartGaugeSupervisorSignatureName",
                table: "FuelReports");

            migrationBuilder.AddColumn<int>(
                name: "FuelingTankLevelEnd",
                table: "FuelEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FuelingTankLevelStart",
                table: "FuelEntries",
                type: "integer",
                nullable: true);
        }
    }
}
