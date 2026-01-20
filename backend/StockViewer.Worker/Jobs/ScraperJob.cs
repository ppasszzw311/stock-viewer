using Quartz;
using StockViewer.Core.Data;
using StockViewer.Core.Entities;
using StockViewer.Worker.Scrapers;
using Microsoft.EntityFrameworkCore;

namespace StockViewer.Worker.Jobs;

[DisallowConcurrentExecution]
public class ScraperJob : IJob
{
    private readonly TwseScraperService _scraper;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<ScraperJob> _logger;

    public ScraperJob(TwseScraperService scraper, AppDbContext dbContext, ILogger<ScraperJob> logger)
    {
        _scraper = scraper;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Execute(IJobExecutionContext context)
    {
        _logger.LogInformation("Starting ScraperJob at {Time}", DateTimeOffset.Now);

        try
        {
            // 1. Fetch Attention Securities
            var attentionDtos = await _scraper.FetchAttentionSecuritiesAsync();
            _logger.LogInformation("Fetched {Count} attention securities", attentionDtos.Count);

            foreach (var dto in attentionDtos)
            {
                if (string.IsNullOrEmpty(dto.Code)) continue;

                // Ensure Stock exists
                var stock = await _dbContext.Stocks.FindAsync(dto.Code);
                if (stock == null)
                {
                    stock = new Stock { Code = dto.Code, Name = dto.Name };
                    _dbContext.Stocks.Add(stock);
                    await _dbContext.SaveChangesAsync();
                }

                // Parse Date (ROC to AD)
                // Format: 1130120 -> 2024-01-20
                if (DateOnly.TryParseExact(dto.Date, "yyyMMdd", null, System.Globalization.DateTimeStyles.None, out var date))
                {
                    // Fix ROC year if needed (if TryParseExact assumes Gregorian)
                    // Actually, "1130120" won't parse as yyyMMdd in Gregorian correctly if year is 3 digits?
                    // Let's do manual parsing for safety.
                    date = ParseRocDate(dto.Date);
                }
                else
                {
                     date = ParseRocDate(dto.Date);
                }

                // Check if record exists
                var exists = await _dbContext.DailyAttentions.AnyAsync(d => d.StockCode == dto.Code && d.Date == date);
                if (!exists)
                {
                    _dbContext.DailyAttentions.Add(new DailyAttention
                    {
                        StockCode = dto.Code,
                        Date = date,
                        Reason = dto.TradingInfoForAttention
                    });
                }
            }
            await _dbContext.SaveChangesAsync();

            // 2. Fetch Disposition Securities
            var dispositionRecords = await _scraper.FetchDispositionSecuritiesAsync();
            _logger.LogInformation("Fetched {Count} disposition records", dispositionRecords.Count);

            foreach (var record in dispositionRecords)
            {
                // Ensure Stock exists
                var stock = await _dbContext.Stocks.FindAsync(record.StockCode);
                if (stock == null)
                {
                    // We might not have the name here if it wasn't in attention list.
                    // We can try to fetch it or leave it empty/placeholder.
                    // For now, create with code as name or leave name empty.
                    stock = new Stock { Code = record.StockCode, Name = record.StockCode };
                    _dbContext.Stocks.Add(stock);
                    await _dbContext.SaveChangesAsync();
                }

                // Check if record exists (by StartDate and StockCode)
                var exists = await _dbContext.DispositionRecords.AnyAsync(d => d.StockCode == record.StockCode && d.StartDate == record.StartDate);
                if (!exists)
                {
                    _dbContext.DispositionRecords.Add(record);
                }
            }
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ScraperJob");
            // JobExecutionException?
        }

        _logger.LogInformation("ScraperJob finished at {Time}", DateTimeOffset.Now);
    }

    private DateOnly ParseRocDate(string dateStr)
    {
        // 1130120
        if (dateStr.Length == 7)
        {
            int year = int.Parse(dateStr.Substring(0, 3)) + 1911;
            int month = int.Parse(dateStr.Substring(3, 2));
            int day = int.Parse(dateStr.Substring(5, 2));
            return new DateOnly(year, month, day);
        }
        return DateOnly.FromDateTime(DateTime.Now); // Fallback
    }
}
