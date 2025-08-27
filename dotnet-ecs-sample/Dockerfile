
# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy only the project file and restore dependencies
COPY dotnet-ecs-sample/*.csproj ./dotnet-ecs-sample/
RUN dotnet restore ./dotnet-ecs-sample/dotnet-ecs-sample.csproj

# Copy the rest of the source code
COPY ./dotnet-ecs-sample/ ./
RUN ls -la
RUN pwd
RUN dotnet build ./dotnet-ecs-sample/dotnet-ecs-sample.csproj -c Release --no-restore
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Create the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy published output from build stage
COPY --from=build /app/publish ./

# Set default environment variables (can be overridden at runtime)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# Create a non-root user and switch to it
RUN addgroup --system appgroup &&     adduser --system --ingroup appgroup appuser &&     mkdir -p /app && chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Declare volume for read-only file system
VOLUME /app

# Health check configuration
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3   CMD curl --fail http://localhost:80/weatherforecast || exit 1

RUN ls -la /app

EXPOSE 80
ENTRYPOINT ["dotnet", "dotnet-ecs-sample.dll"]
