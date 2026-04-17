using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace DrinShop.Models
{
    public class Bundle
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BundleId { get; set; }

        [Required(ErrorMessage = "Tên combo không được để trống")]
        [MaxLength(300, ErrorMessage = "Tên combo tối đa 300 ký tự")]
        public string Name { get; set; }

        [MaxLength(100, ErrorMessage = "Mã nội bộ tối đa 100 ký tự")]
        public string Code { get; set; }

        [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string Description { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Giảm giá phải >= 0")]
        public decimal DiscountAmount { get; set; } = 0;

        [MaxLength(500, ErrorMessage = "URL ảnh tối đa 500 ký tự")]
        public string? ImageUrl { get; set; }

        public bool Status { get; set; } = true;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string? CreatedBy { get; set; }

        [ValidateNever]
        public ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();

        [ValidateNever]
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

        [ValidateNever]
        public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();

        // ✅ Tính tổng giá gốc từ các variants
        [NotMapped]
        public decimal OriginalPrice
        {
            get
            {
                if (BundleItems == null || !BundleItems.Any())
                    return 0;

                return BundleItems.Sum(bi => bi.Variant.UnitPrice * bi.Quantity);
            }
        }

        // ✅ Giá bán cuối cùng (sau giảm giá)
        [NotMapped]
        public decimal FinalPrice
        {
            get
            {
                var original = OriginalPrice;
                return original > DiscountAmount ? original - DiscountAmount : 0;
            }
        }

        // ✅ Tính phần trăm giảm giá
        [NotMapped]
        public decimal DiscountPercentage
        {
            get
            {
                var original = OriginalPrice;
                if (original == 0) return 0;
                return Math.Round((DiscountAmount / original) * 100, 1);
            }
        }

        // ✅ Giá hiển thị (backward compatibility)
        [NotMapped]
        public decimal? Price
        {
            get => FinalPrice;
            set { /* Không làm gì - chỉ để tương thích */ }
        }

        // ✅ Property tính số lượng combo tối đa có thể bán dựa trên stock variants
        [NotMapped]
        public int AvailableStock
        {
            get
            {
                if (BundleItems == null || !BundleItems.Any())
                    return 0;

                // Tính số combo tối đa dựa trên variant có ít tồn kho nhất
                return BundleItems.Min(bi => bi.Variant.Stock / bi.Quantity);
            }
        }

        // ✅ Kiểm tra combo còn hàng không
        [NotMapped]
        public bool IsInStock => AvailableStock > 0;

        // ✅ Kiểm tra combo sắp hết hàng
        [NotMapped]
        public bool IsLowStock => AvailableStock > 0 && AvailableStock <= 5;

        // ✅ Lấy thông tin variant giới hạn stock
        [NotMapped]
        public BundleItem LimitingItem
        {
            get
            {
                if (BundleItems == null || !BundleItems.Any())
                    return null;

                return BundleItems.OrderBy(bi => bi.Variant.Stock / bi.Quantity).First();
            }
        }
    }
}
