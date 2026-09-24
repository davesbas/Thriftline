using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftlineApi.Migrations
{
    /// <inheritdoc />
    public partial class AddForumStoreIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ForumPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "StoreId",
                table: "ForumComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_StoreId",
                table: "ForumPosts",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_StoreId",
                table: "ForumComments",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumComments_Stores_StoreId",
                table: "ForumComments",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_Stores_StoreId",
                table: "ForumPosts",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumComments_Stores_StoreId",
                table: "ForumComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_Stores_StoreId",
                table: "ForumPosts");

            migrationBuilder.DropIndex(
                name: "IX_ForumPosts_StoreId",
                table: "ForumPosts");

            migrationBuilder.DropIndex(
                name: "IX_ForumComments_StoreId",
                table: "ForumComments");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "ForumComments");
        }
    }
}
