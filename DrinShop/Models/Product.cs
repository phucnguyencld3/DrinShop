using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Product
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductID { get; set; }

        public int? CategoryID { get; set; }
        public int? BrandID { get; set; }

        [MaxLength(100, ErrorMessage = "Mã sản phẩm tối đa 100 ký tự")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [MaxLength(300, ErrorMessage = "Tên sản phẩm tối đa 300 ký tự")]
        public string ProductName { get; set; }

        [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string Description { get; set; }

        public DateTime? CreateDate { get; set; }

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string CreatedBy { get; set; }

        public bool Status { get; set; } = true;

        [ForeignKey(nameof(CategoryID))]
        [ValidateNever]
        public Category? Category { get; set; }

        [ForeignKey(nameof(BrandID))]
        [ValidateNever]
        public Brand? Brand { get; set; }

        [NotMapped]
        public int TotalStock => Variants?.Sum(v => v.Stock) ?? 0;

        [NotMapped]
        public decimal MinPrice => Variants?.Where(v => v.Status).Any() == true
            ? Variants.Where(v => v.Status).Min(v => v.UnitPrice)
            : 0;

        [NotMapped]
        public decimal MaxPrice => Variants?.Where(v => v.Status).Any() == true
            ? Variants.Where(v => v.Status).Max(v => v.UnitPrice)
            : 0;

        [ValidateNever]
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();

        [ValidateNever]
        public ICollection<ProductOption> ProductOptions { get; set; } = new List<ProductOption>();

        [ValidateNever]
        public ICollection<Variant> Variants { get; set; } = new List<Variant>();
    }
}
