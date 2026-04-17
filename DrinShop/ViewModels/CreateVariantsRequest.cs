using DrinShop.ViewModels;

namespace DrinShop.ViewModels
{
    public class CreateVariantsRequest
    {
        public int productId { get; set; }
        public List<VariantCreateModel> variants { get; set; } = new();
    }
}

