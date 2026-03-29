using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddFoodShopAndMenuItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""FoodShop"" (
                    ""Id"" text NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Description"" character varying(500) NULL,
                    ""Address"" character varying(200) NOT NULL,
                    ""PhoneNumber"" character varying(20) NULL,
                    ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                    CONSTRAINT ""PK_FoodShop"" PRIMARY KEY (""Id"")
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""MenuItems"" (
                    ""Id"" text NOT NULL,
                    ""FoodShopId"" text NOT NULL,
                    ""Name"" character varying(100) NOT NULL,
                    ""Description"" character varying(300) NULL,
                    ""Price"" numeric(10,2) NOT NULL,
                    ""PhotoUrl"" character varying(500) NULL,
                    CONSTRAINT ""PK_MenuItems"" PRIMARY KEY (""Id""),
                    CONSTRAINT ""FK_MenuItems_FoodShop_FoodShopId"" FOREIGN KEY (""FoodShopId"")
                        REFERENCES ""FoodShop"" (""Id"") ON DELETE CASCADE
                );
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ""IX_MenuItems_FoodShopId"" ON ""MenuItems"" (""FoodShopId"");
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "FoodShop");
        }
    }
}
