using DrinkShop.Data;
using DrinkShop.Models;
using DrinShop.Models;
using DrinShop.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký CloudinarySettings
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings"));

// Đăng ký CloudinaryService
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

// Đăng ký VariantService
builder.Services.AddScoped<IVariantService, VariantService>();

// Add services
builder.Services.AddControllersWithViews();

// Đăng ký Background Service
builder.Services.AddHostedService<OrderAutoCompleteService>();


// Database
builder.Services.AddDbContext<DrinShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<DrinShopDbContext>()
.AddDefaultTokenProviders();

// Cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
});

// ✅ Đăng ký HttpClient và SeedDataService đúng cách
builder.Services.AddHttpClient<SeedDataService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(60);
    client.DefaultRequestHeaders.Add("User-Agent", "DrinShop/1.0");
});

builder.Services.AddSession();

// Authentication
builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie()
    .AddGoogle(googleOptions =>
    {
        googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
        googleOptions.CallbackPath = "/signin-google";

        googleOptions.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var uriBuilder = new UriBuilder(context.RedirectUri);
            var queryParams = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);

            queryParams["prompt"] = "select_account";
            queryParams["access_type"] = "offline";

            uriBuilder.Query = queryParams.ToString();
            context.Response.Redirect(uriBuilder.ToString());
            return Task.CompletedTask;
        };

        googleOptions.Scope.Add("profile");
        googleOptions.Scope.Add("email");

        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "sub");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
        googleOptions.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
        googleOptions.ClaimActions.MapJsonKey("picture", "picture");
    });

var app = builder.Build();

// Seed Identity data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await IdentitySeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the Identity DB.");
    }
}

// ✅ Sửa: Seed Provinces data sử dụng DI
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var seedService = services.GetRequiredService<SeedDataService>();
        await seedService.SeedProvincesAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the Provinces data: {Error}", ex.Message);
        // ✅ Không throw exception để app vẫn có thể start
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseCookiePolicy();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Route cho Areas Admin
app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Dashboard}/{action=Index}/{id?}");

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();


