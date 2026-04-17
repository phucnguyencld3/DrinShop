using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class PromotionTarget
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PromotionTargetId { get; set; }
        [MaxLength(50)]
        public string TargetType { get; set; }
        [Required] public int PromotionId { get; set; }
        [ForeignKey(nameof(PromotionId))]
        [ValidateNever]
        public Promotion Promotion { get; set; }
        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        [ValidateNever]
        public Product Product { get; set; }

        public int? VariantID { get; set; }
        [ForeignKey(nameof(VariantID))]
        [ValidateNever]
        public Variant Variant { get; set; }

        public int? BundleId { get; set; }
        [ForeignKey(nameof(BundleId))]
        [ValidateNever]
        public Bundle Bundle { get; set; }
    }
}
