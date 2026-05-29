using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_macro_goals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_calories = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    target_protein = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    target_fat = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    target_carbohydrates = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_macro_goals", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    barcode = table.Column<string>(type: "text", nullable: true),
                    calories_per_100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    protein_per_100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    fat_per_100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    carbohydrates_per_100g = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    portions = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "meal_plan_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    portions = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_meal_plan_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_meal_plan_entries_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "recipe_ingredients",
                columns: table => new
                {
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    whole_recipe_grams = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_ingredients", x => new { x.recipe_id, x.product_id });
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_recipe_ingredients_recipes_recipe_id",
                        column: x => x.recipe_id,
                        principalTable: "recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_macro_goals_user_id",
                table: "daily_macro_goals",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_recipe_id",
                table: "meal_plan_entries",
                column: "recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_user_id_date",
                table: "meal_plan_entries",
                columns: new[] { "user_id", "date" });

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id",
                table: "products",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_barcode",
                table: "products",
                columns: new[] { "user_id", "barcode" });

            migrationBuilder.CreateIndex(
                name: "IX_recipe_ingredients_product_id",
                table: "recipe_ingredients",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipes_user_id",
                table: "recipes",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_macro_goals");

            migrationBuilder.DropTable(
                name: "meal_plan_entries");

            migrationBuilder.DropTable(
                name: "recipe_ingredients");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "recipes");
        }
    }
}
