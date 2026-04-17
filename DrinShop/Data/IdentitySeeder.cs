using System;
using System.Linq;
using System.Threading.Tasks;
using DrinkShop.Data;
using DrinShop.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace DrinkShop.Data
{
    public static class IdentitySeeder
    {
        private static readonly string[] Roles = new[] { "Admin", "Staff", "User" };

        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Tạo roles
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Tạo Admin account
            var adminUser = await userManager.FindByNameAsync("admin");
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@drinkshop.com",
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    IsActive = true,
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create admin: {errors}");
                }
            }

            // Gán role Admin
            var roles = await userManager.GetRolesAsync(adminUser);
            if (!roles.Contains("Admin"))
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }

            // Tạo Staff account
            var staffUser = await userManager.FindByNameAsync("staff");
            if (staffUser == null)
            {
                staffUser = new ApplicationUser
                {
                    UserName = "staff",
                    Email = "staff@drinkshop.com",
                    EmailConfirmed = true,
                    FullName = "Staff User",
                    IsActive = true,
                };

                var result = await userManager.CreateAsync(staffUser, "Staff@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(staffUser, "Staff");
                }
            }

            // ✅ Thêm User account mẫu (Khách hàng)
            var regularUser = await userManager.FindByNameAsync("customer");
            if (regularUser == null)
            {
                regularUser = new ApplicationUser
                {
                    UserName = "customer",
                    Email = "customer@drinkshop.com",
                    EmailConfirmed = true,
                    FullName = "Sample Customer",
                    IsActive = true,
                };

                var result = await userManager.CreateAsync(regularUser, "Customer@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(regularUser, "User");
                }
            }
        }
    }
}

