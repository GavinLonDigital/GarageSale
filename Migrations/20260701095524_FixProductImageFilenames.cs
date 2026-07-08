using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GarageSale.Migrations
{
    /// <inheritdoc />
    public partial class FixProductImageFilenames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Beauty
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Beauty/Beauty6.jpg' WHERE [ImagePath] = 'images/Beauty/Beauty6.png';");

            // Electronics — case fixes and extension fixes
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/electronics2.png' WHERE [ImagePath] = 'images/Electronics/Electronics2.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/electronic4.png' WHERE [ImagePath] = 'images/Electronics/Electronics4.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/electronic5.png' WHERE [ImagePath] = 'images/Electronics/Electronics5.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/technology6.png' WHERE [ImagePath] = 'images/Electronics/Electronics6.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronic7.jpg' WHERE [ImagePath] = 'images/Electronics/Electronics7.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronic8.jpg' WHERE [ImagePath] = 'images/Electronics/Electronics8.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics9.jpg' WHERE [ImagePath] = 'images/Electronics/Electronics9.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronic10.jpg' WHERE [ImagePath] = 'images/Electronics/Electronics10.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronic11.jpg' WHERE [ImagePath] = 'images/Electronics/Electronics11.png';");

            // Footware — case fix and extension fixes
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/shoes4.png' WHERE [ImagePath] = 'images/Footware/Shoes4.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes7.jpg' WHERE [ImagePath] = 'images/Footware/Shoes7.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes8.jpg' WHERE [ImagePath] = 'images/Footware/Shoes8.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes9.jpg' WHERE [ImagePath] = 'images/Footware/Shoes9.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes10.jpg' WHERE [ImagePath] = 'images/Footware/Shoes10.png';");

            // Furniture — extension fixes
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture8.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture8.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture9.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture9.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture10.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture10.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture11.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture11.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture12.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture12.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture13.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture13.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture14.jpg' WHERE [ImagePath] = 'images/Furniture/Furniture14.png';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Beauty
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Beauty/Beauty6.png' WHERE [ImagePath] = 'images/Beauty/Beauty6.jpg';");

            // Electronics
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics2.png' WHERE [ImagePath] = 'images/Electronics/electronics2.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics4.png' WHERE [ImagePath] = 'images/Electronics/electronic4.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics5.png' WHERE [ImagePath] = 'images/Electronics/electronic5.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics6.png' WHERE [ImagePath] = 'images/Electronics/technology6.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics7.png' WHERE [ImagePath] = 'images/Electronics/Electronic7.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics8.png' WHERE [ImagePath] = 'images/Electronics/Electronic8.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics9.png' WHERE [ImagePath] = 'images/Electronics/Electronics9.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics10.png' WHERE [ImagePath] = 'images/Electronics/Electronic10.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Electronics/Electronics11.png' WHERE [ImagePath] = 'images/Electronics/Electronic11.jpg';");

            // Footware
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes4.png' WHERE [ImagePath] = 'images/Footware/shoes4.png';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes7.png' WHERE [ImagePath] = 'images/Footware/Shoes7.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes8.png' WHERE [ImagePath] = 'images/Footware/Shoes8.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes9.png' WHERE [ImagePath] = 'images/Footware/Shoes9.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Footware/Shoes10.png' WHERE [ImagePath] = 'images/Footware/Shoes10.jpg';");

            // Furniture
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture8.png' WHERE [ImagePath] = 'images/Furniture/Furniture8.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture9.png' WHERE [ImagePath] = 'images/Furniture/Furniture9.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture10.png' WHERE [ImagePath] = 'images/Furniture/Furniture10.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture11.png' WHERE [ImagePath] = 'images/Furniture/Furniture11.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture12.png' WHERE [ImagePath] = 'images/Furniture/Furniture12.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture13.png' WHERE [ImagePath] = 'images/Furniture/Furniture13.jpg';");
            migrationBuilder.Sql("UPDATE [Products] SET [ImagePath] = 'images/Furniture/Furniture14.png' WHERE [ImagePath] = 'images/Furniture/Furniture14.jpg';");
        }
    }
}
