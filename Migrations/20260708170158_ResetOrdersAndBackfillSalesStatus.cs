using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class ResetOrdersAndBackfillSalesStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear existing order data (OrderItems first — FK references Orders).
            // Identity columns are intentionally left as-is: reseeding an empty table
            // makes the next generated Id equal the reseed value itself (a SQL Server
            // quirk), which could produce Order.Id = 0 — a value OrderService already
            // uses as a sentinel for "no order created".
            migrationBuilder.Sql("DELETE FROM [OrderItems];");
            migrationBuilder.Sql("DELETE FROM [Orders];");

            // Backfill every existing product as available now that orders are cleared.
            migrationBuilder.Sql("UPDATE [Products] SET [SalesStatus] = 'A';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversible data reset — deleted Orders/OrderItems rows cannot be restored.
        }
    }
}
