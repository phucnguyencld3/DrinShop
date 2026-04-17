using System.ComponentModel.DataAnnotations;

namespace DrinShop.ViewModels
{
    public class CompleteProfileViewModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        [MaxLength(300, ErrorMessage = "Họ tên tối đa 300 ký tự")]
        [Display(Name = "Họ tên")]
        public string FullName { get; set; } = "";

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        [Display(Name = "Giới tính")]
        public bool Gender { get; set; }

        [Display(Name = "Ngày sinh")]
        public DateTime? DateOfBirth { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public int? ProvinceCode { get; set; }

        [Display(Name = "Quận/Huyện")]
        public int? DistrictCode { get; set; }

        [Display(Name = "Phường/Xã")]
        public int? WardCode { get; set; }

        [MaxLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        [Display(Name = "Địa chỉ")]
        public string? Address { get; set; }

        [MaxLength(500, ErrorMessage = "Địa chỉ chi tiết tối đa 500 ký tự")]
        [Display(Name = "Địa chỉ chi tiết")]
        public string? StreetAddress { get; set; }

        public string? Email { get; set; } // Chỉ hiển thị, không cho edit
    }
}
