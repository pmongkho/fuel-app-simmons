using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace dotnet_server.Migrations
{
    public partial class EnsureIdentityColumnsPresent : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "UserName" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "NormalizedUserName" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "NormalizedEmail" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "EmailConfirmed" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "SecurityStamp" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "ConcurrencyStamp" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PhoneNumber" text;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "PhoneNumberConfirmed" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "TwoFactorEnabled" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "LockoutEnd" timestamp with time zone;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "LockoutEnabled" boolean NOT NULL DEFAULT FALSE;
                ALTER TABLE "Users" ADD COLUMN IF NOT EXISTS "AccessFailedCount" integer NOT NULL DEFAULT 0;

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_Users_NormalizedUserName" ON "Users" ("NormalizedUserName") WHERE "NormalizedUserName" IS NOT NULL;
                CREATE INDEX IF NOT EXISTS "IX_Users_NormalizedEmail" ON "Users" ("NormalizedEmail");

                UPDATE "Users"
                SET
                    "UserName" = COALESCE("UserName", "Email"),
                    "NormalizedUserName" = COALESCE("NormalizedUserName", UPPER("Email")),
                    "NormalizedEmail" = COALESCE("NormalizedEmail", UPPER("Email"));
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: this migration only ensures required Identity columns/indexes exist.
        }
    }
}
