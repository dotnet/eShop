FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test

WORKDIR /app

# Copy solution and project files
COPY eShop.sln ./
COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY nuget.config ./

# Copy all source project files
COPY src/eShop.AppHost/eShop.AppHost.csproj ./src/eShop.AppHost/
COPY src/Basket.API/Basket.API.csproj ./src/Basket.API/
COPY src/Catalog.API/Catalog.API.csproj ./src/Catalog.API/
COPY src/eShop.ServiceDefaults/eShop.ServiceDefaults.csproj ./src/eShop.ServiceDefaults/
COPY src/EventBus/EventBus.csproj ./src/EventBus/
COPY src/EventBusRabbitMQ/EventBusRabbitMQ.csproj ./src/EventBusRabbitMQ/
COPY src/Identity.API/Identity.API.csproj ./src/Identity.API/
COPY src/IntegrationEventLogEF/IntegrationEventLogEF.csproj ./src/IntegrationEventLogEF/
COPY src/Mobile.Bff.Shopping/Mobile.Bff.Shopping.csproj ./src/Mobile.Bff.Shopping/
COPY src/Ordering.API/Ordering.API.csproj ./src/Ordering.API/
COPY src/OrderProcessor/OrderProcessor.csproj ./src/OrderProcessor/
COPY src/Ordering.Domain/Ordering.Domain.csproj ./src/Ordering.Domain/
COPY src/Ordering.Infrastructure/Ordering.Infrastructure.csproj ./src/Ordering.Infrastructure/
COPY src/PaymentProcessor/PaymentProcessor.csproj ./src/PaymentProcessor/
COPY src/WebApp/WebApp.csproj ./src/WebApp/
COPY src/WebhookClient/WebhookClient.csproj ./src/WebhookClient/
COPY src/Webhooks.API/Webhooks.API.csproj ./src/Webhooks.API/
COPY src/WebAppComponents/WebAppComponents.csproj ./src/WebAppComponents/
COPY src/HybridApp/HybridApp.csproj ./src/HybridApp/
COPY src/ClientApp/ClientApp.csproj ./src/ClientApp/

# Copy all test project files
COPY tests/Basket.UnitTests/Basket.UnitTests.csproj ./tests/Basket.UnitTests/
COPY tests/Catalog.FunctionalTests/Catalog.FunctionalTests.csproj ./tests/Catalog.FunctionalTests/
COPY tests/Ordering.FunctionalTests/Ordering.FunctionalTests.csproj ./tests/Ordering.FunctionalTests/
COPY tests/Ordering.UnitTests/Ordering.UnitTests.csproj ./tests/Ordering.UnitTests/
COPY tests/ClientApp.UnitTests/ClientApp.UnitTests.csproj ./tests/ClientApp.UnitTests/

# Restore dependencies
RUN dotnet restore

# Copy the rest of the code
COPY . .

# Install ReportGenerator tool for test coverage reports
RUN dotnet tool install -g dotnet-reportgenerator-globaltool

# Add dotnet tools to PATH
ENV PATH="${PATH}:/root/.dotnet/tools"

# Run tests with coverage
CMD ["bash", "-c", "echo GLIBC VERSION && ldd --version && echo GLIBC VERSION CHECK && dotnet test --collect:'XPlat Code Coverage' --results-directory ./tests/TestResults --logger trx --logger 'console;verbosity=detailed' && find ./tests/TestResults -name 'coverage.cobertura.xml' -exec cp {} ./tests/TestResults/coverage.cobertura.xml \\; && reportgenerator -reports:./tests/TestResults/coverage.cobertura.xml -targetdir:./tests/TestResults/CoverageReport -reporttypes:'Html;Cobertura' && echo 'Coverage report generated in ./tests/TestResults/CoverageReport'"]