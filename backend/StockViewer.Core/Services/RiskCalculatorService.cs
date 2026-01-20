using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StockViewer.Core.Data;
using StockViewer.Core.Entities;

namespace StockViewer.Core.Services;

public enum RiskLevel
{
    Safe,
    Warning,
    Danger
}

public class RiskAssessment
{
    public string StockCode { get; set; } = string.Empty;
    public RiskLevel Level { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int ConsecutiveDays { get; set; }
    public int DaysInLast10 { get; set; }
    public int DaysInLast30 { get; set; }
}

public class RiskCalculatorService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RiskCalculatorService> _logger;

    public RiskCalculatorService(AppDbContext context, ILogger<RiskCalculatorService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RiskAssessment> CheckDispositionRiskAsync(string stockCode, DateOnly referenceDate)
    {
        // Fetch attention records for the last 30 trading days
        // Note: We should strictly use trading days, but for simplicity we'll query by date range 
        // and assume the DB only contains trading days or we filter gaps.
        // A better approach is to fetch the last 30 records ordered by date desc.
        
        var records = await _context.DailyAttentions
            .Where(d => d.StockCode == stockCode && d.Date <= referenceDate)
            .OrderByDescending(d => d.Date)
            .Take(30)
            .ToListAsync();

        var assessment = new RiskAssessment
        {
            StockCode = stockCode,
            Level = RiskLevel.Safe
        };

        if (!records.Any()) return assessment;

        // Check 1: Consecutive days (records are ordered desc, so records[0] is latest)
        // If records[0].Date is not referenceDate, it means it wasn't flagged today (if referenceDate is today).
        // But we want to check risk assuming today MIGHT be flagged or just based on history.
        // Let's assume we are checking AFTER today's data is ingested.

        int consecutive = 0;
        DateOnly expectedDate = referenceDate;
        
        // We need to be careful about weekends/holidays. 
        // Ideally we check if records are consecutive *trading days*.
        // Since we don't have a trading calendar table yet, we'll approximate:
        // If the gap between dates is > 1, check if it's just a weekend.
        // For now, let's just count how many records are in the top N sequence.
        // Actually, the most robust way without a calendar is to just look at the list of dates we HAVE.
        // If we have records for today, yesterday, day before... that's consecutive.
        
        // Let's count consecutive presence in the fetched list.
        // We iterate and check if the gap is small (<= 3 days to account for weekends).
        // This is a heuristic.
        
        var sortedRecords = records.OrderByDescending(d => d.Date).ToList();
        
        // 1. Consecutive Days
        for (int i = 0; i < sortedRecords.Count; i++)
        {
            if (i == 0)
            {
                consecutive = 1;
                continue;
            }

            var diff = sortedRecords[i - 1].Date.DayNumber - sortedRecords[i].Date.DayNumber;
            if (diff <= 3) // Allow weekend gap
            {
                consecutive++;
            }
            else
            {
                break;
            }
        }
        assessment.ConsecutiveDays = consecutive;

        // 2. Count in last 10 records (approx 10 trading days)
        // We take the last 10 *potential* trading days. 
        // But we only have *attention* records.
        // We need to know if we are within a 10-day window.
        // Let's use a date window: referenceDate minus approx 14 calendar days.
        var tenDayWindowStart = referenceDate.AddDays(-14);
        int countIn10 = records.Count(r => r.Date >= tenDayWindowStart);
        assessment.DaysInLast10 = countIn10;

        // 3. Count in last 30 records
        var thirtyDayWindowStart = referenceDate.AddDays(-45); // Approx 30 trading days
        int countIn30 = records.Count(r => r.Date >= thirtyDayWindowStart);
        assessment.DaysInLast30 = countIn30;

        // Evaluate Rules
        // Rule 1: 3 consecutive days -> Disposition
        // Rule 2: 5 consecutive days -> Disposition
        // Rule 3: 6 in 10 days -> Disposition
        // Rule 4: 12 in 30 days -> Disposition

        if (consecutive >= 3 || countIn10 >= 6 || countIn30 >= 12)
        {
            assessment.Level = RiskLevel.Danger;
            assessment.Reason = $"Consecutive: {consecutive}, In 10 Days: {countIn10}, In 30 Days: {countIn30}";
        }
        else if (consecutive == 2 || countIn10 >= 4 || countIn30 >= 9)
        {
            assessment.Level = RiskLevel.Warning;
            assessment.Reason = "Approaching threshold";
        }

        return assessment;
    }
}
