using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GarageSale.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Categories> Categories { get; set; }
    public DbSet<CategoriesToUsers> CategoriesToUsers { get; set; }
    public DbSet<ConditionRatings> ConditionRatings { get; set; }
    public DbSet<Products> Products { get; set; }
    public DbSet<SellerDetails> SellerDetails { get; set; }
    public DbSet<ShoppingCartItems> ShoppingCartItems { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<CategoriesToUsers>(entity =>
        {
            entity.HasKey(ctu => new { ctu.UserId, ctu.CategoryId });

            entity.HasOne(ctu => ctu.User)
                .WithMany(u => u.CategoriesToUsers)
                .HasForeignKey(ctu => ctu.UserId);

            entity.HasOne(ctu => ctu.Category)
                .WithMany(c => c.CategoriesToUsers)
                .HasForeignKey(ctu => ctu.CategoryId);
        });

        builder.Entity<SellerDetails>(entity =>
        {
            entity.HasKey(sd => sd.Id);

            entity.Property(sd => sd.UserId).HasMaxLength(450);
            entity.Property(sd => sd.SellerBusinessName).HasMaxLength(1000);
            entity.Property(sd => sd.LogoPath).HasMaxLength(500);

            entity.HasIndex(sd => sd.UserId).IsUnique();

            entity.HasOne(sd => sd.User)
                .WithOne(u => u.SellerDetails)
                .HasForeignKey<SellerDetails>(sd => sd.UserId);
        });

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.Address1).HasMaxLength(200);
            entity.Property(u => u.Address2).HasMaxLength(200);
            entity.Property(u => u.Address3).HasMaxLength(200);
            entity.Property(u => u.ZipOrPostCode).HasMaxLength(200);
        });

        builder.Entity<Products>(entity =>
        {
            entity.Property(p => p.SalesStatus)
                .HasConversion(v => ((char)v).ToString(), v => (ProductSalesStatus)v[0])
                .HasColumnType("char(1)")
                .HasDefaultValueSql("'A'")
                .HasSentinel((ProductSalesStatus)0);
        });

        builder.Entity<ShoppingCartItems>(entity =>
        {
            entity.Property(sci => sci.UserId).HasMaxLength(450);
            entity.Property(sci => sci.SellerId).HasMaxLength(450);

            entity.Property(sci => sci.DateAdded)
                .HasDefaultValueSql("GETUTCDATE()");

            // UserId cascades so that deleting a user removes their cart items.
            // SellerId and ProductId use Restrict to prevent SQL Server's multiple
            // cascade paths error (Products.SellerId already cascades from AspNetUsers).
            entity.HasOne(sci => sci.User)
                .WithMany()
                .HasForeignKey(sci => sci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(sci => sci.Seller)
                .WithMany()
                .HasForeignKey(sci => sci.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(sci => sci.Product)
                .WithMany()
                .HasForeignKey(sci => sci.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(sci => sci.UserId);
            entity.HasIndex(sci => sci.SellerId);
            entity.HasIndex(sci => sci.ProductId);
        });

        builder.Entity<Order>(entity =>
        {
            entity.Property(o => o.UserId).HasMaxLength(450);

            entity.Property(o => o.DateAdded)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(o => o.Status)
                .HasConversion(v => ((char)v).ToString(), v => (OrderStatus)v[0])
                .HasColumnType("char(1)")
                .HasDefaultValueSql("'P'")
                .HasSentinel((OrderStatus)0);

            entity.Property(o => o.UpdatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(o => o.RowVersion)
                .IsRowVersion();

            // Order.UserId cascades so that deleting a user removes their orders.
            // OrderItem.SellerId uses Restrict to avoid SQL Server's multiple cascade
            // paths error (AspNetUsers → Orders → OrderItems and AspNetUsers → OrderItems).
            entity.HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(o => o.UserId);
            entity.HasIndex(o => o.Status);
        });

        builder.Entity<OrderItem>(entity =>
        {
            entity.Property(oi => oi.SellerId).HasMaxLength(450);

            entity.HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(oi => oi.Product)
                .WithMany()
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(oi => oi.Seller)
                .WithMany()
                .HasForeignKey(oi => oi.SellerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(oi => oi.OrderId);
            entity.HasIndex(oi => oi.ProductId);
            entity.HasIndex(oi => oi.SellerId);
        });

        builder.Entity<Categories>().HasData(
            new Categories { Id = 1, CategoryName = "Beauty",      CategoryDescription = "Beauty products"    },
            new Categories { Id = 2, CategoryName = "Electronics", CategoryDescription = "Electronic products" },
            new Categories { Id = 3, CategoryName = "Furniture",   CategoryDescription = "Furniture products"  },
            new Categories { Id = 4, CategoryName = "Footwear",    CategoryDescription = "Footwear products"   },
            new Categories { Id = 5, CategoryName = "Bedding",     CategoryDescription = "Bedding products"    },
            new Categories { Id = 6, CategoryName = "Bicycles",    CategoryDescription = "Bicycle products"    },
            new Categories { Id = 7, CategoryName = "Camping",     CategoryDescription = "Camping products"    },
            new Categories { Id = 8, CategoryName = "Backpacks",   CategoryDescription = "Backpack products"   }
        );

        builder.Entity<ConditionRatings>().HasData(
            new ConditionRatings { Id = 1, ConditionRatingNumber = 1,   ConditionRatingNumberDescription = "Excellent" },
            new ConditionRatings { Id = 2, ConditionRatingNumber = 2,   ConditionRatingNumberDescription = "Good"      },
            new ConditionRatings { Id = 3, ConditionRatingNumber = 3,   ConditionRatingNumberDescription = "OK"        },
            new ConditionRatings { Id = 4, ConditionRatingNumber = 4,   ConditionRatingNumberDescription = "Bad"       },
            new ConditionRatings { Id = 5, ConditionRatingNumber = 5,   ConditionRatingNumberDescription = "Terrible"  },
            new ConditionRatings { Id = 6, ConditionRatingNumber = 999, ConditionRatingNumberDescription = "NA"        }
        );
    }
}
