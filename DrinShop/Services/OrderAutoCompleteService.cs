using DrinShop.Models;
using Microsoft.EntityFrameworkCore;

namespace DrinShop.Services
{
    public class OrderAutoCompleteService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OrderAutoCompleteService> _logger;

        public OrderAutoCompleteService(IServiceProvider serviceProvider, ILogger<OrderAutoCompleteService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<DrinShopDbContext>();

                    await AutoCompleteExpiredShippedOrders(context);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while auto-completing orders");
                }

                // Chạy mỗi 6 giờ
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task AutoCompleteExpiredShippedOrders(DrinShopDbContext context)
        {
            var cutoffDate = DateTime.Now.AddDays(-14);

            var expiredOrders = await context.Invoices
                .Where(i => i.OrderStatus == OrderStatus.Shipped &&
                           i.ShippedAt.HasValue &&
                           i.ShippedAt.Value <= cutoffDate &&
                           !i.IsDeleted)
                .ToListAsync();

            if (expiredOrders.Any())
            {
                foreach (var order in expiredOrders)
                {
                    order.OrderStatus = OrderStatus.Completed;
                    // Note: Không cần set CompletedAt vì đây là auto complete
                }

                await context.SaveChangesAsync();

                _logger.LogInformation($"Auto-completed {expiredOrders.Count} orders that were shipped for more than 14 days");
            }
        }
    }
}

