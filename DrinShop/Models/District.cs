using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DrinShop.Models
{
    public class District
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ← THÊM DÒNG NÀY
        public int Code { get; set; }

        public int ProvinceCode { get; set; }
        [ForeignKey(nameof(ProvinceCode))]
        [ValidateNever]
        public Province Province { get; set; }

        [Required]
        [MaxLength(100)]
        public string Codename { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string DivisionType { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [ValidateNever]
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();

        [ValidateNever]
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
