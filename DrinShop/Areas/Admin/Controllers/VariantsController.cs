using DrinShop.Models;
using DrinShop.Services;
using DrinShop.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class VariantsController : BaseAdminController
    {
        private readonly DrinShopDbContext _context;
        private readonly IVariantService _variantService;

        public VariantsController(DrinShopDbContext context, IVariantService variantService)
        {
            _context = context;
            _variantService = variantService;
        }

        // POST: Variants/ToggleStatus
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int variantId, bool status)
        {
            var variant = await _context.Variants.FindAsync(variantId);
            if (variant == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể" });
            }

            variant.Status = status;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã {(status ? "bật" : "tắt")} biến thể" });
        }

        // Helper method để tạo SKU
        private string GenerateVariantSKU(string productCode)
        {
            var timestamp = DateTime.Now.ToString("MMdd");
            var random = new Random().Next(100, 999);
            return $"{productCode}-V{timestamp}{random}";
        }

        // GET: Variants/Index/5 - Hiển thị danh sách variant của sản phẩm
        public async Task<IActionResult> Index(int id, string searchTerm = "")
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var variants = await _variantService.SearchVariantsAsync(id, searchTerm);

            ViewBag.ProductId = id;
            ViewBag.ProductName = product.ProductName;
            ViewBag.SearchTerm = searchTerm;

            return View(variants);
        }

        // GET: Variants/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var variant = await _variantService.GetVariantByIdAsync(id);
            if (variant == null)
            {
                return NotFound();
            }

            return View(variant);
        }

        // GET: Variants/Create/5
        public async Task<IActionResult> Create(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .FirstOrDefaultAsync(p => p.ProductID == productId);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.Product = product;
            ViewBag.ProductOptions = product.ProductOptions;

            var variant = new Variant
            {
                ProductID = productId,
                CreateDate = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "Admin",
                Status = true,
                UnitPrice = 0 // Đặt giá mặc định
            };

            return View(variant);
        }

        // POST: Variants/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Variant variant, List<int> optionValueIds)
        {
            if (ModelState.IsValid)
            {
                // ✅ Tính giá tự động từ option values (không cần basePrice)
                var calculatedPrice = await _variantService.CalculateVariantPriceAsync(variant.ProductID, optionValueIds, 0);
                variant.UnitPrice = calculatedPrice;

                var success = await _variantService.CreateVariantAsync(variant, optionValueIds);
                if (success)
                {
                    TempData["Success"] = "Tạo biến thể thành công!";
                    return RedirectToAction(nameof(Index), new { id = variant.ProductID });
                }
                else
                {
                    ModelState.AddModelError("", "Biến thể với các thuộc tính này đã tồn tại!");
                }
            }

            // Reload data if validation fails
            var product = await _context.Products
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .FirstOrDefaultAsync(p => p.ProductID == variant.ProductID);

            ViewBag.Product = product;
            ViewBag.ProductOptions = product?.ProductOptions;

            return View(variant);
        }



        // GET: Variants/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var variant = await _variantService.GetVariantByIdAsync(id);
            if (variant == null)
            {
                return NotFound();
            }

            return View(variant);
        }

        // POST: Variants/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Variant variant)
        {
            if (id != variant.VariantID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var success = await _variantService.UpdateVariantAsync(variant);
                if (success)
                {
                    TempData["Success"] = "Cập nhật biến thể thành công!";
                    return RedirectToAction(nameof(Index), new { id = variant.ProductID });
                }
                else
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi cập nhật biến thể!");
                }
            }

            return View(variant);
        }

        // POST: Variants/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var variant = await _variantService.GetVariantByIdAsync(id);
                if (variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể" });
                }

                // Kiểm tra xem variant có đang được sử dụng không
                var isUsed = await CheckVariantInUse(id);
                if (isUsed.isUsed)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Không thể xóa biến thể này vì {isUsed.reason}",
                        isUsed.canForceDelete,
                        usageDetails = isUsed.details
                    });
                }

                var success = await _variantService.DeleteVariantAsync(id);
                if (success)
                {
                    return Json(new { success = true, message = "Xóa biến thể thành công" });
                }
                else
                {
                    return Json(new { success = false, message = "Có lỗi xảy ra khi xóa biến thể" });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi hệ thống: {ex.Message}" });
            }
        }
        // POST: Variants/ForceDelete/5 - Xóa cưỡng bức (chỉ Admin)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ForceDelete(int id, bool confirmForce = false)
        {
            if (!confirmForce)
            {
                return Json(new { success = false, message = "Cần xác nhận xóa cưỡng bức" });
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var variant = await _context.Variants
                    .Include(v => v.VariantOptionValues)
                    .Include(v => v.CartDetails)
                    .Include(v => v.BundleItems)
                    .FirstOrDefaultAsync(v => v.VariantID == id);

                if (variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy biến thể" });
                }

                // Xóa các dependencies (trừ InvoiceDetails)
                if (variant.CartDetails.Any())
                {
                    _context.CartDetails.RemoveRange(variant.CartDetails);
                }

                if (variant.BundleItems.Any())
                {
                    _context.BundleItems.RemoveRange(variant.BundleItems);
                }

                if (variant.VariantOptionValues.Any())
                {
                    _context.VariantOptionValues.RemoveRange(variant.VariantOptionValues);
                }

                // Xóa variant
                _context.Variants.Remove(variant);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã xóa biến thể và tất cả dữ liệu liên quan" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Lỗi khi xóa cưỡng bức: {ex.Message}" });
            }
        }





        // Helper method để kiểm tra variant có đang được sử dụng không
        private async Task<(bool isUsed, string reason, bool canForceDelete, object details)> CheckVariantInUse(int variantId)
        {
            var cartCount = await _context.CartDetails.CountAsync(cd => cd.VariantID == variantId);
            var invoiceCount = await _context.InvoiceDetails.CountAsync(id => id.VariantID == variantId);
            var bundleCount = await _context.BundleItems.CountAsync(bi => bi.VariantId == variantId);

            var details = new
            {
                cartItems = cartCount,
                invoiceItems = invoiceCount,
                bundleItems = bundleCount
            };

            if (invoiceCount > 0)
            {
                return (true, $"đã có {invoiceCount} đơn hàng sử dụng", false, details);
            }

            if (cartCount > 0)
            {
                return (true, $"đang có {cartCount} sản phẩm trong giỏ hàng", true, details);
            }

            if (bundleCount > 0)
            {
                return (true, $"đang được sử dụng trong {bundleCount} combo/bundle", true, details);
            }

            return (false, "", false, details);
        }





        // POST: Variants/UpdateStock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int variantId, int newStock)
        {
            var success = await _variantService.UpdateStockAsync(variantId, newStock);
            if (success)
            {
                return Json(new { success = true, message = "Cập nhật tồn kho thành công" });
            }
            else
            {
                return Json(new { success = false, message = "Có lỗi xảy ra khi cập nhật tồn kho" });
            }
        }

        // GET: Variants/GenerateVariants/5
        public async Task<IActionResult> GenerateVariants(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .Include(p => p.Variants) // Include variants để lấy giá tham chiếu
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            if (!product.ProductOptions.Any() || product.ProductOptions.All(po => !po.Values.Any()))
            {
                TempData["Error"] = "Vui lòng thêm thuộc tính và giá trị trước khi tạo biến thể";
                return RedirectToAction(nameof(Index), new { id });
            }

            var combinations = await _variantService.GenerateVariantCombinationsAsync(id);

            ViewBag.ProductId = id;
            ViewBag.ProductName = product.ProductName;
            // Sử dụng giá thấp nhất của các variant hiện có làm base price, hoặc 50000 nếu chưa có variant
            ViewBag.BasePrice = product.MinPrice > 0 ? product.MinPrice : 50000;

            return View(combinations);
        }

        // POST: Variants/CreateVariants
        [HttpPost]
        public async Task<IActionResult> CreateVariants([FromBody] CreateVariantsRequest request)
        {
            try
            {
                if (request?.variants?.Any(v => v.IsSelected) != true)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một biến thể" });
                }

                // Validate dữ liệu đầu vào
                var selectedVariants = request.variants.Where(v => v.IsSelected).ToList();
                var validationErrors = new List<string>();

                foreach (var variant in selectedVariants)
                {
                    if (string.IsNullOrWhiteSpace(variant.Name))
                        validationErrors.Add("Tên biến thể không được để trống");

                    if (variant.Price <= 0)
                        validationErrors.Add($"Giá của biến thể '{variant.Name}' phải lớn hơn 0");

                    if (variant.Stock < 0)
                        validationErrors.Add($"Tồn kho của biến thể '{variant.Name}' không được âm");
                }

                if (validationErrors.Any())
                {
                    return Json(new { success = false, message = string.Join("; ", validationErrors) });
                }

                int createdCount = 0;
                int skippedCount = 0;
                var createdVariants = new List<string>();

                foreach (var variantModel in selectedVariants)
                {
                    // Kiểm tra xem variant đã tồn tại chưa
                    if (await _variantService.VariantExistsAsync(request.productId, variantModel.OptionValueIds))
                    {
                        skippedCount++;
                        continue;
                    }

                    // Tạo SKU tự động nếu chưa có
                    if (string.IsNullOrWhiteSpace(variantModel.SKU))
                    {
                        var product = await _context.Products.FindAsync(request.productId);
                        variantModel.SKU = GenerateVariantSKU(product?.Code ?? "PROD");
                    }

                    // Đảm bảo SKU duy nhất
                    var originalSKU = variantModel.SKU;
                    int counter = 1;
                    while (await _context.Variants.AnyAsync(v => v.SKU == variantModel.SKU))
                    {
                        variantModel.SKU = $"{originalSKU}-{counter}";
                        counter++;
                    }

                    var variant = new Variant
                    {
                        ProductID = request.productId,
                        VariantName = variantModel.Name,
                        UnitPrice = variantModel.Price,
                        Stock = variantModel.Stock,
                        SKU = variantModel.SKU,
                        Description = variantModel.Description,
                        CreateDate = DateTime.Now,
                        CreatedBy = User.Identity?.Name ?? "Admin",
                        Status = true
                    };

                    var success = await _variantService.CreateVariantAsync(variant, variantModel.OptionValueIds);
                    if (success)
                    {
                        createdCount++;
                        createdVariants.Add(variant.VariantName);
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                var message = $"Đã tạo {createdCount} biến thể thành công";
                if (skippedCount > 0)
                {
                    message += $", bỏ qua {skippedCount} biến thể đã tồn tại";
                }

                return Json(new
                {
                    success = true,
                    message,
                    createdCount,
                    skippedCount,
                    createdVariants
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // API: Variants/GetVariantsByProduct/5
        [HttpGet]
        public async Task<IActionResult> GetVariantsByProduct(int productId)
        {
            var variants = await _variantService.GetVariantsByProductIdAsync(productId);
            var result = variants.Select(v => new
            {
                id = v.VariantID,
                name = v.VariantName,
                price = v.UnitPrice,
                stock = v.Stock,
                sku = v.SKU,
                status = v.Status,
                options = v.VariantOptionValues.Select(vov => new
                {
                    optionName = vov.ProductOptionValue.ProductOption.Name,
                    valueName = vov.ProductOptionValue.Value
                }).ToList()
            });

            return Json(result);
        }

        // API: Variants/CheckVariantAvailability
        [HttpPost]
        public async Task<IActionResult> CheckVariantAvailability(int productId, List<int> optionValueIds, decimal basePrice = 0)
        {
            var exists = await _variantService.VariantExistsAsync(productId, optionValueIds);
            var price = await _variantService.CalculateVariantPriceAsync(productId, optionValueIds, basePrice);

            return Json(new { exists, price });
        }

        // GET: Variants/Overview - Trang tổng quan biến thể
        public async Task<IActionResult> Overview(string searchTerm = "", int? productId = null, bool? status = null)
        {
            var query = _context.Variants
                .Include(v => v.Product)
                    .ThenInclude(p => p.Category)
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.ProductOptionValue)
                        .ThenInclude(pov => pov.ProductOption)
                .AsQueryable();

            // Filtering
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(v =>
                    v.VariantName.Contains(searchTerm) ||
                    v.Product.ProductName.Contains(searchTerm) ||
                    v.SKU.Contains(searchTerm));
            }

            if (productId.HasValue)
            {
                query = query.Where(v => v.ProductID == productId);
            }

            if (status.HasValue)
            {
                query = query.Where(v => v.Status == status);
            }

            var variants = await query
                .OrderByDescending(v => v.CreateDate)
                .Take(100) // Limit for performance
                .ToListAsync();

            // Statistics
            var stats = new
            {
                TotalVariants = await _context.Variants.CountAsync(),
                ActiveVariants = await _context.Variants.CountAsync(v => v.Status),
                InactiveVariants = await _context.Variants.CountAsync(v => !v.Status),
                TotalStock = await _context.Variants.SumAsync(v => v.Stock),
                ProductsWithVariants = await _context.Products
                    .CountAsync(p => p.Variants.Any())
            };

            ViewBag.Statistics = stats;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.ProductId = productId;
            ViewBag.Status = status;

            // Products for filter dropdown
            ViewBag.Products = await _context.Products
                .Where(p => p.Variants.Any())
                .Select(p => new { p.ProductID, p.ProductName })
                .ToListAsync();

            return View(variants);
        }

        // API: Get products for variant creation modal
        [HttpGet]
        public async Task<IActionResult> GetProductsForVariant()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .Select(p => new
                {
                    id = p.ProductID,
                    name = p.ProductName,
                    categoryName = p.Category != null ? p.Category.CategoryName : null,
                    variantCount = p.Variants.Count
                })
                .ToListAsync();

            return Json(products);
        }

        // GET: Variants/ManageOptions/5 - Quản lý thuộc tính sản phẩm
        public async Task<IActionResult> ManageOptions(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .FirstOrDefaultAsync(p => p.ProductID == id);

            if (product == null)
            {
                return NotFound();
            }

            ViewBag.ProductId = id;
            ViewBag.ProductName = product.ProductName;
            return View(product.ProductOptions);
        }

        // POST: Variants/AddOption - Thêm thuộc tính
        [HttpPost]
        public async Task<IActionResult> AddOption(int productId, string name, decimal priceModifier = 0)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Json(new { success = false, message = "Tên thuộc tính không được để trống" });
            }

            var option = new ProductOption
            {
                ProductId = productId,
                Name = name.Trim(),
                PriceModifer = priceModifier,
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            _context.ProductOptions.Add(option);
            await _context.SaveChangesAsync();

            return Json(new { success = true, optionId = option.ProductOptionId, message = "Thêm thuộc tính thành công" });
        }

        // POST: Variants/AddOptionValue - Thêm giá trị thuộc tính
        [HttpPost]
        public async Task<IActionResult> AddOptionValue(int optionId, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Json(new { success = false, message = "Giá trị không được để trống" });
            }

            var optionValue = new ProductOptionValue
            {
                ProductOptionId = optionId,
                Value = value.Trim(),
                CreatedAt = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "Admin"
            };

            _context.ProductOptionValues.Add(optionValue);
            await _context.SaveChangesAsync();

            return Json(new { success = true, valueId = optionValue.ProductOptionValueId, message = "Thêm giá trị thành công" });
        }

        // POST: Variants/DeleteOption - Xóa thuộc tính
        [HttpPost]
        public async Task<IActionResult> DeleteOption(int id)
        {
            var option = await _context.ProductOptions
                .Include(po => po.Values)
                .FirstOrDefaultAsync(po => po.ProductOptionId == id);

            if (option == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thuộc tính" });
            }

            // Kiểm tra xem có variant nào đang sử dụng thuộc tính này không
            var hasVariants = await _context.VariantOptionValues
                .AnyAsync(vov => option.Values.Select(v => v.ProductOptionValueId).Contains(vov.ProductOptionValueId));

            if (hasVariants)
            {
                return Json(new { success = false, message = "Không thể xóa thuộc tính này vì đã có biến thể sử dụng" });
            }

            _context.ProductOptions.Remove(option);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa thuộc tính thành công" });
        }

        // POST: Variants/DeleteOptionValue - Xóa giá trị thuộc tính
        [HttpPost]
        public async Task<IActionResult> DeleteOptionValue(int id)
        {
            var value = await _context.ProductOptionValues.FindAsync(id);

            if (value == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giá trị" });
            }

            // Kiểm tra xem có variant nào đang sử dụng giá trị này không
            var hasVariants = await _context.VariantOptionValues.AnyAsync(vov => vov.ProductOptionValueId == id);

            if (hasVariants)
            {
                return Json(new { success = false, message = "Không thể xóa giá trị này vì đã có biến thể sử dụng" });
            }

            _context.ProductOptionValues.Remove(value);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa giá trị thành công" });
        }

    }
}


