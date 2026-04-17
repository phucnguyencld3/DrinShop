using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceToProductOptionValue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "ProductOptionValues",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "ProductOptionValues");
        }
    }
}
