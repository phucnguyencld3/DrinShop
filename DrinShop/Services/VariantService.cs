using DrinShop.Models;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Services
{
    public interface IVariantService
    {
        Task<List<Variant>> GetVariantsByProductIdAsync(int productId);
        Task<Variant> GetVariantByIdAsync(int variantId);
        Task<List<Variant>> SearchVariantsAsync(int productId, string searchTerm);
        Task<bool> CreateVariantAsync(Variant variant, List<int> optionValueIds);
        Task<bool> UpdateVariantAsync(Variant variant);
        Task<bool> DeleteVariantAsync(int variantId);
        Task<bool> UpdateStockAsync(int variantId, int newStock);
        Task<List<VariantCombination>> GenerateVariantCombinationsAsync(int productId);
        Task<bool> VariantExistsAsync(int productId, List<int> optionValueIds);
        Task<decimal> CalculateVariantPriceAsync(int productId, List<int> optionValueIds, decimal basePrice = 0);
    }

    public class VariantService : IVariantService
    {
        private readonly DrinShopDbContext _context;

        public VariantService(DrinShopDbContext context)
        {
            _context = context;
        }

        public async Task<List<Variant>> GetVariantsByProductIdAsync(int productId)
        {
            return await _context.Variants
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.ProductOptionValue)
                        .ThenInclude(pov => pov.ProductOption)
                .Where(v => v.ProductID == productId)
                .OrderBy(v => v.VariantName)
                .ToListAsync();
        }

        public async Task<Variant> GetVariantByIdAsync(int variantId)
        {
            return await _context.Variants
                .Include(v => v.Product)
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.ProductOptionValue)
                        .ThenInclude(pov => pov.ProductOption)
                .FirstOrDefaultAsync(v => v.VariantID == variantId);
        }

        public async Task<List<Variant>> SearchVariantsAsync(int productId, string searchTerm)
        {
            var query = _context.Variants
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.ProductOptionValue)
                        .ThenInclude(pov => pov.ProductOption)  // THÊM DÒNG NÀY
                .Where(v => v.ProductID == productId);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v =>
                    v.VariantName.Contains(searchTerm) ||
                    v.SKU.Contains(searchTerm) ||
                    v.VariantOptionValues.Any(vov => vov.ProductOptionValue.Value.Contains(searchTerm)));
            }

            return await query.OrderBy(v => v.VariantName).ToListAsync();
        }



        public async Task<bool> CreateVariantAsync(Variant variant, List<int> optionValueIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Kiểm tra variant đã tồn tại chưa
                if (await VariantExistsAsync(variant.ProductID, optionValueIds))
                {
                    return false;
                }

                // Tạo variant
                _context.Variants.Add(variant);
                await _context.SaveChangesAsync();

                // Tạo các liên kết với option values
                foreach (var optionValueId in optionValueIds)
                {
                    var variantOptionValue = new VariantOptionValue
                    {
                        VariantId = variant.VariantID,
                        ProductOptionValueId = optionValueId
                    };
                    _context.VariantOptionValues.Add(variantOptionValue);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }

        public async Task<bool> UpdateVariantAsync(Variant variant)
        {
            try
            {
                var existingVariant = await _context.Variants.FindAsync(variant.VariantID);
                if (existingVariant == null) return false;

                existingVariant.VariantName = variant.VariantName;
                existingVariant.UnitPrice = variant.UnitPrice;
                existingVariant.Stock = variant.Stock;
                existingVariant.SKU = variant.SKU;
                existingVariant.Description = variant.Description;
                existingVariant.Status = variant.Status;

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DeleteVariantAsync(int variantId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var variant = await _context.Variants
                    .Include(v => v.VariantOptionValues)
                    .Include(v => v.CartDetails)
                    .Include(v => v.InvoiceDetails)
                    .Include(v => v.BundleItems)
                    .FirstOrDefaultAsync(v => v.VariantID == variantId);

                if (variant == null) return false;

                // Kiểm tra xem variant có đang được sử dụng trong các bảng khác không
                var hasCartDetails = variant.CartDetails.Any();
                var hasInvoiceDetails = variant.InvoiceDetails.Any();
                var hasBundleItems = variant.BundleItems.Any();

                if (hasCartDetails || hasInvoiceDetails || hasBundleItems)
                {
                    // Không thể xóa vì đang được sử dụng
                    return false;
                }

                // Xóa VariantOptionValues trước (nếu có cascade delete được cấu hình thì không cần)
                if (variant.VariantOptionValues.Any())
                {
                    _context.VariantOptionValues.RemoveRange(variant.VariantOptionValues);
                }

                // Xóa Variant
                _context.Variants.Remove(variant);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception nếu cần
                return false;
            }
        }





        public async Task<bool> UpdateStockAsync(int variantId, int newStock)
        {
            try
            {
                var variant = await _context.Variants.FindAsync(variantId);
                if (variant == null) return false;

                variant.Stock = newStock;
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<VariantCombination>> GenerateVariantCombinationsAsync(int productId)
        {
            var options = await _context.ProductOptions
                .Include(po => po.Values)
                .Where(po => po.ProductId == productId)
                .ToListAsync();

            return GenerateVariantCombinations(options);
        }

        public async Task<bool> VariantExistsAsync(int productId, List<int> optionValueIds)
        {
            var existingVariant = await _context.Variants
                .Include(v => v.VariantOptionValues)
                .Where(v => v.ProductID == productId)
                .FirstOrDefaultAsync(v =>
                    v.VariantOptionValues.Count == optionValueIds.Count &&
                    v.VariantOptionValues.All(vov => optionValueIds.Contains(vov.ProductOptionValueId)));

            return existingVariant != null;
        }

        public async Task<decimal> CalculateVariantPriceAsync(int productId, List<int> optionValueIds, decimal basePrice = 0)
        {
            var optionValues = await _context.ProductOptionValues
                .Where(pov => optionValueIds.Contains(pov.ProductOptionValueId))
                .ToListAsync();

            // Tính tổng giá từ các option values
            decimal totalPrice = optionValues.Sum(ov => ov.Price);

            return totalPrice;
        }



        private List<VariantCombination> GenerateVariantCombinations(List<ProductOption> options)
        {
            var result = new List<VariantCombination>();
            var valuesList = options.Select(o => o.Values.ToList()).ToList();

            if (!valuesList.Any() || valuesList.Any(vl => !vl.Any()))
                return result;

            GenerateCombinationsRecursive(valuesList, 0, new List<ProductOptionValue>(), result);
            return result;
        }

        private void GenerateCombinationsRecursive(List<List<ProductOptionValue>> valuesList,
            int depth, List<ProductOptionValue> current, List<VariantCombination> result)
        {
            if (depth == valuesList.Count)
            {
                var combination = new VariantCombination
                {
                    Values = new List<ProductOptionValue>(current),
                    Name = string.Join(" - ", current.Select(v => v.Value)),
                    OptionValueIds = current.Select(v => v.ProductOptionValueId).ToList()
                };
                result.Add(combination);
                return;
            }

            foreach (var value in valuesList[depth])
            {
                current.Add(value);
                GenerateCombinationsRecursive(valuesList, depth + 1, current, result);
                current.RemoveAt(current.Count - 1);
            }
        }
    }

    public class VariantCombination
    {
        public List<ProductOptionValue> Values { get; set; } = new();
        public string Name { get; set; } = string.Empty;
        public List<int> OptionValueIds { get; set; } = new();
    }
}

