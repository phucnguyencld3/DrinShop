using DrinShop.Controllers;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DrinShop.Models
{
    public class CheckoutViewModel
    {
        // ✅ THÊM ValidateNever để bỏ qua validation
        [ValidateNever]
        public User Customer { get; set; }

        [ValidateNever]
        public Cart Cart { get; set; }

        [ValidateNever]
        public List<CheckoutController.PaymentMethodOption> PaymentMethods { get; set; }

        [ValidateNever]
        public List<CheckoutController.ShippingMethodOption> ShippingMethods { get; set; }

        [ValidateNever]
        public List<Province> Provinces { get; set; } = new List<Province>();

        [ValidateNever]
        public List<District> Districts { get; set; } = new List<District>();

        [ValidateNever]
        public List<Ward> Wards { get; set; } = new List<Ward>();

        // CÁC FIELD THỰC SỰ CẦN VALIDATE KHI SUBMIT
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string SelectedPaymentMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức giao hàng")]
        public string SelectedShippingMethod { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DeliveryAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string CustomerName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ chi tiết")]
        public string CustomerAddress { get; set; }

        public string Notes { get; set; }

        // Selected values
        public int? SelectedProvinceCode { get; set; }
        public int? SelectedDistrictCode { get; set; }
        public int? SelectedWardCode { get; set; }

        // CÁC PROPERTY TÍNH TOÁN (CHỈ ĐỌC)
        [ValidateNever]
        public decimal SubTotal => Cart?.TotalAmount ?? 0;
        [ValidateNever]
        public decimal ShippingFee => SelectedShippingMethod == "express" ? 30000 : 0;
        [ValidateNever]
        public decimal FinalTotal => SubTotal + ShippingFee;
    }
}
