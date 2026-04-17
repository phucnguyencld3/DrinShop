using System.ComponentModel.DataAnnotations;

namespace DrinShop.ViewModels
{
    public class VariantCreateModel
    {
        public bool IsSelected { get; set; }

        [Required(ErrorMessage = "Tên biến thể không được để trống")]
        [MaxLength(300, ErrorMessage = "Tên biến thể tối đa 300 ký tự")]
        public string Name { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tồn kho không được âm")]
        public int Stock { get; set; }

        [MaxLength(100, ErrorMessage = "SKU tối đa 100 ký tự")]
        public string SKU { get; set; } = string.Empty;

        [MaxLength(1000, ErrorMessage = "Mô tả tối đa 1000 ký tự")]
        public string Description { get; set; } = string.Empty;

        public List<int> OptionValueIds { get; set; } = new();
    }

}
