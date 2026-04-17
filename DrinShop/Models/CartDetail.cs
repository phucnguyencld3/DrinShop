using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class CartDetail
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartDetailID { get; set; }

        public int? BundleID { get; set; }

        [ForeignKey(nameof(BundleID))]
        [ValidateNever]
        public Bundle Bundle { get; set; }

        public int? VariantID { get; set; }

        [ForeignKey(nameof(VariantID))]
        [ValidateNever]
        public Variant Variant { get; set; }

        [Required(ErrorMessage = "Mã giỏ hàng không được để trống")]
        public int CartID { get; set; }

        [ForeignKey(nameof(CartID))]
        [ValidateNever]
        public Cart Cart { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Đơn giá không hợp lệ")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }

        [MaxLength(100, ErrorMessage = "Trạng thái tối đa 100 ký tự")]
        public string Status { get; set; }
    }
}
