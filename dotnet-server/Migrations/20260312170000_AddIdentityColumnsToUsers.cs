using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class AddIdentityColumnsToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: Identity columns are managed on AspNetUsers.
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op
        }
    }
}
