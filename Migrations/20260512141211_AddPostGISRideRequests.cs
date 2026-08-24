using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddPostGISRideRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffLat",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "DropoffLon",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "PickupLat",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "PickupLon",
                table: "RideRequests");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<Point>(
                name: "DropoffLocation",
                table: "RideRequests",
                type: "geometry",
                nullable: false);

            migrationBuilder.AddColumn<Point>(
                name: "PickupLocation",
                table: "RideRequests",
                type: "geometry",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DropoffLocation",
                table: "RideRequests");

            migrationBuilder.DropColumn(
                name: "PickupLocation",
                table: "RideRequests");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<double>(
                name: "DropoffLat",
                table: "RideRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DropoffLon",
                table: "RideRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PickupLat",
                table: "RideRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PickupLon",
                table: "RideRequests",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
