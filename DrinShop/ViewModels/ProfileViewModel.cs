using System.ComponentModel.DataAnnotations;

namespace DrinShop.ViewModels
{
    public class ProfileViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email không được để trống")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public bool Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        public string? Address { get; set; }

        [StringLength(200, ErrorMessage = "Địa chỉ cụ thể tối đa 200 ký tự")]
        public string? StreetAddress { get; set; }

        public int? ProvinceCode { get; set; }
        public int? DistrictCode { get; set; }
        public int? WardCode { get; set; }

        // For dropdowns
        public string? ProvinceName { get; set; }
        public string? DistrictName { get; set; }
        public string? WardName { get; set; }
    }
}
