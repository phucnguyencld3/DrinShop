using DrinShop.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DrinShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,Staff")]
    public class BaseAdminController : Controller
    {
        protected string CurrentUserName => User?.Identity?.Name ?? "System";

        protected void SetSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void SetErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        protected void SetInfoMessage(string message)
        {
            TempData["InfoMessage"] = message;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Thêm thông tin user hiện tại vào ViewBag
            ViewBag.CurrentUser = CurrentUserName;
            ViewBag.IsAdmin = User?.IsInRole("Admin") == true;
            ViewBag.IsStaff = User?.IsInRole("Staff") == true;

            base.OnActionExecuting(context);
        }
    }
}

