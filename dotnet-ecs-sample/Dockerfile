# # Stage 1: Build
# FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# WORKDIR /src

# # Copy and restore project
# COPY dotnet-ecs-sample/*.csproj ./dotnet-ecs-sample/
# RUN dotnet restore ./dotnet-ecs-sample/dotnet-ecs-sample.csproj

# COPY . .
# WORKDIR /src/dotnet-ecs-sample
# RUN dotnet publish -c Release -o /app/publish

# # Stage 2: Runtime
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# WORKDIR /app

# COPY --from=build /app/publish ./

# # ✅ Important for ECS: Force app to listen on port 8080
# ENV ASPNETCORE_URLS=http://+:8080 \
#     DOTNET_RUNNING_IN_CONTAINER=true \
#     DOTNET_USE_POLLING_FILE_WATCHER=true \
#     DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
#     DOTNET_NOLOGO=true \
#     DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# # Add non-root user for security
# RUN addgroup --system appgroup && \
#     adduser --system --ingroup appgroup appuser && \
#     chown -R appuser:appgroup /app
# USER appuser

# # Healthcheck (update if your app has a real endpoint)
# HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
#   CMD curl --fail http://localhost:8080/weatherforecast || exit 1

# EXPOSE 8080
# ENTRYPOINT ["dotnet", "dotnet-ecs-sample.dll"]

# Stage 1: Runtime only (no SDK needed)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Accept build path from GitHub Action
ARG APP_PATH=./output
COPY ${APP_PATH}/ ./

# ✅ Important for ECS: Force app to listen on port 8080
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_NOLOGO=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

RUN addgroup --system appgroup && \
    adduser --system --ingroup appgroup appuser && \
    chown -R appuser:appgroup /app
USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:8080/weatherforecast || exit 1

EXPOSE 8080
ENTRYPOINT ["dotnet", "dotnet-ecs-sample.dll"]
