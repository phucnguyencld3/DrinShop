using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class ProductImage
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductImageId { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        [Required(ErrorMessage = "Ảnh sản phẩm không được để trống")]
        [MaxLength(500, ErrorMessage = "Đường dẫn ảnh tối đa 500 ký tự")]
        public string ImageUrl { get; set; }

        public bool IsDefault { get; set; } = false;
        public int SortOrder { get; set; }
        public DateTime? CreatedAt { get; set; }

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string CreatedBy { get; set; }
    }
}
