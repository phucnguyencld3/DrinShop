using DrinShop.Models;
using DrinShop.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class ProductsController : BaseAdminController
    {
        private readonly DrinShopDbContext _context;
        private readonly ICloudinaryService _cloudinaryService;

        public ProductsController(DrinShopDbContext context, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _cloudinaryService = cloudinaryService;
        }

        // GET: Danh sách sản phẩm
        public IActionResult Index()
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .OrderByDescending(p => p.CreateDate)
                .ToList();
            return View(products);
        }

        // GET: Trang thêm sản phẩm mới
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

        // POST: Xử lý thêm sản phẩm mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, List<IFormFile> images)
        {
            // Xóa validation cho các field tự động
            ModelState.Remove("CreatedBy");
            ModelState.Remove("CreateDate");

            if (!ModelState.IsValid)
            {
                ViewData["Categories"] = _context.Categories.Where(c => c.Status == true).ToList();
                ViewData["Brands"] = _context.Brands.Where(b => b.Status == true).ToList();
                return View(product);
            }

            // Đảm bảo mã sản phẩm unique
            if (string.IsNullOrEmpty(product.Code))
            {
                product.Code = GenerateProductCode();
            }

            while (_context.Products.Any(p => p.Code == product.Code))
            {
                product.Code = GenerateProductCode();
            }

            // Gán thông tin tạo
            product.CreateDate = DateTime.Now;
            product.CreatedBy = CurrentUserName;

            // Thêm vào database
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Upload ảnh nếu có
            if (images != null && images.Any())
            {
                await UploadProductImages(product.ProductID, images);
            }

            SetSuccessMessage("Thêm sản phẩm thành công!");
            return RedirectToAction(nameof(Index));
        }

        // GET: Trang chỉnh sửa sản phẩm
        public IActionResult Edit(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            ViewData["Categories"] = _context.Categories.Where(c => c.Status == true).ToList();
            ViewData["Brands"] = _context.Brands.Where(b => b.Status == true).ToList();

            return View(product);
        }

        // POST: Xử lý chỉnh sửa sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product product, List<IFormFile> images)
        {
            if (id != product.ProductID)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData["Categories"] = _context.Categories.Where(c => c.Status == true).ToList();
                ViewData["Brands"] = _context.Brands.Where(b => b.Status == true).ToList();
                return View(product);
            }

            var existing = await _context.Products.FindAsync(id);
            if (existing == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            // Cập nhật thông tin
            existing.ProductName = product.ProductName;
            existing.Description = product.Description;
            existing.CategoryID = product.CategoryID;
            existing.BrandID = product.BrandID;
            existing.Status = product.Status;

            // Upload thêm ảnh mới nếu có
            if (images != null && images.Any())
            {
                await UploadProductImages(product.ProductID, images);
            }

            await _context.SaveChangesAsync();
            SetSuccessMessage("Cập nhật sản phẩm thành công!");
            return RedirectToAction(nameof(Index));
        }

        // GET: Xem chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            var product = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.ProductOptionValue)
                            .ThenInclude(pov => pov.ProductOption)
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            return View(product);
        }

        // POST: Upload thêm ảnh cho sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImages(int productId, List<IFormFile> images)
        {
            var product = await _context.Products.FindAsync(productId);
            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            if (images != null && images.Any())
            {
                await UploadProductImages(productId, images);
                SetSuccessMessage($"Đã upload {images.Count} hình ảnh thành công!");
            }
            else
            {
                SetErrorMessage("Vui lòng chọn ít nhất 1 hình ảnh!");
            }

            return RedirectToAction(nameof(Edit), new { id = productId });
        }

        // POST: Xóa một ảnh
        [HttpPost]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return Json(new { success = false, message = "Không tìm thấy ảnh" });

            // Xóa trên Cloudinary
            await _cloudinaryService.DeleteImageAsync(image.ImageUrl);

            // Xóa trong database
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Xóa ảnh thành công" });
        }

        // POST: Đặt ảnh mặc định
        [HttpPost]
        public async Task<IActionResult> SetDefaultImage(int imageId, int productId)
        {
            // Bỏ tất cả ảnh mặc định của sản phẩm
            var allImages = _context.ProductImages.Where(pi => pi.ProductId == productId);
            foreach (var img in allImages)
            {
                img.IsDefault = false;
            }

            // Đặt ảnh được chọn làm mặc định
            var selectedImage = await _context.ProductImages.FindAsync(imageId);
            if (selectedImage != null)
            {
                selectedImage.IsDefault = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã đặt làm ảnh mặc định" });
        }

        // POST: Xóa sản phẩm (Chỉ Admin)
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var product = _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefault(p => p.ProductID == id);

            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra ràng buộc với variants
            var hasVariants = _context.Variants.Any(v => v.ProductID == id);
            if (hasVariants)
            {
                SetErrorMessage("Không thể xóa sản phẩm này vì có biến thể đang sử dụng!");
                return RedirectToAction(nameof(Index));
            }

            // Xóa tất cả ảnh trên Cloudinary
            if (product.ProductImages != null && product.ProductImages.Any())
            {
                foreach (var image in product.ProductImages)
                {
                    await _cloudinaryService.DeleteImageAsync(image.ImageUrl);
                }
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            SetSuccessMessage("Xóa sản phẩm thành công!");
            return RedirectToAction(nameof(Index));
        }

        // Chuyển đến quản lý thuộc tính sản phẩm
        public IActionResult ManageOptions(int id)
        {
            var product = _context.Products.Find(id);
            if (product == null)
            {
                SetErrorMessage("Không tìm thấy sản phẩm!");
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction("Index", "ProductOptions", new { productId = id });
        }
        // Add this method to ProductOptionsController.cs

       


        #region Private Methods

        // Upload ảnh cho sản phẩm
        private async Task UploadProductImages(int productId, List<IFormFile> images)
        {
            var existingImagesCount = _context.ProductImages.Count(pi => pi.ProductId == productId);
            var isFirstImage = existingImagesCount == 0;

            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                if (image != null && image.Length > 0)
                {
                    var imageUrl = await _cloudinaryService.UploadImageAsync(image);
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var productImage = new ProductImage
                        {
                            ProductId = productId,
                            ImageUrl = imageUrl,
                            IsDefault = isFirstImage && i == 0,
                            SortOrder = existingImagesCount + i,
                            CreatedAt = DateTime.Now,
                            CreatedBy = CurrentUserName
                        };
                        _context.ProductImages.Add(productImage);
                    }
                }
            }
            await _context.SaveChangesAsync();
        }

        // Sinh mã sản phẩm tự động
        private string GenerateProductCode()
        {
            var prefix = "SP";
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(1000, 9999);
            return $"{prefix}{timestamp}{random}";
        }

        #endregion

        #region API Methods

        // API: Sinh mã sản phẩm mới
        [HttpGet]
        public JsonResult GenerateNewCode()
        {
            var newCode = GenerateProductCode();
            while (_context.Products.Any(p => p.Code == newCode))
            {
                newCode = GenerateProductCode();
            }
            return Json(new { code = newCode });
        }

        #endregion
    }
}