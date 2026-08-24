using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddRideModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "RideHistories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RideId = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    RiderId = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    FinalFare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CalculatedDistance = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RideRequests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    StudentId = table.Column<string>(type: "text", nullable: false),
                    RiderId = table.Column<string>(type: "text", nullable: true),
                    ServiceType = table.Column<string>(type: "text", nullable: false),
                    Details = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    PickupLocation = table.Column<Point>(type: "geography(Point, 4326)", nullable: false),
                    DropoffLocation = table.Column<Point>(type: "geography(Point, 4326)", nullable: false),
                    OTP = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: true),
                    OtpExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OTPAttempts = table.Column<int>(type: "integer", nullable: false),
                    EstimatedFare = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    CalculatedDistance = table.Column<decimal>(type: "numeric(6,3)", nullable: false),
                    PriceRateSnapshot = table.Column<string>(type: "jsonb", nullable: false),
                    PenaltyApplied = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ArrivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InProgressAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastHeartbeatAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RideRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RideRequests_Users_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RideRequests_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RiderStatuses",
                columns: table => new
                {
                    RiderId = table.Column<string>(type: "text", nullable: false),
                    IsOnline = table.Column<bool>(type: "boolean", nullable: false),
                    LastLocation = table.Column<Point>(type: "geography(Point, 4326)", nullable: true),
                    LastSeen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovalStatus = table.Column<string>(type: "text", nullable: false),
                    RiderTier = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiderStatuses", x => x.RiderId);
                    table.ForeignKey(
                        name: "FK_RiderStatuses_Users_RiderId",
                        column: x => x.RiderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RideId = table.Column<string>(type: "text", nullable: false),
                    Coordinates = table.Column<Point>(type: "geography(Point, 4326)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrackingLogs_RideRequests_RideId",
                        column: x => x.RideId,
                        principalTable: "RideRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RideHistories_CompletedAt",
                table: "RideHistories",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RideHistories_RideId",
                table: "RideHistories",
                column: "RideId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_CreatedAt",
                table: "RideRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_RiderId",
                table: "RideRequests",
                column: "RiderId");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_Status",
                table: "RideRequests",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RideRequests_StudentId",
                table: "RideRequests",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLogs_RideId",
                table: "TrackingLogs",
                column: "RideId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingLogs_Timestamp",
                table: "TrackingLogs",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RideHistories");

            migrationBuilder.DropTable(
                name: "RiderStatuses");

            migrationBuilder.DropTable(
                name: "TrackingLogs");

            migrationBuilder.DropTable(
                name: "RideRequests");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
