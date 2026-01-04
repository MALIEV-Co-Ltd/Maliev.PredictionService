# Maliev Prediction Service

[![Build Status](https://img.shields.io/badge/Build-Passing-success)](https://github.com/ORGANIZATION/Maliev.PredictionService)
[![.NET Version](https://img.shields.io/badge/.NET-10.0-blue)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Database](https://img.shields.io/badge/Database-PostgreSQL%2018-blue)](https://www.postgresql.org/)

Intelligent analytics and machine learning service for the Maliev manufacturing ecosystem.

**Role in MALIEV Architecture**: The primary predictive engine of the platform. It leverages historical data from Order, Customer, and Material services to provide demand forecasting, price optimization, and customer behavior insights, driving proactive business decisions.

---

## 🏗️ Architecture & Tech Stack

- **Framework**: ASP.NET Core 10.0 (C# 13)
- **ML Engine**: ML.NET (Core ML framework)
- **Database**: PostgreSQL 18 with Entity Framework Core 10.x
- **Distributed Cache**: Redis 7.x (High-speed prediction caching)
- **Messaging**: RabbitMQ via MassTransit (Async model retraining)
- **Format Support**: ONNX (For advanced model interoperability)
- **Observability**: OpenTelemetry (Metrics, Traces, Logging)

---

## ⚖️ Constitution Rules

This service strictly adheres to the platform development mandates:

### Banned Libraries
To maintain high performance and low complexity, the following are **NOT** used:
- ❌ **AutoMapper**: Explicit manual mapping only.
- ❌ **FluentValidation**: Standard Data Annotations (`[Required]`, `[EmailAddress]`) only.
- ❌ **FluentAssertions**: Standard xUnit `Assert` methods only.
- ❌ **In-memory Test DB**: All integration tests use **Testcontainers** with real PostgreSQL 18.

### Mandatory Practices
- ✅ **TreatWarningsAsErrors**: Enabled in all `.csproj` files.
- ✅ **XML Documentation**: Required on all public methods and properties.
- ✅ **No Secrets in Code**: All sensitive configuration injected via environment variables.
- ✅ **No Test Config in Program.cs**: Test configuration in test fixtures only.
- ✅ **IAM Integration**: Self-registers permissions with the IAM Service using GCP-style naming: `{service}.{resource}.{action}`.

---

## ✨ Key Features

- **Demand Forecasting Engine**: High-precision time-series analysis for inventory optimization and production planning.
- **Dynamic Price Optimization**: ML-driven pricing recommendations based on market elasticity and historical trends.
- **Customer Churn Analysis**: Proactive identification of at-risk business accounts using behavioral pattern recognition.
- **Personalized Recommendations**: Intelligent cross-sell and up-sell suggestions powered by collaborative filtering.
- **Automated Model Lifecycle**: Seamless async retraining and deployment of models as new platform data becomes available.

---

## 🚀 Quick Start

### Prerequisites
- .NET 10.0 SDK
- Docker Desktop (for infrastructure)
- PostgreSQL 18 (Alpine)

### Local Development Setup

1. **Clone the repository**
```bash
git clone https://github.com/ORGANIZATION/Maliev.PredictionService.git
cd Maliev.PredictionService
```

2. **Spin up Infrastructure**
```bash
docker run --name prediction-db -e POSTGRES_PASSWORD=YOUR_PASSWORD -p 5432:5432 -d postgres:18-alpine
docker run --name prediction-redis -p 6379:6379 -d redis:7-alpine
```

3. **Configure Environment**
```powershell
# Windows PowerShell
$env:ConnectionStrings__PredictionDbContext="YOUR_POSTGRES_CONNECTION_STRING"
$env:ConnectionStrings__Cache="YOUR_REDIS_CONNECTION_STRING"
```

4. **Apply Migrations & Run**
```bash
dotnet ef database update --project Maliev.PredictionService.Data
dotnet run --project Maliev.PredictionService.Api
```

The service will be available at `http://localhost:5000/predictions`. Access the interactive documentation at `http://localhost:5000/predictions/scalar`.

---

## 📡 API Endpoints

All endpoints are prefixed with `/predictions/v1/`.

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/demand-forecast` | Predict product demand for a specific period |
| POST | `/price-prediction` | Calculate optimal strategic pricing |
| POST | `/churn-analysis` | Predict customer churn probability |
| GET | `/models/{id}/metrics` | Retrieve model performance and accuracy metrics |

---

## 🏥 Health & Monitoring

Standardized health probes for Kubernetes orchestration:
- **Liveness**: `GET /predictions/liveness`
- **Readiness**: `GET /predictions/readiness` (Checks DB and Redis connectivity)
- **Metrics**: `GET /predictions/metrics` (Prometheus format)

---

## 🧪 Testing

We prioritize reliable tests over mock-heavy unit tests.

```bash
# Run all tests using Testcontainers
dotnet test --verbosity normal
```

- **Integration Tests**: Use real PostgreSQL 18 containers.
- **Contract Tests**: Ensure API stability for consumers.

---

## 📦 Deployment

Infrastructure management is handled via GitOps patterns.

- **Docker Image**: `REGION-docker.pkg.dev/PROJECT_ID/REPOSITORY/maliev-prediction-service:{sha}`
- **Environments**: Development, Staging, Production

---

## 📄 License

Proprietary - © 2025 MALIEV Co., Ltd. All rights reserved.
