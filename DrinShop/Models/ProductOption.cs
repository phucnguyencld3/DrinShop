using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class ProductOption
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ProductOptionId { get; set; }

        [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
        public int ProductId { get; set; }

        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        [Required(ErrorMessage = "Tên thuộc tính không được để trống")]
        [MaxLength(200, ErrorMessage = "Tên thuộc tính tối đa 200 ký tự")]
        public string Name { get; set; }

        public DateTime? CreatedAt { get; set; }

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string CreatedBy { get; set; }
        public decimal PriceModifer { get; set; }

        [ValidateNever]
        public ICollection<ProductOptionValue> Values { get; set; } = new List<ProductOptionValue>();
    }
}
