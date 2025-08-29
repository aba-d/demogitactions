# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0.100 AS build
WORKDIR /src

COPY dotnet-ecs-sample/*.csproj ./dotnet-ecs-sample/
RUN dotnet restore ./dotnet-ecs-sample/dotnet-ecs-sample.csproj

COPY . .
WORKDIR /src/dotnet-ecs-sample
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENV DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=true \
    DOTNET_NOLOGO=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true

RUN useradd -m appuser
USER appuser

HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
  CMD curl --fail http://localhost:80/health || exit 1

RUN ls -la /app

EXPOSE 80
ENTRYPOINT ["dotnet", "dotnet-ecs-sample.dll"]
