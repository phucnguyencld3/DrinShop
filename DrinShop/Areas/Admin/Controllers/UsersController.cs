using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
        private readonly DrinShopDbContext _context;

        public UsersController(DrinShopDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Users
        public async Task<IActionResult> Index(string? filter)
        {
            var query = _context.Users
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .Include(u => u.ApplicationUser)
                .AsQueryable();

            // ✅ Lọc theo trạng thái hoàn thành thông tin
            ViewBag.Filter = filter;
            ViewBag.TotalUsers = await query.CountAsync();
            ViewBag.IncompleteUsers = await query.CountAsync(u => u.IsFirstLogin == true);
            ViewBag.CompleteUsers = await query.CountAsync(u => u.IsFirstLogin == false);

            if (filter == "incomplete")
            {
                query = query.Where(u => u.IsFirstLogin == true);
            }
            else if (filter == "complete")
            {
                query = query.Where(u => u.IsFirstLogin == false);
            }

            var users = await query
                .OrderByDescending(u => u.RegisterDate)
                .ToListAsync();

            return View(users);
        }

        // GET: /Admin/Users/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .Include(u => u.ApplicationUser)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // POST: /Admin/Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
                await _context.SaveChangesAsync();
                TempData["success"] = "Xóa khách hàng thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Admin/Users/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.Users
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .Include(u => u.ApplicationUser)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            return View(user);
        }

        // ✅ POST: Đánh dấu user đã hoàn tất thông tin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsComplete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsFirstLogin = false;
            await _context.SaveChangesAsync();

            TempData["success"] = "Đã đánh dấu user hoàn tất thông tin!";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ✅ POST: Bật/Tắt trạng thái user
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.Status = !user.Status;
            await _context.SaveChangesAsync();

            TempData["success"] = $"Đã {(user.Status ? "kích hoạt" : "vô hiệu hóa")} tài khoản!";
            return RedirectToAction(nameof(Index));
        }




        // ✅ SỬA LẠI ACTION DEBUG AN TOÀN HỚN
        // ✅ SỬA LẠI DEBUG ACTION HIỂN THỊ CHI TIẾT HỚN
        [HttpGet]
        public async Task<IActionResult> Debug()
        {
            try
            {
                // ✅ KIỂM TRA STRUCTURE BẢNG TRƯỚC
                var allUsers = await _context.Users
                    .Include(u => u.ApplicationUser)
                    .ToListAsync();

                // ✅ HIỂN THI THÔNG TIN RAW ĐỂ DEBUG
                var debugData = allUsers.Select(u => new {
                    u.UserId,
                    u.FullName,
                    u.Email,
                    GoogleId = u.GoogleId ?? "NULL", // Hiển thị NULL nếu trống
                    IsFirstLogin = u.IsFirstLogin,
                    u.Status,
                    ApplicationUserEmail = u.ApplicationUser?.Email ?? "NO_APP_USER",
                    u.RegisterDate,
                    HasApplicationUser = u.ApplicationUser != null,
                    ApplicationUserId = u.ApplicationUserId ?? "NULL"
                }).ToList();

                var googleUsers = debugData.Where(u => !string.IsNullOrEmpty(u.GoogleId) && u.GoogleId != "NULL").ToList();
                var incompleteUsers = debugData.Where(u => u.IsFirstLogin == true).ToList();

                ViewBag.AllUsers = debugData;
                ViewBag.GoogleUsers = googleUsers;
                ViewBag.IncompleteUsers = incompleteUsers;
                ViewBag.TotalCount = debugData.Count;

                // ✅ LOG CHI TIẾT
                var logger = HttpContext.RequestServices.GetService<ILogger<UsersController>>();
                logger?.LogInformation("🔍 DEBUG DATA:");
                logger?.LogInformation("Total users in DB: {Total}", debugData.Count);
                logger?.LogInformation("Users with GoogleId: {Google}", googleUsers.Count);
                logger?.LogInformation("Users with IsFirstLogin=true: {Incomplete}", incompleteUsers.Count);

                foreach (var user in debugData)
                {
                    logger?.LogInformation("User {Id}: GoogleId='{GoogleId}', IsFirstLogin={IsFirstLogin}, Email='{Email}'",
                        user.UserId, user.GoogleId, user.IsFirstLogin, user.Email);
                }

                return View();
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<UsersController>>();
                logger?.LogError(ex, "❌ Error in Debug action");

                ViewBag.AllUsers = new List<dynamic>();
                ViewBag.GoogleUsers = new List<dynamic>();
                ViewBag.IncompleteUsers = new List<dynamic>();
                ViewBag.TotalCount = 0;
                ViewBag.ErrorMessage = ex.Message;

                return View();
            }
        }


        // ✅ TẠO TEST GOOGLE USER
        // ✅ TẠO TEST GOOGLE USER - SỬA LẠI ĐỂ XỬ LÝ LỖI CHI TIẾT HỚN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTestGoogleUser()
        {
            try
            {
                var timestamp = DateTime.Now.Ticks.ToString();

                var testUser = new User
                {
                    GoogleId = "test_google_" + timestamp,
                    Email = $"test.google.{timestamp}@gmail.com",
                    FullName = "Test Google User " + timestamp,
                    Phone = "", // Có thể để trống
                    Address = "", // Có thể để trống
                    StreetAddress = "", // Có thể để trống

                    // ✅ ĐẢM BẢO NULLABLE FIELDS ĐƯỢC SET NULL THAY VÌ 0
                    ProvinceCode = null, // Nullable field - set null thay vì 0
                    DistrictCode = null, // Nullable field - set null thay vì 0
                    WardCode = null,     // Nullable field - set null thay vì 0

                    Gender = false, // Required field
                    Status = true,
                    RegisterDate = DateTime.Now,
                    IsFirstLogin = true, // Quan trọng!

                    // ✅ NULLABLE FIELDS
                    DateOfBirth = null,
                    ApplicationUserId = null // Để test user không liên kết với ApplicationUser
                };

                _context.Users.Add(testUser);
                await _context.SaveChangesAsync();

                var logger = HttpContext.RequestServices.GetService<ILogger<UsersController>>();
                logger?.LogInformation("✅ Created test Google user: ID={UserId}, GoogleId={GoogleId}, Email={Email}",
                    testUser.UserId, testUser.GoogleId, testUser.Email);

                TempData["success"] = $"Đã tạo test Google user với ID: {testUser.UserId}, Email: {testUser.Email}";
            }
            catch (DbUpdateException dbEx)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<UsersController>>();
                logger?.LogError(dbEx, "❌ Database error creating test user");

                var innerException = dbEx.InnerException?.Message ?? "Không có chi tiết";
                TempData["error"] = $"Lỗi database khi tạo test user: {dbEx.Message}. Chi tiết: {innerException}";
            }
            catch (Exception ex)
            {
                var logger = HttpContext.RequestServices.GetService<ILogger<UsersController>>();
                logger?.LogError(ex, "❌ General error creating test user");

                TempData["error"] = $"Lỗi tạo test user: {ex.Message}. Inner: {ex.InnerException?.Message ?? "Không có"}";
            }

            return RedirectToAction("Debug");
        }


        // ✅ RESET FIRST LOGIN CHO TẤT CẢ
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetFirstLogin()
        {
            try
            {
                var allUsers = await _context.Users.ToListAsync();
                foreach (var user in allUsers)
                {
                    user.IsFirstLogin = true;
                }
                await _context.SaveChangesAsync();

                TempData["success"] = $"Đã reset IsFirstLogin=true cho {allUsers.Count} users";
            }
            catch (Exception ex)
            {
                TempData["error"] = $"Lỗi reset: {ex.Message}";
            }

            return RedirectToAction("Debug");
        }


    }
}