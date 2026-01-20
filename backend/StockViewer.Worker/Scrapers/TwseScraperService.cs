using System.Text.Json;
using System.Text.Json.Nodes;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using StockViewer.Core.Entities;
using StockViewer.Worker.Dtos;

namespace StockViewer.Worker.Scrapers;

public class TwseScraperService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TwseScraperService> _logger;

    public TwseScraperService(HttpClient httpClient, ILogger<TwseScraperService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<TwseAttentionDto>> FetchAttentionSecuritiesAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync("https://openapi.twse.com.tw/v1/announcement/notice");
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<List<TwseAttentionDto>>(response, options);
            return data ?? new List<TwseAttentionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching attention securities");
            return new List<TwseAttentionDto>();
        }
    }

    public async Task<List<DispositionRecord>> FetchDispositionSecuritiesAsync()
    {
        var records = new List<DispositionRecord>();
        try
        {
            // TWSE RWD API for Disposition (Punish)
            // The URL might need query parameters for date, but usually returns recent data.
            // We'll try the base URL first.
            var response = await _httpClient.GetStringAsync("https://www.twse.com.tw/rwd/zh/announcement/punish");
            
            // The response is a JSON structure where data is mixed.
            // Based on observation: it might be a JSON object with a 'data' field or a raw array.
            // Let's parse as JsonNode to be safe.
            var jsonNode = JsonNode.Parse(response);
            
            JsonArray? dataArray = null;

            if (jsonNode is JsonObject obj && obj.ContainsKey("data"))
            {
                dataArray = obj["data"] as JsonArray;
            }
            else if (jsonNode is JsonArray arr)
            {
                dataArray = arr;
            }

            if (dataArray == null) return records;

            foreach (var item in dataArray)
            {
                // We expect arrays: [No, Date, Code, Name, ?, Reason, Period, Type, Details, ...]
                if (item is JsonArray row && row.Count > 8)
                {
                    try
                    {
                        var dateStr = row[1]?.ToString(); // 115/01/19
                        var code = row[2]?.ToString();
                        var name = row[3]?.ToString();
                        var periodStr = row[6]?.ToString(); // 115/01/20～115/02/02
                        var measures = row[8]?.ToString();

                        if (string.IsNullOrEmpty(code)) continue;

                        // Parse dates (ROC to AD)
                        // Simple parser for now, can be extracted to a helper
                        var startDate = ParseRocDateRangeStart(periodStr);
                        var endDate = ParseRocDateRangeEnd(periodStr);

                        records.Add(new DispositionRecord
                        {
                            StockCode = code,
                            StartDate = startDate,
                            EndDate = endDate,
                            Measures = measures ?? ""
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error parsing disposition row: {Row}", item.ToJsonString());
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching disposition securities");
        }

        return records;
    }

    private DateOnly ParseRocDateRangeStart(string? range)
    {
        if (string.IsNullOrEmpty(range)) return DateOnly.MinValue;
        // Format: 115/01/20～115/02/02
        var parts = range.Split('～');
        if (parts.Length > 0) return ParseRocDate(parts[0].Trim());
        return DateOnly.MinValue;
    }

    private DateOnly ParseRocDateRangeEnd(string? range)
    {
        if (string.IsNullOrEmpty(range)) return DateOnly.MinValue;
        var parts = range.Split('～');
        if (parts.Length > 1) return ParseRocDate(parts[1].Trim());
        return DateOnly.MinValue;
    }

    private DateOnly ParseRocDate(string dateStr)
    {
        // 115/01/20
        var parts = dateStr.Split('/');
        if (parts.Length == 3)
        {
            int year = int.Parse(parts[0]) + 1911;
            int month = int.Parse(parts[1]);
            int day = int.Parse(parts[2]);
            return new DateOnly(year, month, day);
        }
        return DateOnly.MinValue;
    }
}
