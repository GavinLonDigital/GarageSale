using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductImagePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Seeded values are stored as e.g. "/Beauty/Beauty1.png".
            // Prepending "images" gives "images/Beauty/Beauty1.png", which
            // resolves correctly against the wwwroot/images/ folder at runtime.
            // The WHERE guard makes the update idempotent.
            migrationBuilder.Sql(@"
                UPDATE [Products]
                SET    [ImagePath] = 'images' + [ImagePath]
                WHERE  [ImagePath] IS NOT NULL
                  AND  [ImagePath] LIKE '/%'
                  AND  [ImagePath] NOT LIKE 'images/%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Strip the leading 'images' prefix to restore the original values.
            migrationBuilder.Sql(@"
                UPDATE [Products]
                SET    [ImagePath] = RIGHT([ImagePath], LEN([ImagePath]) - LEN('images'))
                WHERE  [ImagePath] LIKE 'images/%';
            ");
        }
    }
}
