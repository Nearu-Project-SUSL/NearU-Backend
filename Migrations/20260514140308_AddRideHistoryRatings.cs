using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddRideHistoryRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RiderRating",
                table: "RideHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StudentRating",
                table: "RideHistories",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Accommodations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    DistanceKm = table.Column<decimal>(type: "numeric", nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "numeric", nullable: false),
                    AvailableBeds = table.Column<int>(type: "integer", nullable: false),
                    Amenities = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accommodations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FoodShops",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    PhotoUrl = table.Column<string>(type: "text", nullable: true),
                    Category = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodShops", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Jobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PayRange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    JobType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Logo = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LongDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Requirements = table.Column<string>(type: "text", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    IsNew = table.Column<bool>(type: "boolean", nullable: false),
                    PostedByName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PostedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Jobs_Users_PostedByUserId",
                        column: x => x.PostedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccommodationItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    AccommodationId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccommodationItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccommodationItems_Accommodations_AccommodationId",
                        column: x => x.AccommodationId,
                        principalTable: "Accommodations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FoodShopId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_FoodShops_FoodShopId",
                        column: x => x.FoodShopId,
                        principalTable: "FoodShops",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccommodationItems_AccommodationId",
                table: "AccommodationItems",
                column: "AccommodationId");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_Category",
                table: "Jobs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_CreatedAt",
                table: "Jobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_IsNew",
                table: "Jobs",
                column: "IsNew");

            migrationBuilder.CreateIndex(
                name: "IX_Jobs_PostedByUserId",
                table: "Jobs",
                column: "PostedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MenuItems_FoodShopId",
                table: "MenuItems",
                column: "FoodShopId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccommodationItems");

            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "Accommodations");

            migrationBuilder.DropTable(
                name: "FoodShops");

            migrationBuilder.DropColumn(
                name: "RiderRating",
                table: "RideHistories");

            migrationBuilder.DropColumn(
                name: "StudentRating",
                table: "RideHistories");
        }
    }
}
