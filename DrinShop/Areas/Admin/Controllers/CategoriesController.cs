using DrinShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CategoriesController : BaseAdminController  // ✅ SỬA: Kế thừa BaseAdminController
    {
        private readonly DrinShopDbContext _context;

        public CategoriesController(DrinShopDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            return View(categories);
        }

        public IActionResult Create()
        {
            var newCategory = new Category
            {
                Status = true,
                CreatedBy = CurrentUserName  // ✅ Sử dụng từ BaseAdminController
            };
            return View(newCategory);
        }

        public IActionResult Details(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null)
            {
                SetErrorMessage("Không tìm thấy danh mục!");  // ✅ Sử dụng từ BaseAdminController
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return RedirectToAction(nameof(Create));
            }

            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();
            return View(category);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Save(Category category)
        {
            if (!ModelState.IsValid)
            {
                return View(category.CategoryID == 0 ? "Create" : "Edit", category);
            }

            if (category.CategoryID == 0)
            {
                category.CreateDate = DateTime.Now;
                category.CreatedBy = CurrentUserName;  // ✅ Sử dụng từ BaseAdminController
                _context.Categories.Add(category);
                SetSuccessMessage("Thêm danh mục thành công!");  // ✅ Sử dụng từ BaseAdminController
            }
            else
            {
                var existing = _context.Categories.Find(category.CategoryID);
                if (existing == null) return NotFound();

                existing.CategoryName = category.CategoryName;
                existing.Description = category.Description;
                existing.Status = category.Status;
                SetSuccessMessage("Cập nhật danh mục thành công!");  // ✅ Sử dụng từ BaseAdminController
            }

            try
            {
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                SetErrorMessage("Lỗi khi lưu dữ liệu: " + ex.Message);  // ✅ Sử dụng từ BaseAdminController
                return View(category.CategoryID == 0 ? "Create" : "Edit", category);
            }

            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {
            var category = _context.Categories.Find(id);
            if (category == null) return NotFound();

            var hasProducts = _context.Products.Any(p => p.CategoryID == id);
            if (hasProducts)
            {
                SetErrorMessage("Không thể xóa danh mục này vì có sản phẩm đang sử dụng!");
                return RedirectToAction(nameof(Index));
            }

            _context.Categories.Remove(category);
            _context.SaveChanges();
            SetSuccessMessage("Xóa danh mục thành công!");
            return RedirectToAction(nameof(Index));
        }
    }
}


