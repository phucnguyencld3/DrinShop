using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace DrinShop.Models
{
    public class Ward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)] 
        public int Code { get; set; }

        // FK tới Province
        public int ProvinceCode { get; set; }
        [ForeignKey(nameof(ProvinceCode))]
        [ValidateNever]
        public Province Province { get; set; }

        // FK mới tới District
        public int DistrictCode { get; set; }
        [ForeignKey(nameof(DistrictCode))]
        [ValidateNever]
        public District District { get; set; }

        public string Codename { get; set; }
        public string DivisionType { get; set; }
        public string Name { get; set; }

        [ValidateNever]
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
