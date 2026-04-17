using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    [AllowAnonymous]
    public class ShopController : Controller
    {
        private readonly DrinShopDbContext _context;

        public ShopController(DrinShopDbContext context)
        {
            _context = context;
        }

        // GET: Shop - Tất cả sản phẩm
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string sortBy = "newest")
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true);

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Variants.Any() ? p.Variants.Min(v => v.UnitPrice) : 0),
                "price_desc" => query.OrderByDescending(p => p.Variants.Any() ? p.Variants.Max(v => v.UnitPrice) : 0),
                "name" => query.OrderBy(p => p.ProductName),
                "oldest" => query.OrderBy(p => p.CreateDate),
                _ => query.OrderByDescending(p => p.CreateDate)
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = totalItems;
            ViewData["Title"] = "Tất cả sản phẩm";

            return View(products);
        }

        // GET: Shop/Category/{id} - Sản phẩm theo danh mục  
        public async Task<IActionResult> Category(int id, int page = 1, int pageSize = 12, string sortBy = "newest")
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null || !category.Status)
            {
                return NotFound();
            }

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true && p.CategoryID == id);

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Variants.Any() ? p.Variants.Min(v => v.UnitPrice) : 0),
                "price_desc" => query.OrderByDescending(p => p.Variants.Any() ? p.Variants.Max(v => v.UnitPrice) : 0),
                "name" => query.OrderBy(p => p.ProductName),
                "oldest" => query.OrderBy(p => p.CreateDate),
                _ => query.OrderByDescending(p => p.CreateDate)
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = totalItems;
            ViewBag.Category = category;
            ViewData["Title"] = $"Danh mục: {category.CategoryName}";
            ViewBag.ShowBreadcrumb = true;

            return View("Index", products);
        }

        // GET: Shop/Search - Tìm kiếm
        public async Task<IActionResult> Search(string q, int? categoryId, int page = 1, int pageSize = 12, string sortBy = "newest")
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true);

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(q) ||
                    p.Description.Contains(q) ||
                    p.Category.CategoryName.Contains(q) ||
                    p.Brand.BrandName.Contains(q));
            }

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryID == categoryId);
            }

            // Sắp xếp
            query = sortBy switch
            {
                "price_asc" => query.OrderBy(p => p.Variants.Any() ? p.Variants.Min(v => v.UnitPrice) : 0),
                "price_desc" => query.OrderByDescending(p => p.Variants.Any() ? p.Variants.Max(v => v.UnitPrice) : 0),
                "name" => query.OrderBy(p => p.ProductName),
                "oldest" => query.OrderBy(p => p.CreateDate),
                _ => query.OrderByDescending(p => p.CreateDate)
            };

            // Phân trang
            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.TotalItems = totalItems;
            ViewBag.SearchQuery = q;
            ViewBag.CategoryId = categoryId;
            ViewData["Title"] = string.IsNullOrWhiteSpace(q) ? "Tìm kiếm sản phẩm" : $"Kết quả tìm kiếm: \"{q}\"";
            ViewBag.ShowBreadcrumb = true;

            // Danh mục cho filter
            ViewBag.Categories = await _context.Categories
                .Where(c => c.Status == true)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View("Search", products);
        }

        // GET: Shop/Product/{id} - Chi tiết sản phẩm
        public async Task<IActionResult> Product(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                    .ThenInclude(v => v.VariantOptionValues)
                        .ThenInclude(vov => vov.ProductOptionValue)
                            .ThenInclude(pov => pov.ProductOption)
                .Include(p => p.ProductOptions)
                    .ThenInclude(po => po.Values)
                .FirstOrDefaultAsync(p => p.ProductID == id && p.Status == true);

            if (product == null)
            {
                return NotFound();
            }

            ViewData["Title"] = product.ProductName;
            ViewBag.ShowBreadcrumb = true;

            // Sản phẩm liên quan
            ViewBag.RelatedProducts = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id && p.Status == true)
                .Take(4)
                .ToListAsync();

            return View(product);
        }

        // GET: Shop/Promotions - Sản phẩm khuyến mãi
        public async Task<IActionResult> Promotions(int page = 1, int pageSize = 12)
        {
            // Giả định sản phẩm khuyến mãi là sản phẩm có variant giá thấp
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.ProductImages)
                .Include(p => p.Variants)
                .Where(p => p.Status == true && p.Variants.Any())
                .OrderBy(p => p.Variants.Min(v => v.UnitPrice));

            var totalItems = await query.CountAsync();
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewData["Title"] = "Sản phẩm khuyến mãi";

            return View("Index", products);
        }

        // API: Get Categories for menu
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Where(c => c.Status == true)
                .OrderBy(c => c.CategoryName)
                .Select(c => new { c.CategoryID, c.CategoryName })
                .ToListAsync();

            return Json(categories);
        }


    }
}
