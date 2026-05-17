using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddAccommodationDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Amenities",
                table: "Accommodations",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AvailableBeds",
                table: "Accommodations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DistanceKm",
                table: "Accommodations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MonthlyRent",
                table: "Accommodations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Accommodations",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amenities",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "AvailableBeds",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "DistanceKm",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "MonthlyRent",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Accommodations");
        }
    }
}
