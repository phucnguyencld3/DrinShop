using DrinShop.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

public class SeedDataService
{
    private readonly DrinShopDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SeedDataService> _logger;

    public SeedDataService(DrinShopDbContext context, HttpClient httpClient, ILogger<SeedDataService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;

        // ✅ Cấu hình HttpClient với timeout và headers
        _httpClient.Timeout = TimeSpan.FromSeconds(60); // 60 giây timeout
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "DrinShop/1.0");
    }

    public async Task SeedProvincesAsync()
    {
        try
        {
            _logger.LogInformation("Starting to seed provinces data...");

            // ✅ Kiểm tra xem đã có data chưa
            var existingProvincesCount = await _context.Provinces.CountAsync();
            if (existingProvincesCount > 0)
            {
                _logger.LogInformation($"Provinces already seeded. Count: {existingProvincesCount}");
                return;
            }

            var apiUrl = "https://provinces.open-api.vn/api/?depth=3";
            _logger.LogInformation($"Fetching data from: {apiUrl}");

            // ✅ Sử dụng using để đảm bảo dispose response
            using var response = await _httpClient.GetAsync(apiUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"API returned status code: {response.StatusCode}");
                return;
            }

            _logger.LogInformation("Successfully fetched data from API");

            var json = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrWhiteSpace(json))
            {
                _logger.LogWarning("API returned empty response");
                return;
            }

            var provinces = JsonSerializer.Deserialize<List<ProvinceApi>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (provinces == null || !provinces.Any())
            {
                _logger.LogWarning("No provinces data found in API response");
                return;
            }

            _logger.LogInformation($"Found {provinces.Count} provinces to process");

            // ✅ Sử dụng transaction để đảm bảo data consistency
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var processedProvinces = 0;
                var processedDistricts = 0;
                var processedWards = 0;

                foreach (var p in provinces)
                {
                    if (await _context.Provinces.AnyAsync(x => x.Code == p.Code))
                        continue;

                    var province = new Province
                    {
                        Code = p.Code,
                        Name = p.Name ?? "Unknown",
                        DivisionType = p.DivisionType ?? "Unknown",
                        Codename = p.Codename ?? "",
                        PhoneCode = p.PhoneCode ?? ""
                    };

                    _context.Provinces.Add(province);
                    processedProvinces++;

                    // THÊM DISTRICTS
                    if (p.Districts != null)
                    {
                        foreach (var d in p.Districts)
                        {
                            if (await _context.Districts.AnyAsync(x => x.Code == d.Code))
                                continue;

                            _context.Districts.Add(new District
                            {
                                Code = d.Code,
                                Name = d.Name ?? "Unknown",
                                ProvinceCode = p.Code,
                                DivisionType = "Quận/Huyện",
                                Codename = ""
                            });
                            processedDistricts++;

                            // THÊM WARDS
                            if (d.Wards != null)
                            {
                                foreach (var w in d.Wards)
                                {
                                    if (await _context.Wards.AnyAsync(x => x.Code == w.Code))
                                        continue;

                                    _context.Wards.Add(new Ward
                                    {
                                        Code = w.Code,
                                        Name = w.Name ?? "Unknown",
                                        DistrictCode = d.Code,
                                        ProvinceCode = p.Code,
                                        Codename = "",
                                        DivisionType = "Phường/Xã"
                                    });
                                    processedWards++;
                                }
                            }
                        }
                    }

                    // ✅ Lưu theo batch để tránh timeout
                    if (processedProvinces % 10 == 0)
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation($"Saved batch: {processedProvinces} provinces processed");
                    }
                }

                // ✅ Lưu dữ liệu cuối cùng
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Successfully seeded: {processedProvinces} provinces, {processedDistricts} districts, {processedWards} wards");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error occurred while saving data to database");
                throw;
            }
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError(httpEx, "HTTP request error while fetching provinces data");

            // ✅ Fallback: Load từ local data nếu API fail
            await SeedProvincesFromLocalDataAsync();
        }
        catch (TaskCanceledException tcEx)
        {
            _logger.LogError(tcEx, "Request timed out while fetching provinces data");
            await SeedProvincesFromLocalDataAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred during provinces seeding");
            throw;
        }
    }

    // ✅ Fallback method để load data từ local file
    private async Task SeedProvincesFromLocalDataAsync()
    {
        try
        {
            _logger.LogInformation("Attempting to seed provinces from local data...");

            // ✅ Tạo một số tỉnh/thành phố chính như fallback
            var fallbackProvinces = new[]
            {
                new Province { Code = 1, Name = "Hà Nội", DivisionType = "Thành phố Trung ương", Codename = "ha_noi", PhoneCode = "024" },
                new Province { Code = 79, Name = "TP Hồ Chí Minh", DivisionType = "Thành phố Trung ương", Codename = "ho_chi_minh", PhoneCode = "028" },
                new Province { Code = 48, Name = "Đà Nẵng", DivisionType = "Thành phố Trung ương", Codename = "da_nang", PhoneCode = "0236" },
                new Province { Code = 92, Name = "Cần Thơ", DivisionType = "Thành phố Trung ương", Codename = "can_tho", PhoneCode = "0292" },
                new Province { Code = 31, Name = "Hải Phòng", DivisionType = "Thành phố Trung ương", Codename = "hai_phong", PhoneCode = "0225" }
            };

            foreach (var province in fallbackProvinces)
            {
                if (!await _context.Provinces.AnyAsync(x => x.Code == province.Code))
                {
                    _context.Provinces.Add(province);
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Successfully seeded {fallbackProvinces.Length} fallback provinces");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while seeding fallback provinces data");
        }
    }
}

// ✅ Model để deserialize API - Không thay đổi
public class ProvinceApi
{
    public int Code { get; set; }
    public string? Name { get; set; }
    public string? DivisionType { get; set; }
    public string? Codename { get; set; }
    public string? PhoneCode { get; set; }
    public List<DistrictApi>? Districts { get; set; } = new();
}

public class DistrictApi
{
    public int Code { get; set; }
    public string? Name { get; set; }
    public List<WardApi>? Wards { get; set; } = new();
}

public class WardApi
{
    public int Code { get; set; }
    public string? Name { get; set; }
}
