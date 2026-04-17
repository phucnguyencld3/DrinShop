using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class InvoiceController : BaseAdminController
    {
        private readonly DrinShopDbContext _context;

        public InvoiceController(DrinShopDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Invoice
        public async Task<IActionResult> Index(string status = "", string search = "", int page = 1, int pageSize = 20)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .Where(i => !i.IsDeleted);

            // Filter by status
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                query = query.Where(i => i.OrderStatus == orderStatus);
            }

            // Search
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => i.InvoiceID.ToString().Contains(search) ||
                                        i.Customer.FullName.Contains(search) ||
                                        i.Customer.Phone.Contains(search) ||
                                        i.ShipAddress.Contains(search));
            }

            var totalInvoices = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalInvoices / pageSize);
            ViewBag.TotalInvoices = totalInvoices;
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentSearch = search;

            // Statistics for filter buttons
            ViewBag.PendingCount = await _context.Invoices.Where(i => !i.IsDeleted && i.OrderStatus == OrderStatus.Pending).CountAsync();
            ViewBag.ConfirmedCount = await _context.Invoices.Where(i => !i.IsDeleted && i.OrderStatus == OrderStatus.Confirmed).CountAsync();
            ViewBag.ShippedCount = await _context.Invoices.Where(i => !i.IsDeleted && i.OrderStatus == OrderStatus.Shipped).CountAsync();
            ViewBag.CompletedCount = await _context.Invoices.Where(i => !i.IsDeleted && i.OrderStatus == OrderStatus.Completed).CountAsync();
            ViewBag.CancelledCount = await _context.Invoices.Where(i => !i.IsDeleted && i.OrderStatus == OrderStatus.Cancelled).CountAsync();

            return View(invoices);
        }

        // GET: Admin/Invoice/Details/5
        public async Task<IActionResult> Details(int id)
        {
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
                .FirstOrDefaultAsync(i => i.InvoiceID == id && !i.IsDeleted);

            if (invoice == null)
            {
                SetErrorMessage("Không tìm thấy đơn hàng!");
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // POST: Admin/Invoice/UpdateStatus
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus, string note = "")
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(id);
                if (invoice == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng!" });
                }

                var oldStatus = invoice.OrderStatus;
                invoice.OrderStatus = newStatus;

                // Update timestamps based on status
                switch (newStatus)
                {
                    case OrderStatus.Confirmed:
                        invoice.ConfirmedAt = DateTime.Now;
                        break;
                    case OrderStatus.Shipped:
                        invoice.ShippedAt = DateTime.Now;
                        break;
                    case OrderStatus.Cancelled:
                        invoice.CancelledAt = DateTime.Now;
                        if (!string.IsNullOrEmpty(note))
                        {
                            invoice.CancelReason = note;
                        }
                        break;
                }

                await _context.SaveChangesAsync();

                var statusText = GetStatusText(newStatus);
                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật trạng thái đơn hàng thành: {statusText}",
                    newStatus = newStatus.ToString(),
                    newStatusText = statusText
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        // POST: Admin/Invoice/BulkUpdateStatus
        [HttpPost]
        public async Task<IActionResult> BulkUpdateStatus(int[] invoiceIds, OrderStatus newStatus)
        {
            try
            {
                if (invoiceIds == null || !invoiceIds.Any())
                {
                    return Json(new { success = false, message = "Vui lòng chọn ít nhất một đơn hàng!" });
                }

                var invoices = await _context.Invoices
                    .Where(i => invoiceIds.Contains(i.InvoiceID))
                    .ToListAsync();

                if (!invoices.Any())
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng nào!" });
                }

                foreach (var invoice in invoices)
                {
                    invoice.OrderStatus = newStatus;

                    switch (newStatus)
                    {
                        case OrderStatus.Confirmed:
                            invoice.ConfirmedAt = DateTime.Now;
                            break;
                        case OrderStatus.Shipped:
                            invoice.ShippedAt = DateTime.Now;
                            break;
                        case OrderStatus.Cancelled:
                            invoice.CancelledAt = DateTime.Now;
                            break;
                    }
                }

                await _context.SaveChangesAsync();

                var statusText = GetStatusText(newStatus);
                return Json(new
                {
                    success = true,
                    message = $"Đã cập nhật {invoices.Count} đơn hàng thành: {statusText}"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi cập nhật: " + ex.Message });
            }
        }

        // GET: Admin/Invoice/Print/5
        public async Task<IActionResult> Print(int id)
        {
            var invoice = await _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Variant)
                        .ThenInclude(v => v.Product)
                .Include(i => i.InvoiceDetails)
                    .ThenInclude(id => id.Bundle)
                .FirstOrDefaultAsync(i => i.InvoiceID == id && !i.IsDeleted);

            if (invoice == null)
            {
                SetErrorMessage("Không tìm thấy đơn hàng!");
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // GET: Admin/Invoice/Export
        public async Task<IActionResult> Export(string status = "", string search = "", DateTime? fromDate = null, DateTime? toDate = null)
        {
            var query = _context.Invoices
                .Include(i => i.Customer)
                .Include(i => i.InvoiceDetails)
                .Where(i => !i.IsDeleted);

            // Apply filters
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var orderStatus))
            {
                query = query.Where(i => i.OrderStatus == orderStatus);
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(i => i.InvoiceID.ToString().Contains(search) ||
                                        i.Customer.FullName.Contains(search) ||
                                        i.Customer.Phone.Contains(search));
            }

            if (fromDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(i => i.CreatedAt <= toDate.Value.AddDays(1));
            }

            var invoices = await query.OrderByDescending(i => i.CreatedAt).ToListAsync();

            // Create CSV content
            var csvContent = GenerateInvoiceCsv(invoices);
            var fileName = $"DonHang_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }

        private string GetStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xác nhận",
                OrderStatus.Confirmed => "Đã xác nhận",
                OrderStatus.Shipped => "Đang giao hàng",
                OrderStatus.Completed => "Hoàn tất",
                OrderStatus.CancelRequested => "Yêu cầu hủy",
                OrderStatus.Cancelled => "Đã hủy",
                _ => status.ToString()
            };
        }

        private string GenerateInvoiceCsv(List<Invoice> invoices)
        {
            var csv = new System.Text.StringBuilder();

            // Header
            csv.AppendLine("Mã đơn hàng,Khách hàng,Điện thoại,Ngày đặt,Trạng thái,Tổng tiền,Phí ship,Thành tiền,Địa chỉ");

            // Data
            foreach (var invoice in invoices)
            {
                csv.AppendLine($"{invoice.InvoiceID}," +
                              $"\"{invoice.Customer?.FullName ?? ""}\"," +
                              $"\"{invoice.Customer?.Phone ?? ""}\"," +
                              $"{invoice.CreatedAt:dd/MM/yyyy HH:mm}," +
                              $"{GetStatusText(invoice.OrderStatus)}," +
                              $"{invoice.TotalAmount:N0}," +
                              $"{invoice.ShippingFee:N0}," +
                              $"{(invoice.TotalAmount + invoice.ShippingFee):N0}," +
                              $"\"{invoice.ShipAddress ?? ""}\"");
            }

            return csv.ToString();
        }
    }
}

