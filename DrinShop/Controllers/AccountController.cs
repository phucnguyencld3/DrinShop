using DrinShop.Models;
using DrinShop.ViewModels;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DrinShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly DrinShopDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            DrinShopDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // POST: /Account/Login
        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                model.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserName} logged in.", model.UserName);

                var user = await _userManager.FindByNameAsync(model.UserName);
                if (user != null)
                {
                    // ✅ BỎ KIỂM TRA IsFirstLogin - Để người dùng tự do sử dụng hệ thống

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }

                    var roles = await _userManager.GetRolesAsync(user);

                    if (roles.Contains("Admin"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                    else if (roles.Contains("Staff"))
                    {
                        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                    }
                    else if (roles.Contains("User"))
                    {
                        return RedirectToAction("Index", "Home");
                    }
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {UserName} account locked out.", model.UserName);
                ModelState.AddModelError("", "Tài khoản đã bị khóa do đăng nhập sai quá nhiều lần. Vui lòng thử lại sau 5 phút.");
                return View(model);
            }

            ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(model);
        }

        // ✅ THÊM PHƯƠNG THỨC BỔ SUNG THÔNG TIN
        [HttpGet]
        public async Task<IActionResult> CompleteProfile(string? returnUrl = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            ViewData["ReturnUrl"] = returnUrl; // ✅ Lưu URL để redirect sau khi hoàn thành

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            var model = new CompleteProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email,
                Phone = user.PhoneNumber,
                Gender = customer?.Gender ?? false,
                DateOfBirth = customer?.DateOfBirth,
                Address = customer?.Address,
                StreetAddress = customer?.StreetAddress,
                ProvinceCode = customer?.ProvinceCode,
                DistrictCode = customer?.DistrictCode,
                WardCode = customer?.WardCode
            };

            ViewBag.Provinces = await _context.Provinces
                .Select(p => new SelectListItem
                {
                    Value = p.Code.ToString(),
                    Text = p.Name
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteProfile(CompleteProfileViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await _context.Provinces
                    .Select(p => new SelectListItem
                    {
                        Value = p.Code.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync();

                ViewData["ReturnUrl"] = returnUrl;
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            if (customer == null)
            {
                return RedirectToAction("Login");
            }

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.PhoneNumber = model.Phone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            customer.FullName = model.FullName;
            customer.Phone = model.Phone ?? "";

            // ✅ FIX: Bỏ ?? operator vì Gender là bool không thể null
            customer.Gender = model.Gender;

            customer.DateOfBirth = model.DateOfBirth;
            customer.Address = model.Address;
            customer.StreetAddress = model.StreetAddress;
            customer.ProvinceCode = model.ProvinceCode ?? 0;
            customer.DistrictCode = model.DistrictCode ?? 0;
            customer.WardCode = model.WardCode ?? 0;
            customer.IsFirstLogin = false; // ✅ Đánh dấu đã hoàn thành

            _context.Users.Update(customer);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Hoàn tất thông tin tài khoản thành công!";

            // ✅ REDIRECT VỀ URL GỐC (có thể là Cart/Checkout)
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin") || roles.Contains("Staff"))
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }

            return RedirectToAction("Index", "Home");
        }




        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // ✅ Kiểm tra null safety cho model
            if (model == null)
            {
                ModelState.AddModelError("", "Dữ liệu đăng ký không hợp lệ.");
                return View(new RegisterViewModel());
            }

            var user = new ApplicationUser
            {
                UserName = model.UserName?.Trim(),
                Email = model.Email?.Trim(),
                FullName = model.FullName?.Trim(),
                PhoneNumber = model.PhoneNumber?.Trim(),
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                try
                {
                    _logger.LogInformation("User {UserName} created a new account.", model.UserName);

                    // ✅ Safe parsing cho address codes
                    int provinceCode = 0;
                    int districtCode = 0;
                    int wardCode = 0;

                    // Parse an toàn
                    if (!string.IsNullOrEmpty(model.ProvinceCode) && int.TryParse(model.ProvinceCode, out int parsedProvince))
                    {
                        provinceCode = parsedProvince;
                    }

                    if (!string.IsNullOrEmpty(model.DistrictCode) && int.TryParse(model.DistrictCode, out int parsedDistrict))
                    {
                        districtCode = parsedDistrict;
                    }

                    if (!string.IsNullOrEmpty(model.WardCode) && int.TryParse(model.WardCode, out int parsedWard))
                    {
                        wardCode = parsedWard;
                    }

                    var customer = new User
                    {
                        ApplicationUserId = user.Id,
                        FullName = model.FullName?.Trim() ?? "",
                        Email = model.Email?.Trim() ?? "",
                        Phone = model.PhoneNumber?.Trim() ?? "",

                        // ✅ FIX: Convert nullable bool to bool safely
                        Gender = model.Gender ?? false, // Default to false if null

                        DateOfBirth = model.DateOfBirth,
                        Address = model.Address?.Trim() ?? "",
                        StreetAddress = model.StreetAddress?.Trim() ?? "",

                        // ✅ Sử dụng parsed values thay vì int.Parse trực tiếp
                        ProvinceCode = provinceCode,
                        DistrictCode = districtCode,
                        WardCode = wardCode,

                        Status = true,
                        RegisterDate = DateTime.Now,
                        IsFirstLogin = false
                    };

                    _context.Users.Add(customer);
                    await _context.SaveChangesAsync();

                    await _userManager.AddToRoleAsync(user, "User");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    TempData["Success"] = "Đăng ký tài khoản thành công! Chào mừng bạn đến với DrinShop!";
                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating customer record for user {UserName}", model.UserName);

                    // ✅ Cleanup - Xóa ApplicationUser nếu tạo User thất bại
                    try
                    {
                        await _userManager.DeleteAsync(user);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogError(deleteEx, "Error deleting user {UserName} after failed customer creation", model.UserName);
                    }

                    ModelState.AddModelError("", "Có lỗi khi tạo tài khoản. Vui lòng thử lại sau.");
                    return View(model);
                }
            }

            // ✅ Hiển thị lỗi validation từ Identity
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction("Index", "Home");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var customer = await _context.Users
                .Include(u => u.Province)
                .Include(u => u.District)
                .Include(u => u.Ward)
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            // ✅ KIỂM TRA VÀ HIỂN THỊ THÔNG BÃO NẾU CHƯA BỔ SUNG
            if (customer?.IsFirstLogin == true)
            {
                ViewBag.ShowCompleteProfileAlert = true;
                ViewBag.AlertMessage = "Bạn chưa hoàn tất thông tin cá nhân. Vui lòng bổ sung để có trải nghiệm tốt hơn!";
            }

            var model = new ProfileViewModel
            {
                FullName = user.FullName ?? "",
                Email = user.Email ?? "",
                Phone = user.PhoneNumber,
                Gender = customer?.Gender ?? false,
                DateOfBirth = customer?.DateOfBirth,
                Address = customer?.Address,
                StreetAddress = customer?.StreetAddress,
                ProvinceCode = customer?.ProvinceCode,
                DistrictCode = customer?.DistrictCode,
                WardCode = customer?.WardCode,
                ProvinceName = customer?.Province?.Name,
                DistrictName = customer?.District?.Name,
                WardName = customer?.Ward?.Name
            };

            ViewBag.Provinces = await _context.Provinces
                .Select(p => new SelectListItem
                {
                    Value = p.Code.ToString(),
                    Text = p.Name
                })
                .ToListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await _context.Provinces
                    .Select(p => new SelectListItem
                    {
                        Value = p.Code.ToString(),
                        Text = p.Name
                    })
                    .ToListAsync();

                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.PhoneNumber = model.Phone;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                foreach (var error in updateResult.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return View(model);
            }

            var customer = await _context.Users
                .FirstOrDefaultAsync(u => u.ApplicationUserId == user.Id);

            if (customer != null)
            {
                customer.FullName = model.FullName;
                customer.Email = model.Email;
                customer.Phone = model.Phone ?? "";

                // ✅ FIX: Bỏ ?? operator vì Gender là bool không thể null
                customer.Gender = model.Gender;

                customer.DateOfBirth = model.DateOfBirth;
                customer.Address = model.Address;
                customer.StreetAddress = model.StreetAddress;
                customer.ProvinceCode = model.ProvinceCode ?? 0;
                customer.DistrictCode = model.DistrictCode ?? 0;
                customer.WardCode = model.WardCode ?? 0;

                _context.Users.Update(customer);
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }



        [HttpGet]
        public async Task<JsonResult> GetDistricts(int provinceCode)
        {
            var districts = await _context.Districts
                .Where(d => d.ProvinceCode == provinceCode)
                .Select(d => new { value = d.Code, text = d.Name })
                .ToListAsync();

            return Json(districts);
        }

        [HttpGet]
        public async Task<JsonResult> GetWards(int districtCode)
        {
            var wards = await _context.Wards
                .Where(w => w.DistrictCode == districtCode)
                .Select(w => new { value = w.Code, text = w.Name })
                .ToListAsync();

            return Json(wards);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy người dùng hiện tại.";
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(
                user,
                model.OldPassword,
                model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} changed password successfully.", user.Id);

                await _userManager.UpdateSecurityStampAsync(user);
                await _signInManager.SignOutAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công! Vui lòng đăng nhập lại để tiếp tục.";
                TempData["PasswordChangedSuccess"] = true;

                return RedirectToAction("Login");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }
        }

        [Route("account/google-login")]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                _signInManager.SignOutAsync().Wait();
            }

            var redirectUrl = Url.Action("GoogleResponse", "Account");
            if (!string.IsNullOrEmpty(returnUrl))
            {
                redirectUrl += $"?returnUrl={Uri.EscapeDataString(returnUrl)}";
            }

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl,
                Items =
        {
            { "prompt", "select_account" },
            { "access_type", "offline" }
        }
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }


        [HttpGet("account/google-response")]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            var result = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            if (!result.Succeeded)
            {
                TempData["ErrorMessage"] = "Đăng nhập Google thất bại. Vui lòng thử lại!";
                return RedirectToAction("Login");
            }

            var claims = result.Principal.Claims;
            var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Không thể lấy thông tin email từ Google.";
                return RedirectToAction("Login");
            }

            try
            {
                var appUser = await _userManager.FindByEmailAsync(email);

                if (appUser == null)
                {
                    // ✅ TẠO APPLICATION USER MỚI
                    appUser = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FullName = name ?? email,
                        EmailConfirmed = true,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    };

                    var createResult = await _userManager.CreateAsync(appUser);
                    if (!createResult.Succeeded)
                    {
                        var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                        _logger.LogError("Failed to create ApplicationUser for Google login: {Errors}", errors);
                        TempData["ErrorMessage"] = $"Lỗi tạo tài khoản: {errors}";
                        return RedirectToAction("Login");
                    }

                    await _userManager.AddToRoleAsync(appUser, "User");
                }

                // ✅ KIỂM TRA VÀ TẠO USER RECORD CHO CẢ TÀI KHOẢN MỚI VÀ CŨ
                var customer = await _context.Users
                    .FirstOrDefaultAsync(u => u.ApplicationUserId == appUser.Id);

                if (customer == null)
                {
                    // ✅ TẠO USER RECORD (cho cả tài khoản mới và cũ)
                    customer = new User
                    {
                        ApplicationUserId = appUser.Id,
                        GoogleId = googleId,
                        Email = email,
                        FullName = name ?? email,
                        Phone = "",
                        Address = "",
                        StreetAddress = "",
                        ProvinceCode = null,
                        DistrictCode = null,
                        WardCode = null,
                        Gender = false,
                        Status = true,
                        RegisterDate = DateTime.Now,
                        IsFirstLogin = true // ✅ Đánh dấu chưa hoàn tất
                    };

                    _context.Users.Add(customer);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("🆕 Created User record for Google user - ApplicationUser ID: {AppUserId}, User ID: {UserId}, IsNew: {IsNew}",
                        appUser.Id, customer.UserId, appUser.CreatedDate >= DateTime.Now.AddMinutes(-1));
                }
                else
                {
                    _logger.LogInformation("✅ Existing Google user with User record logged in: {Email}", email);
                }

                await _signInManager.SignInAsync(appUser, isPersistent: false);

                // Redirect logic
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                var roles = await _userManager.GetRolesAsync(appUser);
                if (roles.Contains("Admin") || roles.Contains("Staff"))
                {
                    return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GoogleResponse for email: {Email}", email);
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý đăng nhập Google.";
                return RedirectToAction("Login");
            }
        }



    }
}

