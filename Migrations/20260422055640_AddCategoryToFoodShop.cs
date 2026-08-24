using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToFoodShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "FoodShops",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "FoodShops");
        }
    }
}
