using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Brand
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BrandID { get; set; }

        [Required(ErrorMessage = "Tên thương hiệu không được để trống")]
        [MaxLength(200, ErrorMessage = "Tên thương hiệu tối đa 200 ký tự")]
        public string BrandName { get; set; }

        [MaxLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string Description { get; set; }

        public DateTime? CreateDate { get; set; }

        [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
        public string CreatedBy { get; set; }

        public bool Status { get; set; } = true;

        [ValidateNever]
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
