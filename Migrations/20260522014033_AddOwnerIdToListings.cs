using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerIdToListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "GiftShops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "FoodShops",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "Accommodations",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GiftShops_OwnerId",
                table: "GiftShops",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_FoodShops_OwnerId",
                table: "FoodShops",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Accommodations_OwnerId",
                table: "Accommodations",
                column: "OwnerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accommodations_Users_OwnerId",
                table: "Accommodations",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FoodShops_Users_OwnerId",
                table: "FoodShops",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_GiftShops_Users_OwnerId",
                table: "GiftShops",
                column: "OwnerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accommodations_Users_OwnerId",
                table: "Accommodations");

            migrationBuilder.DropForeignKey(
                name: "FK_FoodShops_Users_OwnerId",
                table: "FoodShops");

            migrationBuilder.DropForeignKey(
                name: "FK_GiftShops_Users_OwnerId",
                table: "GiftShops");

            migrationBuilder.DropIndex(
                name: "IX_GiftShops_OwnerId",
                table: "GiftShops");

            migrationBuilder.DropIndex(
                name: "IX_FoodShops_OwnerId",
                table: "FoodShops");

            migrationBuilder.DropIndex(
                name: "IX_Accommodations_OwnerId",
                table: "Accommodations");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "GiftShops");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "FoodShops");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "Accommodations");
        }
    }
}
