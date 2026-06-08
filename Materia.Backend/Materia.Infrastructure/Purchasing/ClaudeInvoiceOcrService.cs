using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Materia.Application.Contracts.Purchasing;
using Microsoft.Extensions.Configuration;

namespace Materia.Infrastructure.Purchasing;

public class ClaudeInvoiceOcrService(IConfiguration configuration) : IInvoiceOcrService
{
    private static readonly string SystemPrompt =
        """
        You are an invoice scanning assistant for a building materials store.
        Extract purchase order data from the provided invoice image.
        Respond ONLY with a JSON object in this exact format (no markdown, no explanation):
        {
          "supplierName": "string or null",
          "items": [
            { "productName": "string", "quantity": number, "unit": "string", "unitPrice": number }
          ]
        }
        - Use the original language found on the invoice for product names and supplier names.
        - If a field cannot be determined, use null for strings and 0 for numbers.
        - "unit" should be the unit of measure (e.g. pcs, kg, m, roll, sak).
        - "unitPrice" should be price per unit (not total).
        """;

    public async Task<OcrInvoiceScanResult> ScanAsync(
        byte[] imageBytes, string mediaType, CancellationToken ct = default)
    {
        var apiKey = configuration["Anthropic:ApiKey"]
            ?? throw new InvalidOperationException("Anthropic:ApiKey is not configured.");

        var client = new AnthropicClient().WithOptions(opts => opts with { ApiKey = apiKey });

        var base64Data = Convert.ToBase64String(imageBytes);

        ContentBlockParam documentBlock = mediaType == "application/pdf"
            ? new(new DocumentBlockParam(new DocumentBlockParamSource(
                new Base64PdfSource { Data = base64Data })))
            : new(new ImageBlockParam(new ImageBlockParamSource(
                new Base64ImageSource { Data = base64Data, MediaType = mediaType })));

        var messageParams = new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = 1024,
            System = SystemPrompt,
            Messages =
            [
                new()
                {
                    Role = Role.User,
                    Content = new MessageParamContent(new List<ContentBlockParam>
                    {
                        documentBlock,
                        new(new TextBlockParam { Text = "Extract the purchase order data from this invoice." }),
                    }),
                },
            ],
        };

        var response = await client.Messages.Create(messageParams, ct);

        string text = "{}";
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                text = textBlock.Text;
                break;
            }
        }

        return ParseResponse(text);
    }

    private static OcrInvoiceScanResult ParseResponse(string json)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var supplierName = root.TryGetProperty("supplierName", out var sn) && sn.ValueKind != JsonValueKind.Null
                ? sn.GetString()
                : null;

            var items = new List<OcrLineItem>();
            if (root.TryGetProperty("items", out var itemsEl) && itemsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsEl.EnumerateArray())
                {
                    var productName = item.TryGetProperty("productName", out var pn) ? pn.GetString() ?? "" : "";
                    var quantity    = item.TryGetProperty("quantity",    out var q)  ? q.GetDecimal()        : 0m;
                    var unit        = item.TryGetProperty("unit",        out var u)  ? u.GetString() ?? ""   : "";
                    var unitPrice   = item.TryGetProperty("unitPrice",   out var up) ? up.GetDecimal()       : 0m;
                    items.Add(new OcrLineItem(productName, quantity, unit, unitPrice));
                }
            }

            return new OcrInvoiceScanResult(supplierName, items);
        }
        catch
        {
            return new OcrInvoiceScanResult(null, []);
        }
    }
}
