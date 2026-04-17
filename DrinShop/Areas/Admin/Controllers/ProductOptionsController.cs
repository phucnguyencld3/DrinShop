using DrinShop.Areas.Admin.Controllers;
using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductOptionsController : BaseAdminController
    {
        private readonly DrinShopDbContext _context;

        public ProductOptionsController(DrinShopDbContext context)
        {
            _context = context;
        }

        // GET: ProductOptions/Index/5 - Danh sách thuộc tính theo sản phẩm
        public IActionResult Index(int productId)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction("Index", "Products");
            }

            var options = _context.ProductOptions
                .Include(po => po.Values)
                .Where(po => po.ProductId == productId)
                .OrderBy(po => po.CreatedAt)
                .ToList();

            ViewData["Product"] = product;
            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.ProductName;

            return View(options);
        }

        // Thêm/Sửa thuộc tính
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Edit(int productId, int? id)
        {
            var product = _context.Products.Find(productId);
            if (product == null) return NotFound();

            ViewData["Product"] = product;

            if (id == null)
            {
                return View(new ProductOption { ProductId = productId });
            }

            var option = _context.ProductOptions
                .Include(po => po.Values)
                .FirstOrDefault(po => po.ProductOptionId == id);

            if (option == null) return NotFound();
            return View(option);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(ProductOption option, List<string> values, List<decimal> prices)
        {
            // Validation existing code...
            if (option.ProductOptionId == 0)
            {
                ModelState.Remove("CreatedBy");
                ModelState.Remove("CreatedAt");
            }

            if (values == null || values.Count == 0 || values.All(string.IsNullOrWhiteSpace))
            {
                ModelState.AddModelError("", "Vui lòng nhập ít nhất một giá trị cho thuộc tính");
            }

            if (!ModelState.IsValid)
            {
                var product = _context.Products.Find(option.ProductId);
                ViewData["Product"] = product;
                return View("Edit", option);
            }

            // ✅ Process values with prices
            var valueData = new List<(string value, decimal price)>();
            for (int i = 0; i < values.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(values[i]))
                {
                    var price = (prices != null && i < prices.Count) ? prices[i] : 0;
                    valueData.Add((values[i].Trim(), price));
                }
            }

            if (option.ProductOptionId == 0)
            {
                // ============ TẠO MỚI ============
                option.CreatedAt = DateTime.Now;
                option.CreatedBy = CurrentUserName;

                _context.ProductOptions.Add(option);
                _context.SaveChanges();

                // ✅ Thêm values với giá riêng
                foreach (var (value, price) in valueData)
                {
                    _context.ProductOptionValues.Add(new ProductOptionValue
                    {
                        ProductOptionId = option.ProductOptionId,
                        Value = value,
                        Price = price,  // ✅ Set giá riêng
                        CreatedAt = DateTime.Now,
                        CreatedBy = CurrentUserName
                    });
                }

                SetSuccessMessage("Thêm thuộc tính thành công!");
            }
            else
            {
                // ============ CẬP NHẬT ============
                var existing = _context.ProductOptions.Find(option.ProductOptionId);
                if (existing == null) return NotFound();

                existing.Name = option.Name;
                existing.PriceModifer = option.PriceModifer;

                // Xóa values cũ
                var existingValues = _context.ProductOptionValues
                    .Where(pov => pov.ProductOptionId == existing.ProductOptionId)
                    .ToList();
                _context.ProductOptionValues.RemoveRange(existingValues);

                // ✅ Thêm values mới với giá
                foreach (var (value, price) in valueData)
                {
                    _context.ProductOptionValues.Add(new ProductOptionValue
                    {
                        ProductOptionId = existing.ProductOptionId,
                        Value = value,
                        Price = price,  // ✅ Set giá riêng
                        CreatedAt = DateTime.Now,
                        CreatedBy = CurrentUserName
                    });
                }

                SetSuccessMessage("Cập nhật thuộc tính thành công!");
            }

            _context.SaveChanges();
            return RedirectToAction("Index", new { productId = option.ProductId });
        }



        // Xóa thuộc tính
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var option = _context.ProductOptions
                .Include(po => po.Values)
                .FirstOrDefault(po => po.ProductOptionId == id);

            if (option == null) return NotFound();

            // ✅ Sửa query để tránh lỗi LINQ translation
            // Lấy danh sách ProductOptionValueId trước
            var optionValueIds = option.Values.Select(v => v.ProductOptionValueId).ToList();

            // Kiểm tra xem có biến thể nào đang sử dụng các giá trị này không
            var isUsed = _context.VariantOptionValues
                .Any(vov => optionValueIds.Contains(vov.ProductOptionValueId));

            if (isUsed)
            {
                SetErrorMessage("Không thể xóa thuộc tính này vì có biến thể đang sử dụng!");
                return RedirectToAction("Index", new { productId = option.ProductId });
            }

            // ✅ Xóa theo thứ tự: Values trước, Option sau
            using var transaction = _context.Database.BeginTransaction();
            try
            {
                // Xóa tất cả values trước
                _context.ProductOptionValues.RemoveRange(option.Values);

                // Xóa option
                _context.ProductOptions.Remove(option);

                _context.SaveChanges();
                transaction.Commit();

                SetSuccessMessage("Xóa thuộc tính thành công!");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                SetErrorMessage($"Lỗi khi xóa thuộc tính: {ex.Message}");
            }

            return RedirectToAction("Index", new { productId = option.ProductId });
        }
        // API xóa một giá trị thuộc tính
        [HttpPost]
        public JsonResult DeleteValue(int valueId)
        {
            try
            {
                var value = _context.ProductOptionValues.Find(valueId);
                if (value == null)
                    return Json(new { success = false, message = "Không tìm thấy giá trị" });

                // ✅ Kiểm tra trực tiếp với valueId
                var isUsed = _context.VariantOptionValues
                    .Any(vov => vov.ProductOptionValueId == valueId);

                if (isUsed)
                    return Json(new { success = false, message = "Không thể xóa giá trị này vì có biến thể đang sử dụng" });

                _context.ProductOptionValues.Remove(value);
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa giá trị thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
        // Tạo sản phẩm - method này có vẻ không thuộc ProductOptions
        public IActionResult Create()
        {
            ViewData["Categories"] = _context.Categories.Where(c => c.Status == true).ToList();
            ViewData["Brands"] = _context.Brands.Where(b => b.Status == true).ToList();

            var newProduct = new Product
            {
                Status = true,
                Code = GenerateProductCode()
            };
            return View(newProduct);
        }
        // Helper method
        private string GenerateProductCode()
        {
            var prefix = "SP";
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        //Các debug method khác nếu cần thiết
        // ✅ Helper method để kiểm tra usage chi tiết
        private (bool isUsed, string details) CheckOptionUsage(ProductOption option)
        {
            var optionValueIds = option.Values.Select(v => v.ProductOptionValueId).ToList();

            var usedVariants = _context.VariantOptionValues
                .Include(vov => vov.Variant)
                .Where(vov => optionValueIds.Contains(vov.ProductOptionValueId))
                .Select(vov => new {
                    VariantName = vov.Variant.VariantName,
                    VariantId = vov.Variant.VariantID
                })
                .Distinct()
                .ToList();

            if (usedVariants.Any())
            {
                var variantNames = string.Join(", ", usedVariants.Take(3).Select(v => v.VariantName));
                var moreCount = usedVariants.Count - 3;
                var details = moreCount > 0
                    ? $"{variantNames} và {moreCount} biến thể khác"
                    : variantNames;

                return (true, $"Đang được sử dụng bởi: {details}");
            }

            return (false, "");
        }


    }
}

