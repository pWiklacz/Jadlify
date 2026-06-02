using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecipeIngredientSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "calories_per_100g",
                table: "recipe_ingredients",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "carbohydrates_per_100g",
                table: "recipe_ingredients",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "fat_per_100g",
                table: "recipe_ingredients",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "recipe_ingredients",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "protein_per_100g",
                table: "recipe_ingredients",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.Sql(
                """
                UPDATE recipe_ingredients AS ri
                SET
                    product_name = p.name,
                    calories_per_100g = p.calories_per_100g,
                    carbohydrates_per_100g = p.carbohydrates_per_100g,
                    fat_per_100g = p.fat_per_100g,
                    protein_per_100g = p.protein_per_100g
                FROM recipes AS r, products AS p
                WHERE r.id = ri.recipe_id
                    AND p.id = ri.product_id
                    AND p.user_id = r.user_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "calories_per_100g",
                table: "recipe_ingredients");

            migrationBuilder.DropColumn(
                name: "carbohydrates_per_100g",
                table: "recipe_ingredients");

            migrationBuilder.DropColumn(
                name: "fat_per_100g",
                table: "recipe_ingredients");

            migrationBuilder.DropColumn(
                name: "product_name",
                table: "recipe_ingredients");

            migrationBuilder.DropColumn(
                name: "protein_per_100g",
                table: "recipe_ingredients");
        }
    }
}
