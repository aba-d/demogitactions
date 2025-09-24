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
# FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
# ARG LANGUAGE_VERSION=8.0
# FROM mcr.microsoft.com/dotnet/aspnet:${LANGUAGE_VERSION} AS runtime
# WORKDIR /app

# # Accept build path from GitHub Action
# ARG APP_PATH=./output
# COPY ${APP_PATH}/ ./

# # ✅ Important for ECS: Force app to listen on port 8080
# ENV ASPNETCORE_URLS=http://+:8080 \
#     DOTNET_RUNNING_IN_CONTAINER=true \
#     DOTNET_USE_POLLING_FILE_WATCHER=true \
#     DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
#     DOTNET_NOLOGO=true \
#     DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

# RUN addgroup --system appgroup && \
#     adduser --system --ingroup appgroup appuser && \
#     chown -R appuser:appgroup /app
# USER appuser

# HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
#   CMD curl --fail http://localhost:8080/weatherforecast || exit 1

# EXPOSE 8080
# ENTRYPOINT ["dotnet", "dotnet-ecs-sample.dll"]



# =========================================
# Multi-language Dockerfile (Production-ready)
# Supports: .NET, Java, Node.js, Python
# =========================================

# ---------- Stage 1: Base runtime images ----------
ARG LANGUAGE=csharp
ARG LANGUAGE_VERSION=8.0

FROM mcr.microsoft.com/dotnet/aspnet:${LANGUAGE_VERSION} AS csharp-runtime
FROM eclipse-temurin:${LANGUAGE_VERSION}-jre AS java-runtime
FROM node:${LANGUAGE_VERSION}-alpine AS node-runtime
FROM python:${LANGUAGE_VERSION}-slim AS python-runtime

# ---------- Stage 2: Final image ----------
FROM ${LANGUAGE}-runtime AS final
WORKDIR /app

# Copy built artifacts from CI/CD workflow
ARG APP_PATH=./output
COPY ${APP_PATH}/ ./

# ---------- Stage 3: General environment variables ----------
# Only truly immutable defaults (safe for all languages)
ENV PORT=8080

# ---------- Stage 4: Non-root user ----------
RUN addgroup --system appgroup && \
    adduser --system --ingroup appgroup appuser && \
    chown -R appuser:appgroup /app
USER appuser

# ---------- Stage 5: Healthcheck ----------
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl --fail http://localhost:$PORT/health || exit 1

EXPOSE $PORT

# ---------- Stage 6: Dynamic entrypoint ----------
CMD ["sh", "-c", "\
if [ \"$LANGUAGE\" = 'csharp' ]; then \
    exec dotnet dotnet-ecs-sample.dll; \
elif [ \"$LANGUAGE\" = 'java' ]; then \
    JAR_FILE=$(ls *.jar | head -n 1); \
    exec java -jar $JAR_FILE; \
elif [ \"$LANGUAGE\" = 'node' ]; then \
    ENTRY_FILE=$(ls *.js | head -n 1); \
    exec node $ENTRY_FILE; \
elif [ \"$LANGUAGE\" = 'python' ]; then \
    ENTRY_FILE=$(ls *.py | head -n 1); \
    exec python $ENTRY_FILE; \
else \
    echo \"Unsupported language: $LANGUAGE\"; exit 1; \
fi"]
