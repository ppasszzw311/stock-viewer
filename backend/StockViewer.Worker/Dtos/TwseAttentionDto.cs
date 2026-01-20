using System.Text.Json.Serialization;

namespace StockViewer.Worker.Dtos;

public class TwseAttentionDto
{
    [JsonPropertyName("Code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Date")]
    public string Date { get; set; } = string.Empty; // Format: 1130120 (ROC)

    [JsonPropertyName("TradingInfoForAttention")]
    public string TradingInfoForAttention { get; set; } = string.Empty;
}
