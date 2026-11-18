# Variants: adapting the Create slice to other operations

The templates in `assets/` and `scripts/new-slice.ps1` produce the POST/create shape. For other
shapes, copy the closest existing Customers file instead of starting from a template, and change
only what the table says. Paths relative to `backend/src/`.

| Shape | Copy from | What differs from Create |
|-------|-----------|--------------------------|
| GET by id | `Api/Features/Customers/CustomerGetById/GetByIdCustomer.Endpoint.cs`, `Application/Features/Customers/GetCustomerById.Handler.cs` | `MapGet("/{id:guid}")`, command carries only the `Guid`, no DTO and no `ValidationFilter`, handler uses `AsNoTracking()`, failure -> `Results.NotFound(result.Error)`, success -> `Results.Ok(result.Value)` |
| PUT / update | `CustomerUpdate/UpdateCustomer.Endpoint.cs`, `UpdateCustomer.Handler.cs` | `MapPut("/{id:guid}")` with `string id` plus `[FromBody]` DTO, command is `(Guid Id, TDto Dto)`, handler loads the entity, returns `NotFound` `ResultError` if missing, calls `entity.Update(...)` (sets `UpdatedAt`), returns the full updated record |
| DELETE | `CustomerDelete/DeleteCustomer.Endpoint.cs`, `DeleteCustomer.Handler.cs` | Handler returns `Result<Success>` (`Success` is the empty result type in `Domain/Results/Result.cs`), endpoint returns `Results.NoContent()`. Use `{id:guid}` in the route (see caveats) |
| List (cursor paging) | `CustomersGet/GetCustomers.Endpoint.cs`; handler: `Features/Senders/GetSenders.Handler.cs` or `Features/Invoices/GetInvoices.Handler.cs` | `[AsParameters]` query DTO with `Guid? Cursor, bool? IsNextPage, int PageSize`; the handler fetches `pageSize + 1`, then calls `CursorPagination.CreatePage(...)`. Use the Senders/Invoices handlers as the model - `GetCustomers.Handler.cs` inlines the same logic instead of calling the helper |

## Existing inconsistencies - do not propagate

These are real quirks in the current code. Match the intent, not the accident:

- **`UpdateCustomer.Endpoint.cs` declares `Produces(204)` but returns `200 OK` with a body.** The
  integration test asserts 200. Declare the status code the endpoint actually returns.
- **`DeleteCustomer.Endpoint.cs` maps `/{id}` without the `:guid` constraint**, so
  `DELETE /api/customers/not-a-guid` throws in `Guid.Parse` and surfaces as a 500 rather than the
  400 its description promises. Always use `/{id:guid}`.
- **Every failure is mapped to one status** (Create -> 400, Get/Update/Delete -> 404) regardless
  of `Error.Type`. That is fine while a handler has a single failure mode. If a new handler can fail
  in more than one way (for example NotFound and Conflict), switch on `result.Error!.Type`
  (`NotFound` -> 404, `Conflict` -> 409, `Validation` -> 400) and declare each in `Produces`.
- **`.Produces<object>(400, ...)` on GET/DELETE** is a placeholder. Prefer `ValidationProblemDetails`
  (validated bodies) or omit it.

## When the operation needs new persistence

The script and templates assume the entity and its DbSet already exist. If the feature is a new
entity, or a change needs a new column, the following are separate decisions to confirm with the
user before making them:

- New entity in `Domain/Entities/` following the sealed / private-setter / `Create` + `Update` pattern
- `DbSet<T>` on `Database/InvoiceBuilderContext.cs` and an `IEntityTypeConfiguration<T>` in
  `Database/Configurations/` (the context calls `ApplyConfigurationsFromAssembly`, so no manual
  registration is needed)
- An EF Core migration under `Database/Migrations/` (schema change - always ask first)
- Seed data in `Api/Shared/Extensions/DataExtensions.cs` (namespace `InvoiceBuilder.Database.Extensions`,
  but the file lives in the Api project) only if the user wants demo rows

## Frontend follow-up (only when asked)

The Blazor app calls the API through Refit interfaces in
`frontend/src/InvoiceBuilder.Web/Services/` (see `ICustomersApiClient.cs`). Add one method per new
endpoint, reusing the Application DTO and result types directly (the Web project references them):

```csharp
[Post("/api/customers/{id}/duplicate")]
Task<DuplicateCustomerResponseDto> DuplicateCustomerAsync(Guid id, [Body] DuplicateCustomerDto request);
```

A brand-new feature needs its own `I{Feature}ApiClient` registered in the Web `Program.cs` with
`AddRefitGeneratedClient<T>()`, mirroring `ICustomersApiClient`.
