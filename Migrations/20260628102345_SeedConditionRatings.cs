using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class SeedConditionRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ConditionRatings",
                columns: new[] { "Id", "ConditionRatingNumber", "ConditionRatingNumberDescription" },
                values: new object[,]
                {
                    { 1, 1, "Excellent" },
                    { 2, 2, "Good" },
                    { 3, 3, "OK" },
                    { 4, 4, "Bad" },
                    { 5, 5, "Terrible" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "ConditionRatings",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
