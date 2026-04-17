using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Promotion
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PromotionId { get; set; }

        [Required(ErrorMessage = "Tên chương trình không được để trống")]
        [MaxLength(300, ErrorMessage = "Tên chương trình tối đa 300 ký tự")]
        public string Name { get; set; }

        [MaxLength(100, ErrorMessage = "Loại giảm giá tối đa 100 ký tự")]
        public string Type { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Value { get; set; }

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public bool IsActive { get; set; } = true;

        [ValidateNever]
        public ICollection<PromotionTarget> PromotionTargets { get; set; } = new List<PromotionTarget>();
    }
}
