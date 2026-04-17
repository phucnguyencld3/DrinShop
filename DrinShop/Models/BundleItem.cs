using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class BundleItem
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BundleItemId { get; set; }

        [Required(ErrorMessage = "Mã combo không được để trống")]
        public int BundleId { get; set; }

        [ForeignKey(nameof(BundleId))]
        [ValidateNever]
        public Bundle Bundle { get; set; }

        [Required(ErrorMessage = "Mã biến thể không được để trống")]
        public int VariantId { get; set; }

        [ForeignKey(nameof(VariantId))]
        [ValidateNever]
        public Variant Variant { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

    }
}
