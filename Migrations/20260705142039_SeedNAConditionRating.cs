using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class SeedNAConditionRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ConditionRatings",
                columns: new[] { "Id", "ConditionRatingNumber", "ConditionRatingNumberDescription" },
                values: new object[] { 6, 999, "NA" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 6);
        }
    }
}
