using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DrinShop.Migrations
{
    /// <inheritdoc />
    public partial class AddUrlImageInBundle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "BundleItems");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Bundles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Bundles",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Bundles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Bundles");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Bundles");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "BundleItems",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
