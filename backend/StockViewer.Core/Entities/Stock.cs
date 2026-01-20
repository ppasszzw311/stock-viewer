using System.ComponentModel.DataAnnotations;

namespace StockViewer.Core.Entities;

public class Stock
{
    [Key]
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
