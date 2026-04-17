using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Cart
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int CartID { get; set; }

        public int? CustomerId { get; set; }

        [ForeignKey(nameof(CustomerId))]
        [ValidateNever]
        public User Customer { get; set; }


        public DateTime? CreateDate { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Tổng tiền không hợp lệ")]
        public decimal TotalAmount { get; set; }

        [MaxLength(100, ErrorMessage = "Trạng thái tối đa 100 ký tự")]
        public string Status { get; set; }

        [ValidateNever]
        public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
    }
}
