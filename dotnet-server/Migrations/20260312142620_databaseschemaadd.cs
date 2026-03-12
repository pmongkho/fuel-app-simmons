using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace dotnet_server.Migrations
{
    /// <inheritdoc />
    public partial class databaseschemaadd : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndGauge",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "SupervisorSigned",
                table: "FuelEntries");

            migrationBuilder.RenameColumn(
                name: "StartGauge",
                table: "FuelEntries",
                newName: "GallonsPumped");

            migrationBuilder.RenameColumn(
                name: "EmployeeName",
                table: "FuelEntries",
                newName: "TrailerNumber");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "FuelEntries",
                newName: "EnteredAtUtc");

            migrationBuilder.AlterColumn<int>(
                name: "FuelType",
                table: "FuelEntries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "EndGaugeLevel",
                table: "FuelEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EnteredByUserId",
                table: "FuelEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "FuelReportId",
                table: "FuelEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "FuelEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
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
                name: "SupervisorSignatureName",
                table: "FuelEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerificationStatus",
                table: "FuelEntries",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "VerifiedAtUtc",
                table: "FuelEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VerifiedBySupervisorId",
                table: "FuelEntries",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FuelReportId = table.Column<int>(type: "integer", nullable: true),
                    FuelEntryId = table.Column<int>(type: "integer", nullable: true),
                    RecipientEmail = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ProviderMessageId = table.Column<string>(type: "text", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelEntryPhotos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FuelEntryId = table.Column<int>(type: "integer", nullable: false),
                    PhotoType = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    FilePath = table.Column<string>(type: "text", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    UploadedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelEntryPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelEntryPhotos_FuelEntries_FuelEntryId",
                        column: x => x.FuelEntryId,
                        principalTable: "FuelEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotificationRecipients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: false),
                    RecipientType = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRecipients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FullName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FuelReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ReportDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "integer", nullable: false),
                    Expectations = table.Column<string>(type: "text", nullable: true),
                    TrailersOnYard = table.Column<string>(type: "text", nullable: true),
                    MechanicalIssues = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TotalRedDiesel = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalClearDiesel = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDef = table.Column<decimal>(type: "numeric", nullable: false),
                    OverallTotalGallons = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FuelReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FuelReports_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FuelEntries_EnteredByUserId",
                table: "FuelEntries",
                column: "EnteredByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelEntries_FuelReportId",
                table: "FuelEntries",
                column: "FuelReportId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelEntries_VerifiedBySupervisorId",
                table: "FuelEntries",
                column: "VerifiedBySupervisorId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelEntryPhotos_FuelEntryId",
                table: "FuelEntryPhotos",
                column: "FuelEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_FuelReports_CreatedByUserId",
                table: "FuelReports",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelEntries_FuelReports_FuelReportId",
                table: "FuelEntries",
                column: "FuelReportId",
                principalTable: "FuelReports",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelEntries_Users_EnteredByUserId",
                table: "FuelEntries",
                column: "EnteredByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FuelEntries_Users_VerifiedBySupervisorId",
                table: "FuelEntries",
                column: "VerifiedBySupervisorId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FuelEntries_FuelReports_FuelReportId",
                table: "FuelEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelEntries_Users_EnteredByUserId",
                table: "FuelEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_FuelEntries_Users_VerifiedBySupervisorId",
                table: "FuelEntries");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "FuelEntryPhotos");

            migrationBuilder.DropTable(
                name: "FuelReports");

            migrationBuilder.DropTable(
                name: "NotificationRecipients");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropIndex(
                name: "IX_FuelEntries_EnteredByUserId",
                table: "FuelEntries");

            migrationBuilder.DropIndex(
                name: "IX_FuelEntries_FuelReportId",
                table: "FuelEntries");

            migrationBuilder.DropIndex(
                name: "IX_FuelEntries_VerifiedBySupervisorId",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "EndGaugeLevel",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "EnteredByUserId",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "FuelReportId",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "StartGaugeLevel",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "SupervisorSignatureName",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "VerificationStatus",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "VerifiedAtUtc",
                table: "FuelEntries");

            migrationBuilder.DropColumn(
                name: "VerifiedBySupervisorId",
                table: "FuelEntries");

            migrationBuilder.RenameColumn(
                name: "TrailerNumber",
                table: "FuelEntries",
                newName: "EmployeeName");

            migrationBuilder.RenameColumn(
                name: "GallonsPumped",
                table: "FuelEntries",
                newName: "StartGauge");

            migrationBuilder.RenameColumn(
                name: "EnteredAtUtc",
                table: "FuelEntries",
                newName: "CreatedAt");

            migrationBuilder.AlterColumn<string>(
                name: "FuelType",
                table: "FuelEntries",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<decimal>(
                name: "EndGauge",
                table: "FuelEntries",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "SupervisorSigned",
                table: "FuelEntries",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
