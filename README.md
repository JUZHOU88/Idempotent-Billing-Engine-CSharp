# Idempotent Billing Engine — C# / .NET 10

**Small, runnable engineering portfolio project** demonstrating clean C# design, monetary calculations, request validation, safe retries and concurrent duplicate prevention.

> Independent educational demonstration by **Justice Zhou**. This is **not** the source code of NetSchool Plus or a production billing system.

## Why this exists

Real business workflows often retry after a timeout. A retry should not create a second invoice for the same billing source. This small project models that requirement and makes the behavior easy to inspect.

## Features

- Typed billing requests and immutable invoice result records.
- Decimal-based totals, rounded to two decimal places using `MidpointRounding.AwayFromZero` **per line**.
- Validates source key, customer identity, item codes, quantities and prices.
- Deterministic SHA-256 fingerprint of the request payload, with line items ordered by code.
- **Idempotency:** same source key + equivalent payload returns the existing invoice.
- **Conflict detection:** same source key + changed payload throws `BillingConflictException`.
- A synchronized in-memory store that prevents duplicate creation from concurrent calls in one process.
- Seven test scenarios using a dependency-free console test runner.
- GitHub Actions build/test workflow.

## Run it

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then from this repository root:

```bash
dotnet run --project src/BillingDemo/BillingDemo.csproj
dotnet run --project tests/BillingTests/BillingTests.csproj
```

Expected demo behavior:

```text
Invoice: <generated-guid>
Total: 151.00
First attempt created an invoice: True
Retry created a new invoice: False
Same invoice ID: True
```

The test runner prints one `PASS` line per scenario and returns a nonzero exit code on failure. See the GitHub Actions tab for real build/test results after publication.

## Structure

```text
src/
  BillingEngine/      Reusable domain library + concurrency-safe in-memory store
  BillingDemo/        Runnable console demonstration
tests/
  BillingTests/       Executable test harness (.NET console app)
.github/workflows/    CI build and test configuration
```

## Design discussion

The `SourceKey` represents an external billing identity (for example, an academic term plus a student ID). Reusing it with an unchanged request returns the original result. Reusing it for a different request is a conflict rather than silently accepting inconsistent data.

The store's lock covers *both* checking for an existing source and inserting the invoice, preventing same-process races. It stores a request fingerprint alongside the invoice. Sorting and length-prefixing the payload fields make the fingerprint deterministic and avoid ambiguities due to delimiter characters.

### Important limitations

- **In-memory only:** data disappears when the process ends; not multi-process or distributed.
- No database transactions, unique database constraints, API endpoints, authentication, taxes, currency conversion, or persistent audit trail.
- Tax, discounts and jurisdiction-specific rounding rules are intentionally omitted.
- This example is not suitable for real billing or financial processing without further engineering and review.
- The .NET SDK was not installed in the authoring environment, so compilation and tests should be confirmed via GitHub Actions or local execution rather than assuming they have passed.

## Next engineering iterations

- Add SQLite persistence and a unique constraint on `SourceKey`.
- Test process restarts and transaction rollback.
- Expose a minimal HTTP API with request idempotency keys.
- Add audit events, robust error handling and structured logging.

## Author

**Justice Zhou** — Software Engineer · C#/.NET · Backend systems · IoT

[GitHub Profile](https://github.com/JUZHOU88) · [NetSchool Plus architecture case study](https://github.com/JUZHOU88/NetSchoolPlus-Engineering-Case-Study)

Licensed under MIT. This is original demonstration code, separate from commercial projects.
