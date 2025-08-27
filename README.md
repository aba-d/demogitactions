# dotnet-ecs-sample

This is a sample ASP.NET Core Web API project designed to demonstrate a full CI/CD pipeline using GitHub Actions and deployment to AWS ECS.

## 🚀 Project Overview
- ASP.NET Core Web API (.NET 8)
- Dockerized for container deployment
- GitHub Actions for CI/CD
- AWS ECS (Fargate) for hosting

## 📋 Prerequisites
- .NET SDK 8.0
- Docker
- GitHub account
- AWS account with ECS and ECR setup

## 🛠️ Setup Instructions
1. Clone the repository:
   ```bash
   git clone https://github.com/your-username/dotnet-ecs-sample.git
   cd dotnet-ecs-sample
   ```
2. Restore dependencies:
   ```bash
   dotnet restore
   ```
3. Build the project:
   ```bash
   dotnet build
   ```
4. Run the application:
   ```bash
   dotnet run
   ```
   Access the API at `http://localhost:5000/weatherforecast`

## 🐳 Docker Usage
1. Build the Docker image:
   ```bash
   docker build -t dotnet-ecs-sample .
   ```
2. Run the container:
   ```bash
   docker run -p 5000:80 dotnet-ecs-sample
   ```

## ⚙️ GitHub Actions CI/CD Pipeline
The workflow includes:
- Build and test the application
- Code and security scanning using CodeQL
- Build and push Docker image to Amazon ECR
- Deploy to AWS ECS using task definition

## 📄 License
This project is licensed under the MIT License.
