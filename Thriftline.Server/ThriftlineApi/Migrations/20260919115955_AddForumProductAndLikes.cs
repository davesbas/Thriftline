using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftlineApi.Migrations
{
    /// <inheritdoc />
    public partial class AddForumProductAndLikes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ForumPosts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProductId",
                table: "ForumComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "ForumComments",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ForumPostLikes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForumPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPostLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumPostLikes_ForumPosts_ForumPostId",
                        column: x => x.ForumPostId,
                        principalTable: "ForumPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ForumPostLikes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPosts_ProductId",
                table: "ForumPosts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_ProductId",
                table: "ForumComments",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ForumComments_UserId1",
                table: "ForumComments",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_ForumPostLikes_ForumPostId_UserId",
                table: "ForumPostLikes",
                columns: new[] { "ForumPostId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForumPostLikes_UserId",
                table: "ForumPostLikes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumComments_Products_ProductId",
                table: "ForumComments",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ForumComments_Users_UserId1",
                table: "ForumComments",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ForumPosts_Products_ProductId",
                table: "ForumPosts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ForumComments_Products_ProductId",
                table: "ForumComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumComments_Users_UserId1",
                table: "ForumComments");

            migrationBuilder.DropForeignKey(
                name: "FK_ForumPosts_Products_ProductId",
                table: "ForumPosts");

            migrationBuilder.DropTable(
                name: "ForumPostLikes");

            migrationBuilder.DropIndex(
                name: "IX_ForumPosts_ProductId",
                table: "ForumPosts");

            migrationBuilder.DropIndex(
                name: "IX_ForumComments_ProductId",
                table: "ForumComments");

            migrationBuilder.DropIndex(
                name: "IX_ForumComments_UserId1",
                table: "ForumComments");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ForumPosts");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "ForumComments");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "ForumComments");
        }
    }
}
