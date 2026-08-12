using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class FixBusinessApplicationIdToText : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// The original AddBusinessApplications migration created BusinessApplications.Id
        /// as integer IDENTITY (auto-increment). The C# model and all later snapshots
        /// define Id as string (text / GUID). This caused every INSERT to fail with a
        /// PostgreSQL type mismatch error (500) because EF Core tries to insert a GUID
        /// string into an integer column.
        ///
        /// This migration:
        ///   1. Drops the FK from BusinessApplications → Users (required before PK change).
        ///   2. Drops the PK constraint.
        ///   3. ALTERs the Id column from integer to text (casting existing int values to text
        ///      so no existing rows are lost).
        ///   4. Re-adds the PK and FK.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Drop the foreign key that references the PK we're about to change.
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessApplications_Users_UserId",
                table: "BusinessApplications");

            // Step 2: Drop the primary key constraint.
            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessApplications",
                table: "BusinessApplications");

            // Step 3: Change Id column type from integer to text.
            // USING id::text casts any existing integer rows (e.g. 1,2,3) to their string
            // representations ('1','2','3') so no data is lost.
            migrationBuilder.Sql(
                @"ALTER TABLE ""BusinessApplications"" ALTER COLUMN ""Id"" TYPE text USING ""Id""::text;");

            // Step 4: Remove the auto-increment sequence binding (no longer needed for text PKs).
            migrationBuilder.Sql(
                @"ALTER TABLE ""BusinessApplications"" ALTER COLUMN ""Id"" DROP DEFAULT;");

            // Step 5: Re-add the primary key.
            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessApplications",
                table: "BusinessApplications",
                column: "Id");

            // Step 6: Re-add the foreign key.
            migrationBuilder.AddForeignKey(
                name: "FK_BusinessApplications_Users_UserId",
                table: "BusinessApplications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop FK and PK, convert back to integer (will fail if values aren't numeric strings).
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessApplications_Users_UserId",
                table: "BusinessApplications");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BusinessApplications",
                table: "BusinessApplications");

            migrationBuilder.Sql(
                @"ALTER TABLE ""BusinessApplications"" ALTER COLUMN ""Id"" TYPE integer USING ""Id""::integer;");

            migrationBuilder.Sql(
                @"CREATE SEQUENCE IF NOT EXISTS ""BusinessApplications_Id_seq"" OWNED BY ""BusinessApplications"".""Id"";");

            migrationBuilder.Sql(
                @"ALTER TABLE ""BusinessApplications"" ALTER COLUMN ""Id"" SET DEFAULT nextval('""BusinessApplications_Id_seq""');");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BusinessApplications",
                table: "BusinessApplications",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessApplications_Users_UserId",
                table: "BusinessApplications",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
