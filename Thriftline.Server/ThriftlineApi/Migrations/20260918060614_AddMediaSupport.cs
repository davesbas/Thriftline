using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ThriftlineApi.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaType",
                table: "ProductImages",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ForumPostMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ForumPostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    MediaType = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForumPostMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForumPostMedia_ForumPosts_ForumPostId",
                        column: x => x.ForumPostId,
                        principalTable: "ForumPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForumPostMedia_ForumPostId",
                table: "ForumPostMedia",
                column: "ForumPostId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForumPostMedia");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "ProductImages");
        }
    }
}
