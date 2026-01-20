using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockViewer.Core.Data;
using StockViewer.Core.Entities;
using StockViewer.Core.Services;

namespace StockViewer.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StockController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly RiskCalculatorService _riskCalculator;
    private readonly ILogger<StockController> _logger;

    public StockController(AppDbContext context, RiskCalculatorService riskCalculator, ILogger<StockController> logger)
    {
        _context = context;
        _riskCalculator = riskCalculator;
        _logger = logger;
    }

    [HttpGet("attention")]
    public async Task<ActionResult<List<DailyAttention>>> GetAttentionStocks([FromQuery] DateOnly? date)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.Now);
        // If today is weekend, maybe fallback to last Friday?
        // For now, just query the date.
        
        var records = await _context.DailyAttentions
            .Include(d => d.Stock)
            .Where(d => d.Date == queryDate)
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("disposition")]
    public async Task<ActionResult<List<DispositionRecord>>> GetDispositionStocks([FromQuery] DateOnly? date)
    {
        var queryDate = date ?? DateOnly.FromDateTime(DateTime.Now);
        
        // Find records where queryDate is within StartDate and EndDate
        var records = await _context.DispositionRecords
            .Include(d => d.Stock)
            .Where(d => d.StartDate <= queryDate && d.EndDate >= queryDate)
            .ToListAsync();

        return Ok(records);
    }

    [HttpGet("risk")]
    public async Task<ActionResult<List<RiskAssessment>>> GetRiskAnalysis()
    {
        // Analyze stocks that have been in attention list recently (e.g., last 5 days)
        // Optimization: Only check stocks that have at least one attention record in the last 10 days.
        var recentDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10));
        
        var recentStockCodes = await _context.DailyAttentions
            .Where(d => d.Date >= recentDate)
            .Select(d => d.StockCode)
            .Distinct()
            .ToListAsync();

        var assessments = new List<RiskAssessment>();
        var today = DateOnly.FromDateTime(DateTime.Now);

        foreach (var code in recentStockCodes)
        {
            var assessment = await _riskCalculator.CheckDispositionRiskAsync(code, today);
            if (assessment.Level != RiskLevel.Safe)
            {
                assessments.Add(assessment);
            }
        }

        return Ok(assessments.OrderByDescending(a => a.Level).ThenByDescending(a => a.ConsecutiveDays));
    }
}
