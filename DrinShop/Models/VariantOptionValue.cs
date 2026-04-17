using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class VariantOptionValue
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int VariantOptionValueId { get; set; }

        [Required(ErrorMessage = "Mã biến thể không được để trống")]
        public int VariantId { get; set; }

        [Required(ErrorMessage = "Mã giá trị thuộc tính không được để trống")]
        public int ProductOptionValueId { get; set; }

        [ForeignKey(nameof(VariantId))]
        [ValidateNever]
        public Variant Variant { get; set; }

        [ForeignKey(nameof(ProductOptionValueId))]
        [ValidateNever]
        public ProductOptionValue ProductOptionValue { get; set; }
    }
}
