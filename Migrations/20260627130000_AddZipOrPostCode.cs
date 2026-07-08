using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class AddZipOrPostCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ZipOrPostCode",
                table: "AspNetUsers",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ZipOrPostCode",
                table: "AspNetUsers");
        }
    }
}
