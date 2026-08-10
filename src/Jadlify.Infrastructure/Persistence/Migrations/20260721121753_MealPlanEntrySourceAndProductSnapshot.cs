using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Turns the recipe-only meal-plan entry into a discriminated one: a recipe reference with
    /// decimal portions, or an owned product snapshot with grams.
    /// <para>
    /// Ordering matters here and is deliberately not the scaffolded order. New columns are added
    /// permissively, existing rows are backfilled as recipe entries carrying their old portion
    /// count, and only then are NOT NULL and the exactly-one-source check applied and the old
    /// <c>portions</c> column dropped. Adding the constraint first would reject every existing
    /// row; dropping <c>portions</c> first would lose the values the backfill reads.
    /// </para>
    /// </summary>
    public partial class MealPlanEntrySourceAndProductSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new columns permissively so existing rows stay valid mid-migration.
            migrationBuilder.AddColumn<string>(
                name: "source",
                table: "meal_plan_entries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "quantity",
                table: "meal_plan_entries",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "product_id",
                table: "meal_plan_entries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_name",
                table: "meal_plan_entries",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "product_category",
                table: "meal_plan_entries",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "product_calories_per_100g",
                table: "meal_plan_entries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "product_protein_per_100g",
                table: "meal_plan_entries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "product_fat_per_100g",
                table: "meal_plan_entries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "product_carbohydrates_per_100g",
                table: "meal_plan_entries",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            // 2. Every pre-existing row is a recipe entry; its portion count becomes the quantity.
            //    Date, meal type, and recipe id are untouched, so day macros stay identical.
            migrationBuilder.Sql(
                """
                UPDATE meal_plan_entries
                SET source = 'Recipe',
                    quantity = portions;
                """);

            // 3. Tighten only after the backfill has given every row a valid value.
            migrationBuilder.AlterColumn<string>(
                name: "source",
                table: "meal_plan_entries",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "quantity",
                table: "meal_plan_entries",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3,
                oldNullable: true);

            // A product entry has no recipe, so the FK column becomes optional. The FK itself
            // stays Restrict: deleting a recipe still planned remains a conflict.
            migrationBuilder.AlterColumn<Guid>(
                name: "recipe_id",
                table: "meal_plan_entries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // 4. The old column is fully superseded by quantity.
            migrationBuilder.DropColumn(
                name: "portions",
                table: "meal_plan_entries");

            migrationBuilder.CreateIndex(
                name: "IX_meal_plan_entries_product_id",
                table: "meal_plan_entries",
                column: "product_id");

            migrationBuilder.AddCheckConstraint(
                name: "ck_meal_plan_entries_exactly_one_source",
                table: "meal_plan_entries",
                sql: "quantity > 0 AND (\n    (source = 'Recipe' AND recipe_id IS NOT NULL AND product_id IS NULL)\n    OR (source = 'Product' AND recipe_id IS NULL AND product_id IS NOT NULL)\n)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_meal_plan_entries_exactly_one_source",
                table: "meal_plan_entries");

            migrationBuilder.DropIndex(
                name: "IX_meal_plan_entries_product_id",
                table: "meal_plan_entries");

            // Product entries have no representation in the recipe-only schema, so rolling back
            // discards them. This is lossy by nature: keep a backup before reversing on data.
            migrationBuilder.Sql("DELETE FROM meal_plan_entries WHERE source = 'Product';");

            migrationBuilder.AddColumn<int>(
                name: "portions",
                table: "meal_plan_entries",
                type: "integer",
                nullable: true);

            // Half portions cannot survive an integer column; round up so a planned meal is
            // never silently reduced to zero, which the old schema would have rejected.
            migrationBuilder.Sql("UPDATE meal_plan_entries SET portions = CEIL(quantity);");

            migrationBuilder.AlterColumn<int>(
                name: "portions",
                table: "meal_plan_entries",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "recipe_id",
                table: "meal_plan_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.DropColumn(name: "source", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "quantity", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_id", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_name", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_category", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_calories_per_100g", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_protein_per_100g", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_fat_per_100g", table: "meal_plan_entries");
            migrationBuilder.DropColumn(name: "product_carbohydrates_per_100g", table: "meal_plan_entries");
        }
    }
}
