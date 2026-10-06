using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanaNoor_Brand.Migrations
{
    /// <inheritdoc />
    public partial class AddProductIdToVideo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "ProductVideos");

            migrationBuilder.DropColumn(
                name: "IsPrimary",
                table: "ProductVideos");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "ProductVideos");

            migrationBuilder.DropColumn(
                name: "VideoType",
                table: "ProductVideos");

            migrationBuilder.DropColumn(
                name: "VideoUrl",
                table: "ProductVideos");

            migrationBuilder.AlterColumn<string>(
                name: "VideoPath",
                table: "ProductVideos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoTitle",
                table: "ProductVideos",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoTitle",
                table: "ProductVideos");

            migrationBuilder.AlterColumn<string>(
                name: "VideoPath",
                table: "ProductVideos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "ProductVideos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrimary",
                table: "ProductVideos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "ProductVideos",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoType",
                table: "ProductVideos",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VideoUrl",
                table: "ProductVideos",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
