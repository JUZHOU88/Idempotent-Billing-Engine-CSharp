namespace BillingEngine;

/// <summary>
/// Small in-memory, thread-safe store for a demonstration project.
/// The source key is unique within the lifetime of this store instance.
/// </summary>
public sealed class InvoiceStore
{
    private readonly object _gate = new();
    private readonly Dictionary<string, (string Fingerprint, Invoice Invoice)> _invoices =
        new(StringComparer.Ordinal);

    internal BillingResult GetOrCreate(string sourceKey, string fingerprint, Func<Invoice> factory)
    {
        lock (_gate)
        {
            if (_invoices.TryGetValue(sourceKey, out var current))
            {
                if (!StringComparer.Ordinal.Equals(current.Fingerprint, fingerprint))
                    throw new BillingConflictException(
                        "This source key is already associated with a different billing request.");

                return new BillingResult(current.Invoice, Created: false);
            }

            var invoice = factory();
            _invoices.Add(sourceKey, (fingerprint, invoice));
            return new BillingResult(invoice, Created: true);
        }
    }

    public int Count
    {
        get { lock (_gate) return _invoices.Count; }
    }
}
