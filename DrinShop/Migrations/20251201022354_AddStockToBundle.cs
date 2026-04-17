using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddStockToBundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Bundles",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Bundles");
        }
    }
}
