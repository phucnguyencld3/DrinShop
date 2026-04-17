using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class User
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }
        public string? GoogleId { get; set; }
        public bool IsFirstLogin { get; set; } = true;
        public int? ProvinceCode { get; set; }
        public int? DistrictCode { get; set; }
        public int? WardCode { get; set; }

        [MaxLength(300, ErrorMessage = "Họ tên tối đa 300 ký tự")]
        public string? FullName { get; set; } // ✅ THÊM NULLABLE

        public DateTime? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Giới tính không được để trống")]
        public bool Gender { get; set; }

        [MaxLength(20, ErrorMessage = "Số điện thoại tối đa 20 ký tự")]
        public string? Phone { get; set; } // ✅ THÊM NULLABLE

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        [MaxLength(200, ErrorMessage = "Email tối đa 200 ký tự")]
        public string? Email { get; set; } // ✅ THÊM NULLABLE

        [MaxLength(500, ErrorMessage = "Địa chỉ tối đa 500 ký tự")]
        public string? Address { get; set; } // ✅ THÊM NULLABLE

        [MaxLength(500, ErrorMessage = "Địa chỉ chi tiết tối đa 500 ký tự")]
        public string? StreetAddress { get; set; } // ✅ THÊM NULLABLE

        public bool Status { get; set; } = true;

        public DateTime? RegisterDate { get; set; }

        // Liên kết với ApplicationUser (Identity)
        public string? ApplicationUserId { get; set; }

        [ForeignKey(nameof(ApplicationUserId))]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(ProvinceCode))]
        [ValidateNever]
        public Province? Province { get; set; }

        [ForeignKey(nameof(DistrictCode))]
        [ValidateNever]
        public District? District { get; set; }

        [ForeignKey(nameof(WardCode))]
        [ValidateNever]
        public Ward? Ward { get; set; }

        [ValidateNever]
        public ICollection<Cart> Carts { get; set; } = new List<Cart>();

        [ValidateNever]
        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}

