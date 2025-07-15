FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test

WORKDIR /app

# Copy solution and project files
COPY eShop.sln ./
COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY nuget.config ./

# Copy only the non-MAUI projects for dependency restoration
COPY src/eShop.AppHost/ ./src/eShop.AppHost/
COPY src/Basket.API/ ./src/Basket.API/
COPY src/Catalog.API/ ./src/Catalog.API/
COPY src/eShop.ServiceDefaults/ ./src/eShop.ServiceDefaults/
COPY src/EventBus/ ./src/EventBus/
COPY src/EventBusRabbitMQ/ ./src/EventBusRabbitMQ/
COPY src/Identity.API/ ./src/Identity.API/
COPY src/IntegrationEventLogEF/ ./src/IntegrationEventLogEF/
COPY src/Mobile.Bff.Shopping/ ./src/Mobile.Bff.Shopping/
COPY src/Ordering.API/ ./src/Ordering.API/
COPY src/OrderProcessor/ ./src/OrderProcessor/
COPY src/Ordering.Domain/ ./src/Ordering.Domain/
COPY src/Ordering.Infrastructure/ ./src/Ordering.Infrastructure/
COPY src/PaymentProcessor/ ./src/PaymentProcessor/
COPY src/WebApp/ ./src/WebApp/
COPY src/WebAppComponents/ ./src/WebAppComponents/
COPY src/WebhookClient/ ./src/WebhookClient/
COPY src/Webhooks.API/ ./src/Webhooks.API/

# Copy only the non-MAUI test projects
COPY tests/Basket.UnitTests/ ./tests/Basket.UnitTests/
COPY tests/Catalog.FunctionalTests/ ./tests/Catalog.FunctionalTests/
COPY tests/Ordering.FunctionalTests/ ./tests/Ordering.FunctionalTests/
COPY tests/Ordering.UnitTests/ ./tests/Ordering.UnitTests/

# Restore dependencies for non-MAUI projects only
RUN dotnet restore src/eShop.AppHost/eShop.AppHost.csproj
RUN dotnet restore src/Basket.API/Basket.API.csproj
RUN dotnet restore src/Catalog.API/Catalog.API.csproj
RUN dotnet restore src/Identity.API/Identity.API.csproj
RUN dotnet restore src/Mobile.Bff.Shopping/Mobile.Bff.Shopping.csproj
RUN dotnet restore src/Ordering.API/Ordering.API.csproj
RUN dotnet restore src/OrderProcessor/OrderProcessor.csproj
RUN dotnet restore src/PaymentProcessor/PaymentProcessor.csproj
RUN dotnet restore src/WebApp/WebApp.csproj
RUN dotnet restore src/WebhookClient/WebhookClient.csproj
RUN dotnet restore src/Webhooks.API/Webhooks.API.csproj
RUN dotnet restore tests/Basket.UnitTests/Basket.UnitTests.csproj
RUN dotnet restore tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj
RUN dotnet restore tests/Ordering.FunctionalTests/Ordering.FunctionalTests.csproj
RUN dotnet restore tests/Ordering.UnitTests/Ordering.UnitTests.csproj

# Copy the rest of the code (excluding MAUI projects)
COPY . .

# Install ReportGenerator tool for test coverage reports
RUN dotnet tool install -g dotnet-reportgenerator-globaltool

# Add dotnet tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Run tests with coverage (excluding MAUI projects)
CMD ["bash", "-c", "echo GLIBC VERSION && ldd --version && echo GLIBC VERSION CHECK && dotnet test --collect:'XPlat Code Coverage' --results-directory ./tests/TestResults --logger trx --logger 'console;verbosity=detailed' tests/Basket.UnitTests/Basket.UnitTests.csproj tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj tests/Ordering.FunctionalTests/Ordering.FunctionalTests.csproj tests/Ordering.UnitTests/Ordering.UnitTests.csproj && find ./tests/TestResults -name 'coverage.cobertura.xml' -exec cp {} ./tests/TestResults/coverage.cobertura.xml \\; && reportgenerator -reports:./tests/TestResults/coverage.cobertura.xml -targetdir:./tests/TestResults/CoverageReport -reporttypes:'Html;Cobertura' && echo 'Coverage report generated in ./tests/TestResults/CoverageReport'"]