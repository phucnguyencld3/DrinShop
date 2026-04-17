using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    [Authorize]
    public class InvoiceController : BaseController
    {
        private readonly DrinShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public InvoiceController(DrinShopDbContext context, UserManager<ApplicationUser> userManager) : base(context)
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

        // GET: Invoice/MyInvoices
        public async Task<IActionResult> MyInvoices(int page = 1, int pageSize = 10)
        {
            var customerId = await GetCurrentCustomerIdAsync();
            if (customerId == 0)
            {
                TempData["Error"] = "Vui lòng đăng nhập để xem đơn hàng!";
                return RedirectToAction("Login", "Account");
            }

            var totalInvoices = await _context.Invoices
                .Where(i => i.CustomerId == customerId && !i.IsDeleted)
                .CountAsync();

            var invoices = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Bundle)
                .Where(i => i.CustomerId == customerId && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalInvoices / pageSize);
            ViewBag.TotalInvoices = totalInvoices;

            return View(invoices);
        }

        // GET: Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var customerId = await GetCurrentCustomerIdAsync();
            if (customerId == 0)
            {
                TempData["Error"] = "Vui lòng đăng nhập!";
                return RedirectToAction("Login", "Account");
            }

            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Bundle)
                        .ThenInclude(b => b.BundleItems)
                            .ThenInclude(bi => bi.Variant)
                                .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(i => i.InvoiceID == id && i.CustomerId == customerId && !i.IsDeleted);

            if (invoice == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("MyInvoices");
            }

            return View(invoice);
        }

        // POST: Invoice/CancelRequest - CHỈ CHO PHÉP HỦY KHI PENDING
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelRequest(int id, string reason)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceID == id &&
                                            i.CustomerId == customerId &&
                                            !i.IsDeleted);

                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // CHỈ CHO PHÉP HỦY KHI Ở TRẠNG THÁI PENDING
                if (invoice.OrderStatus != OrderStatus.Pending)
                {
                    return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang chờ xác nhận!" });
                }

                invoice.OrderStatus = OrderStatus.CancelRequested;
                invoice.CancelReason = reason?.Trim() ?? "Khách hàng yêu cầu hủy";

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Yêu cầu hủy đơn hàng đã được gửi thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại!" });
            }
        }

        // POST: Invoice/ConfirmReceived - CHỈ CHO PHÉP KHI SHIPPED
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmReceived(int id)
        {
            try
            {
                var customerId = await GetCurrentCustomerIdAsync();
                if (customerId == 0)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập!" });
                }

                var invoice = await _context.Invoices
                    .FirstOrDefaultAsync(i => i.InvoiceID == id &&
                                            i.CustomerId == customerId &&
                                            !i.IsDeleted);

                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                // CHỈ CHO PHÉP XÁC NHẬN NHẬN HÀNG KHI Ở TRẠNG THÁI SHIPPED
                if (invoice.OrderStatus != OrderStatus.Shipped)
                {
                    return Json(new { success = false, message = "Chỉ có thể xác nhận nhận hàng khi đơn hàng đang được giao!" });
                }

                invoice.OrderStatus = OrderStatus.Completed;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Cảm ơn bạn! Đơn hàng đã được hoàn tất." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại!" });
            }
        }

       
    }
}
