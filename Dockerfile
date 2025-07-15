FROM mcr.microsoft.com/dotnet/sdk:9.0 AS test

WORKDIR /app

# Install MAUI workloads
RUN dotnet workload install maui

# Copy solution and project files
COPY eShop.sln ./
COPY global.json ./
COPY Directory.Build.props ./
COPY Directory.Build.targets ./
COPY Directory.Packages.props ./
COPY nuget.config ./

# Copy all project files for dependency restoration
COPY src/ ./src/
COPY tests/ ./tests/

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