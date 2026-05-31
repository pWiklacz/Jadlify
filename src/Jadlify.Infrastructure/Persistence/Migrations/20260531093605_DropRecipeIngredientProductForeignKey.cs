using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropRecipeIngredientProductForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_recipe_ingredients_products_product_id",
                table: "recipe_ingredients");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddForeignKey(
                name: "FK_recipe_ingredients_products_product_id",
                table: "recipe_ingredients",
                column: "product_id",
                principalTable: "products",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
