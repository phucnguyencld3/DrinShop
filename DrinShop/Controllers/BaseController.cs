using DrinShop.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Controllers
{
    public class BaseController : Controller
    {
        private readonly DrinShopDbContext _context;

        public BaseController()
        {
        }

        protected BaseController(DrinShopDbContext context)
        {
            _context = context;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Load categories cho menu
            if (_context != null)
            {
                ViewBag.MenuCategories = _context.Categories
                    .Where(c => c.Status == true)
                    .OrderBy(c => c.CategoryName)
                    .Take(10) 
                    .ToList();
            }

            base.OnActionExecuting(context);
        }
    }
}
