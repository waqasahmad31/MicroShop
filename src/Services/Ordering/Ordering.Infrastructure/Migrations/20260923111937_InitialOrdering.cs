using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOrdering : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.CheckConstraint("ck_orders_ids", "id <> '00000000-0000-0000-0000-000000000000'::uuid AND customer_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_orders_reason", "(status <> 'Rejected' OR (status_reason IS NOT NULL AND length(btrim(status_reason)) > 0)) AND (status NOT IN ('Pending', 'Confirmed') OR status_reason IS NULL)");
                    table.CheckConstraint("ck_orders_status", "status IN ('Pending', 'Confirmed', 'Rejected', 'Cancelled')");
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_items", x => new { x.order_id, x.product_id });
                    table.CheckConstraint("ck_order_items_name", "length(btrim(product_name)) > 0");
                    table.CheckConstraint("ck_order_items_price", "unit_price BETWEEN 0 AND 99999999.99");
                    table.CheckConstraint("ck_order_items_product", "product_id <> '00000000-0000-0000-0000-000000000000'::uuid");
                    table.CheckConstraint("ck_order_items_quantity", "quantity BETWEEN 1 AND 1000");
                    table.ForeignKey(
                        name: "fk_order_items_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_orders_customer_created_id",
                table: "orders",
                columns: new[] { "customer_id", "created_at_utc", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "orders");
        }
    }
}
