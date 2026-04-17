using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public enum PayMethod
    {
        [Display(Name = "Tiền mặt (COD)")]
        Cash = 0,

        [Display(Name = "Thẻ tín dụng")]
        CreditCard = 1,

        [Display(Name = "Thẻ ghi nợ")]
        DebitCard = 2,

        [Display(Name = "Ví điện tử")]
        MobilePayment = 3
    }

    public enum OrderStatus
    {
        [Display(Name = "Chờ xác nhận")]
        Pending = 0,

        [Display(Name = "Đã xác nhận")]
        Confirmed = 1,

        [Display(Name = "Đang giao hàng")]
        Shipped = 2,

        [Display(Name = "Hoàn tất")]
        Completed = 3,

        [Display(Name = "Yêu cầu hủy")]
        CancelRequested = 4,

        [Display(Name = "Đã hủy")]
        Cancelled = 5
    }
    public class Invoice
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int InvoiceID { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [ValidateNever]
        public User Customer { get; set; }


        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không hợp lệ")]
        public decimal TotalAmount { get; set; }

        [Required(ErrorMessage = "Phương thức thanh toán không được để trống")]
        [EnumDataType(typeof(PayMethod), ErrorMessage = "Phương thức thanh toán không hợp lệ")]
        public PayMethod PayMethod { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [MaxLength(500, ErrorMessage = "Địa chỉ giao hàng tối đa 500 ký tự")]
        public string ShipAddress { get; set; }

        [MaxLength(500, ErrorMessage = "Lý do hủy tối đa 500 ký tự")]
        public string CancelReason { get; set; } = string.Empty; // ✅ THÊM default value

        public DateTime? CreatedAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        [Required(ErrorMessage = "Trạng thái đơn hàng không được để trống")]
        [EnumDataType(typeof(OrderStatus), ErrorMessage = "Trạng thái không hợp lệ")]
        public OrderStatus OrderStatus { get; set; }

        [MaxLength(1000, ErrorMessage = "Ghi chú tối đa 1000 ký tự")]
        public string Note { get; set; } = string.Empty; 

        public bool IsDeleted { get; set; } = false;

        [ValidateNever]
        public ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }
}
