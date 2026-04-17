using DrinShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    public class CategoriesController : BaseController
    {
        private readonly DrinShopDbContext _context;

        public CategoriesController(DrinShopDbContext context) : base(context)
        {
            _context = context;
        }

        // Các methods giữ nguyên như cũ...
        public IActionResult Index()
        {
            var categories = _context.Categories
                .Where(c => c.Status == true)
                .Select(c => new
                {
                    Category = c,
                    ProductCount = _context.Products.Count(p => p.CategoryID == c.CategoryID && p.Status == true)
                })
                .OrderBy(x => x.Category.CategoryName)
                .ToList();

            return View(categories.Select(x => x.Category).ToList());
        }

        public IActionResult Details(int id, int page = 1, int pageSize = 12)
        {
            var category = _context.Categories.Find(id);
            if (category == null || !category.Status)
            {
                TempData["ErrorMessage"] = "Danh mục không tồn tại!";
                return RedirectToAction(nameof(Index));
            }

            // Lấy sản phẩm trong danh mục
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.CategoryID == id && p.Status == true);

            var totalProducts = query.Count();
            var totalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

            var products = query
                .OrderByDescending(p => p.CreateDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Category = category;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalProducts = totalProducts;

            return View(products);
        }
    }
}
