using DrinShop.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class ProductOptionValue
{
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int ProductOptionValueId { get; set; }

    [Required(ErrorMessage = "Mã thuộc tính không được để trống")]
    public int ProductOptionId { get; set; }

    [ForeignKey(nameof(ProductOptionId))]
    [ValidateNever]
    public ProductOption ProductOption { get; set; }

    [Required(ErrorMessage = "Giá trị thuộc tính không được để trống")]
    [MaxLength(200, ErrorMessage = "Giá trị tối đa 200 ký tự")]
    public string Value { get; set; }

    // ✅ THÊM: Giá riêng cho từng option value
    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue, ErrorMessage = "Giá không hợp lệ")]
    public decimal Price { get; set; } = 0;

    public DateTime? CreatedAt { get; set; }

    [MaxLength(100, ErrorMessage = "Người tạo tối đa 100 ký tự")]
    public string CreatedBy { get; set; }

    [ValidateNever]
    public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();
}
