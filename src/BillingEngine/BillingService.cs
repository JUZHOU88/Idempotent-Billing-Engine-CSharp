using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace BillingEngine;

/// <summary>
/// Creates invoices once per source key, including on simultaneous requests.
/// Demo only: persistence, authorization and tax rules are deliberately out of scope.
/// </summary>
public sealed class BillingService(InvoiceStore store)
{
    public BillingResult Bill(BillingRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Validate(request);

        var fingerprint = Fingerprint(request);
        return store.GetOrCreate(request.SourceKey, fingerprint, () =>
        {
            var lines = request.Items.Select(item => new InvoiceLine(
                item.Code, item.Description, item.UnitPrice, item.Quantity,
                decimal.Round(item.UnitPrice * item.Quantity, 2, MidpointRounding.AwayFromZero)))
                .ToArray();

            var total = lines.Sum(line => line.Total);
            return new Invoice(Guid.NewGuid(), request.SourceKey, request.CustomerId,
                Array.AsReadOnly(lines), total, DateTimeOffset.UtcNow);
        });
    }

    private static void Validate(BillingRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SourceKey))
            throw new ArgumentException("Source key is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.CustomerId))
            throw new ArgumentException("Customer ID is required.", nameof(request));
        if (request.Items is null || request.Items.Count == 0)
            throw new ArgumentException("At least one item is required.", nameof(request));
        if (request.Items.Any(item => item is null))
            throw new ArgumentException("Items may not be null.", nameof(request));
        if (request.Items.Any(item => string.IsNullOrWhiteSpace(item.Code)
                                   || string.IsNullOrWhiteSpace(item.Description)
                                   || item.Quantity <= 0 || item.UnitPrice < 0))
            throw new ArgumentException("Item code, description, price and quantity must be valid.", nameof(request));
        if (request.Items.GroupBy(item => item.Code, StringComparer.Ordinal).Any(g => g.Count() > 1))
            throw new ArgumentException("Item codes must be unique within a request.", nameof(request));
    }

    // Length-prefixed fields prevent ambiguity from separators inside user input.
    private static string Fingerprint(BillingRequest request)
    {
        var sb = new StringBuilder();
        Add(sb, request.CustomerId);
        // Sorting makes equivalent item sets produce the same fingerprint.
        foreach (var item in request.Items.OrderBy(x => x.Code, StringComparer.Ordinal))
        {
            Add(sb, item.Code);
            Add(sb, item.Description);
            Add(sb, item.UnitPrice.ToString("G29", CultureInfo.InvariantCulture));
            Add(sb, item.Quantity.ToString(CultureInfo.InvariantCulture));
        }
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString())));
    }

    private static void Add(StringBuilder sb, string field) =>
        sb.Append(field.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(field);
}
