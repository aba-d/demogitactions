#!/bin/bash
set -e  # exit if any command fails

# 🔑 Replace these with your details
ORG_KEY="org-testfordemo"
PROJECT_KEY="org-testfordemo_myapp-netcore"
SONAR_HOST="https://sonarcloud.io"
SONAR_TOKEN="a94e6b04519573bc772705ab69e5ce027fe31bc3"

echo "🚀 Starting SonarScanner analysis..."

# 1️⃣ Start analysis
dotnet sonarscanner begin \
  /o:"$ORG_KEY" \
  /k:"$PROJECT_KEY" \
  /d:sonar.host.url="$SONAR_HOST" \
  /d:sonar.token="$SONAR_TOKEN" \
  /d:sonar.cs.opencover.reportsPaths="./coverage/**/coverage.opencover.xml" \
  /d:sonar.sources="./"

# 2️⃣ Build solution
dotnet build --configuration Release

# 3️⃣ Run tests with coverage
dotnet test ./dotnet-ecs-sample.Tests/dotnet-ecs-sample.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory ./coverage \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=opencover

# 4️⃣ End analysis (upload results)
dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"

echo "✅ Sonar analysis complete!"
