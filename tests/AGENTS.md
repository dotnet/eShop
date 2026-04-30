# Test Rules

- Unit tests use MSTest and NSubstitute. Ordering tests use builders in `tests\Ordering.UnitTests\Builders.cs`.
- Use `dotnet test --project <test-project.csproj>`; directory paths fail under Microsoft.Testing.Platform in this repo.
- Functional tests use Aspire `DistributedApplication` fixtures plus `WebApplicationFactory`; they require Docker and may start PostgreSQL/Identity resources.
- Ordering functional tests inject `AutoAuthorizeMiddleware` instead of using real auth.
- Use filters for single-test runs, e.g. `--filter "FullyQualifiedName~CreateOrderCommandHandlerTest"`.
