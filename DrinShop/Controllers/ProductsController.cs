using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    public class ProductsController : BaseController
    {
        private readonly DrinShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProductsController(DrinShopDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
        }

        // Các methods khác giữ nguyên...
        // GET: /Products - Danh sách tất cả sản phẩm
        public IActionResult Index(int? categoryId, int? brandId, string searchTerm, int page = 1, int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true); // Chỉ hiện sản phẩm active

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryID == categoryId.Value);
            }

            // Lọc theo thương hiệu
            if (brandId.HasValue && brandId.Value > 0)
            {
                query = query.Where(p => p.BrandID == brandId.Value);
            }

            // Tìm kiếm theo tên
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(p => p.ProductName.Contains(searchTerm) ||
                                        p.Description.Contains(searchTerm));
            }

            // Tổng số sản phẩm
            var totalProducts = query.Count();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            // Phân trang
            var products = query
                .OrderByDescending(p => p.CreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền dữ liệu cho view
            ViewBag.Categories = _context.Categories.Where(c => c.Status == true).ToList();
            ViewBag.Brands = _context.Brands.Where(b => b.Status == true).ToList();
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentBrand = brandId;
            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            return View(products);
        }

        // Các methods khác giữ nguyên như cũ...
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages.OrderBy(pi => pi.SortOrder))
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.ProductOptionValue)
                .FirstOrDefault(p => p.ProductID == id && p.Status == true);

            if (product == null)
            {
                TempData["ErrorMessage"] = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!";
                return RedirectToAction(nameof(Index));
            }

            // Sản phẩm liên quan (cùng danh mục)
            var relatedProducts = _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.CategoryID == product.CategoryID &&
                           p.ProductID != id &&
                           p.Status == true)
                .Take(4)
                .ToList();

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        // Các methods khác...
        public IActionResult Category(int id, int page = 1, int pageSize = 12)
        {
            var category = _context.Categories.Find(id);
            if (category == null || !category.Status)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.CategoryName = category.CategoryName;
            ViewBag.CategoryDescription = category.Description;

            return Index(categoryId: id, brandId: null, searchTerm: null, page: page, pageSize: pageSize);
        }

        public IActionResult Brand(int id, int page = 1, int pageSize = 12)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null || !brand.Status)
            {
                TempData["ErrorMessage"] = "Thương hiệu không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BrandName = brand.BrandName;
            ViewBag.BrandDescription = brand.Description;

            return Index(categoryId: null, brandId: id, searchTerm: null, page: page, pageSize: pageSize);
        }

        public IActionResult Search(string term, int page = 1)
        {
            ViewBag.SearchTitle = $"Kết quả tìm kiếm cho: {term}";
            return Index(categoryId: null, brandId: null, searchTerm: term, page: page);
        }

        public class GetVariantByOptionsRequest
        {
            public int ProductId { get; set; }
            public Dictionary<int, int> SelectedOptions { get; set; }
        }

        [HttpPost]
        public JsonResult GetVariantByOptions([FromBody] GetVariantByOptionsRequest request)
        {
            try
            {
                // ✅ Enhanced debug logging
                System.Console.WriteLine($"=== GetVariantByOptions Called ===");
                System.Console.WriteLine($"ProductId: {request.ProductId}");
                System.Console.WriteLine($"Selected Options: {string.Join(", ", request.SelectedOptions.Select(o => $"{o.Key}:{o.Value}"))}");
                System.Console.WriteLine($"Request Time: {DateTime.Now}");

                var variants = _context.Variants
                    .Include(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.ProductOptionValue)
                            .ThenInclude(pov => pov.ProductOption)
                    .Where(v => v.ProductID == request.ProductId && v.Status == true)
                    .ToList();

                System.Console.WriteLine($"Found {variants.Count} active variants for product {request.ProductId}");

                // ✅ Kiểm tra nếu không có variants
                if (!variants.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Sản phẩm này chưa có biến thể nào được tạo",
                        debug = new
                        {
                            productId = request.ProductId,
                            selectedOptions = request.SelectedOptions,
                            totalVariantsInDb = _context.Variants.Count(v => v.ProductID == request.ProductId)
                        }
                    });
                }

                // Debug: Log all variants
                foreach (var v in variants)
                {
                    var options = v.VariantOptionValues
                        .Select(vov => $"{vov.ProductOptionValue.ProductOption.ProductOptionId}:{vov.ProductOptionValueId}")
                        .ToList();
                    System.Console.WriteLine($"Variant {v.VariantID} ({v.VariantName}): Options [{string.Join(", ", options)}]");
                }

                var matchingVariant = variants.FirstOrDefault(variant =>
                {
                    var variantOptions = variant.VariantOptionValues
                        .ToDictionary(
                            vov => vov.ProductOptionValue.ProductOption.ProductOptionId,
                            vov => vov.ProductOptionValueId
                        );

                    if (variantOptions.Count != request.SelectedOptions.Count)
                    {
                        System.Console.WriteLine($"Count mismatch for variant {variant.VariantID}: {variantOptions.Count} vs {request.SelectedOptions.Count}");
                        return false;
                    }

                    var matches = request.SelectedOptions.All(selectedOption =>
                        variantOptions.ContainsKey(selectedOption.Key) &&
                        variantOptions[selectedOption.Key] == selectedOption.Value
                    );

                    System.Console.WriteLine($"Variant {variant.VariantID} matches: {matches}");
                    return matches;
                });

                if (matchingVariant == null)
                {
                    System.Console.WriteLine("❌ No matching variant found!");
                    return Json(new
                    {
                        success = false,
                        message = "Không tìm thấy phiên bản phù hợp với lựa chọn",
                        debug = new
                        {
                            selectedOptions = request.SelectedOptions,
                            availableVariants = variants.Select(v => new {
                                variantId = v.VariantID,
                                variantName = v.VariantName,
                                options = v.VariantOptionValues.Select(vov => new {
                                    optionId = vov.ProductOptionValue.ProductOption.ProductOptionId,
                                    valueId = vov.ProductOptionValueId,
                                    optionName = vov.ProductOptionValue.ProductOption.Name,
                                    valueName = vov.ProductOptionValue.Value
                                }).ToList()
                            }).ToList()
                        }
                    });
                }

                System.Console.WriteLine($"✅ Found matching variant: {matchingVariant.VariantID} ({matchingVariant.VariantName})");

                return Json(new
                {
                    success = true,
                    variantId = matchingVariant.VariantID,
                    sku = matchingVariant.SKU,
                    price = matchingVariant.UnitPrice,
                    comparePrice = (decimal?)null,
                    stock = matchingVariant.Stock,
                    isAvailable = matchingVariant.Stock > 0
                });
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ Error in GetVariantByOptions: {ex.Message}");
                return Json(new
                {
                    success = false,
                    message = $"Lỗi: {ex.Message}",
                    stackTrace = ex.StackTrace
                });
            }
        }

        // API để tính giá real-time cho frontend
        [HttpPost]
        public JsonResult CalculatePrice([FromBody] CalculatePriceRequest request)
        {
            try
            {
                var optionValues = _context.ProductOptionValues
                    .Where(pov => request.OptionValueIds.Contains(pov.ProductOptionValueId))
                    .ToList();

                decimal totalPrice = optionValues.Sum(ov => ov.Price);

                return Json(new
                {
                    success = true,
                    price = totalPrice,
                    breakdown = optionValues.Select(ov => new {
                        name = ov.Value,
                        price = ov.Price
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }


        // Thêm các methods sau vào ProductsController

        private async Task<int> GetCurrentCustomerIdAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return 0;

            // Tìm User thông qua ApplicationUserId
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ApplicationUserId == currentUser.Id);

            return user?.UserId ?? 0;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // Gọi CartController để thêm sản phẩm
                var cartController = new CartController(_context, _userManager);
                var result = await cartController.AddVariant(new CartController.AddVariantRequest
                {
                    VariantId = request.VariantId,
                    Quantity = request.Quantity
                });

                return result;
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra!" });
            }
        }

        public class AddToCartRequest
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
        }

        public class CalculatePriceRequest
        {
            public int ProductId { get; set; }
            public List<int> OptionValueIds { get; set; }
        }

        [HttpGet]
        public JsonResult DebugVariants(int productId)
        {
            var variants = _context.Variants
                .Include(v => v.VariantOptionValues)
                    .ThenInclude(vov => vov.ProductOptionValue)
                        .ThenInclude(pov => pov.ProductOption)
                .Where(v => v.ProductID == productId)
                .Select(v => new
                {
                    VariantId = v.VariantID,
                    Name = v.VariantName,
                    Status = v.Status,
                    Stock = v.Stock,
                    Options = v.VariantOptionValues.Select(vov => new
                    {
                        OptionId = vov.ProductOptionValue.ProductOption.ProductOptionId,
                        OptionName = vov.ProductOptionValue.ProductOption.Name,
                        ValueId = vov.ProductOptionValueId,
                        ValueName = vov.ProductOptionValue.Value
                    }).ToList()
                })
                .ToList();

            return Json(variants);
        }
        // API debug để kiểm tra product options
        [HttpGet]
        // API debug để kiểm tra product options
        [HttpGet]
        public JsonResult DebugProductOptions(int productId)
        {
            try
            {
                System.Console.WriteLine($"DebugProductOptions called with productId: {productId}");

                var options = _context.ProductOptions
                    .Include(po => po.Values)
                    .Where(po => po.ProductId == productId) 
                    .Select(po => new
                    {
                        OptionId = po.ProductOptionId,
                        Name = po.Name,
                        ProductId = po.ProductId, 
                        Values = po.Values.Select(v => new
                        {
                            ValueId = v.ProductOptionValueId,
                            Value = v.Value
                        }).ToList()
                    })
                    .ToList();

                System.Console.WriteLine($"Found {options.Count} options for product {productId}");

                return Json(options);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error in DebugProductOptions: {ex.Message}");
                return Json(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet]
        public JsonResult DebugProductSchema(int productId)
        {
            try
            {
                // Kiểm tra tất cả ProductOptions trong database
                var allOptions = _context.ProductOptions.ToList();

                // Kiểm tra product có tồn tại không
                var product = _context.Products.Find(productId);

                return Json(new
                {
                    productExists = product != null,
                    productName = product?.ProductName,
                    totalOptionsInDb = allOptions.Count,
                    allOptions = allOptions.Select(po => new
                    {
                        id = po.ProductOptionId,
                        name = po.Name,
                        productId = po.ProductId, // Sẽ thấy property name thực tế
                    }).Take(5).ToList() // Chỉ lấy 5 để debug
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult DebugDataConsistency(int productId)
        {
            try
            {
                var product = _context.Products.Find(productId);

                // Lấy tất cả variants của product
                var variants = _context.Variants
                    .Include(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.ProductOptionValue)
                            .ThenInclude(pov => pov.ProductOption)
                    .Where(v => v.ProductID == productId)
                    .ToList();

                // Lấy tất cả product options
                var productOptions = _context.ProductOptions
                    .Include(po => po.Values)
                    .Where(po => po.ProductId == productId)
                    .ToList();

                // Lấy tất cả option IDs từ variants
                var optionIdsFromVariants = variants
                    .SelectMany(v => v.VariantOptionValues)
                    .Select(vov => vov.ProductOptionValue.ProductOption.ProductOptionId)
                    .Distinct()
                    .ToList();

                // Lấy tất cả option IDs từ ProductOptions
                var optionIdsFromProductOptions = productOptions
                    .Select(po => po.ProductOptionId)
                    .ToList();

                return Json(new
                {
                    productExists = product != null,
                    productName = product?.ProductName,
                    variantsCount = variants.Count,
                    productOptionsCount = productOptions.Count,
                    optionIdsFromVariants = optionIdsFromVariants,
                    optionIdsFromProductOptions = optionIdsFromProductOptions,
                    missingProductOptions = optionIdsFromVariants.Except(optionIdsFromProductOptions).ToList(),
                    variants = variants.Select(v => new
                    {
                        variantId = v.VariantID,
                        variantName = v.VariantName,
                        options = v.VariantOptionValues.Select(vov => new
                        {
                            optionId = vov.ProductOptionValue.ProductOption.ProductOptionId,
                            optionName = vov.ProductOptionValue.ProductOption.Name,
                            valueId = vov.ProductOptionValueId,
                            valueName = vov.ProductOptionValue.Value
                        }).ToList()
                    }).ToList(),
                    productOptions = productOptions.Select(po => new
                    {
                        optionId = po.ProductOptionId,
                        name = po.Name,
                        values = po.Values.Select(v => new
                        {
                            valueId = v.ProductOptionValueId,
                            value = v.Value
                        }).ToList()
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
