namespace BillingEngine;

public sealed record BillingItem(string Code, string Description, decimal UnitPrice, int Quantity);

public sealed record BillingRequest(string SourceKey, string CustomerId, IReadOnlyList<BillingItem> Items);

public sealed record InvoiceLine(string Code, string Description, decimal UnitPrice, int Quantity, decimal Total);

public sealed record Invoice(
    Guid Id,
    string SourceKey,
    string CustomerId,
    IReadOnlyList<InvoiceLine> Lines,
    decimal Total,
    DateTimeOffset CreatedAt);

public sealed record BillingResult(Invoice Invoice, bool Created);

public sealed class BillingConflictException(string message) : Exception(message);
