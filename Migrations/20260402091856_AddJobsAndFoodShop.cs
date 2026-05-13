using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NearU_Backend_Revised.Migrations
{
    /// <inheritdoc />
    public partial class AddJobsAndFoodShop : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use SQL to check if tables exist before creating
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'FoodShop') THEN
                        CREATE TABLE ""FoodShop"" (
                            ""Id"" text NOT NULL,
                            ""Name"" character varying(100) NOT NULL,
                            ""Description"" character varying(500),
                            ""Address"" character varying(200) NOT NULL,
                            ""PhoneNumber"" character varying(20),
                            ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                            CONSTRAINT ""PK_FoodShop"" PRIMARY KEY (""Id"")
                        );
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'MenuItems') THEN
                        CREATE TABLE ""MenuItems"" (
                            ""Id"" text NOT NULL,
                            ""FoodShopId"" text NOT NULL,
                            ""Name"" character varying(100) NOT NULL,
                            ""Description"" character varying(300),
                            ""Price"" numeric(10,2) NOT NULL,
                            ""PhotoUrl"" character varying(500),
                            CONSTRAINT ""PK_MenuItems"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_MenuItems_FoodShop_FoodShopId"" FOREIGN KEY (""FoodShopId"") 
                                REFERENCES ""FoodShop"" (""Id"") ON DELETE CASCADE
                        );
                        CREATE INDEX ""IX_MenuItems_FoodShopId"" ON ""MenuItems"" (""FoodShopId"");
                    END IF;
                END $$;
            ");

            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'Jobs') THEN
                        CREATE TABLE ""Jobs"" (
                            ""Id"" text NOT NULL,
                            ""Title"" character varying(200) NOT NULL,
                            ""Company"" character varying(100) NOT NULL,
                            ""Location"" character varying(100) NOT NULL,
                            ""PayRange"" character varying(100) NOT NULL,
                            ""JobType"" character varying(50) NOT NULL,
                            ""Category"" character varying(50) NOT NULL,
                            ""Logo"" text,
                            ""Description"" character varying(500) NOT NULL,
                            ""LongDescription"" character varying(2000),
                            ""Requirements"" text,
                            ""Tags"" text,
                            ""IsNew"" boolean NOT NULL,
                            ""PostedByName"" character varying(100) NOT NULL,
                            ""PostedByUserId"" text NOT NULL,
                            ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW(),
                            ""UpdatedAt"" timestamp with time zone,
                            CONSTRAINT ""PK_Jobs"" PRIMARY KEY (""Id""),
                            CONSTRAINT ""FK_Jobs_Users_PostedByUserId"" FOREIGN KEY (""PostedByUserId"") 
                                REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
                        );
                        CREATE INDEX ""IX_Jobs_Category"" ON ""Jobs"" (""Category"");
                        CREATE INDEX ""IX_Jobs_CreatedAt"" ON ""Jobs"" (""CreatedAt"");
                        CREATE INDEX ""IX_Jobs_IsNew"" ON ""Jobs"" (""IsNew"");
                        CREATE INDEX ""IX_Jobs_PostedByUserId"" ON ""Jobs"" (""PostedByUserId"");
                    END IF;
                END $$;
            ");

            // Original migration code commented out
            /*
            migrationBuilder.CreateTable(
                name: "FoodShop",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FoodShop", x => x.Id);
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
                name: "MenuItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    FoodShopId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Price = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MenuItems_FoodShop_FoodShopId",
                        column: x => x.FoodShopId,
                        principalTable: "FoodShop",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Jobs");

            migrationBuilder.DropTable(
                name: "MenuItems");

            migrationBuilder.DropTable(
                name: "FoodShop");
        }
    }
}
