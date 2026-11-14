using BillingEngine;

var service = new BillingService(new InvoiceStore());
var request = new BillingRequest(
    "school-2026-T3-student-001",
    "student-001",
    [
        new BillingItem("TUITION", "Term tuition", 120.00m, 1),
        new BillingItem("LAB", "Laboratory charge", 15.50m, 2)
    ]);

var first = service.Bill(request);
var retry = service.Bill(request);

Console.WriteLine($"Invoice: {first.Invoice.Id}");
Console.WriteLine($"Total: {first.Invoice.Total:0.00}");
Console.WriteLine($"First attempt created an invoice: {first.Created}");
Console.WriteLine($"Retry created a new invoice: {retry.Created}");
Console.WriteLine($"Same invoice ID: {first.Invoice.Id == retry.Invoice.Id}");
