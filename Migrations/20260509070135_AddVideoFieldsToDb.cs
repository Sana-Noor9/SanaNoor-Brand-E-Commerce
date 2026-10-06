using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SanaNoor_Brand.Migrations
{
    /// <inheritdoc />
    public partial class AddVideoFieldsToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "VideoTitle",
                table: "ProductVideos",
                newName: "Title");

            migrationBuilder.AddColumn<string>(
                name: "VideoType",
                table: "ProductVideos",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoType",
                table: "ProductVideos");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "ProductVideos",
                newName: "VideoTitle");
        }
    }
}
