using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class MoveIdentityUsersToAspNetUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public."Users"') IS NOT NULL
                       AND to_regclass('public."AspNetUsers"') IS NULL THEN
                        ALTER TABLE "Users" RENAME TO "AspNetUsers";
                    END IF;
                END $$;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DO $$
                BEGIN
                    IF to_regclass('public."AspNetUsers"') IS NOT NULL
                       AND to_regclass('public."Users"') IS NULL THEN
                        ALTER TABLE "AspNetUsers" RENAME TO "Users";
                    END IF;
                END $$;
                """);
        }
    }
}
