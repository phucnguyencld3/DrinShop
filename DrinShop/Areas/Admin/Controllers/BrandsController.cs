using DrinShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class BrandsController : Controller
    {
        private readonly DrinShopDbContext _context;
        public BrandsController(DrinShopDbContext context) => _context = context;

        public IActionResult Index()
        {
            var brands = _context.Brands.ToList();
            return View(brands);
        }
        public IActionResult Details(int id)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thương hiệu!";
                return RedirectToAction(nameof(Index));
            }
            return View(brand);
        }

        // Gộp Create và Edit thành một action duy nhất
        [Authorize(Roles = "Admin,Staff")]
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                // Tạo mới thương hiệu với CreatedBy
                var newBrand = new Brand
                {
                    Status = true,
                    CreatedBy = HttpContext.User.Identity?.Name ?? "System"
                };
                return View(newBrand);
            }

            // Sửa thương hiệu
            var brand = _context.Brands.Find(id);
            if (brand == null) return NotFound();
            return View(brand);
        }

        // Phương thức Save xử lý cả thêm mới và cập nhật
        [HttpPost]
        [Authorize(Roles = "Admin,Staff")]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Brand brand)
        {
            // Xóa ModelState error cho CreatedBy nếu đang tạo mới
            if (brand.BrandID == 0)
            {
                ModelState.Remove("CreatedBy");
                ModelState.Remove("CreateDate");
            }

            if (!ModelState.IsValid)
                return View("Edit", brand);

            if (brand.BrandID == 0)
            {
                // Thêm mới
                brand.CreateDate = DateTime.Now;
                brand.CreatedBy = HttpContext.User.Identity?.Name ?? "System";
                _context.Brands.Add(brand);
                TempData["SuccessMessage"] = "Thêm thương hiệu thành công!";
            }
            else
            {
                // Cập nhật
                var existing = _context.Brands.Find(brand.BrandID);
                if (existing == null) return NotFound();

                existing.BrandName = brand.BrandName;
                existing.Description = brand.Description;
                existing.Status = brand.Status;
                TempData["SuccessMessage"] = "Cập nhật thương hiệu thành công!";
            }

            _context.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var brand = _context.Brands.Find(id);
            if (brand == null) return NotFound();

            // Kiểm tra xem có sản phẩm nào đang sử dụng thương hiệu này không
            var hasProducts = _context.Products.Any(p => p.BrandID == id);
            if (hasProducts)
            {
                TempData["ErrorMessage"] = "Không thể xóa thương hiệu này vì có sản phẩm đang sử dụng!";
                return RedirectToAction(nameof(Index));
            }

            _context.Brands.Remove(brand);
            _context.SaveChanges();
            TempData["SuccessMessage"] = "Xóa thương hiệu thành công!";
            return RedirectToAction(nameof(Index));
        }
    }
}

