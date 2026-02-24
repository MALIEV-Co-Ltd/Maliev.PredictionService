# syntax=docker/dockerfile:1.4
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config and all project files for proper restore
COPY nuget.config ./
COPY ["Maliev.PredictionService.slnx", "./"]
COPY ["Maliev.PredictionService.Api/Maliev.PredictionService.Api.csproj", "Maliev.PredictionService.Api/"]
COPY ["Maliev.PredictionService.Application/Maliev.PredictionService.Application.csproj", "Maliev.PredictionService.Application/"]
COPY ["Maliev.PredictionService.Domain/Maliev.PredictionService.Domain.csproj", "Maliev.PredictionService.Domain/"]
COPY ["Maliev.PredictionService.Infrastructure/Maliev.PredictionService.Infrastructure.csproj", "Maliev.PredictionService.Infrastructure/"]

# Restore dependencies with private NuGet feed credentials
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.PredictionService.Api/Maliev.PredictionService.Api.csproj"

# Copy everything and build
COPY . .
WORKDIR "/src/Maliev.PredictionService.Api"
RUN dotnet build "Maliev.PredictionService.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "Maliev.PredictionService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Ensure 'app' owns the workdir (app user already exists in ASP.NET runtime image)
RUN chown -R app:app /app

# Switch to non-root user
USER app

# Copy published application (now owned by app)
COPY --chown=app:app --from=publish /app/publish .

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:8080/predictionservice/liveness || exit 1

ENTRYPOINT ["dotnet", "Maliev.PredictionService.Api.dll"]
