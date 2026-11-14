using BillingEngine;

var tests = new (string Name, Action Run)[]
{
    ("creates invoice and rounds line amounts", CreatesInvoice),
    ("same request is idempotent", RepeatDoesNotDuplicate),
    ("different payload using same key conflicts", ConflictingPayload),
    ("item order does not change identity", ReorderedItems),
    ("invalid input is rejected", InvalidInput),
    ("distinct keys create distinct invoices", DistinctKeys),
    ("concurrent requests create exactly one invoice", ConcurrentRetries)
};

var failures = 0;
foreach (var (name, run) in tests)
{
    try { run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception error)
    {
        failures++;
        Console.Error.WriteLine($"FAIL {name}: {error.Message}");
    }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed");
return failures == 0 ? 0 : 1;

static BillingRequest Request(string key = "source-001") => new(key, "student-001", [
    new BillingItem("A", "Fee A", 10.1275m, 2),
    new BillingItem("B", "Fee B", 2.00m, 1)
]);
static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}
static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}");
}
static void CreatesInvoice()
{
    var r = new BillingService(new InvoiceStore()).Bill(Request());
    Check(r.Created && r.Invoice.Total == 22.26m, "Wrong amount or creation flag");
    Check(r.Invoice.Lines.Count == 2, "Incorrect line count");
}
static void RepeatDoesNotDuplicate()
{
    var store = new InvoiceStore(); var service = new BillingService(store);
    var first = service.Bill(Request()); var second = service.Bill(Request());
    Check(first.Created && !second.Created && first.Invoice.Id == second.Invoice.Id && store.Count == 1,
        "Repeated request created a duplicate");
}
static void ConflictingPayload()
{
    var service = new BillingService(new InvoiceStore());
    service.Bill(Request());
    Throws<BillingConflictException>(() => service.Bill(new BillingRequest(
        "source-001", "student-002", [new BillingItem("X", "Other", 1m, 1)])));
}
static void ReorderedItems()
{
    var store = new InvoiceStore(); var service = new BillingService(store);
    var first = service.Bill(Request());
    var reversed = new BillingRequest("source-001", "student-001", Request().Items.Reverse().ToArray());
    var retry = service.Bill(reversed);
    Check(!retry.Created && first.Invoice.Id == retry.Invoice.Id, "Item ordering changed identity");
}
static void InvalidInput()
{
    var service = new BillingService(new InvoiceStore());
    Throws<ArgumentException>(() => service.Bill(new BillingRequest("", "student", [new BillingItem("X", "Fee", 1m, 1)])));
    Throws<ArgumentException>(() => service.Bill(new BillingRequest("key", "student", [new BillingItem("X", "Fee", -1m, 1)])));
    Throws<ArgumentException>(() => service.Bill(new BillingRequest("key", "student", [])));
    Throws<ArgumentException>(() => service.Bill(new BillingRequest("key", "student", [new BillingItem("X", "One", 1m, 1), new BillingItem("X", "Two", 2m, 1)])));
}
static void DistinctKeys()
{
    var store = new InvoiceStore(); var service = new BillingService(store);
    var a = service.Bill(Request("source-a")); var b = service.Bill(Request("source-b"));
    Check(a.Invoice.Id != b.Invoice.Id && store.Count == 2, "Distinct sources were incorrectly merged");
}
static void ConcurrentRetries()
{
    var store = new InvoiceStore(); var service = new BillingService(store);
    var results = new BillingResult[40];
    Parallel.For(0, results.Length, i => results[i] = service.Bill(Request()));
    Check(results.Count(r => r.Created) == 1, "More than one invoice was created");
    Check(results.Select(r => r.Invoice.Id).Distinct().Count() == 1 && store.Count == 1,
        "Concurrent callers received inconsistent results");
}
