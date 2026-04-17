using DrinShop.Models;
using DrinShop.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BundleController : Controller
    {
        private readonly DrinShopDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public BundleController(DrinShopDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Bundle
        public async Task<IActionResult> Index(string searchTerm = "")
        {
            var bundlesQuery = _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                bundlesQuery = bundlesQuery.Where(b =>
                    b.Name.Contains(searchTerm) ||
                    b.Code.Contains(searchTerm) ||
                    (b.Description != null && b.Description.Contains(searchTerm)));
            }

            var bundles = await bundlesQuery
                .OrderByDescending(b => b.BundleId)
                .ToListAsync();

            ViewBag.SearchTerm = searchTerm;
            return View(bundles);
        }

        // GET: Bundle/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var bundle = await _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Category)  
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages) 
                .FirstOrDefaultAsync(b => b.BundleId == id);

            if (bundle == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy combo!";
                return RedirectToAction(nameof(Index));
            }

            return View(bundle);
        }


        // GET: Bundle/Create
        public async Task<IActionResult> Create()
        {
            var bundle = new Bundle
            {
                Status = true,
                Code = GenerateBundleCode(),
                CreatedDate = DateTime.Now
            };

            var products = await _context.Products
                .Include(p => p.Variants.Where(v => v.Status && v.Stock > 0))
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Where(p => p.Status && p.Variants.Any(v => v.Status && v.Stock > 0))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            // ✅ DEBUG: Kiểm tra ProductImages
            foreach (var product in products)
            {
                System.Diagnostics.Debug.WriteLine($"Product: {product.ProductName}, Images: {product.ProductImages?.Count ?? 0}");
                if (product.ProductImages?.Any() == true)
                {
                    foreach (var img in product.ProductImages)
                    {
                        System.Diagnostics.Debug.WriteLine($"  - Image: {img.ImageUrl}, IsDefault: {img.IsDefault}");
                    }
                }
            }

            ViewBag.Products = products;
            return View(bundle);
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Bundle bundle, IFormFile bundleImage, string BundleItemsData)
        {
            // ✅ DEBUG: Log đầu vào
            System.Diagnostics.Debug.WriteLine("=== CREATE BUNDLE DEBUG ===");
            System.Diagnostics.Debug.WriteLine($"Bundle: {System.Text.Json.JsonSerializer.Serialize(bundle)}");
            System.Diagnostics.Debug.WriteLine($"BundleItemsData: {BundleItemsData}");
            System.Diagnostics.Debug.WriteLine($"BundleImage: {bundleImage?.FileName ?? "null"}");

            // ✅ XÓA: Tất cả computed properties và auto-generated fields TRƯỚC KHI validate
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreatedDate");
            ModelState.Remove("BundleItemsData");
            ModelState.Remove("LimitingItem");
            ModelState.Remove("BundleItems");
            ModelState.Remove("OriginalPrice");
            ModelState.Remove("FinalPrice");
            ModelState.Remove("DiscountPercentage");
            ModelState.Remove("Price");
            ModelState.Remove("AvailableStock");
            ModelState.Remove("IsInStock");
            ModelState.Remove("IsLowStock");
            ModelState.Remove("InvoiceDetails");
            ModelState.Remove("CartDetails");

            // ✅ DEBUG: Hiển thị ModelState sau khi clean
            System.Diagnostics.Debug.WriteLine("=== CLEANED MODELSTATE ===");
            foreach (var kvp in ModelState)
            {
                var errors = string.Join(", ", kvp.Value.Errors.Select(e => e.ErrorMessage));
                System.Diagnostics.Debug.WriteLine($"[{kvp.Key}] Valid: {kvp.Value.ValidationState}" + (errors != "" ? $", Errors: {errors}" : ""));
            }

            // ✅ Business validation
            if (await _context.Bundles.AnyAsync(b => b.Code == bundle.Code))
            {
                ModelState.AddModelError("Code", "Mã combo đã tồn tại!");
            }

            if (string.IsNullOrEmpty(BundleItemsData) || BundleItemsData == "[]")
            {
                ModelState.AddModelError("", "Vui lòng thêm ít nhất 1 sản phẩm vào combo!");
            }

            // ✅ Validation summary nếu có lỗi
            if (!ModelState.IsValid)
            {
                System.Diagnostics.Debug.WriteLine("=== VALIDATION FAILED ===");
                foreach (var kvp in ModelState.Where(x => x.Value.Errors.Any()))
                {
                    foreach (var error in kvp.Value.Errors)
                    {
                        System.Diagnostics.Debug.WriteLine($"❌ [{kvp.Key}]: {error.ErrorMessage}");
                    }
                }

                await ReloadCreateViewData();
                return View(bundle);
            }

            // ✅ Tất cả validation passed - bắt đầu transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                System.Diagnostics.Debug.WriteLine("=== STARTING TRANSACTION ===");

                // Set thông tin audit
                bundle.CreatedBy = User.Identity?.Name ?? "Admin";
                bundle.CreatedDate = DateTime.Now;

                // Upload ảnh nếu có
                if (bundleImage != null && bundleImage.Length > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Uploading image: {bundleImage.FileName}");
                    var imageUrl = await _cloudinaryService.UploadImageAsync(bundleImage);

                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        bundle.ImageUrl = imageUrl;
                        System.Diagnostics.Debug.WriteLine($"✅ Image uploaded: {imageUrl}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("❌ Image upload failed");
                        await transaction.RollbackAsync();
                        TempData["ErrorMessage"] = "Lỗi upload ảnh! Vui lòng thử lại.";
                        await ReloadCreateViewData();
                        return View(bundle);
                    }
                }

                // Lưu bundle
                _context.Add(bundle);
                await _context.SaveChangesAsync();
                System.Diagnostics.Debug.WriteLine($"✅ Bundle saved with ID: {bundle.BundleId}");

                // Xử lý bundle items
                int itemsProcessed = await ProcessBundleItems(bundle.BundleId, BundleItemsData);
                System.Diagnostics.Debug.WriteLine($"✅ Processed {itemsProcessed} bundle items");

                await transaction.CommitAsync();
                System.Diagnostics.Debug.WriteLine("✅ TRANSACTION COMMITTED");

                TempData["SuccessMessage"] = "Tạo combo thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ TRANSACTION ERROR: {ex.Message}");
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = $"Lỗi tạo combo: {ex.Message}";
                await ReloadCreateViewData();
                return View(bundle);
            }
        }

        // ✅ THÊM: Helper method xử lý bundle items
        private async Task<int> ProcessBundleItems(int bundleId, string bundleItemsData)
        {
            if (string.IsNullOrEmpty(bundleItemsData) || bundleItemsData == "[]")
                return 0;

            using JsonDocument doc = JsonDocument.Parse(bundleItemsData);
            int itemCount = 0;

            foreach (JsonElement item in doc.RootElement.EnumerateArray())
            {
                var variantId = item.GetProperty("variantId").GetInt32();
                var quantity = item.GetProperty("quantity").GetInt32();

                var variant = await _context.Variants.FindAsync(variantId);
                if (variant != null && variant.Stock >= quantity)
                {
                    // Trừ stock và tạo bundle item
                    variant.Stock -= quantity;

                    var bundleItem = new BundleItem
                    {
                        BundleId = bundleId,
                        VariantId = variantId,
                        Quantity = quantity
                    };

                    _context.Update(variant);
                    _context.BundleItems.Add(bundleItem);
                    itemCount++;

                    System.Diagnostics.Debug.WriteLine($"Added item: Variant {variantId} x{quantity}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"⚠️ Skipped variant {variantId}: not found or insufficient stock");
                }
            }

            await _context.SaveChangesAsync();
            return itemCount;
        }

        // ✅ Cập nhật method ReloadCreateViewData
        private async Task ReloadCreateViewData()
        {
            ViewBag.Products = await _context.Products
                .Include(p => p.Variants.Where(v => v.Status && v.Stock > 0))
                .Include(p => p.Category)
                .Include(p => p.ProductImages) // ✅ THÊM dòng này
                .Where(p => p.Status && p.Variants.Any(v => v.Status && v.Stock > 0))
                .OrderBy(p => p.ProductName)
                .ToListAsync();
        }




        // GET: Bundle/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var bundle = await _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.BundleId == id);

            if (bundle == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy combo!";
                return RedirectToAction(nameof(Index));
            }

            // Lấy danh sách sản phẩm có variants để thêm vào combo
            ViewBag.Products = await _context.Products
                .Include(p => p.Variants.Where(v => v.Status && v.Stock > 0))
                .Include(p => p.Category)
                .Where(p => p.Status && p.Variants.Any(v => v.Status && v.Stock > 0))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            // Tính tổng giá từ các items
            var calculatedPrice = bundle.BundleItems.Sum(bi => bi.Variant.UnitPrice * bi.Quantity);
            ViewBag.CalculatedPrice = calculatedPrice;

            return View(bundle);
        }

        // POST: Bundle/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Bundle bundle, IFormFile bundleImage)
        {
            if (id != bundle.BundleId)
            {
                return NotFound();
            }

            // Kiểm tra mã combo trùng lặp (trừ chính nó)
            if (await _context.Bundles.AnyAsync(b => b.Code == bundle.Code && b.BundleId != id))
            {
                ModelState.AddModelError("Code", "Mã combo đã tồn tại!");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingBundle = await _context.Bundles.FindAsync(id);
                    if (existingBundle == null)
                    {
                        return NotFound();
                    }

                    // Lưu URL ảnh cũ để xóa trên Cloudinary nếu cần
                    var oldImageUrl = existingBundle.ImageUrl;

                    // Cập nhật thông tin combo
                    existingBundle.Name = bundle.Name;
                    existingBundle.Code = bundle.Code;
                    existingBundle.Description = bundle.Description;
                    existingBundle.DiscountAmount = bundle.DiscountAmount; 
                    existingBundle.Status = bundle.Status;
                    // Xử lý upload ảnh mới
                    if (bundleImage != null && bundleImage.Length > 0)
                    {
                        var newImageUrl = await _cloudinaryService.UploadImageAsync(bundleImage);
                        if (!string.IsNullOrEmpty(newImageUrl))
                        {
                            existingBundle.ImageUrl = newImageUrl;

                            // Xóa ảnh cũ trên Cloudinary
                            if (!string.IsNullOrEmpty(oldImageUrl))
                            {
                                await _cloudinaryService.DeleteImageAsync(oldImageUrl);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật combo thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await BundleExists(bundle.BundleId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = $"Có lỗi xảy ra: {ex.Message}";
                }
            }

            // Reload data nếu có lỗi
            ViewBag.Products = await _context.Products
                .Include(p => p.Variants.Where(v => v.Status && v.Stock > 0))
                .Include(p => p.Category)
                .Where(p => p.Status && p.Variants.Any(v => v.Status && v.Stock > 0))
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return View(bundle);
        }

        // POST: Bundle/AddItem - THÊM SẢN PHẨM VÀO COMBO VÀ TRỪ STOCK
        [HttpPost]
        public async Task<IActionResult> AddItem(int bundleId, int variantId, int quantity = 1)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                            .ThenInclude(v => v.Product)
                    .FirstOrDefaultAsync(b => b.BundleId == bundleId);

                var variant = await _context.Variants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantID == variantId);

                if (bundle == null || variant == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo hoặc sản phẩm!" });
                }

                if (!variant.Status)
                {
                    return Json(new { success = false, message = "Sản phẩm đã ngừng hoạt động!" });
                }

                // ✅ Kiểm tra tồn kho variant
                if (variant.Stock < quantity)
                {
                    return Json(new
                    {
                        success = false,
                        message = $"Sản phẩm {variant.Product.ProductName} - {variant.VariantName} chỉ còn {variant.Stock} trong kho!"
                    });
                }

                // Kiểm tra xem sản phẩm đã có trong combo chưa
                var existingItem = await _context.BundleItems
                    .FirstOrDefaultAsync(bi => bi.BundleId == bundleId && bi.VariantId == variantId);

                if (existingItem != null)
                {
                    // Kiểm tra tồn kho khi tăng số lượng
                    if (variant.Stock < quantity)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Không đủ tồn kho! Cần thêm {quantity}, còn lại {variant.Stock}"
                        });
                    }

                    // ✅ TRỪ STOCK VARIANT
                    variant.Stock -= quantity;
                    existingItem.Quantity += quantity;

                    _context.Update(variant);
                    _context.Update(existingItem);
                }
                else
                {
                    // ✅ TRỪ STOCK VARIANT KHI THÊM MỚI
                    variant.Stock -= quantity;

                    var bundleItem = new BundleItem
                    {
                        BundleId = bundleId,
                        VariantId = variantId,
                        Quantity = quantity
                    };

                    _context.Update(variant);
                    _context.BundleItems.Add(bundleItem);
                }

                // Tự động cập nhật giá combo
                await UpdateBundlePriceAsync(bundleId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Tải lại bundle để lấy thông tin mới
                bundle = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                            .ThenInclude(v => v.Product)
                    .FirstOrDefaultAsync(b => b.BundleId == bundleId);

                return Json(new
                {
                    success = true,
                    message = "Thêm sản phẩm vào combo thành công!",
                    productName = $"{variant.Product.ProductName} - {variant.VariantName}",
                    availableStock = bundle.AvailableStock,
                    stockWarning = bundle.AvailableStock <= 5 && bundle.AvailableStock > 0
                        ? $"Cảnh báo: Combo chỉ còn có thể bán {bundle.AvailableStock} cái!"
                        : bundle.AvailableStock == 0
                            ? "Combo đã hết hàng!"
                            : ""
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/UpdateItemQuantity - CẬP NHẬT SỐ LƯỢNG VÀ ĐIỀU CHỈNH STOCK
        [HttpPost]
        public async Task<IActionResult> UpdateItemQuantity(int bundleItemId, int newQuantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bundleItem = await _context.BundleItems
                    .Include(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                    .FirstOrDefaultAsync(bi => bi.BundleItemId == bundleItemId);

                if (bundleItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong combo!" });
                }

                var bundleId = bundleItem.BundleId;
                var currentQuantity = bundleItem.Quantity;
                var variant = bundleItem.Variant;

                if (newQuantity <= 0)
                {
                    // ✅ HOÀN TRẢ STOCK KHI XÓA
                    variant.Stock += currentQuantity;
                    _context.Update(variant);
                    _context.BundleItems.Remove(bundleItem);
                }
                else
                {
                    var quantityDiff = newQuantity - currentQuantity;

                    if (quantityDiff > 0)
                    {
                        // Tăng số lượng - cần trừ thêm stock
                        if (variant.Stock < quantityDiff)
                        {
                            return Json(new
                            {
                                success = false,
                                message = $"Không đủ stock! Cần thêm {quantityDiff}, chỉ còn {variant.Stock}"
                            });
                        }
                        variant.Stock -= quantityDiff;
                    }
                    else if (quantityDiff < 0)
                    {
                        // Giảm số lượng - hoàn trả stock
                        variant.Stock += Math.Abs(quantityDiff);
                    }

                    bundleItem.Quantity = newQuantity;
                    _context.Update(variant);
                    _context.Update(bundleItem);
                }

                // Cập nhật giá combo
                await UpdateBundlePriceAsync(bundleId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new
                {
                    success = true,
                    message = newQuantity <= 0 ? "Xóa sản phẩm khỏi combo thành công!" : "Cập nhật số lượng thành công!"
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/RemoveItem - XÓA VÀ HOÀN TRẢ STOCK
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int bundleItemId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bundleItem = await _context.BundleItems
                    .Include(bi => bi.Variant)
                    .FirstOrDefaultAsync(bi => bi.BundleItemId == bundleItemId);

                if (bundleItem == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong combo!" });
                }

                var bundleId = bundleItem.BundleId;
                var variant = bundleItem.Variant;

                // ✅ HOÀN TRẢ STOCK
                variant.Stock += bundleItem.Quantity;

                _context.Update(variant);
                _context.BundleItems.Remove(bundleItem);

                // Cập nhật giá combo
                await UpdateBundlePriceAsync(bundleId);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Xóa sản phẩm khỏi combo thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/UploadImage - Upload ảnh riêng biệt
        [HttpPost]
        public async Task<IActionResult> UploadImage(int bundleId, IFormFile image)
        {
            try
            {
                var bundle = await _context.Bundles.FindAsync(bundleId);
                if (bundle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                if (image == null || image.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn ảnh!" });
                }

                // Upload ảnh lên Cloudinary
                var imageUrl = await _cloudinaryService.UploadImageAsync(image);
                if (string.IsNullOrEmpty(imageUrl))
                {
                    return Json(new { success = false, message = "Lỗi khi upload ảnh!" });
                }

                // Xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(bundle.ImageUrl))
                {
                    await _cloudinaryService.DeleteImageAsync(bundle.ImageUrl);
                }

                // Cập nhật URL ảnh mới
                bundle.ImageUrl = imageUrl;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Upload ảnh thành công!",
                    imageUrl = imageUrl
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/DeleteImage - Xóa ảnh combo
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int bundleId)
        {
            try
            {
                var bundle = await _context.Bundles.FindAsync(bundleId);
                if (bundle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                if (string.IsNullOrEmpty(bundle.ImageUrl))
                {
                    return Json(new { success = false, message = "Combo chưa có ảnh!" });
                }

                // Xóa ảnh trên Cloudinary
                await _cloudinaryService.DeleteImageAsync(bundle.ImageUrl);

                // Xóa URL ảnh trong database
                bundle.ImageUrl = null;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Xóa ảnh thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/Delete/5
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                    .Include(b => b.CartDetails)
                    .Include(b => b.InvoiceDetails)
                    .FirstOrDefaultAsync(b => b.BundleId == id);

                if (bundle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                // Kiểm tra ràng buộc
                if (bundle.InvoiceDetails.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa combo đã có trong đơn hàng!"
                    });
                }

                if (bundle.CartDetails.Any())
                {
                    return Json(new
                    {
                        success = false,
                        message = "Không thể xóa combo đang có trong giỏ hàng!"
                    });
                }

                // ✅ HOÀN TRẢ STOCK KHI XÓA COMBO
                foreach (var item in bundle.BundleItems)
                {
                    item.Variant.Stock += item.Quantity;
                    _context.Update(item.Variant);
                }

                // Xóa ảnh trên Cloudinary nếu có
                if (!string.IsNullOrEmpty(bundle.ImageUrl))
                {
                    await _cloudinaryService.DeleteImageAsync(bundle.ImageUrl);
                }

                // Xóa các bundle items trước
                _context.BundleItems.RemoveRange(bundle.BundleItems);

                // Xóa bundle
                _context.Bundles.Remove(bundle);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Xóa combo thành công!" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id, bool status)
        {
            try
            {
                var bundle = await _context.Bundles.FindAsync(id);
                if (bundle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                bundle.Status = status;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã {(status ? "kích hoạt" : "vô hiệu hóa")} combo thành công!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Bundle/UpdateAllBundlePrices - Cập nhật giá cho tất cả combo
        [HttpPost]
        public async Task<IActionResult> UpdateAllBundlePrices()
        {
            try
            {
                var bundlesWithItems = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                    .Where(b => b.BundleItems.Any())
                    .ToListAsync();

                int updatedCount = 0;
                foreach (var bundle in bundlesWithItems)
                {
                    var originalPrice = bundle.BundleItems.Sum(bi => bi.Variant.UnitPrice * bi.Quantity);
                    var newDiscountAmount = Math.Round(originalPrice * 0.1m); // 10% giảm giá mặc định

                    // Chỉ cập nhật nếu discount amount thay đổi
                    if (bundle.DiscountAmount != newDiscountAmount)
                    {
                        bundle.DiscountAmount = newDiscountAmount;
                        updatedCount++;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật giảm giá cho {updatedCount} combo!"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: API để lấy variants theo product
        [HttpGet]
        public async Task<IActionResult> GetVariantsByProduct(int productId)
        {
            try
            {
                var variants = await _context.Variants
                    .Where(v => v.ProductID == productId && v.Status && v.Stock > 0)
                    .Select(v => new
                    {
                        id = v.VariantID,
                        name = v.VariantName,
                        price = v.UnitPrice,
                        stock = v.Stock,
                        sku = v.SKU
                    })
                    .ToListAsync();

                return Json(variants);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // ✅ Helper method để tự động cập nhật giá combo
        private async Task UpdateBundlePriceAsync(int bundleId, decimal? customDiscountAmount = null)
        {
            var bundle = await _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                .FirstOrDefaultAsync(b => b.BundleId == bundleId);

            if (bundle != null)
            {
                // Nếu có custom discount amount thì sử dụng, không thì giữ nguyên
                if (customDiscountAmount.HasValue)
                {
                    bundle.DiscountAmount = customDiscountAmount.Value;
                }
                // Nếu chưa có discount amount và có sản phẩm, tự động set 10% discount
                else if (bundle.DiscountAmount == 0 && bundle.BundleItems.Any())
                {
                    var originalPrice = bundle.BundleItems.Sum(bi => bi.Variant.UnitPrice * bi.Quantity);
                    bundle.DiscountAmount = Math.Round(originalPrice * 0.1m); // 10% giảm giá mặc định
                }

                _context.Update(bundle);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDiscount(int bundleId, decimal discountAmount)
        {
            try
            {
                var bundle = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                    .FirstOrDefaultAsync(b => b.BundleId == bundleId);

                if (bundle == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                var originalPrice = bundle.OriginalPrice;

                if (discountAmount < 0)
                {
                    return Json(new { success = false, message = "Số tiền giảm phải >= 0!" });
                }

                if (discountAmount > originalPrice)
                {
                    return Json(new { success = false, message = $"Số tiền giảm không thể lớn hơn tổng giá gốc ({originalPrice:N0}₫)!" });
                }

                bundle.DiscountAmount = discountAmount;
                await _context.SaveChangesAsync();

                var discountPercentage = originalPrice > 0 ? Math.Round((discountAmount / originalPrice) * 100, 1) : 0;

                return Json(new
                {
                    success = true,
                    message = "Cập nhật giảm giá thành công!",
                    data = new
                    {
                        originalPrice = originalPrice,
                        discountAmount = discountAmount,
                        finalPrice = originalPrice - discountAmount,
                        discountPercentage = discountPercentage
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // Helper methods
        private string GenerateBundleCode()
        {
            var timestamp = DateTime.Now.ToString("yyMMddHHmm");
            var random = new Random().Next(100, 999);
            return $"COMBO{timestamp}{random}";
        }

        private async Task<bool> BundleExists(int id)
        {
            return await _context.Bundles.AnyAsync(e => e.BundleId == id);
        }

        // ✅ THÊM method test tạo sản phẩm mẫu
        [HttpGet]
        public async Task<IActionResult> CreateTestData()
        {
            try
            {
                // Kiểm tra đã có dữ liệu chưa
                var existingProducts = await _context.Products.CountAsync();
                if (existingProducts > 0)
                {
                    TempData["InfoMessage"] = $"Đã có {existingProducts} sản phẩm trong hệ thống";
                    return RedirectToAction(nameof(Create));
                }

                // Tạo Category mẫu
                var category = new Category
                {
                    CategoryName = "Đồ uống test",
                    Description = "Category mẫu cho test",
                    Status = true,
                    CreatedBy = "Admin",
                    CreateDate = DateTime.Now
                };
                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                // Tạo Product mẫu
                var product1 = new Product
                {
                    ProductName = "Combo Test 1",
                    Description = "Sản phẩm test cho combo",
                    CategoryID = category.CategoryID,
                    Status = true,
                    CreatedBy = "Admin",
                    CreateDate = DateTime.Now
                };

                var product2 = new Product
                {
                    ProductName = "Combo Test 2",
                    Description = "Sản phẩm test cho combo",
                    CategoryID = category.CategoryID,
                    Status = true,
                    CreatedBy = "Admin",
                    CreateDate = DateTime.Now
                };

                _context.Products.AddRange(product1, product2);
                await _context.SaveChangesAsync();

                // Tạo Variants
                var variants = new List<Variant>
        {
            new Variant
            {
                ProductID = product1.ProductID,
                VariantName = "Size S",
                UnitPrice = 50000,
                Stock = 100,
                Status = true,
                CreatedBy = "Admin",
                CreateDate = DateTime.Now,
                SKU = "TEST001-S"
            },
            new Variant
            {
                ProductID = product1.ProductID,
                VariantName = "Size M",
                UnitPrice = 70000,
                Stock = 50,
                Status = true,
                CreatedBy = "Admin",
                CreateDate = DateTime.Now,
                SKU = "TEST001-M"
            },
            new Variant
            {
                ProductID = product2.ProductID,
                VariantName = "Regular",
                UnitPrice = 30000,
                Stock = 200,
                Status = true,
                CreatedBy = "Admin",
                CreateDate = DateTime.Now,
                SKU = "TEST002-R"
            }
        };

                _context.Variants.AddRange(variants);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã tạo dữ liệu test thành công! 2 sản phẩm, 3 variants";
                return RedirectToAction(nameof(Create));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Lỗi tạo dữ liệu test: {ex.Message}";
                return RedirectToAction(nameof(Create));
            }
        }


    }
}
