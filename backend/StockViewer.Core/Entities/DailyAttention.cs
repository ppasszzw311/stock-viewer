using System.ComponentModel.DataAnnotations;

namespace StockViewer.Core.Entities;

public class DailyAttention
{
    public int Id { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    // We can store the raw reason or details if needed
    public string? Reason { get; set; }
    
    public Stock? Stock { get; set; }
}
