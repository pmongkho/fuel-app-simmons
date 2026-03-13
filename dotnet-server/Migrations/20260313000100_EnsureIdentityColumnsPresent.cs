using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class EnsureIdentityColumnsPresent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "FullName" text NOT NULL DEFAULT '';
                ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "Role" integer NOT NULL DEFAULT 0;
                ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT TRUE;
                ALTER TABLE "AspNetUsers" ADD COLUMN IF NOT EXISTS "CreatedAtUtc" timestamp with time zone NOT NULL DEFAULT now();

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AspNetUsers_Email" ON "AspNetUsers" ("Email") WHERE "Email" IS NOT NULL;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_AspNetUsers_Email";

                ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "CreatedAtUtc";
                ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "IsActive";
                ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "Role";
                ALTER TABLE "AspNetUsers" DROP COLUMN IF EXISTS "FullName";
                """);
        }
    }
}
