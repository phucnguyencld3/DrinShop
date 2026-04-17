using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    [Route("admin/dashboard")]
    public class DashboardController : BaseAdminController
    {
        private readonly DrinShopDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(DrinShopDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Route("")]
        [Route("index")]
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Dashboard";

            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            // ✅ THỐNG KÊ INVOICE CHI TIẾT
            var invoiceStats = new
            {
                // Thống kê tổng quan
                TotalInvoices = await _context.Invoices.Where(i => !i.IsDeleted).CountAsync(),
                TodayInvoices = await _context.Invoices
                    .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value.Date == today && !i.IsDeleted)
                    .CountAsync(),
                ThisMonthInvoices = await _context.Invoices
                    .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value >= thisMonth && !i.IsDeleted)
                    .CountAsync(),
                LastMonthInvoices = await _context.Invoices
                    .Where(i => i.CreatedAt.HasValue && i.CreatedAt.Value >= lastMonth && i.CreatedAt.Value < thisMonth && !i.IsDeleted)
                    .CountAsync(),

                // Thống kê theo trạng thái
                PendingInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Pending && !i.IsDeleted)
                    .CountAsync(),
                ConfirmedInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Confirmed && !i.IsDeleted)
                    .CountAsync(),
                ShippedInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Shipped && !i.IsDeleted)
                    .CountAsync(),
                CompletedInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Completed && !i.IsDeleted)
                    .CountAsync(),
                CancelledInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Cancelled && !i.IsDeleted)
                    .CountAsync(),
                CancelRequestedInvoices = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.CancelRequested && !i.IsDeleted)
                    .CountAsync(),

                // Thống kê doanh thu
                TotalRevenue = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Completed && !i.IsDeleted)
                    .SumAsync(i => i.TotalAmount + i.ShippingFee),
                TodayRevenue = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Completed && !i.IsDeleted &&
                               i.CreatedAt.HasValue && i.CreatedAt.Value.Date == today)
                    .SumAsync(i => i.TotalAmount + i.ShippingFee),
                ThisMonthRevenue = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Completed && !i.IsDeleted &&
                               i.CreatedAt.HasValue && i.CreatedAt.Value >= thisMonth)
                    .SumAsync(i => i.TotalAmount + i.ShippingFee),

                // Thống kê theo phương thức thanh toán
                CodInvoices = await _context.Invoices
                    .Where(i => i.PayMethod == PayMethod.Cash && !i.IsDeleted)
                    .CountAsync(),
                OnlinePaymentInvoices = await _context.Invoices
                    .Where(i => i.PayMethod != PayMethod.Cash && !i.IsDeleted)
                    .CountAsync(),

                // Thống kê trung bình
                AverageOrderValue = await _context.Invoices
                    .Where(i => i.OrderStatus == OrderStatus.Completed && !i.IsDeleted)
                    .AverageAsync(i => (double?)(i.TotalAmount + i.ShippingFee)) ?? 0
            };

            // ✅ THỐNG KÊ SẢN PHẨM VÀ KHO
            var productStats = new
            {
                TotalProducts = await _context.Products.CountAsync(),
                ActiveProducts = await _context.Products.CountAsync(p => p.Status),
                InactiveProducts = await _context.Products.CountAsync(p => !p.Status),

                TotalVariants = await _context.Variants.CountAsync(),
                ActiveVariants = await _context.Variants.CountAsync(v => v.Status),
                VariantsLowStock = await _context.Variants.CountAsync(v => v.Stock <= 5 && v.Status),
                VariantsOutOfStock = await _context.Variants.CountAsync(v => v.Stock == 0 && v.Status),
                TotalStock = await _context.Variants.Where(v => v.Status).SumAsync(v => v.Stock),

                TotalBundles = await _context.Bundles.CountAsync(),
                ActiveBundles = await _context.Bundles.CountAsync(b => b.Status),
                BundlesWithoutItems = await _context.Bundles.CountAsync(b => !b.BundleItems.Any()),
                BundlesWithoutImages = await _context.Bundles.CountAsync(b => string.IsNullOrEmpty(b.ImageUrl))
            };

            // ✅ THỐNG KÊ KHÁCH HÀNG
            var customerStats = new
            {
                TotalCustomers = await _context.Users.CountAsync(),
                NewCustomersThisMonth = await _context.Users
                    .Where(u => u.RegisterDate.HasValue && u.RegisterDate.Value >= thisMonth)
                    .CountAsync(),
                ActiveCustomers = await _context.Invoices
                    .Where(i => !i.IsDeleted)
                    .Select(i => i.CustomerId)
                    .Distinct()
                    .CountAsync()
            };

            // ✅ RECENT INVOICES CHO DASHBOARD
            var recentInvoices = await _context.Invoices
                .Include(i => i.Customer)
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .Take(10)
                .Select(i => new
                {
                    i.InvoiceID,
                    CustomerName = i.Customer.FullName,
                    i.TotalAmount,
                    i.ShippingFee,
                    i.OrderStatus,
                    i.CreatedAt
                })
                .ToListAsync();

            // Gộp tất cả stats
            var allStats = new
            {
                // Invoice stats
                invoiceStats.TotalInvoices,
                invoiceStats.TodayInvoices,
                invoiceStats.ThisMonthInvoices,
                invoiceStats.PendingInvoices,
                invoiceStats.ConfirmedInvoices,
                invoiceStats.ShippedInvoices,
                invoiceStats.CompletedInvoices,
                invoiceStats.CancelledInvoices,
                invoiceStats.CancelRequestedInvoices,
                invoiceStats.TotalRevenue,
                invoiceStats.TodayRevenue,
                invoiceStats.ThisMonthRevenue,
                invoiceStats.CodInvoices,
                invoiceStats.OnlinePaymentInvoices,
                invoiceStats.AverageOrderValue,

                // Product stats
                productStats.TotalProducts,
                productStats.ActiveProducts,
                productStats.TotalVariants,
                productStats.ActiveVariants,
                productStats.VariantsLowStock,
                productStats.VariantsOutOfStock,
                productStats.TotalStock,
                productStats.TotalBundles,
                productStats.ActiveBundles,
                productStats.BundlesWithoutItems,
                productStats.BundlesWithoutImages,

                // Customer stats
                customerStats.TotalCustomers,
                customerStats.NewCustomersThisMonth,
                customerStats.ActiveCustomers,

                // Calculated fields
                MonthlyGrowth = invoiceStats.ThisMonthInvoices - invoiceStats.LastMonthInvoices,
                ProductsWithVariants = await _context.Products.CountAsync(p => p.Variants.Any()),
                BundlesWithoutPrice = 0 // Sẽ tính toán ở client nếu cần
            };

            ViewBag.Stats = allStats;
            ViewBag.RecentInvoices = recentInvoices;

            return View();
        }
    }
}
