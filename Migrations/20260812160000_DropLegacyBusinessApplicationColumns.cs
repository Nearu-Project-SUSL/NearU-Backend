using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class DropLegacyBusinessApplicationColumns : Migration
    {
        /// <inheritdoc />
        /// <remarks>
        /// Drops two legacy NOT NULL columns from BusinessApplications that were
        /// added in the original AddBusinessApplications migration but never
        /// removed when the model was simplified. Their presence caused every
        /// INSERT to fail with a PostgreSQL NOT NULL violation, silently preventing
        /// business applications from ever being saved to the database.
        /// </remarks>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "BusinessApplications");

            migrationBuilder.DropColumn(
                name: "ApplicationDataJson",
                table: "BusinessApplications");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Re-add the columns as nullable so Down() doesn't fail on existing rows.
            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "BusinessApplications",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ApplicationDataJson",
                table: "BusinessApplications",
                type: "text",
                nullable: true);
        }
    }
}
