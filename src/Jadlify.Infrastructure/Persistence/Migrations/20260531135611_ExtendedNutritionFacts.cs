using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ExtendedNutritionFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "calcium_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "fiber_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "iron_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "monounsaturated_fat_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "package_size_grams",
                table: "products",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "polyunsaturated_fat_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "potassium_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "salt_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "saturated_fat_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sodium_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sugars_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "trans_fat_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vitamin_a_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vitamin_c_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "vitamin_d_per_100g",
                table: "products",
                type: "numeric(12,6)",
                precision: 12,
                scale: 6,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "calcium_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "fiber_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "iron_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "monounsaturated_fat_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "package_size_grams",
                table: "products");

            migrationBuilder.DropColumn(
                name: "polyunsaturated_fat_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "potassium_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "salt_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "saturated_fat_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sodium_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "sugars_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "trans_fat_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "vitamin_a_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "vitamin_c_per_100g",
                table: "products");

            migrationBuilder.DropColumn(
                name: "vitamin_d_per_100g",
                table: "products");
        }
    }
}
