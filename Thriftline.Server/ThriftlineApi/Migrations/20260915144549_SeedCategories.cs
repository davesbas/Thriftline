using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ThriftlineApi.Migrations
{
    /// <inheritdoc />
    public partial class SeedCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Description", "IconUrl", "Name", "ParentCategoryId" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111101"), null, null, "Fashion Pria", null },
                    { new Guid("11111111-1111-1111-1111-111111111104"), null, null, "Fashion Wanita", null },
                    { new Guid("11111111-1111-1111-1111-111111111107"), null, null, "Sepatu", null },
                    { new Guid("11111111-1111-1111-1111-111111111108"), null, null, "Tas", null },
                    { new Guid("11111111-1111-1111-1111-111111111109"), null, null, "Elektronik", null },
                    { new Guid("11111111-1111-1111-1111-111111111110"), null, null, "Aksesoris", null },
                    { new Guid("11111111-1111-1111-1111-111111111102"), null, null, "Atasan Pria", new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("11111111-1111-1111-1111-111111111103"), null, null, "Celana Pria", new Guid("11111111-1111-1111-1111-111111111101") },
                    { new Guid("11111111-1111-1111-1111-111111111105"), null, null, "Atasan Wanita", new Guid("11111111-1111-1111-1111-111111111104") },
                    { new Guid("11111111-1111-1111-1111-111111111106"), null, null, "Bawahan Wanita", new Guid("11111111-1111-1111-1111-111111111104") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111102"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111103"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111105"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111106"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111107"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111108"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111109"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111110"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111101"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111104"));
        }
    }
}
