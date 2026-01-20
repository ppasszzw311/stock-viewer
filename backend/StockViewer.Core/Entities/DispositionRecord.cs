using System.ComponentModel.DataAnnotations;

namespace StockViewer.Core.Entities;

public class DispositionRecord
{
    public int Id { get; set; }
    public string StockCode { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Measures { get; set; } = string.Empty;
    
    public Stock? Stock { get; set; }
}
