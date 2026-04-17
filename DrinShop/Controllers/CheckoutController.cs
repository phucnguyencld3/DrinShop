using CloudinaryDotNet.Actions;
using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    [Authorize]
    public class CheckoutController : BaseController
    {
        private readonly DrinShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CheckoutController(DrinShopDbContext context, UserManager<ApplicationUser> userManager) : base(context)
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

        // GET: Checkout
        public async Task<IActionResult> Index()
        {
            var customerId = await GetCurrentCustomerIdAsync();
            if (customerId == 0)
            {
                TempData["Error"] = "Vui lòng đăng nhập để thanh toán!";
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin khách hàng
            var customer = await _context.Users
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .FirstOrDefaultAsync(u => u.UserId == customerId);

            if (customer == null)
            {
                TempData["Error"] = "Không tìm thấy thông tin khách hàng!";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy giỏ hàng
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
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

            if (cart == null || !cart.CartDetails.Any())
            {
                TempData["Error"] = "Giỏ hàng trống! Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }

            // ✅ Lấy danh sách tỉnh/thành phố - SỬA xử lý nullable
            var provinces = await _context.Provinces.OrderBy(p => p.Name).ToListAsync();
            var districts = customer.ProvinceCode.HasValue && customer.ProvinceCode > 0 ?
                await _context.Districts.Where(d => d.ProvinceCode == customer.ProvinceCode.Value).OrderBy(d => d.Name).ToListAsync() :
                new List<District>();
            var wards = customer.DistrictCode.HasValue && customer.DistrictCode > 0 ?
                await _context.Wards.Where(w => w.DistrictCode == customer.DistrictCode.Value).OrderBy(w => w.Name).ToListAsync() :
                new List<Ward>();

            // Tạo ViewModel
            var checkoutViewModel = new CheckoutViewModel
            {
                Customer = customer,
                Cart = cart,
                PaymentMethods = GetPaymentMethods(),
                ShippingMethods = GetShippingMethods(),

                // ✅ Điền thông tin có thể chỉnh sửa
                CustomerName = customer.FullName ?? "",
                CustomerPhone = customer.Phone ?? "",
                CustomerAddress = customer.Address ?? "",
                SelectedProvinceCode = customer.ProvinceCode ?? 0, 
                SelectedDistrictCode = customer.DistrictCode ?? 0, 
                SelectedWardCode = customer.WardCode ?? 0,

                // Dropdown data
                Provinces = provinces,
                Districts = districts,
                Wards = wards,

                DeliveryAddress = $"{customer.Address}, {customer.Ward?.Name}, {customer.District?.Name}, {customer.Province?.Name}"
            };

            return View(checkoutViewModel);
        }

        // ✅ AJAX method để lấy districts theo province
        [HttpGet]
        public async Task<IActionResult> GetDistricts(int provinceCode)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceCode == provinceCode)
                .OrderBy(d => d.Name)
                .Select(d => new { value = d.Code, text = d.Name })
                .ToListAsync();

            return Json(districts);
        }

        // ✅ AJAX method để lấy wards theo district
        [HttpGet]
        public async Task<IActionResult> GetWards(int districtCode)
        {
            var wards = await _context.Wards
                .Where(w => w.DistrictCode == districtCode)
                .OrderBy(w => w.Name)
                .Select(w => new { value = w.Code, text = w.Name })
                .ToListAsync();

            return Json(wards);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                // ✅ BỎ QUA VALIDATION CHO CÁC THUỘC TÍNH KHÔNG CẦN THIẾT
                ModelState.Remove(nameof(model.Cart));
                ModelState.Remove(nameof(model.Customer));
                ModelState.Remove(nameof(model.PaymentMethods));
                ModelState.Remove(nameof(model.ShippingMethods));
                ModelState.Remove(nameof(model.Provinces));
                ModelState.Remove(nameof(model.Districts));
                ModelState.Remove(nameof(model.Wards));

                // Validate các field quan trọng
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = "Thông tin không hợp lệ: " + string.Join(", ", errors) });
                }

                // Validate manual thêm
                if (string.IsNullOrWhiteSpace(model.CustomerName))
                {
                    return Json(new { success = false, message = "Vui lòng nhập họ tên!" });
                }

                if (string.IsNullOrWhiteSpace(model.CustomerPhone))
                {
                    return Json(new { success = false, message = "Vui lòng nhập số điện thoại!" });
                }

                if (string.IsNullOrWhiteSpace(model.CustomerAddress))
                {
                    return Json(new { success = false, message = "Vui lòng nhập địa chỉ chi tiết!" });
                }

                if (!model.SelectedProvinceCode.HasValue || model.SelectedProvinceCode <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn tỉnh/thành phố!" });
                }

                if (!model.SelectedDistrictCode.HasValue || model.SelectedDistrictCode <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn quận/huyện!" });
                }

                if (!model.SelectedWardCode.HasValue || model.SelectedWardCode <= 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn phường/xã!" });
                }

                // Kiểm tra giỏ hàng
                var cart = await _context.Carts
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Bundle)
                            .ThenInclude(b => b.BundleItems)
                                .ThenInclude(bi => bi.Variant)
                    .Include(c => c.CartDetails)
                        .ThenInclude(cd => cd.Variant)
                    .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

                if (cart == null || !cart.CartDetails.Any())
                {
                    return Json(new { success = false, message = "Giỏ hàng trống! Vui lòng thêm sản phẩm trước khi thanh toán." });
                }

                // ✅ Kiểm tra tồn kho trước khi đặt hàng
                foreach (var cartItem in cart.CartDetails)
                {
                    if (cartItem.VariantID.HasValue)
                    {
                        var variant = await _context.Variants
                            .Include(v => v.Product)
                            .FirstOrDefaultAsync(v => v.VariantID == cartItem.VariantID.Value);

                        if (variant == null || !variant.Status || !variant.Product.Status)
                        {
                            return Json(new { success = false, message = $"Sản phẩm {variant?.Product?.ProductName} đã ngừng kinh doanh!" });
                        }

                        if (variant.Stock < cartItem.Quantity)
                        {
                            return Json(new { success = false, message = $"Sản phẩm {variant.Product.ProductName} chỉ còn {variant.Stock} trong kho!" });
                        }
                    }
                    else if (cartItem.BundleID.HasValue)
                    {
                        var bundle = await _context.Bundles
                            .Include(b => b.BundleItems)
                                .ThenInclude(bi => bi.Variant)
                                    .ThenInclude(v => v.Product)
                            .FirstOrDefaultAsync(b => b.BundleId == cartItem.BundleID.Value);

                        if (bundle == null || !bundle.Status)
                        {
                            return Json(new { success = false, message = $"Combo {bundle?.Name} đã ngừng kinh doanh!" });
                        }

                        if (bundle.AvailableStock < cartItem.Quantity)
                        {
                            return Json(new { success = false, message = $"Combo {bundle.Name} chỉ còn {bundle.AvailableStock} trong kho!" });
                        }

                        // Kiểm tra tồn kho từng sản phẩm trong combo
                        foreach (var bundleItem in bundle.BundleItems)
                        {
                            if (bundleItem.Variant == null)
                            {
                                return Json(new { success = false, message = "Dữ liệu combo không hợp lệ!" });
                            }

                            var requiredQuantity = bundleItem.Quantity * cartItem.Quantity;
                            if (bundleItem.Variant.Stock < requiredQuantity)
                            {
                                return Json(new { success = false, message = $"Sản phẩm {bundleItem.Variant.Product?.ProductName} trong combo chỉ còn {bundleItem.Variant.Stock} trong kho!" });
                            }
                        }
                    }
                }

                // ✅ Lấy thông tin địa chỉ với validation
                Province province = null;
                District district = null;
                Ward ward = null;

                try
                {
                    if (model.SelectedProvinceCode.HasValue && model.SelectedProvinceCode > 0)
                    {
                        province = await _context.Provinces.FindAsync(model.SelectedProvinceCode.Value);
                    }

                    if (model.SelectedDistrictCode.HasValue && model.SelectedDistrictCode > 0)
                    {
                        district = await _context.Districts.FindAsync(model.SelectedDistrictCode.Value);
                    }

                    if (model.SelectedWardCode.HasValue && model.SelectedWardCode > 0)
                    {
                        ward = await _context.Wards.FindAsync(model.SelectedWardCode.Value);
                    }
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi kiểm tra địa chỉ: " + ex.Message });
                }

                if (province == null)
                {
                    return Json(new { success = false, message = "Tỉnh/thành phố không hợp lệ!" });
                }

                if (district == null)
                {
                    return Json(new { success = false, message = "Quận/huyện không hợp lệ!" });
                }

                if (ward == null)
                {
                    return Json(new { success = false, message = "Phường/xã không hợp lệ!" });
                }

                // Xây dựng địa chỉ đầy đủ
                var fullAddress = $"{model.CustomerAddress.Trim()}, {ward.Name}, {district.Name}, {province.Name}";

                // Chuyển đổi phương thức thanh toán
                var paymentMethod = ConvertPaymentMethod(model.SelectedPaymentMethod);

                // Tính phí giao hàng
                decimal shippingFee = model.SelectedShippingMethod == "express" ? 30000 : 0;

                // ✅ Tạo hóa đơn với validation data
                var invoice = new Invoice
                {
                    CustomerId = customerId,
                    TotalAmount = Math.Max(0, cart.TotalAmount),
                    PayMethod = paymentMethod,
                    ShippingFee = Math.Max(0, shippingFee),
                    ShipAddress = fullAddress.Length > 500 ? fullAddress.Substring(0, 500) : fullAddress,
                    CancelReason = string.Empty, // ✅ Explicit empty string
                    Note = string.IsNullOrWhiteSpace(model.Notes) ? string.Empty :
                           (model.Notes.Trim().Length > 1000 ? model.Notes.Trim().Substring(0, 1000) : model.Notes.Trim()),
                    CreatedAt = DateTime.Now,
                    OrderStatus = OrderStatus.Pending,
                    IsDeleted = false
                };



                try
                {
                    // ✅ KIỂM TRA DỮ LIỆU TRƯỚC KHI LƯU
                    // Debug thông tin invoice
                    if (invoice.CustomerId <= 0)
                    {
                        return Json(new { success = false, message = "Customer ID không hợp lệ!" });
                    }

                    if (invoice.TotalAmount < 0)
                    {
                        return Json(new { success = false, message = "Tổng tiền không hợp lệ!" });
                    }

                    if (string.IsNullOrEmpty(invoice.ShipAddress))
                    {
                        return Json(new { success = false, message = "Địa chỉ giao hàng không hợp lệ!" });
                    }

                    // Kiểm tra customer có tồn tại không
                    var customerExists = await _context.Users.AnyAsync(u => u.UserId == customerId);
                    if (!customerExists)
                    {
                        return Json(new { success = false, message = "Không tìm thấy thông tin khách hàng trong hệ thống!" });
                    }

                    _context.Invoices.Add(invoice);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    // ✅ HIỂN THỊ CHI TIẾT LỖI DATABASE
                    var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                    var sqlException = dbEx.InnerException?.InnerException?.Message ?? "";

                    var errorMessage = $"Lỗi database khi tạo hóa đơn:\n" +
                                      $"DbUpdate: {dbEx.Message}\n" +
                                      $"Inner: {innerException}\n" +
                                      $"SQL: {sqlException}";

                    return Json(new { success = false, message = errorMessage });
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi tạo hóa đơn: " + ex.Message + "\nInner: " + (ex.InnerException?.Message ?? "") });
                }


                // ✅ Tạo chi tiết hóa đơn với validation
                try
                {
                    foreach (var cartItem in cart.CartDetails)
                    {
                        var invoiceDetail = new InvoiceDetail
                        {
                            InvoiceID = invoice.InvoiceID,
                            VariantID = cartItem.VariantID,
                            BundleID = cartItem.BundleID,
                            Quantity = Math.Max(1, cartItem.Quantity), // Đảm bảo >= 1
                            UnitPrice = Math.Max(0, cartItem.UnitPrice), // Đảm bảo >= 0
                            TotalPrice = Math.Max(0, cartItem.TotalPrice) // Đảm bảo >= 0
                        };

                        _context.InvoiceDetails.Add(invoiceDetail);
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Rollback invoice nếu có lỗi
                    try
                    {
                        _context.Invoices.Remove(invoice);
                        await _context.SaveChangesAsync();
                    }
                    catch { }

                    return Json(new { success = false, message = "Lỗi khi tạo chi tiết hóa đơn: " + ex.Message });
                }

                // ✅ Cập nhật tồn kho
                try
                {
                    foreach (var cartItem in cart.CartDetails)
                    {
                        if (cartItem.VariantID.HasValue)
                        {
                            var variant = await _context.Variants.FindAsync(cartItem.VariantID.Value);
                            if (variant != null)
                            {
                                variant.Stock = Math.Max(0, variant.Stock - cartItem.Quantity);
                            }
                        }
                        else if (cartItem.BundleID.HasValue)
                        {
                            var bundle = await _context.Bundles
                                .Include(b => b.BundleItems)
                                    .ThenInclude(bi => bi.Variant)
                                .FirstOrDefaultAsync(b => b.BundleId == cartItem.BundleID.Value);

                            if (bundle != null && bundle.BundleItems != null)
                            {
                                foreach (var bundleItem in bundle.BundleItems)
                                {
                                    if (bundleItem.Variant != null)
                                    {
                                        var requiredQuantity = bundleItem.Quantity * cartItem.Quantity;
                                        bundleItem.Variant.Stock = Math.Max(0, bundleItem.Variant.Stock - requiredQuantity);
                                    }
                                }
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật tồn kho: " + ex.Message });
                }

                // ✅ Cập nhật thông tin khách hàng và xóa giỏ hàng
                try
                {
                    var customer = await _context.Users.FindAsync(customerId);
                    if (customer != null)
                    {
                        customer.FullName = model.CustomerName.Trim();
                        customer.Phone = model.CustomerPhone.Trim();
                        customer.Address = model.CustomerAddress.Trim();
                        customer.ProvinceCode = model.SelectedProvinceCode.Value;
                        customer.DistrictCode = model.SelectedDistrictCode.Value;
                        customer.WardCode = model.SelectedWardCode.Value;
                    }

                    // Xóa giỏ hàng
                    _context.CartDetails.RemoveRange(cart.CartDetails);
                    cart.Status = "Completed";
                    cart.TotalAmount = 0;

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    return Json(new { success = false, message = "Lỗi khi cập nhật thông tin: " + ex.Message });
                }

                // Trả về kết quả thành công
                return Json(new
                {
                    success = true,
                    message = "Đặt hàng thành công! Cảm ơn bạn đã tin tưởng DrinShop.",
                    invoiceId = invoice.InvoiceID,
                    invoiceCode = $"DH{DateTime.Now:yyyyMMdd}{invoice.InvoiceID:D6}",
                    totalAmount = cart.TotalAmount + shippingFee,
                    redirectUrl = $"/Checkout/OrderSuccess/{invoice.InvoiceID}"
                });
            }
            catch (DbUpdateException dbEx)
            {
                // Log chi tiết lỗi database
                var innerException = dbEx.InnerException?.Message ?? dbEx.Message;
                return Json(new { success = false, message = $"Lỗi cơ sở dữ liệu: {innerException}" });
            }
            catch (Exception ex)
            {
                // Log lỗi chung
                return Json(new { success = false, message = $"Lỗi không mong muốn: {ex.Message}" });
            }
        }




        // GET: Checkout/OrderSuccess/5
        public async Task<IActionResult> OrderSuccess(int id)
        {
            var customerId = await GetCurrentCustomerIdAsync();
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)  
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Bundle)
                .FirstOrDefaultAsync(i => i.InvoiceID == id && i.CustomerId == customerId);


            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index", "Home");
            }

            return View(invoice);
        }

        private PayMethod ConvertPaymentMethod(string paymentMethod)
        {
            return paymentMethod switch
            {
                "cod" => PayMethod.Cash,
                "bank_transfer" => PayMethod.DebitCard,
                "momo" => PayMethod.MobilePayment,
                "zalopay" => PayMethod.MobilePayment,
                _ => PayMethod.Cash
            };
        }

        private List<PaymentMethodOption> GetPaymentMethods()
        {
            return new List<PaymentMethodOption>
            {
                new PaymentMethodOption { Value = "cod", Text = "Thanh toán khi nhận hàng (COD)", Description = "Thanh toán bằng tiền mặt khi nhận hàng", Icon = "fas fa-money-bill-wave" },
                new PaymentMethodOption { Value = "bank_transfer", Text = "Chuyển khoản ngân hàng", Description = "Chuyển khoản qua ATM/Internet Banking", Icon = "fas fa-university" },
                new PaymentMethodOption { Value = "momo", Text = "Ví MoMo", Description = "Thanh toán qua ví điện tử MoMo", Icon = "fab fa-cc-visa" },
                new PaymentMethodOption { Value = "zalopay", Text = "ZaloPay", Description = "Thanh toán qua ví ZaloPay", Icon = "fas fa-mobile-alt" }
            };
        }

        private List<ShippingMethodOption> GetShippingMethods()
        {
            return new List<ShippingMethodOption>
            {
                new ShippingMethodOption { Value = "standard", Text = "Giao hàng tiêu chuẩn", Description = "Giao trong 2-3 ngày làm việc", Fee = 0, Icon = "fas fa-truck" },
                //new ShippingMethodOption { Value = "express", Text = "Giao hàng nhanh", Description = "Giao trong 24h", Fee = 30000, Icon = "fas fa-tachometer-alt" }
            };
        }

        // Helper classes
        public class PaymentMethodOption
        {
            public string Value { get; set; }
            public string Text { get; set; }
            public string Description { get; set; }
            public string Icon { get; set; }
        }

        public class ShippingMethodOption
        {
            public string Value { get; set; }
            public string Text { get; set; }
            public string Description { get; set; }
            public decimal Fee { get; set; }
            public string Icon { get; set; }
        }


        private async Task<string> ValidateInvoiceData(Invoice invoice)
        {
            try
            {
                // Kiểm tra customer tồn tại
                var customer = await _context.Users.FindAsync(invoice.CustomerId);
                if (customer == null)
                {
                    return "Customer không tồn tại";
                }

                // Kiểm tra ràng buộc dữ liệu
                if (invoice.TotalAmount < 0)
                {
                    return "TotalAmount phải >= 0";
                }

                if (invoice.ShippingFee < 0)
                {
                    return "ShippingFee phải >= 0";
                }

                if (string.IsNullOrEmpty(invoice.ShipAddress))
                {
                    return "ShipAddress không được null";
                }

                if (invoice.ShipAddress.Length > 500)
                {
                    return "ShipAddress quá dài (>500 ký tự)";
                }

                if (!string.IsNullOrEmpty(invoice.Note) && invoice.Note.Length > 1000)
                {
                    return "Note quá dài (>1000 ký tự)";
                }

                return null; // Không có lỗi
            }
            catch (Exception ex)
            {
                return $"Lỗi validation: {ex.Message}";
            }
        }

    }
}
