using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DrinShop.Models;

namespace DrinShop.Controllers
{
    public class BundleController : Controller
    {
        private readonly DrinShopDbContext _context;

        public BundleController(DrinShopDbContext context)
        {
            _context = context;
        }

        // GET: Combo - Danh sách combo cho khách hàng
        public async Task<IActionResult> Index(string searchTerm, string sortBy = "name")
        {
            // ✅ SỬA: Lấy tất cả combo trước, sau đó filter trong memory
            var combosQuery = _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .Where(b => b.Status); // Chỉ filter Status trong database

            // Tìm kiếm trong database trước
            if (!string.IsNullOrEmpty(searchTerm))
            {
                combosQuery = combosQuery.Where(b =>
                    b.Name.Contains(searchTerm) ||
                    b.Description.Contains(searchTerm) ||
                    b.Code.Contains(searchTerm));
                ViewBag.SearchTerm = searchTerm;
            }

            // ✅ Lấy data về memory và filter AvailableStock
            var allCombos = await combosQuery.ToListAsync();
            var availableCombos = allCombos.Where(b => b.AvailableStock > 0).ToList();

            // ✅ Sắp xếp trong memory
            var sortedCombos = sortBy.ToLower() switch
            {
                "price_asc" => availableCombos.OrderBy(b => b.FinalPrice).ToList(),
                "price_desc" => availableCombos.OrderByDescending(b => b.FinalPrice).ToList(),
                "discount" => availableCombos.OrderByDescending(b => b.DiscountPercentage).ToList(),
                "newest" => availableCombos.OrderByDescending(b => b.CreatedDate).ToList(),
                _ => availableCombos.OrderBy(b => b.Name).ToList()
            };

            ViewBag.SortBy = sortBy;
            return View(sortedCombos);
        }

        // GET: Combo/Details/5 - Chi tiết combo
        public async Task<IActionResult> Details(int id)
        {
            var combo = await _context.Bundles
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(b => b.BundleId == id && b.Status);

            if (combo == null || combo.AvailableStock <= 0)
            {
                TempData["ErrorMessage"] = "Không tìm thấy combo này hoặc combo đã hết hàng!";
                return RedirectToAction(nameof(Index));
            }

            return View(combo);
        }

        // POST: Combo/AddToCart - Thêm combo vào giỏ hàng  
        [HttpPost]
        public async Task<IActionResult> AddToCart(int bundleId, int quantity = 1)
        {
            try
            {
                var combo = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                    .FirstOrDefaultAsync(b => b.BundleId == bundleId && b.Status);

                if (combo == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy combo!" });
                }

                if (combo.AvailableStock < quantity)
                {
                    return Json(new { success = false, message = $"Combo chỉ còn {combo.AvailableStock} phần!" });
                }

                // Kiểm tra đăng nhập
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng đăng nhập để thêm vào giỏ hàng!",
                        requireLogin = true
                    });
                }

                // ✅ SỬA: Lấy thông tin customer qua UserManager
                var userName = User.Identity.Name;
                var customer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userName);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng!" });
                }

                // Tìm hoặc tạo giỏ hàng
                var cart = await _context.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.UserId && c.Status == "Active");

                if (cart == null)
                {
                    cart = new Cart
                    {
                        CustomerId = customer.UserId,
                        Status = "Active",
                        CreateDate = DateTime.Now,
                        TotalAmount = 0
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Kiểm tra combo đã có trong giỏ chưa
                var existingCartDetail = await _context.CartDetails
                    .FirstOrDefaultAsync(cd => cd.CartID == cart.CartID && cd.BundleID == bundleId);

                if (existingCartDetail != null)
                {
                    var totalQuantityAfterAdd = existingCartDetail.Quantity + quantity;
                    if (totalQuantityAfterAdd > combo.AvailableStock)
                    {
                        return Json(new
                        {
                            success = false,
                            message = $"Không thể thêm {quantity} combo. Hiện có {existingCartDetail.Quantity}, tối đa {combo.AvailableStock}!"
                        });
                    }

                    existingCartDetail.Quantity += quantity;
                    existingCartDetail.UnitPrice = combo.FinalPrice;
                    existingCartDetail.TotalPrice = existingCartDetail.Quantity * combo.FinalPrice;
                    _context.Update(existingCartDetail);
                }
                else
                {
                    var cartDetail = new CartDetail
                    {
                        CartID = cart.CartID,
                        BundleID = bundleId,
                        Quantity = quantity,
                        UnitPrice = combo.FinalPrice,
                        TotalPrice = quantity * combo.FinalPrice
                    };
                    _context.CartDetails.Add(cartDetail);
                }

                await _context.SaveChangesAsync();

                // Tính lại tổng tiền và số lượng
                var cartDetails = await _context.CartDetails
                    .Where(cd => cd.CartID == cart.CartID)
                    .ToListAsync();

                cart.TotalAmount = cartDetails.Sum(cd => cd.TotalPrice);
                var cartItemCount = cartDetails.Sum(cd => cd.Quantity);

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Đã thêm {quantity} combo '{combo.Name}' vào giỏ hàng!",
                    cartCount = cartItemCount,
                    cartTotal = cart.TotalAmount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // ✅ THÊM: API để lấy số lượng giỏ hàng
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    return Json(new { count = 0 });
                }

                var userName = User.Identity.Name;
                var customer = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == userName);

                if (customer == null)
                {
                    return Json(new { count = 0 });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.CustomerId == customer.UserId && c.Status == "Active");

                if (cart == null)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartDetails
                    .Where(cd => cd.CartID == cart.CartID)
                    .SumAsync(cd => cd.Quantity);

                return Json(new { count = count });
            }
            catch (Exception)
            {
                return Json(new { count = 0 });
            }
        }
    }
}
