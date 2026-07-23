using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jadlify.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Adds the persistent shopping-list aggregate: the list root plus its items, per-item source
    /// contributions, and selected source days. Purely additive — it creates new tables and does
    /// not touch the old read-only <c>GET /api/shopping-list</c> projection, which stays until the
    /// UI migrates. A partial unique index enforces at most one active list per user while any
    /// number of completed lists coexist.
    /// </summary>
    public partial class ShoppingLists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "shopping_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_lists", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    product_category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    grams = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    is_bought = table.Column<bool>(type: "boolean", nullable: false),
                    shopping_list_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_list_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_shopping_list_items_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_source_days",
                columns: table => new
                {
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    shopping_list_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_list_source_days", x => new { x.shopping_list_id, x.date });
                    table.ForeignKey(
                        name: "FK_shopping_list_source_days_shopping_lists_shopping_list_id",
                        column: x => x.shopping_list_id,
                        principalTable: "shopping_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shopping_list_item_sources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    meal_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    grams = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    shopping_list_item_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shopping_list_item_sources", x => x.id);
                    table.ForeignKey(
                        name: "FK_shopping_list_item_sources_shopping_list_items_shopping_lis~",
                        column: x => x.shopping_list_item_id,
                        principalTable: "shopping_list_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_item_sources_shopping_list_item_id",
                table: "shopping_list_item_sources",
                column: "shopping_list_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_product_id",
                table: "shopping_list_items",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_list_items_shopping_list_id",
                table: "shopping_list_items",
                column: "shopping_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_shopping_lists_user_id_status",
                table: "shopping_lists",
                columns: new[] { "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ux_shopping_lists_active_per_user",
                table: "shopping_lists",
                column: "user_id",
                unique: true,
                filter: "status = 'Active'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shopping_list_item_sources");

            migrationBuilder.DropTable(
                name: "shopping_list_source_days");

            migrationBuilder.DropTable(
                name: "shopping_list_items");

            migrationBuilder.DropTable(
                name: "shopping_lists");
        }
    }
}
