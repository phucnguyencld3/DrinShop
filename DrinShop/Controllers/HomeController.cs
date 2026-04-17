using DrinShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    public class HomeController : BaseController
    {
        private readonly DrinShopDbContext _context;

        public HomeController(DrinShopDbContext context) : base(context)
        {
            _context = context;
        }

        public IActionResult Index(int? categoryId)
        {
            // Lấy sản phẩm theo danh mục nếu có categoryId
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true);

            // Lọc theo danh mục nếu có
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId.Value);
                var selectedCategory = _context.Categories.Find(categoryId.Value);
                ViewBag.SelectedCategory = selectedCategory;
            }

            // Sản phẩm mới nhất
            var newProducts = productsQuery
                .OrderByDescending(p => p.CreateDate)
                .Take(8)
                .ToList();

            // Sản phẩm nổi bật (có thể dựa vào tồn kho hoặc đánh giá)
            var featuredProducts = productsQuery
                .Where(p => p.Variants.Sum(v => v.Stock) > 0) // Còn hàng
                .OrderByDescending(p => p.CreateDate)
                .Take(8)
                .ToList();

            // Danh mục nổi bật
            var featuredCategories = _context.Categories
                .Where(c => c.Status == true)
                .Take(6)
                .ToList();

            ViewBag.NewProducts = newProducts;
            ViewBag.FeaturedProducts = featuredProducts;
            ViewBag.FeaturedCategories = featuredCategories;

            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
