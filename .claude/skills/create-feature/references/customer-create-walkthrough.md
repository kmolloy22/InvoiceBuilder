# The reference slice: Customers / CustomerCreate

`POST /api/customers` is the canonical example. Read the real files when in doubt - they are
the source of truth and this page can drift. Paths are relative to `backend/src/`.

## Request path, top to bottom

| # | Layer | File | Role |
|---|-------|------|------|
| 1 | Api | `InvoiceBuilder.Api/Program.cs` | Calls `app.MapCustomers()`; MediatR and FluentValidation are registered by assembly scan, so new handlers/validators need no DI wiring |
| 2 | Api | `Features/Customers/CustomerEndpoint.cs` | `MapGroup("api/customers").WithTags("Customers")`, then one `group.Map...Endpoint()` per operation |
| 3 | Api | `Features/Customers/CustomerCreate/CreateCustomer.Endpoint.cs` | Static class, one extension method. Binds the DTO, sends the command, maps `Result<T>` to HTTP |
| 4 | Api | `Shared/Infrastructure/Validation/ValidationFilter.cs` | Endpoint filter added with `.AddEndpointFilter<ValidationFilter<TDto>>()`. Finds the DTO in the endpoint arguments, runs the FluentValidation validator, returns `Results.ValidationProblem` (400) keyed by property name |
| 5 | Application | `Features/Customers/Validation/CreateCustomerDtoValidator.cs` | `public sealed` `AbstractValidator<TDto>` with `RuleFor(...)` |
| 6 | Application | `Features/Customers/Models/Create/CreateCustomerDto.cs` | `public sealed record` request body, positional parameters |
| 7 | Application | `Features/Customers/CreateCustomer.Handler.cs` | Command record and handler in one file. Handler injects `InvoiceBuilderContext` directly |
| 8 | Domain | `InvoiceBuilder.Domain/Entities/Customer.cs` | `Customer.Create(...)` factory sets `Id = Guid.NewGuid()` and `CreatedAt = DateTime.UtcNow`; setters are private |
| 9 | Application | `Shared/Responses/Customers/CreateCustomerResult.cs` | Small record the handler returns inside `Result<T>` |
| 10 | Application | `Features/Customers/Models/Create/CreateCustomerResponseDto.cs` | What the endpoint returns to the client (`Id`, `Location`, `CreatedAt`) |

Steps 9 and 10 are deliberately separate: the handler returns a domain-flavoured result, and the
endpoint decides the wire format (including building `Location`). Do not return EF entities from
handlers or endpoints.

## Naming rules (derive everything from Feature / Entity / Operation)

With Feature = `Customers`, Entity = `Customer`, Operation = `Create`:

- Api folder and namespace segment: `{Entity}{Op}` -> `CustomerCreate`
- Api file / class / method: `{Op}{Entity}.Endpoint.cs`, `{Op}{Entity}Endpoint`, `Map{Op}{Entity}Endpoint`
- Application handler file: `{Op}{Entity}.Handler.cs`, holding `{Op}{Entity}Command` and `{Op}{Entity}Handler`
- DTOs: `Models/{Op}/{Op}{Entity}Dto.cs` (+ `{Op}{Entity}ResponseDto.cs` for POST)
- Validator: `Validation/{Op}{Entity}DtoValidator.cs`
- Result: `Shared/Responses/{Feature}/{Op}{Entity}Result.cs`
- Endpoint name (`WithName`): `{Op}{Entity}`
- Route group: `api/{feature lowercased}`
- DbSet on `InvoiceBuilderContext`: the plural Feature name (`Customers`)

The Invoices feature has a typo (`GetInvoicess.Endpoint.cs`, `InvoicesGet`) - do not copy it.

## Behaviour worth reproducing

- **Handler returns `Result<T>`**, never throws for expected failures. Failures use
  `new ResultError(code, message, ResultErrorType.X)` (`Validation`, `NotFound`, `Conflict`,
  `Forbidden`, `FailedDependency`, `Retryable`, `Unexpected`).
- **Create returns 201** with `Results.Created(location, response)`. The id in `Id`/`Location` is
  formatted `ToString("N")` (32 hex characters, no hyphens); the GET route's `{id:guid}` accepts it.
- **Validation is declarative.** The endpoint never inspects the DTO itself. Validation error keys
  are PascalCase property names, and the Blazor form maps them straight back onto fields.
- **Reads use `AsNoTracking()`**; writes go through the entity's `Create`/`Update` methods.
- **Unexpected exceptions** bubble to `GlobalExceptionHandler` (500 problem details with a
  `TraceId`). Do not add try/catch in endpoints or handlers.
- **OpenAPI metadata** on every endpoint: `WithName`, `WithSummary`, `WithDescription`, and
  `Produces...` for each status code the endpoint really returns.
