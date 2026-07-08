using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Create the SellerDetails table first so data can be migrated into it.
            migrationBuilder.CreateTable(
                name: "SellerDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    SellerBusinessName = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    LogoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SellerDetails_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SellerDetails_UserId",
                table: "SellerDetails",
                column: "UserId",
                unique: true);

            // 2. Populate SellerDetails for all existing users in the SELLER role.
            //    Role lookup uses NormalizedName so no role ID is hardcoded.
            //    This must happen before LogoPath is dropped from AspNetUsers.
            migrationBuilder.Sql(@"
                INSERT INTO [SellerDetails] ([UserId], [SellerBusinessName], [LogoPath])
                SELECT
                    u.[Id],
                    ISNULL(u.[FirstName], '') + '''s Garage Sale',
                    u.[LogoPath]
                FROM [AspNetUsers]      u
                INNER JOIN [AspNetUserRoles] ur ON u.[Id]       = ur.[UserId]
                INNER JOIN [AspNetRoles]     r  ON ur.[RoleId]  = r.[Id]
                WHERE r.[NormalizedName] = 'SELLER';
            ");

            // 3. Now it is safe to drop the column — data has already been copied.
            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "AspNetUsers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // 1. Restore the LogoPath column (nullable — values are back-filled below).
            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "AspNetUsers",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            // 2. Restore LogoPath values from SellerDetails before dropping that table.
            migrationBuilder.Sql(@"
                UPDATE u
                SET    u.[LogoPath] = sd.[LogoPath]
                FROM   [AspNetUsers]  u
                INNER JOIN [SellerDetails] sd ON u.[Id] = sd.[UserId];
            ");

            // 3. Remove the SellerDetails table.
            migrationBuilder.DropTable(
                name: "SellerDetails");
        }
    }
}
