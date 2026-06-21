using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddTransportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BusRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartPoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EndPoint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartureTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ArrivalTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BusNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainRoutes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RouteName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartStation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EndStation = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartureTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    ArrivalTime = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TrainName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainRoutes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TukTukDrivers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PlateNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OperatingArea = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TukTukDrivers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BusRoutes");

            migrationBuilder.DropTable(
                name: "TrainRoutes");

            migrationBuilder.DropTable(
                name: "TukTukDrivers");
        }
    }
}
