using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftlineApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProductToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "Messages",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ProductId",
                table: "Messages",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Messages_Products_ProductId",
                table: "Messages",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Products_ProductId",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Messages_ProductId",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Messages");
        }
    }
}
