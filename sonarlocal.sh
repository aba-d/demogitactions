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
  /d:sonar.exclusions="**/bin/**,**/obj/**,**/*.json,**/*.config,**/*.md,**/*.yml,**/Dockerfile" \
  /d:sonar.test.exclusions="**/bin/**,**/obj/**"

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

# 5️⃣ Show code coverage summary locally
COVERAGE_FILE=$(find ./coverage -type f -name "coverage.opencover.xml" | head -n 1)
if [ -f "$COVERAGE_FILE" ]; then
  echo "🔍 .NET Code Coverage Summary:"
  dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.1.22
  reportgenerator -reports:$COVERAGE_FILE -targetdir:coverage-summary -reporttypes:TextSummary
  cat coverage-summary/Summary.txt
else
  echo "⚠️ No coverage report found."
fi

# 6️⃣ Fetch SonarCloud Quality Gate summary
STATUS_JSON=$(curl -s -u "$SONAR_TOKEN:" "$SONAR_HOST/api/qualitygates/project_status?projectKey=$PROJECT_KEY")
GATE_STATUS=$(echo "$STATUS_JSON" | jq -r '.projectStatus.status')
FAILED_CONDITIONS=$(echo "$STATUS_JSON" | jq -r '.projectStatus.conditions[] | select(.status=="ERROR") | "\(.metricKey): \(.actualValue) (expected: \(.errorThreshold))"')

echo "================== SonarCloud Quality Gate =================="
echo "Project: $PROJECT_KEY"
echo "Status: $GATE_STATUS"
if [[ -n "$FAILED_CONDITIONS" ]]; then
  echo "❌ Failed conditions:"
  echo "$FAILED_CONDITIONS"
else
  echo "✅ All conditions passed!"
fi
echo "=============================================================="