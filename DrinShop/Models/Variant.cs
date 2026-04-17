using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Variant
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VariantID { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public int ProductID { get; set; }

        [ForeignKey(nameof(ProductID))]
        [ValidateNever]
        public Product Product { get; set; }

        [Required(ErrorMessage = "Tên biến thể không được để trống")]
        [MaxLength(300, ErrorMessage = "Tên biến thể tối đa 300 ký tự")]
        public string VariantName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ")]
        public decimal UnitPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không hợp lệ")]
        public int Stock { get; set; }

        [MaxLength(100, ErrorMessage = "SKU tối đa 100 ký tự")]
        public string SKU { get; set; }

        [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string Description { get; set; }

        public DateTime? CreateDate { get; set; }

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string CreatedBy { get; set; }

        public bool Status { get; set; } = true;

        [ValidateNever]
        public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();

        [ValidateNever]
        public ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();

        [ValidateNever]
        public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();

        [ValidateNever]
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
