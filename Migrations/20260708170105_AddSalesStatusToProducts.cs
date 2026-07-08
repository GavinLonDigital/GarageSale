using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesStatusToProducts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SalesStatus",
                table: "Products",
                type: "char(1)",
                nullable: false,
                defaultValueSql: "'A'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesStatus",
                table: "Products");
        }
    }
}
