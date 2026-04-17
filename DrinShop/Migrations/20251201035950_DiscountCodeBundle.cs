using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrinShop.Migrations
{
    /// <inheritdoc />
    public partial class DiscountCodeBundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Bundles");

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "Bundles",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "Bundles");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Bundles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Bundles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
