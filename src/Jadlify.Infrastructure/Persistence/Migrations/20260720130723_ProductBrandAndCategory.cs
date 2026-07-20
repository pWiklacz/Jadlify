using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProductBrandAndCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "brand",
                table: "products",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "category",
                table: "products",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_products_user_id_category_name",
                table: "products",
                columns: new[] { "user_id", "category", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_products_user_id_category_name",
                table: "products");

            migrationBuilder.DropColumn(
                name: "brand",
                table: "products");

            migrationBuilder.DropColumn(
                name: "category",
                table: "products");
        }
    }
}
