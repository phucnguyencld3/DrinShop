using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DrinShop.Models
{
    public class Province
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] // ← THÊM DÒNG NÀY
        public int Code { get; set; } // PK from open-api
        public string Codename { get; set; }
        public string DivisionType { get; set; }
        public string Name { get; set; }
        public string PhoneCode { get; set; }

        [ValidateNever]
        public ICollection<Ward> Wards { get; set; } = new List<Ward>();
        public ICollection<User> Customers { get; set; } = new List<User>();
    }
}
