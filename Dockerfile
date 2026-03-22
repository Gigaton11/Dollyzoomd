# Multi-stage Dockerfile for DollyZoomd on Google Cloud Run

# Stage 1: Builder
FROM mcr.microsoft.com/dotnet/nightly/sdk:10.0-alpine AS builder
WORKDIR /src

# Copy project file
COPY ["DollyZoomd/DollyZoomd.csproj", "DollyZoomd/"]

# Restore dependencies
RUN dotnet restore "DollyZoomd/DollyZoomd.csproj"

# Copy source code
COPY DollyZoomd/ DollyZoomd/

# Build in Release mode
WORKDIR /src/DollyZoomd
RUN dotnet build -c Release -o /app/build

# Stage 2: Publish
FROM builder AS publish
RUN dotnet publish -c Release -o /app/publish

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/nightly/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install curl for health checks
RUN apk add --no-cache curl

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Expose port (Cloud Run reads PORT environment variable)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check endpoint for Cloud Run
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run application
ENTRYPOINT ["dotnet", "DollyZoomd.dll"]
