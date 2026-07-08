using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSellerLogoPaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rewrite e.g. "images/wendy_logo.png" → "images/Hero/wendy_logo.png"
            // for every row that isn't already under the Hero sub-folder.
            migrationBuilder.Sql(@"
                UPDATE [SellerDetails]
                SET    [LogoPath] = 'images/Hero/' + RIGHT([LogoPath], CHARINDEX('/', REVERSE([LogoPath])) - 1)
                WHERE  [LogoPath] IS NOT NULL
                  AND  [LogoPath] NOT LIKE 'images/Hero/%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert "images/Hero/wendy_logo.png" → "images/wendy_logo.png".
            migrationBuilder.Sql(@"
                UPDATE [SellerDetails]
                SET    [LogoPath] = 'images/' + RIGHT([LogoPath], CHARINDEX('/', REVERSE([LogoPath])) - 1)
                WHERE  [LogoPath] LIKE 'images/Hero/%';
            ");
        }
    }
}
