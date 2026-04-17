using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace DrinShop.Models
{
    public class DrinShopDbContext : IdentityDbContext<ApplicationUser>
    {
        public DrinShopDbContext(DbContextOptions<DrinShopDbContext> options) : base(options) { }

        public DbSet<Brand> Brands { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductImage> ProductImages { get; set; }
        public DbSet<ProductOption> ProductOptions { get; set; }
        public DbSet<ProductOptionValue> ProductOptionValues { get; set; }
        public DbSet<Variant> Variants { get; set; }
        public DbSet<VariantOptionValue> VariantOptionValues { get; set; }
        public DbSet<Bundle> Bundles { get; set; }
        public DbSet<BundleItem> BundleItems { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<PromotionTarget> PromotionTargets { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; } // ← THÊM DÒNG NÀY
        public DbSet<Ward> Wards { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceDetail> InvoiceDetails { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Đổi tên bảng Identity
            modelBuilder.Entity<ApplicationUser>().ToTable("AspNetUsers");
            modelBuilder.Entity<IdentityRole>().ToTable("AspNetRoles");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens");

            // Bảng Users
            modelBuilder.Entity<User>().ToTable("Users");

            // Quan hệ giữa ApplicationUser và User (1-1)
            modelBuilder.Entity<ApplicationUser>()
                .HasOne(u => u.Customer)
                .WithOne(c => c.ApplicationUser)
                .HasForeignKey<User>(c => c.ApplicationUserId)
                .OnDelete(DeleteBehavior.Restrict);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var fk in entityType.GetForeignKeys())
                {
                    if (fk.DeleteBehavior != DeleteBehavior.Restrict)
                    {
                        fk.DeleteBehavior = DeleteBehavior.Restrict;
                    }
                }
            }

            modelBuilder.Entity<Invoice>()
                .HasMany(i => i.InvoiceDetails)
                .WithOne(d => d.Invoice)
                .HasForeignKey(d => d.InvoiceID)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void PreventInvoiceDeletion()
        {
            var deletingInvoices = ChangeTracker.Entries()
                .Where(e => e.Entity is Invoice && e.State == EntityState.Deleted)
                .ToList();
            foreach (var entry in deletingInvoices)
            {
                var invoice = (Invoice)entry.Entity;
                invoice.IsDeleted = true;
                entry.State = EntityState.Modified;
            }
        }
    }
}
