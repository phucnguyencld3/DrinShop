using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    [Authorize]
    public class CartController : BaseController
    {
        private readonly DrinShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(DrinShopDbContext context, UserManager<ApplicationUser> userManager) : base(context)
        {
            _context = context;
            _userManager = userManager;
        }

        private async Task<int> GetCurrentCustomerIdAsync()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
                return 0;

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.ApplicationUserId == currentUser.Id);

            return user?.UserId ?? 0;
        }

        private async Task<Cart> GetOrCreateCartAsync(int customerId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Bundle)
                        .ThenInclude(b => b.BundleItems)
                            .ThenInclude(bi => bi.Variant)
                                .ThenInclude(v => v.Product)
                                    .ThenInclude(p => p.ProductImages)
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

            if (cart == null)
            {
                cart = new Cart
                {
                    CustomerId = customerId,
                    CreateDate = DateTime.Now,
                    TotalAmount = 0,
                    Status = "Active"
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }


        private async Task UpdateCartTotalAsync(int cartId)
        {
            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                .FirstOrDefaultAsync(c => c.CartID == cartId);

            if (cart != null)
            {
                cart.TotalAmount = cart.CartDetails.Sum(cd => cd.TotalPrice);
                await _context.SaveChangesAsync();
            }
        }

        // GET: Cart
        public async Task<IActionResult> Index()
        {
            var customerId = await GetCurrentCustomerIdAsync();
            if (customerId == 0)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem giỏ hàng!";
                return RedirectToAction("Login", "Account");
            }

            var cart = await _context.Carts
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Bundle)
                        .ThenInclude(b => b.BundleItems)
                            .ThenInclude(bi => bi.Variant)
                                .ThenInclude(v => v.Product)
                                    .ThenInclude(p => p.ProductImages)
                .Include(c => c.CartDetails)
                    .ThenInclude(cd => cd.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .Include(c => c.Customer)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

            if (cart == null)
            {
                cart = await GetOrCreateCartAsync(customerId);
            }

            return View(cart);
        }


        // GET: Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartDetails
                    .Where(cd => cd.Cart.CustomerId == customerId && cd.Cart.Status == "Active")
                    .SumAsync(cd => cd.Quantity);

                return Json(new { count = count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        // POST: Cart/AddVariant
        [HttpPost]
        public async Task<IActionResult> AddVariant([FromBody] AddVariantRequest request)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                if (request.VariantId <= 0 || request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var variant = await _context.Variants
                    .Include(v => v.Product)
                    .FirstOrDefaultAsync(v => v.VariantID == request.VariantId);

                if (variant == null || !variant.Status || !variant.Product.Status)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại hoặc đã ngừng kinh doanh!" });
                }

                if (request.Quantity > variant.Stock)
                {
                    return Json(new { success = false, message = $"Chỉ còn {variant.Stock} sản phẩm trong kho!" });
                }

                var cart = await GetOrCreateCartAsync(customerId);

                var existingItem = await _context.CartDetails
                    .FirstOrDefaultAsync(cd => cd.CartID == cart.CartID && cd.VariantID == request.VariantId);

                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + request.Quantity;
                    if (newQuantity > variant.Stock)
                    {
                        return Json(new { success = false, message = $"Tổng số lượng vượt quá tồn kho ({variant.Stock})" });
                    }

                    existingItem.Quantity = newQuantity;
                    existingItem.TotalPrice = newQuantity * existingItem.UnitPrice;
                }
                else
                {
                    var cartDetail = new CartDetail
                    {
                        CartID = cart.CartID,
                        VariantID = request.VariantId,
                        Quantity = request.Quantity,
                        UnitPrice = variant.UnitPrice,
                        TotalPrice = request.Quantity * variant.UnitPrice,
                        Status = "Active"
                    };

                    _context.CartDetails.Add(cartDetail);
                }

                await _context.SaveChangesAsync();
                await UpdateCartTotalAsync(cart.CartID);

                var itemsCount = await _context.CartDetails
                    .Where(cd => cd.CartID == cart.CartID)
                    .SumAsync(cd => cd.Quantity);

                return Json(new
                {
                    success = true,
                    message = "Đã thêm sản phẩm vào giỏ hàng!",
                    cartCount = itemsCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm vào giỏ hàng!" });
            }
        }

        // POST: Cart/AddBundle
        [HttpPost]
        public async Task<IActionResult> AddBundle([FromBody] AddBundleRequest request)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                if (request.BundleId <= 0 || request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                var bundle = await _context.Bundles
                    .Include(b => b.BundleItems)
                        .ThenInclude(bi => bi.Variant)
                    .FirstOrDefaultAsync(b => b.BundleId == request.BundleId);

                if (bundle == null || !bundle.Status)
                {
                    return Json(new { success = false, message = "Combo không tồn tại hoặc đã ngừng kinh doanh!" });
                }

                if (request.Quantity > bundle.AvailableStock)
                {
                    return Json(new { success = false, message = $"Chỉ còn {bundle.AvailableStock} combo trong kho!" });
                }

                var cart = await GetOrCreateCartAsync(customerId);

                var existingItem = await _context.CartDetails
                    .FirstOrDefaultAsync(cd => cd.CartID == cart.CartID && cd.BundleID == request.BundleId);

                if (existingItem != null)
                {
                    var newQuantity = existingItem.Quantity + request.Quantity;
                    if (newQuantity > bundle.AvailableStock)
                    {
                        return Json(new { success = false, message = $"Tổng số lượng vượt quá tồn kho ({bundle.AvailableStock})" });
                    }

                    existingItem.Quantity = newQuantity;
                    existingItem.TotalPrice = newQuantity * existingItem.UnitPrice;
                }
                else
                {
                    var cartDetail = new CartDetail
                    {
                        CartID = cart.CartID,
                        BundleID = request.BundleId,
                        Quantity = request.Quantity,
                        UnitPrice = bundle.FinalPrice,
                        TotalPrice = request.Quantity * bundle.FinalPrice,
                        Status = "Active"
                    };

                    _context.CartDetails.Add(cartDetail);
                }

                await _context.SaveChangesAsync();
                await UpdateCartTotalAsync(cart.CartID);

                var itemsCount = await _context.CartDetails
                    .Where(cd => cd.CartID == cart.CartID)
                    .SumAsync(cd => cd.Quantity);

                return Json(new
                {
                    success = true,
                    message = $"Đã thêm {request.Quantity} combo \"{bundle.Name}\" vào giỏ hàng!",
                    cartCount = itemsCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi thêm combo vào giỏ hàng!" });
            }
        }


        // POST: Cart/UpdateQuantity
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();

                var cartDetail = await _context.CartDetails
                    .Include(cd => cd.Cart)
                    .Include(cd => cd.Variant)
                    .Include(cd => cd.Bundle)
                    .FirstOrDefaultAsync(cd => cd.CartDetailID == request.CartDetailId &&
                                              cd.Cart.CustomerId == customerId);

                if (cartDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng!" });
                }

                if (request.Quantity <= 0)
                {
                    _context.CartDetails.Remove(cartDetail);
                }
                else
                {
                    if (cartDetail.VariantID.HasValue)
                    {
                        if (request.Quantity > cartDetail.Variant.Stock)
                        {
                            return Json(new { success = false, message = $"Chỉ còn {cartDetail.Variant.Stock} sản phẩm trong kho!" });
                        }
                    }

                    cartDetail.Quantity = request.Quantity;
                    cartDetail.TotalPrice = request.Quantity * cartDetail.UnitPrice;
                }

                await _context.SaveChangesAsync();
                await UpdateCartTotalAsync(cartDetail.CartID);

                var itemsCount = await _context.CartDetails
                    .Where(cd => cd.CartID == cartDetail.CartID)
                    .SumAsync(cd => cd.Quantity);

                var cartTotal = await _context.Carts
                    .Where(c => c.CartID == cartDetail.CartID)
                    .Select(c => c.TotalAmount)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    cartCount = itemsCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // POST: Cart/RemoveItem
        [HttpPost]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();

                var cartDetail = await _context.CartDetails
                    .Include(cd => cd.Cart)
                    .FirstOrDefaultAsync(cd => cd.CartDetailID == request.CartDetailId &&
                                              cd.Cart.CustomerId == customerId);

                if (cartDetail == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm!" });
                }

                var cartId = cartDetail.CartID;
                _context.CartDetails.Remove(cartDetail);

                await _context.SaveChangesAsync();
                await UpdateCartTotalAsync(cartId);

                var itemsCount = await _context.CartDetails
                    .Where(cd => cd.CartID == cartId)
                    .SumAsync(cd => cd.Quantity);

                var cartTotal = await _context.Carts
                    .Where(c => c.CartID == cartId)
                    .Select(c => c.TotalAmount)
                    .FirstOrDefaultAsync();

                return Json(new
                {
                    success = true,
                    message = "Đã xóa sản phẩm khỏi giỏ hàng!",
                    cartCount = itemsCount,
                    cartTotal = cartTotal
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // POST: Cart/Clear
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();

                var cart = await _context.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

                if (cart == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy giỏ hàng!" });
                }

                _context.CartDetails.RemoveRange(cart.CartDetails);
                cart.TotalAmount = 0;

                await _context.SaveChangesAsync();

                TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!";
                return Json(new { success = true, cartCount = 0 });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }

        // POST: Cart/Checkout - Kiểm tra thông tin trước khi thanh toán
        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!", redirectUrl = "/Account/Login" });
                }

                // ✅ KIỂM TRA THÔNG TIN CUSTOMER
                var customer = await _context.Users
                    .FirstOrDefaultAsync(u => u.UserId == customerId);

                if (customer == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng!" });
                }

                // ✅ KIỂM TRA CÁC THÔNG TIN CẦN THIẾT CHO ĐẶT HÀNG
                bool needCompleteProfile = customer.IsFirstLogin ||
                                          string.IsNullOrEmpty(customer.Phone) ||
                                          string.IsNullOrEmpty(customer.Address) ||
                                          customer.ProvinceCode == 0;

                if (needCompleteProfile)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng bổ sung thông tin giao hàng trước khi đặt hàng!",
                        redirectUrl = "/Account/CompleteProfile?returnUrl=" + Uri.EscapeDataString("/Cart")
                    });
                }

                // ✅ KIỂM TRA GIỎ HÀNG
                var cart = await _context.Carts
                    .Include(c => c.CartDetails)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

                if (cart == null || !cart.CartDetails.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống!" });
                }

                // ✅ CHUYỂN ĐẾN TRANG CHECKOUT (sẽ tạo sau)
                return Json(new
                {
                    success = true,
                    message = "Chuyển đến trang thanh toán...",
                    redirectUrl = "/Checkout"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi!" });
            }
        }
        // Request DTOs
        public class AddVariantRequest
        {
            public int VariantId { get; set; }
            public int Quantity { get; set; }
        }

        public class UpdateQuantityRequest
        {
            public int CartDetailId { get; set; }
            public int Quantity { get; set; }
        }

        public class RemoveItemRequest
        {
            public int CartDetailId { get; set; }
        }
        public class AddBundleRequest
        {
            public int BundleId { get; set; }
            public int Quantity { get; set; }
        }

    }
}

