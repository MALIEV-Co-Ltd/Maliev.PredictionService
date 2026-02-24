# Maliev Prediction Service - Implementation Plan

**Version:** 1.0.0
**Status:** Draft
**Last Updated:** 2026-02-14
**Owner:** MALIEV Platform Team

---

## 1. Executive Summary

### 1.1 Purpose
This document provides a comprehensive implementation roadmap for building the Maliev Prediction Service - the intelligent machine learning layer of the MALIEV manufacturing ecosystem. It translates the product specification into actionable technical guidance.

### 1.2 Scope
- Complete architecture design (Clean Architecture + CQRS)
- Technology stack selection and configuration
- Database schema and data pipeline design
- ML.NET model implementation strategies
- Phase-by-phase development plan
- Testing, deployment, and operational readiness

### 1.3 Timeline Overview
- **Phase 1 (Weeks 1-4)**: Foundation - Infrastructure, data pipeline, model registry
- **Phase 2 (Weeks 5-8)**: Core ML - Print time prediction, demand forecasting
- **Phase 3 (Weeks 9-12)**: Advanced ML - Pricing, churn, material forecasting
- **Phase 4 (Weeks 13-16)**: Production Readiness - Testing, optimization, deployment
- **Total Duration**: 16 weeks to v1.0 production release

### 1.4 Success Criteria
- All 8 core prediction models operational with target accuracy
- < 500ms P95 latency for API endpoints
- 99.5% API availability
- Automated model retraining pipeline functional
- Complete observability and monitoring
- 100% compliance with MALIEV platform standards

---

## 1.5 MALIEV Platform Standards & Restrictions

### 1.5.1 Mandatory Requirements

**✅ REQUIRED:**
- **Aspire ServiceDefaults**: All services MUST use `Maliev.ServiceDefaults` for standardized configuration
- **XML Documentation**: All public APIs, classes, and methods MUST have XML documentation comments
- **MassTransit**: Event-driven communication via RabbitMQ (no direct RabbitMQ client usage)
- **Redis**: Distributed caching using `StackExchange.Redis`
- **PostgreSQL**: Primary data store with EF Core
- **OpenTelemetry**: Metrics, traces, and logging via ServiceDefaults
- **Health Checks**: Comprehensive health checks for all dependencies
- **Conventional Commits**: Git commits follow conventional format with `Co-Authored-By: Claude Sonnet 4.5`

### 1.5.2 Banned Libraries

**❌ PROHIBITED - DO NOT USE:**
- **AutoMapper**: Explicit mapping required, no reflection-based mapping
- **FluentValidation**: Manual validation or built-in Data Annotations only
- **FluentAssertions**: Use standard xUnit assertions only
- **Newtonsoft.Json**: Use System.Text.Json instead
- **Any reflection-heavy ORMs**: EF Core only

**Rationale:** These libraries introduce:
- Hidden complexity and magic behavior
- Performance overhead from reflection
- Difficult-to-debug issues
- Inconsistent coding patterns

### 1.5.3 Code Quality Standards

- **Test Coverage**: Minimum 80% code coverage for business logic
- **No Magic Strings**: Use constants, enums, or configuration
- **No Nullable Reference Warnings**: Enable nullable reference types, fix all warnings
- **Async/Await**: All I/O operations must be async
- **Dependency Injection**: Constructor injection only, no service locator pattern
- **Immutability**: Prefer `record` types and `init` properties
- **Pattern Matching**: Use modern C# 13 patterns where appropriate

### 1.5.4 Project Structure

```
Maliev.PredictionService/
├── Maliev.PredictionService.Api/          # ASP.NET Core Web API
│   ├── Controllers/
│   ├── Middleware/
│   ├── Program.cs                         # Startup configuration
│   └── appsettings.json
├── Maliev.PredictionService.Application/  # Use cases (CQRS)
│   ├── Commands/
│   ├── Queries/
│   ├── DTOs/
│   └── Validators/
├── Maliev.PredictionService.Domain/       # Business logic
│   ├── Entities/
│   ├── ValueObjects/
│   ├── Interfaces/
│   └── Services/
├── Maliev.PredictionService.Infrastructure/ # External dependencies
│   ├── Persistence/
│   ├── ML/
│   ├── Events/
│   ├── BackgroundServices/
│   └── Caching/
└── Tests/
    ├── Maliev.PredictionService.UnitTests/
    └── Maliev.PredictionService.IntegrationTests/
```

---

## 2. Architecture Design

### 2.1 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    API Gateway / Load Balancer                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Maliev.PredictionService.Api                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Controllers  │  │ Middleware   │  │ Health       │          │
│  │ (REST API)   │  │ (Auth/Logs)  │  │ Checks       │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              Maliev.PredictionService.Application                │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Use Cases    │  │ DTOs         │  │ Validators   │          │
│  │ (CQRS)       │  │              │  │              │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  ┌──────────────────────────────────────────────────┐          │
│  │         Prediction Orchestration Service          │          │
│  │  (Caching, Model Selection, Explainability)      │          │
│  └──────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                Maliev.PredictionService.Domain                   │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Entities     │  │ Value        │  │ Domain       │          │
│  │              │  │ Objects      │  │ Services     │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  ┌──────────────────────────────────────────────────┐          │
│  │              ML Model Interfaces                  │          │
│  │  (IPredictor, IModelTrainer, IExplainer)         │          │
│  └──────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│            Maliev.PredictionService.Infrastructure               │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │ Repositories │  │ Event        │  │ File         │          │
│  │ (PostgreSQL) │  │ Consumers    │  │ Storage      │          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
│  ┌──────────────────────────────────────────────────┐          │
│  │            ML.NET Implementation                  │          │
│  │  (Model Training, Inference, Feature Eng.)       │          │
│  └──────────────────────────────────────────────────┘          │
│  ┌──────────────────────────────────────────────────┐          │
│  │      Background Services (IHostedService)         │          │
│  │  (Model Training, Data ETL, Cleanup)             │          │
│  └──────────────────────────────────────────────────┘          │
└─────────────────────────────────────────────────────────────────┘
                              │
              ┌───────────────┼───────────────┐
              ▼               ▼               ▼
      ┌─────────────┐ ┌─────────────┐ ┌─────────────┐
      │ PostgreSQL  │ │   Redis     │ │  RabbitMQ   │
      │ (Data/Models)│ │  (Cache)    │ │ (Events)    │
      └─────────────┘ └─────────────┘ └─────────────┘
```

### 2.2 Layer Responsibilities

#### API Layer (Maliev.PredictionService.Api)
- **Controllers**: REST endpoint handlers, request/response mapping
- **Middleware**: Authentication, authorization, request logging, error handling
- **Health Checks**: Database connectivity, model availability, cache status
- **OpenAPI Documentation**: Swagger/Swashbuckle integration
- **Rate Limiting**: Per-client throttling and quota management

#### Application Layer (Maliev.PredictionService.Application)
- **Commands**: `TrainModelCommand`, `PredictPrintTimeCommand`, etc.
- **Queries**: `GetModelHealthQuery`, `GetPredictionHistoryQuery`, etc.
- **Handlers**: MediatR command/query handlers (CQRS pattern)
- **DTOs**: Request/response models, view models
- **Validators**: FluentValidation-free manual validation
- **Services**: `PredictionOrchestrator`, `CacheService`, `ExplainabilityService`

#### Domain Layer (Maliev.PredictionService.Domain)
- **Entities**: `MLModel`, `TrainingDataset`, `PredictionAuditLog`
- **Value Objects**: `PerformanceMetrics`, `ConfidenceInterval`, `FeatureContribution`
- **Domain Services**: Business logic not tied to entities
- **Interfaces**: Repository contracts, ML model contracts
- **Enums**: `ModelType`, `ModelStatus`, `PredictionStatus`

#### Infrastructure Layer (Maliev.PredictionService.Infrastructure)
- **Repositories**: EF Core implementations for PostgreSQL
- **ML.NET Services**: Model training, inference, feature engineering
- **Event Consumers**: MassTransit consumers for upstream data events
- **Background Services**: IHostedService implementations for scheduled training
- **File Storage**: Local file system or blob storage for models
- **Caching**: Redis distributed cache implementation

### 2.3 Data Flow Diagrams

#### Prediction Request Flow
```
User/Service → API Controller → Validator → Cache Check
                                                  │
                                         ┌────────┴────────┐
                                         ▼                 ▼
                                   Cache Hit         Cache Miss
                                         │                 │
                                         │                 ▼
                                         │      Load Model from Registry
                                         │                 │
                                         │                 ▼
                                         │      Feature Engineering
                                         │                 │
                                         │                 ▼
                                         │      ML.NET Prediction Engine
                                         │                 │
                                         │                 ▼
                                         │      Explainability Service
                                         │                 │
                                         │                 ▼
                                         │      Cache Result (TTL)
                                         │                 │
                                         └────────┬────────┘
                                                  ▼
                                        Audit Log → PostgreSQL
                                                  │
                                                  ▼
                                        Return Response
```

#### Model Training Flow
```
Scheduled Job / Trigger Event
           │
           ▼
Training Orchestrator
           │
           ▼
Data Validation & Quality Checks
           │
           ▼
Feature Engineering Pipeline
           │
           ▼
Train/Test Split (80/20)
           │
           ▼
ML.NET Training Pipeline
           │
           ▼
Model Evaluation (Holdout Set)
           │
     ┌────┴────┐
     ▼         ▼
Meets      Fails
Threshold  Threshold
     │         │
     ▼         ▼
 Save to   Log Error
 Registry  & Alert
     │
     ▼
A/B Testing (Canary 10%)
     │
     ▼
Gradual Rollout (100%)
     │
     ▼
Deprecate Old Model
```

#### Data Ingestion Flow
```
Upstream Service → Domain Event (RabbitMQ)
                         │
                         ▼
               MassTransit Consumer
                         │
                         ▼
               Data Transformation
                         │
                         ▼
               Validation & Deduplication
                         │
                         ▼
               PostgreSQL Insert (Event Store)
                         │
                         ▼
               Feature Store Update (if applicable)
```

---

## 3. Technology Stack

### 3.1 Core Technologies

| Component | Technology | Version | Justification |
|-----------|-----------|---------|---------------|
| **Runtime** | .NET | 10.0 | Platform standard, async/await, performance |
| **Language** | C# | 13 | Modern language features, LINQ, pattern matching |
| **Web Framework** | ASP.NET Core | 10.0 | High-performance, built-in DI, middleware pipeline |
| **Database** | PostgreSQL | 18 | ACID compliance, JSON support, full-text search |
| **Cache** | Redis | 7 | Sub-ms latency, distributed, TTL support |
| **Messaging** | RabbitMQ | 3.x | Reliable message delivery, MassTransit integration |
| **ML Framework** | ML.NET | 4.0+ | Native .NET, production-ready, ONNX support |

### 3.2 Libraries & Packages

#### Required Packages (MUST USE)

| Package | Version | Purpose | Notes |
|---------|---------|---------|-------|
| **Maliev.ServiceDefaults** | Latest | Platform standards | Aspire service defaults (required) |
| **Maliev.MessagingContracts** | Latest | Event contracts | Shared message definitions |
| **Microsoft.ML** | 4.0+ | Core ML.NET framework | Regression, classification, time-series |
| **Microsoft.ML.TimeSeries** | 4.0+ | Time-series forecasting | SSA (Singular Spectrum Analysis) |
| **Microsoft.ML.Mkl.Components** | 4.0+ | Math kernel library | Performance optimization for training |
| **Microsoft.ML.OnnxConverter** | 4.0+ | ONNX export | Model portability and optimization |
| **MassTransit** | 8.x+ | Messaging abstraction | Event consumers, publishers |
| **MassTransit.RabbitMQ** | 8.x+ | RabbitMQ transport | Required for all MALIEV services |
| **Npgsql.EntityFrameworkCore.PostgreSQL** | 10.x | EF Core provider | PostgreSQL ORM |
| **StackExchange.Redis** | 2.x+ | Redis client | Distributed caching (required) |
| **MediatR** | 12.x+ | CQRS mediator | Command/query pattern |
| **OpenTelemetry.Exporter.OpenTelemetryProtocol** | Latest | Observability | Via ServiceDefaults |
| **Polly** | 8.x+ | Resilience | Retry, circuit breaker, timeout |
| **Scalar.AspNetCore** | Latest | API documentation | Modern OpenAPI UI (replaces Swagger) |

#### Optional Packages (MAY USE)

| Package | Version | Purpose | Notes |
|---------|---------|---------|-------|
| **MassTransit.Quartz** | 8.x+ | Message scheduling | Optional: for advanced scheduling |
| **Bogus** | 35.x+ | Test data generation | Realistic fake data for tests |

#### Testing Packages

| Package | Version | Purpose | Notes |
|---------|---------|---------|-------|
| **xUnit** | 2.x+ | Unit testing | MALIEV standard test framework |
| **xUnit.runner.visualstudio** | 2.x+ | Test runner | Visual Studio integration |
| **Testcontainers** | 4.x+ | Integration testing | Docker-based test dependencies |
| **Microsoft.NET.Test.Sdk** | 17.x+ | Test SDK | Required for test execution |
| **Moq** | 4.x+ | Mocking | For unit tests only |

#### ❌ BANNED PACKAGES - DO NOT ADD

| Package | Reason | Alternative |
|---------|--------|-------------|
| **AutoMapper** | ❌ Reflection overhead, hidden mapping logic | Manual mapping with extension methods |
| **FluentValidation** | ❌ Overcomplicated, inconsistent patterns | Manual validation or Data Annotations |
| **FluentAssertions** | ❌ Unnecessary abstraction over xUnit | Standard xUnit `Assert.*` methods |
| **Newtonsoft.Json** | ❌ Legacy, superseded by System.Text.Json | `System.Text.Json` (built-in) |
| **Dapper** | ❌ Bypasses EF Core standards | EF Core queries |
| **Serilog** | ⚠️ Use via ServiceDefaults only | OpenTelemetry logging (ServiceDefaults) |
| **Swashbuckle.AspNetCore** | ❌ Legacy, replaced by Scalar | `Scalar.AspNetCore` |

### 3.3 Infrastructure

| Service | Purpose | Configuration |
|---------|---------|---------------|
| **PostgreSQL 18** | Primary database | Separate schemas: `public`, `ml_models`, `training`, `predictions`, `audit` |
| **Redis 7** | Distributed cache | Separate DBs: 0 (predictions), 1 (model metadata) |
| **RabbitMQ** | Event bus & scheduling | Exchange: `maliev-events`, Queues: `predictions-*`, Quartz scheduling |
| **Quartz.NET (via MassTransit)** | Message scheduling | Optional: advanced recurring schedules |
| **Azure Blob Storage (Future)** | Model storage | Large model files (ONNX, ZIP archives) |
| **GitHub Actions** | CI/CD | Build, test, Docker, deploy |

---

## 4. Database Design

### 4.1 Schema Overview

```sql
-- Schema: ml_models (Model Registry)
CREATE SCHEMA ml_models;

-- Schema: training (Training Data & Jobs)
CREATE SCHEMA training;

-- Schema: predictions (Prediction Results & Cache)
CREATE SCHEMA predictions;

-- Schema: audit (Audit Logs)
CREATE SCHEMA audit;
```

### 4.2 Table Definitions

#### ml_models.models
```sql
CREATE TABLE ml_models.models (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_type VARCHAR(100) NOT NULL, -- 'print-time', 'demand-forecast', etc.
    version VARCHAR(50) NOT NULL, -- 'v2.3.1'
    status VARCHAR(20) NOT NULL DEFAULT 'Draft', -- Draft, Testing, Active, Deprecated
    model_path TEXT NOT NULL, -- File path or blob URL
    model_format VARCHAR(20) NOT NULL DEFAULT 'ML.NET', -- ML.NET, ONNX
    file_size_bytes BIGINT,

    -- Performance Metrics (JSONB for flexibility)
    performance_metrics JSONB NOT NULL DEFAULT '{}',
    -- {
    --   "accuracy": 0.94,
    --   "precision": 0.92,
    --   "recall": 0.91,
    --   "f1": 0.915,
    --   "mae": 8.2,
    --   "rmse": 12.5,
    --   "r2": 0.89
    -- }

    -- Training Configuration
    training_config JSONB NOT NULL DEFAULT '{}',
    -- {
    --   "algorithm": "FastTreeRegression",
    --   "hyperparameters": {...},
    --   "trainingDatasetId": "uuid",
    --   "trainingSamples": 12000,
    --   "validationSamples": 3000
    -- }

    trained_at TIMESTAMPTZ NOT NULL,
    deployed_at TIMESTAMPTZ,
    deprecated_at TIMESTAMPTZ,
    created_by VARCHAR(100),

    -- Metadata
    description TEXT,
    tags JSONB DEFAULT '[]',

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (model_type, version)
);

CREATE INDEX idx_models_type_status ON ml_models.models(model_type, status);
CREATE INDEX idx_models_trained_at ON ml_models.models(trained_at DESC);
```

#### training.datasets
```sql
CREATE TABLE training.datasets (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_type VARCHAR(100) NOT NULL,
    version VARCHAR(50) NOT NULL,

    -- Data Range
    data_start_date DATE NOT NULL,
    data_end_date DATE NOT NULL,

    -- Record Counts
    total_records INT NOT NULL,
    training_records INT NOT NULL,
    validation_records INT NOT NULL,
    test_records INT NOT NULL,

    -- Features
    feature_columns JSONB NOT NULL, -- ["volume", "surfaceArea", "layerCount", ...]
    target_column VARCHAR(100) NOT NULL, -- "actualPrintTimeMinutes"

    -- Data Quality
    quality_report JSONB NOT NULL DEFAULT '{}',
    -- {
    --   "missingValuesByColumn": {...},
    --   "outliersByColumn": {...},
    --   "warnings": [...]
    -- }

    -- Storage
    storage_path TEXT NOT NULL,
    file_format VARCHAR(20) NOT NULL DEFAULT 'Parquet', -- CSV, Parquet
    compressed BOOLEAN DEFAULT FALSE,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (model_type, version)
);

CREATE INDEX idx_datasets_model_type ON training.datasets(model_type);
CREATE INDEX idx_datasets_date_range ON training.datasets(data_start_date, data_end_date);
```

#### training.jobs
```sql
CREATE TABLE training.jobs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    model_type VARCHAR(100) NOT NULL,
    dataset_id UUID REFERENCES training.datasets(id),

    status VARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending, Running, Completed, Failed
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,
    duration_seconds INT,

    -- Results
    model_id UUID REFERENCES ml_models.models(id),
    training_metrics JSONB DEFAULT '{}',
    error_message TEXT,

    -- Configuration
    training_config JSONB NOT NULL DEFAULT '{}',

    -- Resource Usage
    cpu_usage_percent DECIMAL(5,2),
    memory_mb INT,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_training_jobs_status ON training.jobs(status, created_at DESC);
CREATE INDEX idx_training_jobs_model_type ON training.jobs(model_type, created_at DESC);
```

#### predictions.results
```sql
CREATE TABLE predictions.results (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    prediction_id VARCHAR(100) NOT NULL UNIQUE, -- External-facing ID

    model_type VARCHAR(100) NOT NULL,
    model_id UUID REFERENCES ml_models.models(id),
    model_version VARCHAR(50) NOT NULL,

    -- Request Context
    user_id VARCHAR(100),
    tenant_id VARCHAR(100),
    source_service VARCHAR(100), -- 'QuotationService', 'OrderService', etc.

    -- Input Data (compressed JSON)
    input_data JSONB NOT NULL,

    -- Output Data
    prediction_value JSONB NOT NULL,
    -- For print-time: {"estimatedMinutes": 182, "confidenceInterval": {...}}
    -- For churn: {"churnRiskScore": 72, "probability30Days": 0.42}

    confidence_score DECIMAL(5,4), -- 0.0000 to 1.0000

    -- Explainability
    explanation JSONB DEFAULT '{}',
    -- {
    --   "topFactors": [...],
    --   "reasoning": "..."
    -- }

    -- Performance
    response_time_ms INT NOT NULL,
    from_cache BOOLEAN NOT NULL DEFAULT FALSE,

    -- Feedback (for model improvement)
    actual_value DECIMAL(15,4), -- Filled in later when ground truth available
    error DECIMAL(15,4), -- prediction_value - actual_value
    feedback_received_at TIMESTAMPTZ,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_predictions_model_type_created ON predictions.results(model_type, created_at DESC);
CREATE INDEX idx_predictions_user_id ON predictions.results(user_id, created_at DESC);
CREATE INDEX idx_predictions_from_cache ON predictions.results(from_cache);
CREATE INDEX idx_predictions_feedback ON predictions.results(actual_value) WHERE actual_value IS NOT NULL;
```

#### audit.prediction_logs
```sql
CREATE TABLE audit.prediction_logs (
    id BIGSERIAL PRIMARY KEY,
    prediction_id VARCHAR(100) NOT NULL,

    event_type VARCHAR(50) NOT NULL, -- 'Requested', 'Completed', 'Cached', 'Failed'
    timestamp TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    user_id VARCHAR(100),
    tenant_id VARCHAR(100),

    model_type VARCHAR(100),
    model_version VARCHAR(50),

    request_data JSONB,
    response_data JSONB,
    error_message TEXT,

    response_time_ms INT,
    from_cache BOOLEAN,

    -- Request Metadata
    ip_address INET,
    user_agent TEXT,
    correlation_id VARCHAR(100),

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_audit_logs_timestamp ON audit.prediction_logs(timestamp DESC);
CREATE INDEX idx_audit_logs_prediction_id ON audit.prediction_logs(prediction_id);
CREATE INDEX idx_audit_logs_user_id ON audit.prediction_logs(user_id, timestamp DESC);
CREATE INDEX idx_audit_logs_event_type ON audit.prediction_logs(event_type, timestamp DESC);

-- Partition by month for performance
CREATE TABLE audit.prediction_logs_2026_02 PARTITION OF audit.prediction_logs
    FOR VALUES FROM ('2026-02-01') TO ('2026-03-01');
-- (Additional partitions created automatically by maintenance job)
```

#### training.feature_store (Optional - Future Enhancement)
```sql
CREATE TABLE training.feature_store (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entity_type VARCHAR(100) NOT NULL, -- 'Customer', 'Order', 'Material'
    entity_id VARCHAR(100) NOT NULL,

    feature_set VARCHAR(100) NOT NULL, -- 'customer_engagement', 'order_history'
    features JSONB NOT NULL,
    -- {
    --   "orderFrequencyLast30Days": 5,
    --   "avgOrderValue": 1250.00,
    --   "daysSinceLastOrder": 12
    -- }

    computed_at TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE (entity_type, entity_id, feature_set)
);

CREATE INDEX idx_feature_store_entity ON training.feature_store(entity_type, entity_id);
CREATE INDEX idx_feature_store_computed_at ON training.feature_store(computed_at DESC);
```

### 4.3 Database Migrations Strategy

- **Tool**: EF Core Migrations
- **Approach**: Code-first with migration scripts
- **Naming**: `YYYYMMDDHHMMSS_DescriptiveName.cs`
- **Deployment**: Automated via CI/CD (Aspire ServiceDefaults)
- **Rollback**: Keep down migrations for emergency rollback
- **Testing**: Migration testing in Testcontainers integration tests

---

## 5. ML.NET Model Implementations

### 5.1 Model Type Selection Guide

| Prediction Type | ML.NET Algorithm | Input Features | Output | Accuracy Target |
|-----------------|------------------|----------------|--------|-----------------|
| **Print Time** | FastTree Regression | Volume, surface area, layer count, support%, infill, material, printer | Estimated minutes (float) | MAE < 10 min, R² > 0.85 |
| **Demand Forecast** | SSA (Time Series) | Historical daily sales, calendar features, promotions | Daily demand (int) | MAPE < 15% |
| **Price Recommendation** | LightGBM Regression | Material cost, complexity, customer LTV, win rate history, capacity | Optimal price (decimal) | ±10% of actual win price |
| **Churn Prediction** | FastTree Binary Classification | RFM metrics, support tickets, payment delays, engagement | Churn probability (0-1) | Precision > 0.75 |
| **Material Demand** | SSA (Time Series) | Historical consumption, open orders, seasonality | Material units (float) | ±10% accuracy |
| **Bottleneck Detection** | FastTree Regression | Queue depths, utilization%, scheduled jobs, employee availability | Wait time (minutes) | ±20% accuracy |
| **Employee Performance** | FastTree Regression | Tenure, certifications, performance history, engagement | Performance score (0-100) | ±15 points |
| **Quality Anomaly** | Isolation Forest (Anomaly Detection) | Material batch, printer ID, operator, complexity, environment | Anomaly score (0-1) | Precision > 0.70 |

### 5.2 Sample ML.NET Implementation

#### Print Time Prediction Model

**File**: `Infrastructure/ML/PrintTimePredictionModel.cs`

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Maliev.PredictionService.Infrastructure.ML;

public class PrintTimePredictionModel
{
    public class PrintTimeData
    {
        [LoadColumn(0)] public float Volume { get; set; }
        [LoadColumn(1)] public float SurfaceArea { get; set; }
        [LoadColumn(2)] public int LayerCount { get; set; }
        [LoadColumn(3)] public float SupportPercentage { get; set; }
        [LoadColumn(4)] public int InfillDensity { get; set; }
        [LoadColumn(5)] public string Material { get; set; } = string.Empty;
        [LoadColumn(6)] public string PrinterType { get; set; } = string.Empty;
        [LoadColumn(7)] public float LayerHeight { get; set; }
        [LoadColumn(8)] public float PrintSpeed { get; set; }

        [LoadColumn(9), ColumnName("Label")]
        public float ActualPrintTimeMinutes { get; set; }
    }

    public class PrintTimePrediction
    {
        [ColumnName("Score")]
        public float EstimatedMinutes { get; set; }
    }

    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public PrintTimePredictionModel(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    public void Train(IEnumerable<PrintTimeData> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Data preprocessing pipeline
        var pipeline = _mlContext.Transforms.Categorical
            .OneHotEncoding("MaterialEncoded", nameof(PrintTimeData.Material))
            .Append(_mlContext.Transforms.Categorical
                .OneHotEncoding("PrinterEncoded", nameof(PrintTimeData.PrinterType)))
            .Append(_mlContext.Transforms.Concatenate("Features",
                nameof(PrintTimeData.Volume),
                nameof(PrintTimeData.SurfaceArea),
                nameof(PrintTimeData.LayerCount),
                nameof(PrintTimeData.SupportPercentage),
                nameof(PrintTimeData.InfillDensity),
                "MaterialEncoded",
                "PrinterEncoded",
                nameof(PrintTimeData.LayerHeight),
                nameof(PrintTimeData.PrintSpeed)))
            // FastTree Regression for accurate predictions
            .Append(_mlContext.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 100,
                numberOfTrees: 500,
                minimumExampleCountPerLeaf: 10,
                learningRate: 0.1));

        _model = pipeline.Fit(dataView);
    }

    public (float prediction, float lowerBound, float upperBound) Predict(PrintTimeData input)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        var predictionEngine = _mlContext.Model
            .CreatePredictionEngine<PrintTimeData, PrintTimePrediction>(_model);

        var prediction = predictionEngine.Predict(input);

        // Calculate confidence interval (±10% for simplicity, can use quantile regression)
        var estimatedMinutes = prediction.EstimatedMinutes;
        var margin = estimatedMinutes * 0.1f;

        return (estimatedMinutes, estimatedMinutes - margin, estimatedMinutes + margin);
    }

    public RegressionMetrics Evaluate(IEnumerable<PrintTimeData> testData)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        var dataView = _mlContext.Data.LoadFromEnumerable(testData);
        var predictions = _model.Transform(dataView);

        return _mlContext.Regression.Evaluate(predictions,
            labelColumnName: "Label",
            scoreColumnName: "Score");
    }

    public void SaveModel(string path)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        _mlContext.Model.Save(_model, null, path);
    }

    public void LoadModel(string path)
    {
        _model = _mlContext.Model.Load(path, out _);
    }
}
```

#### Demand Forecasting Model (Time Series)

**File**: `Infrastructure/ML/DemandForecastModel.cs`

```csharp
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;

namespace Maliev.PredictionService.Infrastructure.ML;

public class DemandForecastModel
{
    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public DemandForecastModel(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    public class DemandData
    {
        public DateTime Date { get; set; }
        public float DemandValue { get; set; }
    }

    public class DemandForecast
    {
        public float[] ForecastedDemand { get; set; } = Array.Empty<float>();
        public float[] LowerBoundConfidence { get; set; } = Array.Empty<float>();
        public float[] UpperBoundConfidence { get; set; } = Array.Empty<float>();
    }

    public void Train(IEnumerable<DemandData> historicalData, int horizon = 30)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(historicalData);

        // SSA (Singular Spectrum Analysis) - powerful for time series
        var pipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(DemandForecast.ForecastedDemand),
            inputColumnName: nameof(DemandData.DemandValue),
            windowSize: 14, // Look back 2 weeks
            seriesLength: historicalData.Count(),
            trainSize: (int)(historicalData.Count() * 0.8),
            horizon: horizon,
            confidenceLevel: 0.95f,
            confidenceLowerBoundColumn: nameof(DemandForecast.LowerBoundConfidence),
            confidenceUpperBoundColumn: nameof(DemandForecast.UpperBoundConfidence));

        _model = pipeline.Fit(dataView);
    }

    public DemandForecast Forecast(int horizon)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        var forecastEngine = _model.CreateTimeSeriesEngine<DemandData, DemandForecast>(_mlContext);
        return forecastEngine.Predict();
    }
}
```

#### Churn Prediction Model (Binary Classification)

**File**: `Infrastructure/ML/ChurnPredictionModel.cs`

```csharp
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Maliev.PredictionService.Infrastructure.ML;

public class ChurnPredictionModel
{
    public class CustomerChurnData
    {
        public float Recency { get; set; } // Days since last order
        public float Frequency { get; set; } // Orders in last 12 months
        public float MonetaryValue { get; set; } // Total spent
        public int SupportTickets { get; set; }
        public int PaymentDelays { get; set; }
        public float EngagementScore { get; set; } // 0-100
        public int DaysSinceFirstOrder { get; set; }
        public float AverageOrderValue { get; set; }

        [ColumnName("Label")]
        public bool Churned { get; set; } // True if churned
    }

    public class ChurnPrediction
    {
        [ColumnName("PredictedLabel")]
        public bool WillChurn { get; set; }

        [ColumnName("Probability")]
        public float ChurnProbability { get; set; }

        [ColumnName("Score")]
        public float Score { get; set; }
    }

    private readonly MLContext _mlContext;
    private ITransformer? _model;

    public ChurnPredictionModel(MLContext mlContext)
    {
        _mlContext = mlContext;
    }

    public void Train(IEnumerable<CustomerChurnData> trainingData)
    {
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(CustomerChurnData.Recency),
                nameof(CustomerChurnData.Frequency),
                nameof(CustomerChurnData.MonetaryValue),
                nameof(CustomerChurnData.SupportTickets),
                nameof(CustomerChurnData.PaymentDelays),
                nameof(CustomerChurnData.EngagementScore),
                nameof(CustomerChurnData.DaysSinceFirstOrder),
                nameof(CustomerChurnData.AverageOrderValue))
            .Append(_mlContext.BinaryClassification.Trainers.FastTree(
                labelColumnName: "Label",
                featureColumnName: "Features",
                numberOfLeaves: 50,
                numberOfTrees: 200,
                minimumExampleCountPerLeaf: 20));

        _model = pipeline.Fit(dataView);
    }

    public ChurnPrediction Predict(CustomerChurnData customer)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        var predictionEngine = _mlContext.Model
            .CreatePredictionEngine<CustomerChurnData, ChurnPrediction>(_model);

        return predictionEngine.Predict(customer);
    }

    public CalibratedBinaryClassificationMetrics Evaluate(IEnumerable<CustomerChurnData> testData)
    {
        if (_model is null)
            throw new InvalidOperationException("Model not trained");

        var dataView = _mlContext.Data.LoadFromEnumerable(testData);
        var predictions = _model.Transform(dataView);

        return _mlContext.BinaryClassification.Evaluate(predictions,
            labelColumnName: "Label");
    }
}
```

### 5.3 Feature Engineering Strategies

#### Geometry Feature Extraction (3D Print Time)
```csharp
public class GeometryFeatureExtractor
{
    public PrintTimeData ExtractFeatures(byte[] stlFileBytes, string material, string printerType)
    {
        var mesh = ParseSTL(stlFileBytes);

        return new PrintTimeData
        {
            Volume = CalculateVolume(mesh),
            SurfaceArea = CalculateSurfaceArea(mesh),
            LayerCount = EstimateLayerCount(mesh, layerHeight: 0.2f),
            SupportPercentage = EstimateSupportPercentage(mesh),
            BoundingBoxVolume = CalculateBoundingBox(mesh).Volume,
            Complexity = CalculateComplexity(mesh), // Triangle count, curvature
            Material = material,
            PrinterType = printerType
        };
    }

    private Mesh ParseSTL(byte[] bytes) { /* ... */ }
    private float CalculateVolume(Mesh mesh) { /* Triangular mesh volume */ }
    private float CalculateSurfaceArea(Mesh mesh) { /* Sum of triangle areas */ }
    // ... additional geometry analysis methods
}
```

#### Customer RFM Features (Churn Prediction)
```csharp
public class CustomerFeatureExtractor
{
    public async Task<CustomerChurnData> ExtractFeaturesAsync(
        Guid customerId,
        IOrderRepository orderRepo,
        ICustomerRepository customerRepo)
    {
        var customer = await customerRepo.GetByIdAsync(customerId);
        var orders = await orderRepo.GetByCustomerIdAsync(customerId);

        var now = DateTime.UtcNow;
        var last12Months = orders.Where(o => o.CreatedAt > now.AddMonths(-12)).ToList();

        return new CustomerChurnData
        {
            Recency = (float)(now - orders.Max(o => o.CreatedAt)).TotalDays,
            Frequency = last12Months.Count,
            MonetaryValue = last12Months.Sum(o => o.TotalAmount),
            AverageOrderValue = last12Months.Any()
                ? last12Months.Average(o => o.TotalAmount)
                : 0,
            SupportTickets = customer.SupportTicketsLast12Months,
            PaymentDelays = customer.PaymentDelaysCount,
            EngagementScore = CalculateEngagementScore(customer),
            DaysSinceFirstOrder = (float)(now - customer.FirstOrderDate).TotalDays
        };
    }
}
```

### 5.4 Model Explainability (SHAP-like)

```csharp
public class ModelExplainer
{
    public List<FeatureContribution> ExplainPrediction<TInput, TOutput>(
        MLContext mlContext,
        ITransformer model,
        TInput input)
        where TInput : class
        where TOutput : class, new()
    {
        var predictionEngine = mlContext.Model
            .CreatePredictionEngine<TInput, TOutput>(model);

        // Feature Contribution Calculation (requires ML.NET 3.0+)
        var schema = predictionEngine.OutputSchema;
        var featureColumnIndex = schema.GetColumnOrNull("Features")?.Index ?? -1;

        if (featureColumnIndex < 0)
            return new List<FeatureContribution>();

        // Extract feature importance (simplified - use SHAP libraries for production)
        var featureImportance = GetFeatureImportance(model);

        return featureImportance
            .OrderByDescending(f => f.Impact)
            .Take(5)
            .ToList();
    }

    private List<FeatureContribution> GetFeatureImportance(ITransformer model)
    {
        // Placeholder - integrate with SHAP.NET or similar library
        // For tree-based models, can extract from TreeEnsemble
        return new List<FeatureContribution>
        {
            new() { Factor = "volume", Impact = 0.45 },
            new() { Factor = "supportStructures", Impact = 0.30 },
            new() { Factor = "layerCount", Impact = 0.25 }
        };
    }
}
```

---

## 6. Startup Configuration

### 6.1 Program.cs (MALIEV Standard)

**File**: `Api/Program.cs`

```csharp
using Maliev.PredictionService.Application;
using Maliev.PredictionService.Infrastructure;
using Maliev.ServiceDefaults;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// MALIEV ServiceDefaults - REQUIRED
// ============================================================================
// Provides: OpenTelemetry, Health Checks, Service Discovery, Logging
builder.AddServiceDefaults();

// ============================================================================
// Database Configuration
// ============================================================================
builder.Services.AddDbContext<PredictionDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============================================================================
// Redis Configuration
// ============================================================================
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(
        builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ============================================================================
// MassTransit Configuration (RabbitMQ)
// ============================================================================
builder.Services.AddMassTransit(x =>
{
    // Register all consumers
    x.AddConsumers(typeof(OrderCreatedConsumer).Assembly);

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        // Configure endpoints for all consumers
        cfg.ConfigureEndpoints(context);
    });
});

// ============================================================================
// MediatR (CQRS)
// ============================================================================
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(PredictPrintTimeCommand).Assembly);
});

// ============================================================================
// Application Services
// ============================================================================
builder.Services.AddScoped<IModelRegistry, ModelRegistry>();
builder.Services.AddScoped<IModelTrainer, ModelTrainer>();
builder.Services.AddScoped<IGeometryFeatureExtractor, GeometryFeatureExtractor>();
builder.Services.AddScoped<IModelExplainer, ModelExplainer>();
builder.Services.AddSingleton<PredictionMetrics>();

// ML.NET Context (singleton)
builder.Services.AddSingleton<MLContext>(new MLContext(seed: 42));

// ============================================================================
// Background Services
// ============================================================================
builder.Services.AddHostedService<ModelTrainingBackgroundService>();
builder.Services.AddHostedService<DataCleanupBackgroundService>();
builder.Services.AddHostedService<ModelPerformanceMonitoringService>();

// ============================================================================
// API Configuration
// ============================================================================
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// OpenAPI with Scalar (NO Swagger/Swashbuckle)
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Info = new()
        {
            Title = "MALIEV Prediction Service API",
            Version = "v1",
            Description = "Machine learning predictions for the MALIEV manufacturing platform",
            Contact = new()
            {
                Name = "MALIEV Platform Team",
                Email = "platform@maliev.com"
            }
        };
        return Task.CompletedTask;
    });
});

// ============================================================================
// Build & Configure Pipeline
// ============================================================================
var app = builder.Build();

// REQUIRED: Map ServiceDefaults endpoints (health, metrics, etc.)
app.MapDefaultEndpoints();

// Map OpenAPI endpoints
app.MapOpenApi();

// Scalar API documentation UI (replaces Swagger UI)
if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("MALIEV Prediction Service API")
            .WithTheme(ScalarTheme.Purple)
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
    });
}

// Apply database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Required for integration tests
public partial class Program { }
```

### 6.2 appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Database=maliev_predictionservice;Username=postgres;Password=postgres",
    "Redis": "localhost:6379",
    "RabbitMQ": "amqp://guest:guest@localhost:5672"
  },
  "ModelTraining": {
    "MinimumSampleSizes": {
      "print-time": 5000,
      "demand-forecast": 365,
      "churn": 2000,
      "price-optimization": 3000
    },
    "TrainingSchedules": {
      "print-time": {
        "IntervalDays": 7,
        "PreferredHour": 3,
        "PreferredDayOfWeek": "Sunday"
      },
      "demand-forecast": {
        "IntervalDays": 1,
        "PreferredHour": 2
      }
    }
  }
}
```

### 6.3 XML Documentation Requirements

**ALL public APIs MUST have XML documentation:**

```csharp
/// <summary>
/// Predicts 3D print time from geometry file and printer parameters.
/// </summary>
/// <param name="request">Print time prediction request containing geometry and parameters.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>
/// A <see cref="PrintTimePredictionResponse"/> containing estimated time, confidence interval,
/// and explanation of contributing factors.
/// </returns>
/// <response code="200">Successfully predicted print time.</response>
/// <response code="400">Invalid request parameters or geometry file.</response>
/// <response code="500">Internal server error during prediction.</response>
/// <exception cref="InvalidOperationException">Thrown when no active model is available.</exception>
[HttpPost("print-time")]
[ProducesResponseType(typeof(PrintTimePredictionResponse), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public async Task<IActionResult> PredictPrintTime(
    [FromBody] PrintTimePredictionRequest request,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

**Enable XML documentation in .csproj:**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>$(NoWarn);1591</NoWarn> <!-- Suppress missing XML comments warning for private members -->
  </PropertyGroup>
</Project>
```

---

## 7. API Implementation

### 7.1 Controller Structure

**File**: `Api/Controllers/PredictionsController.cs`

```csharp
namespace Maliev.PredictionService.Api.Controllers;

/// <summary>
/// Provides machine learning prediction endpoints for the MALIEV platform.
/// </summary>
[ApiController]
[Route("predictions/v1")]
[Produces("application/json")]
public class PredictionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<PredictionsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PredictionsController"/> class.
    /// </summary>
    /// <param name="mediator">MediatR mediator for CQRS pattern.</param>
    /// <param name="logger">Logger instance.</param>
    public PredictionsController(IMediator mediator, ILogger<PredictionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Predicts 3D print time from geometry file and printer parameters.
    /// </summary>
    /// <param name="request">Print time prediction request containing geometry and parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Print time prediction with confidence interval and explanation.</returns>
    /// <response code="200">Successfully predicted print time.</response>
    /// <response code="400">Invalid request parameters or geometry file format.</response>
    /// <response code="500">Internal server error during prediction.</response>
    [HttpPost("print-time")]
    [ProducesResponseType(typeof(PrintTimePredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PredictPrintTime(
        [FromBody] PrintTimePredictionRequest request,
        CancellationToken cancellationToken)
    {
        // Manual validation (NO FluentValidation)
        var validationErrors = ValidatePrintTimeRequest(request);
        if (validationErrors.Any())
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        // Manual mapping (NO AutoMapper)
        var command = new PredictPrintTimeCommand
        {
            GeometryFile = request.GeometryFile,
            Material = request.Material,
            PrinterType = request.PrinterType,
            InfillDensity = request.InfillDensity,
            LayerHeight = request.LayerHeight,
            UserId = User.Identity?.Name ?? "anonymous"
        };

        var result = await _mediator.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Forecasts sales demand for the specified time horizon.
    /// </summary>
    /// <param name="request">Demand forecast request with horizon and filters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Daily demand forecast with confidence bands.</returns>
    /// <response code="200">Successfully generated demand forecast.</response>
    /// <response code="400">Invalid request parameters.</response>
    [HttpPost("demand-forecast")]
    [ProducesResponseType(typeof(DemandForecastResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForecastDemand(
        [FromBody] DemandForecastRequest request,
        CancellationToken cancellationToken)
    {
        // Manual validation
        var validationErrors = ValidateDemandForecastRequest(request);
        if (validationErrors.Any())
        {
            return BadRequest(new ValidationProblemDetails(validationErrors));
        }

        var query = new ForecastDemandQuery
        {
            ForecastHorizonDays = request.ForecastHorizonDays,
            ProductCategories = request.ProductCategories,
            Granularity = request.Granularity
        };

        var result = await _mediator.Send(query, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Gets churn risk prediction for a specific customer.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Churn risk score, probability, and recommended interventions.</returns>
    /// <response code="200">Successfully retrieved churn prediction.</response>
    /// <response code="404">Customer not found.</response>
    [HttpGet("churn-risk/{customerId}")]
    [ProducesResponseType(typeof(ChurnPredictionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChurnRisk(
        Guid customerId,
        CancellationToken cancellationToken)
    {
        var query = new GetChurnRiskQuery { CustomerId = customerId };
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    /// <summary>
    /// Gets health status and performance metrics for a specific model type.
    /// </summary>
    /// <param name="modelType">Model type identifier (e.g., "print-time", "churn").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Model health status, version, and performance metrics.</returns>
    /// <response code="200">Successfully retrieved model health.</response>
    /// <response code="404">Model type not found.</response>
    [HttpGet("models/{modelType}/health")]
    [ProducesResponseType(typeof(ModelHealthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetModelHealth(
        string modelType,
        CancellationToken cancellationToken)
    {
        var query = new GetModelHealthQuery { ModelType = modelType };
        var result = await _mediator.Send(query, cancellationToken);

        if (result is null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    // ========================================================================
    // Manual Validation Methods (NO FluentValidation)
    // ========================================================================

    private static Dictionary<string, string[]> ValidatePrintTimeRequest(PrintTimePredictionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.GeometryFile))
        {
            errors[nameof(request.GeometryFile)] = new[] { "Geometry file is required." };
        }
        else
        {
            try
            {
                var bytes = Convert.FromBase64String(request.GeometryFile);
                if (bytes.Length == 0 || bytes.Length > 50 * 1024 * 1024) // 50MB max
                {
                    errors[nameof(request.GeometryFile)] = new[] { "Geometry file must be between 1 byte and 50MB." };
                }
            }
            catch (FormatException)
            {
                errors[nameof(request.GeometryFile)] = new[] { "Geometry file must be valid base64 string." };
            }
        }

        if (string.IsNullOrWhiteSpace(request.Material))
        {
            errors[nameof(request.Material)] = new[] { "Material is required." };
        }

        if (string.IsNullOrWhiteSpace(request.PrinterType))
        {
            errors[nameof(request.PrinterType)] = new[] { "Printer type is required." };
        }

        if (request.InfillDensity < 0 || request.InfillDensity > 100)
        {
            errors[nameof(request.InfillDensity)] = new[] { "Infill density must be between 0 and 100." };
        }

        if (request.LayerHeight <= 0 || request.LayerHeight > 1)
        {
            errors[nameof(request.LayerHeight)] = new[] { "Layer height must be between 0 and 1mm." };
        }

        return errors;
    }

    private static Dictionary<string, string[]> ValidateDemandForecastRequest(DemandForecastRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ForecastHorizonDays < 1 || request.ForecastHorizonDays > 365)
        {
            errors[nameof(request.ForecastHorizonDays)] = new[] { "Forecast horizon must be between 1 and 365 days." };
        }

        if (request.ProductCategories is null || !request.ProductCategories.Any())
        {
            errors[nameof(request.ProductCategories)] = new[] { "At least one product category is required." };
        }

        var validGranularities = new[] { "daily", "weekly", "monthly" };
        if (!validGranularities.Contains(request.Granularity?.ToLowerInvariant()))
        {
            errors[nameof(request.Granularity)] = new[] { "Granularity must be 'daily', 'weekly', or 'monthly'." };
        }

        return errors;
    }
}
```

### 6.2 Manual Mapping (NO AutoMapper)

**File**: `Application/Mappings/PredictionMappings.cs`

```csharp
namespace Maliev.PredictionService.Application.Mappings;

/// <summary>
/// Extension methods for mapping between DTOs and domain entities.
/// MALIEV Standard: Manual mapping only. AutoMapper is BANNED.
/// </summary>
public static class PredictionMappings
{
    /// <summary>
    /// Maps MLModel entity to ModelHealthResponse DTO.
    /// </summary>
    public static ModelHealthResponse ToHealthResponse(
        this MLModel model,
        int predictionVolumeToday,
        double averageLatencyMs)
    {
        return new ModelHealthResponse
        {
            ModelType = model.ModelType,
            Version = model.Version,
            Status = model.Status.ToString(),
            LastTrainingDate = model.TrainedAt,
            NextScheduledTraining = CalculateNextTrainingDate(model.ModelType),
            PerformanceMetrics = new PerformanceMetricsDto
            {
                Accuracy = model.PerformanceMetrics.Accuracy,
                MAE = model.PerformanceMetrics.MAE,
                R2 = model.PerformanceMetrics.R2
            },
            PredictionVolumeToday = predictionVolumeToday,
            AverageLatencyMs = averageLatencyMs
        };
    }

    /// <summary>
    /// Maps PredictionResult entity to PrintTimePredictionResponse DTO.
    /// </summary>
    public static PrintTimePredictionResponse ToPrintTimeResponse(this PredictionResult result)
    {
        var predictionValue = JsonSerializer.Deserialize<PrintTimePredictionValue>(
            result.PredictionValue.GetRawText());

        return new PrintTimePredictionResponse
        {
            PredictionId = result.PredictionId,
            EstimatedMinutes = predictionValue.EstimatedMinutes,
            ConfidenceInterval = predictionValue.ConfidenceInterval,
            Breakdown = predictionValue.Breakdown,
            Confidence = result.ConfidenceScore,
            Explanation = MapExplanation(result.Explanation),
            ModelVersion = result.ModelVersion,
            Timestamp = result.CreatedAt
        };
    }

    private static ExplanationData MapExplanation(JsonElement? explanationJson)
    {
        if (explanationJson is null || explanationJson.Value.ValueKind == JsonValueKind.Null)
        {
            return new ExplanationData
            {
                TopFactors = Array.Empty<FeatureContribution>(),
                HumanReadableReasoning = string.Empty
            };
        }

        var explanation = JsonSerializer.Deserialize<ExplanationValue>(
            explanationJson.Value.GetRawText());

        return new ExplanationData
        {
            TopFactors = explanation.TopFactors,
            HumanReadableReasoning = explanation.Reasoning
        };
    }

    private static DateTime CalculateNextTrainingDate(string modelType) =>
        modelType switch
        {
            "print-time" => DateTime.UtcNow.Date.AddDays(7),
            "demand-forecast" => DateTime.UtcNow.Date.AddDays(1),
            "churn" => DateTime.UtcNow.Date.AddDays(1),
            "price-optimization" => DateTime.UtcNow.Date.AddDays(7),
            _ => DateTime.UtcNow.Date.AddDays(7)
        };
}
```

### 6.3 CQRS Command/Query Handlers

**File**: `Application/Commands/PredictPrintTimeCommandHandler.cs`

```csharp
public class PredictPrintTimeCommandHandler
    : IRequestHandler<PredictPrintTimeCommand, PrintTimePredictionResponse>
{
    private readonly IModelRegistry _modelRegistry;
    private readonly ICacheService _cacheService;
    private readonly IPredictionAuditRepository _auditRepository;
    private readonly IGeometryFeatureExtractor _featureExtractor;
    private readonly IModelExplainer _explainer;
    private readonly ILogger<PredictPrintTimeCommandHandler> _logger;

    public async Task<PrintTimePredictionResponse> Handle(
        PredictPrintTimeCommand request,
        CancellationToken cancellationToken)
    {
        var predictionId = Guid.NewGuid().ToString();
        var sw = Stopwatch.StartNew();

        try
        {
            // 1. Check cache
            var cacheKey = GenerateCacheKey(request);
            var cachedResult = await _cacheService.GetAsync<PrintTimePredictionResponse>(cacheKey);
            if (cachedResult is not null)
            {
                _logger.LogInformation("Cache hit for print time prediction");
                await LogAuditAsync(predictionId, request, cachedResult, sw.ElapsedMilliseconds, fromCache: true);
                return cachedResult;
            }

            // 2. Load active model
            var model = await _modelRegistry.GetActiveModelAsync("print-time", cancellationToken);
            if (model is null)
                throw new InvalidOperationException("No active print-time model available");

            // 3. Extract features from geometry
            var geometryBytes = Convert.FromBase64String(request.GeometryFile);
            var features = _featureExtractor.ExtractFeatures(
                geometryBytes,
                request.Material,
                request.PrinterType);

            // 4. Make prediction
            var mlModel = await LoadMLModelAsync(model.ModelPath);
            var (prediction, lower, upper) = mlModel.Predict(features);

            // 5. Generate explanation
            var explanation = _explainer.ExplainPrediction(features);

            // 6. Build response
            var response = new PrintTimePredictionResponse
            {
                PredictionId = predictionId,
                EstimatedMinutes = (int)Math.Round(prediction),
                ConfidenceInterval = new ConfidenceInterval
                {
                    Lower = (int)Math.Floor(lower),
                    Upper = (int)Math.Ceiling(upper)
                },
                Breakdown = new PrintTimeBreakdown
                {
                    PrintTime = (int)(prediction * 0.85),
                    PostProcessing = (int)(prediction * 0.10),
                    QualityControl = (int)(prediction * 0.05)
                },
                Confidence = CalculateConfidence(prediction, lower, upper),
                Explanation = explanation,
                ModelVersion = model.Version,
                Timestamp = DateTime.UtcNow
            };

            // 7. Cache result
            await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromHours(4));

            // 8. Audit log
            await LogAuditAsync(predictionId, request, response, sw.ElapsedMilliseconds, fromCache: false);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to predict print time");
            await LogErrorAsync(predictionId, request, ex.Message, sw.ElapsedMilliseconds);
            throw;
        }
    }

    private string GenerateCacheKey(PredictPrintTimeCommand request)
    {
        var hash = SHA256.HashData(Convert.FromBase64String(request.GeometryFile));
        var hashString = Convert.ToHexString(hash);
        return $"print-time:{hashString}:{request.Material}:{request.PrinterType}";
    }
}
```

---

## 7. Data Pipeline & ETL

### 7.1 Event Consumers

**File**: `Infrastructure/Events/OrderCreatedConsumer.cs`

```csharp
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ITrainingDataRepository _trainingDataRepo;
    private readonly ILogger<OrderCreatedConsumer> _logger;

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var evt = context.Message;

        // Extract training data for multiple model types
        await StoreForPrintTimePrediction(evt);
        await StoreForDemandForecasting(evt);
        await StoreForPriceOptimization(evt);

        _logger.LogInformation("Processed OrderCreated event for training data: {OrderId}", evt.OrderId);
    }

    private async Task StoreForPrintTimePrediction(OrderCreated evt)
    {
        if (evt.ActualManufacturingTimeMinutes.HasValue)
        {
            var trainingRecord = new PrintTimeTrainingRecord
            {
                OrderId = evt.OrderId,
                GeometryHash = evt.GeometryFileHash,
                Volume = evt.GeometryMetadata.Volume,
                SurfaceArea = evt.GeometryMetadata.SurfaceArea,
                // ... other features
                ActualPrintTimeMinutes = evt.ActualManufacturingTimeMinutes.Value,
                RecordedAt = DateTime.UtcNow
            };

            await _trainingDataRepo.AddAsync(trainingRecord);
        }
    }

    private async Task StoreForDemandForecasting(OrderCreated evt)
    {
        var demandRecord = new DemandTrainingRecord
        {
            Date = evt.CreatedAt.Date,
            ProductCategory = evt.ProductCategory,
            DemandValue = 1, // Increment daily count
            RecordedAt = DateTime.UtcNow
        };

        await _trainingDataRepo.UpsertDailyDemandAsync(demandRecord);
    }
}
```

### 7.2 Data Quality Validation

```csharp
public class DataQualityValidator
{
    public DataQualityReport Validate(IEnumerable<TrainingRecord> data)
    {
        var report = new DataQualityReport
        {
            TotalRecords = data.Count(),
            InvalidRecords = 0,
            MissingValuesByColumn = new Dictionary<string, int>(),
            OutliersByColumn = new Dictionary<string, OutlierInfo>(),
            Warnings = new List<string>()
        };

        // 1. Check for missing values
        var properties = typeof(TrainingRecord).GetProperties();
        foreach (var prop in properties)
        {
            var missingCount = data.Count(r => prop.GetValue(r) is null);
            if (missingCount > 0)
            {
                report.MissingValuesByColumn[prop.Name] = missingCount;
                report.Warnings.Add($"{prop.Name} has {missingCount} missing values");
            }
        }

        // 2. Detect outliers (IQR method)
        var numericColumns = GetNumericColumns(data);
        foreach (var (column, values) in numericColumns)
        {
            var outliers = DetectOutliers(values);
            if (outliers.Count > 0)
            {
                report.OutliersByColumn[column] = new OutlierInfo
                {
                    Count = outliers.Count,
                    Percentage = (outliers.Count / (double)values.Length) * 100
                };
            }
        }

        // 3. Check data freshness
        var latestRecord = data.Max(r => r.RecordedAt);
        if (latestRecord < DateTime.UtcNow.AddDays(-7))
        {
            report.Warnings.Add($"Latest data is {(DateTime.UtcNow - latestRecord).TotalDays:F1} days old");
        }

        // 4. Check minimum sample size
        if (data.Count() < 1000)
        {
            report.Warnings.Add($"Sample size ({data.Count()}) is below recommended minimum (1000)");
        }

        return report;
    }

    private List<double> DetectOutliers(double[] values)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        var q1 = sorted[(int)(sorted.Length * 0.25)];
        var q3 = sorted[(int)(sorted.Length * 0.75)];
        var iqr = q3 - q1;
        var lowerBound = q1 - (1.5 * iqr);
        var upperBound = q3 + (1.5 * iqr);

        return values.Where(v => v < lowerBound || v > upperBound).ToList();
    }
}
```

---

## 8. Background Services

### 8.1 Model Training Background Service

**File**: `Infrastructure/BackgroundServices/ModelTrainingBackgroundService.cs`

```csharp
using Microsoft.Extensions.Hosting;

namespace Maliev.PredictionService.Infrastructure.BackgroundServices;

public class ModelTrainingBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ModelTrainingBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

    // Training schedules (model type -> schedule)
    private readonly Dictionary<string, TrainingSchedule> _schedules = new()
    {
        ["print-time"] = new TrainingSchedule
        {
            Interval = TimeSpan.FromDays(7), // Weekly
            PreferredHour = 3, // 3 AM
            PreferredDayOfWeek = DayOfWeek.Sunday
        },
        ["demand-forecast"] = new TrainingSchedule
        {
            Interval = TimeSpan.FromDays(1), // Daily
            PreferredHour = 2 // 2 AM
        },
        ["churn"] = new TrainingSchedule
        {
            Interval = TimeSpan.FromDays(1),
            PreferredHour = 1
        },
        ["price-optimization"] = new TrainingSchedule
        {
            Interval = TimeSpan.FromDays(7),
            PreferredHour = 4,
            PreferredDayOfWeek = DayOfWeek.Sunday
        }
    };

    private readonly Dictionary<string, DateTime> _lastTrainingTimes = new();

    public ModelTrainingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ModelTrainingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Model Training Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndTrainModelsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in model training background service");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Model Training Background Service stopped");
    }

    private async Task CheckAndTrainModelsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        foreach (var (modelType, schedule) in _schedules)
        {
            if (ShouldTrainModel(modelType, schedule, now))
            {
                _logger.LogInformation("Starting scheduled training for {ModelType}", modelType);

                try
                {
                    await TrainModelAsync(modelType, cancellationToken);
                    _lastTrainingTimes[modelType] = now;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to train {ModelType} model", modelType);
                }
            }
        }
    }

    private bool ShouldTrainModel(string modelType, TrainingSchedule schedule, DateTime now)
    {
        // Check if we've trained before
        if (!_lastTrainingTimes.TryGetValue(modelType, out var lastTraining))
        {
            lastTraining = DateTime.MinValue;
        }

        // Check if enough time has passed
        if (now - lastTraining < schedule.Interval)
        {
            return false;
        }

        // Check if we're in the preferred time window
        if (schedule.PreferredDayOfWeek.HasValue &&
            now.DayOfWeek != schedule.PreferredDayOfWeek.Value)
        {
            return false;
        }

        if (now.Hour != schedule.PreferredHour)
        {
            return false;
        }

        return true;
    }

    private async Task TrainModelAsync(string modelType, CancellationToken cancellationToken)
    {
        // Create a scope for scoped services
        using var scope = _serviceProvider.CreateScope();
        var trainer = scope.ServiceProvider.GetRequiredService<IModelTrainer>();
        var dataRepo = scope.ServiceProvider.GetRequiredService<ITrainingDataRepository>();
        var registry = scope.ServiceProvider.GetRequiredService<IModelRegistry>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            // 1. Fetch training data
            var startDate = DateTime.UtcNow.AddDays(-90);
            var trainingData = await dataRepo.GetTrainingDataAsync(modelType, startDate, DateTime.UtcNow);

            if (trainingData.Count < GetMinimumSampleSize(modelType))
            {
                _logger.LogWarning("Insufficient training data for {ModelType}: {Count} records",
                    modelType, trainingData.Count);
                return;
            }

            // 2. Validate data quality
            var validator = scope.ServiceProvider.GetRequiredService<IDataQualityValidator>();
            var qualityReport = validator.Validate(trainingData);

            if (qualityReport.InvalidRecords > trainingData.Count * 0.1)
            {
                _logger.LogError("Data quality too low for {ModelType}: {InvalidPercent}%",
                    modelType, (qualityReport.InvalidRecords / (double)trainingData.Count) * 100);
                return;
            }

            // 3. Train model
            var (model, metrics) = await trainer.TrainModelAsync(modelType, trainingData, cancellationToken);

            // 4. Evaluate model
            if (!MeetsPerformanceThreshold(modelType, metrics))
            {
                _logger.LogWarning("{ModelType} model performance below threshold: {Metrics}",
                    modelType, metrics);
                await notifications.NotifyAsync($"{modelType} model training completed but performance is suboptimal");
                return;
            }

            // 5. Save to registry
            var version = $"v{DateTime.UtcNow:yyyyMMdd.HHmm}";
            var modelEntity = new MLModel
            {
                ModelType = modelType,
                Version = version,
                Status = ModelStatus.Testing,
                ModelPath = $"/models/{modelType}/{version}/model.zip",
                PerformanceMetrics = metrics,
                TrainedAt = DateTime.UtcNow
            };

            await registry.SaveModelAsync(modelEntity);

            // 6. Enable A/B test with canary deployment
            await registry.EnableABTestAsync(modelType, version, canaryPercentage: 10);

            _logger.LogInformation("Successfully trained {ModelType} model {Version}",
                modelType, version);

            await notifications.NotifyAsync($"New {modelType} model {version} deployed to 10% canary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to train {ModelType} model", modelType);
            await notifications.NotifyAsync($"{modelType} model training failed: {ex.Message}");
            throw;
        }
    }

    private int GetMinimumSampleSize(string modelType) => modelType switch
    {
        "print-time" => 5000,
        "demand-forecast" => 365,
        "churn" => 2000,
        "price-optimization" => 3000,
        _ => 1000
    };

    private bool MeetsPerformanceThreshold(string modelType, PerformanceMetrics metrics) => modelType switch
    {
        "print-time" => metrics.R2 >= 0.80 && metrics.MAE <= 15,
        "demand-forecast" => metrics.MAPE <= 20,
        "churn" => metrics.Precision >= 0.70 && metrics.Recall >= 0.60,
        "price-optimization" => metrics.MAE <= 100,
        _ => true
    };
}

public class TrainingSchedule
{
    public TimeSpan Interval { get; init; }
    public int PreferredHour { get; init; }
    public DayOfWeek? PreferredDayOfWeek { get; init; }
}
```

### 8.2 Data Cleanup Background Service

**File**: `Infrastructure/BackgroundServices/DataCleanupBackgroundService.cs`

```csharp
public class DataCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupBackgroundService> _logger;
    private readonly TimeSpan _runInterval = TimeSpan.FromHours(24);

    public DataCleanupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<DataCleanupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Cleanup Background Service started");

        // Wait until 1 AM UTC for first run
        await WaitUntilScheduledTime(hour: 1, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PerformCleanupAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in data cleanup background service");
            }

            await Task.Delay(_runInterval, stoppingToken);
        }

        _logger.LogInformation("Data Cleanup Background Service stopped");
    }

    private async Task PerformCleanupAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();

        _logger.LogInformation("Starting data cleanup");

        // 1. Delete old prediction results (older than 90 days)
        var cutoffDate = DateTime.UtcNow.AddDays(-90);
        var deletedPredictions = await dbContext.Database.ExecuteSqlAsync(
            $"DELETE FROM predictions.results WHERE created_at < {cutoffDate}",
            cancellationToken);

        _logger.LogInformation("Deleted {Count} old prediction results", deletedPredictions);

        // 2. Archive old audit logs (older than 180 days)
        var archiveCutoffDate = DateTime.UtcNow.AddDays(-180);
        var archivedLogs = await dbContext.Database.ExecuteSqlAsync(
            $"DELETE FROM audit.prediction_logs WHERE created_at < {archiveCutoffDate}",
            cancellationToken);

        _logger.LogInformation("Archived {Count} old audit logs", archivedLogs);

        // 3. Clean up deprecated models (older than 30 days)
        var deprecatedDate = DateTime.UtcNow.AddDays(-30);
        var modelsToArchive = await dbContext.Models
            .Where(m => m.Status == ModelStatus.Deprecated && m.DeprecatedAt < deprecatedDate)
            .ToListAsync(cancellationToken);

        foreach (var model in modelsToArchive)
        {
            // Delete model file
            if (File.Exists(model.ModelPath))
            {
                File.Delete(model.ModelPath);
                _logger.LogInformation("Deleted model file: {Path}", model.ModelPath);
            }

            model.Status = ModelStatus.Archived;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Data cleanup completed");
    }

    private async Task WaitUntilScheduledTime(int hour, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var scheduledTime = now.Date.AddHours(hour);

        if (now.Hour >= hour)
        {
            scheduledTime = scheduledTime.AddDays(1);
        }

        var delay = scheduledTime - now;
        _logger.LogInformation("Waiting {Delay} until scheduled time {Time}", delay, scheduledTime);

        await Task.Delay(delay, cancellationToken);
    }
}
```

### 8.3 Service Registration

**File**: `Api/Program.cs`

```csharp
// Register background services
builder.Services.AddHostedService<ModelTrainingBackgroundService>();
builder.Services.AddHostedService<DataCleanupBackgroundService>();
builder.Services.AddHostedService<ModelPerformanceMonitoringService>();
```

### 8.4 Alternative: MassTransit Message Scheduling

For more complex scheduling needs, you can use MassTransit's built-in scheduling with Quartz:

**File**: `Infrastructure/BackgroundServices/ModelTrainingScheduler.cs`

```csharp
using MassTransit;

public class ModelTrainingScheduler : BackgroundService
{
    private readonly IBus _bus;
    private readonly ILogger<ModelTrainingScheduler> _logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Setting up recurring model training schedules");

        // Schedule print-time model training (weekly, Sundays at 3 AM)
        await _bus.ScheduleRecurringSend(
            new Uri("queue:model-training"),
            new RecurringSchedule
            {
                CronExpression = "0 3 * * 0", // Cron: Sunday at 3 AM
                TimeZoneId = "UTC"
            },
            new TrainModelCommand { ModelType = "print-time" });

        // Schedule demand forecast training (daily at 2 AM)
        await _bus.ScheduleRecurringSend(
            new Uri("queue:model-training"),
            new RecurringSchedule
            {
                CronExpression = "0 2 * * *", // Cron: Daily at 2 AM
                TimeZoneId = "UTC"
            },
            new TrainModelCommand { ModelType = "demand-forecast" });

        _logger.LogInformation("Recurring schedules configured");

        // Keep service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }
}

public class TrainModelCommand
{
    public string ModelType { get; set; } = string.Empty;
}

public class TrainModelConsumer : IConsumer<TrainModelCommand>
{
    private readonly IModelTrainer _trainer;
    private readonly ILogger<TrainModelConsumer> _logger;

    public async Task Consume(ConsumeContext<TrainModelCommand> context)
    {
        _logger.LogInformation("Received training command for {ModelType}",
            context.Message.ModelType);

        // Trigger model training
        await _trainer.TrainModelAsync(context.Message.ModelType, CancellationToken.None);
    }
}
```

**MassTransit Configuration with Quartz:**

```csharp
// Program.cs
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TrainModelConsumer>();
    x.AddConsumer<OrderCreatedConsumer>();
    // ... other consumers

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));

        // Configure Quartz for message scheduling
        cfg.UseMessageScheduler(new Uri("queue:quartz"));

        cfg.ConfigureEndpoints(context);
    });

    // Add Quartz integration for scheduling
    x.AddQuartzConsumers();
});

// Add Quartz
builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
});

builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});
```

### 8.5 Approach Comparison

| Feature | IHostedService | MassTransit + Quartz |
|---------|----------------|----------------------|
| **Simplicity** | ✅ Very simple, no dependencies | ⚠️ More complex, requires Quartz package |
| **Built-in** | ✅ Native .NET feature | ❌ Requires additional NuGet packages |
| **Scheduling Flexibility** | ⚠️ Manual scheduling logic | ✅ Cron expressions, rich scheduling |
| **Distributed** | ❌ Runs on single instance | ✅ Distributed scheduling across instances |
| **UI/Monitoring** | ❌ No built-in dashboard | ⚠️ Limited (Quartz dashboard available) |
| **Persistence** | ❌ In-memory only | ✅ Persisted schedules in database |
| **Retry Logic** | ⚠️ Manual implementation | ✅ Built-in retry and error handling |
| **Scalability** | ⚠️ Single instance per service | ✅ Multi-instance coordination |
| **Message-driven** | ❌ Time-based only | ✅ Can trigger from events |
| **Recommended For** | Simple periodic tasks | Complex schedules, distributed systems |

**Recommendation for MALIEV PredictionService:**

- **Start with IHostedService** (Phase 1-3): Simple, no dependencies, sufficient for initial implementation
- **Migrate to MassTransit + Quartz** (Phase 4 or future): If you need:
  - Distributed scheduling across multiple instances
  - Complex cron-based schedules
  - Centralized schedule management
  - Better observability and retry logic

---

## 9. Testing Strategy

### 9.1 Unit Tests (xUnit - NO FluentAssertions)

**Example**: `Tests/Unit/PrintTimePredictionModelTests.cs`

```csharp
namespace Maliev.PredictionService.UnitTests.ML;

/// <summary>
/// Unit tests for print time prediction ML model.
/// MALIEV Standard: Use standard xUnit assertions. FluentAssertions is BANNED.
/// </summary>
public class PrintTimePredictionModelTests
{
    private readonly MLContext _mlContext = new(seed: 42);

    [Fact]
    public void Train_WithValidData_ShouldProduceAccurateModel()
    {
        // Arrange
        var trainingData = GenerateTrainingData(1000);
        var testData = GenerateTrainingData(200);
        var model = new PrintTimePredictionModel(_mlContext);

        // Act
        model.Train(trainingData);
        var metrics = model.Evaluate(testData);

        // Assert - Use standard xUnit assertions (NO FluentAssertions)
        Assert.NotNull(metrics);
        Assert.InRange(metrics.R2, 0.75, 1.0);
        Assert.InRange(metrics.MAE, 0, 15);
        Assert.InRange(metrics.RMSE, 0, 20);
    }

    [Theory]
    [InlineData(100, 500, 250, 5, 20, "PLA", "Prusa-MK4", 145)] // Expected ~145 min
    [InlineData(50, 200, 100, 2, 15, "ABS", "Prusa-MK3", 68)]
    [InlineData(200, 1000, 500, 10, 25, "PETG", "Prusa-MK4", 290)]
    public void Predict_WithKnownInputs_ShouldReturnReasonableEstimate(
        float volume, float surfaceArea, int layerCount, float support,
        int infill, string material, string printer, int expectedMinutes)
    {
        // Arrange
        var model = TrainedModel();
        var input = new PrintTimePredictionModel.PrintTimeData
        {
            Volume = volume,
            SurfaceArea = surfaceArea,
            LayerCount = layerCount,
            SupportPercentage = support,
            InfillDensity = infill,
            Material = material,
            PrinterType = printer,
            LayerHeight = 0.2f,
            PrintSpeed = 50
        };

        // Act
        var (prediction, lower, upper) = model.Predict(input);

        // Assert - Standard xUnit assertions
        Assert.InRange(prediction, expectedMinutes * 0.85f, expectedMinutes * 1.15f);
        Assert.True(lower < prediction, "Lower bound should be less than prediction");
        Assert.True(upper > prediction, "Upper bound should be greater than prediction");
        Assert.InRange(upper - lower, 10, 50); // Confidence interval should be reasonable
    }

    [Fact]
    public void Predict_WithoutTraining_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var model = new PrintTimePredictionModel(_mlContext);
        var input = new PrintTimePredictionModel.PrintTimeData
        {
            Volume = 100,
            SurfaceArea = 500,
            LayerCount = 250,
            Material = "PLA",
            PrinterType = "Prusa-MK4"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => model.Predict(input));
        Assert.Equal("Model not trained", exception.Message);
    }

    [Fact]
    public void SaveAndLoad_ShouldPreservePredictions()
    {
        // Arrange
        var trainingData = GenerateTrainingData(500);
        var model = new PrintTimePredictionModel(_mlContext);
        model.Train(trainingData);

        var testInput = new PrintTimePredictionModel.PrintTimeData
        {
            Volume = 150,
            SurfaceArea = 600,
            LayerCount = 300,
            Material = "PLA",
            PrinterType = "Prusa-MK4"
        };
        var (originalPrediction, _, _) = model.Predict(testInput);

        var tempPath = Path.GetTempFileName();

        try
        {
            // Act - Save and load
            model.SaveModel(tempPath);
            var loadedModel = new PrintTimePredictionModel(_mlContext);
            loadedModel.LoadModel(tempPath);
            var (loadedPrediction, _, _) = loadedModel.Predict(testInput);

            // Assert
            Assert.Equal(originalPrediction, loadedPrediction, precision: 2);
        }
        finally
        {
            File.Delete(tempPath);
        }
    }

    private static List<PrintTimePredictionModel.PrintTimeData> GenerateTrainingData(int count)
    {
        var faker = new Faker<PrintTimePredictionModel.PrintTimeData>()
            .RuleFor(x => x.Volume, f => f.Random.Float(10, 500))
            .RuleFor(x => x.SurfaceArea, f => f.Random.Float(50, 2000))
            .RuleFor(x => x.LayerCount, f => f.Random.Int(50, 1000))
            .RuleFor(x => x.SupportPercentage, f => f.Random.Float(0, 30))
            .RuleFor(x => x.InfillDensity, f => f.Random.Int(10, 30))
            .RuleFor(x => x.Material, f => f.PickRandom("PLA", "ABS", "PETG"))
            .RuleFor(x => x.PrinterType, f => f.PickRandom("Prusa-MK4", "Prusa-MK3", "Bambu-X1"))
            .RuleFor(x => x.LayerHeight, f => f.Random.Float(0.1f, 0.3f))
            .RuleFor(x => x.PrintSpeed, f => f.Random.Float(40, 60))
            .RuleFor(x => x.ActualPrintTimeMinutes, (f, x) =>
            {
                // Simple formula to simulate actual print time
                var baseTime = x.Volume * 0.5f + x.LayerCount * 0.3f;
                var materialMultiplier = x.Material switch
                {
                    "PLA" => 1.0f,
                    "ABS" => 1.1f,
                    "PETG" => 1.15f,
                    _ => 1.0f
                };
                return baseTime * materialMultiplier + f.Random.Float(-10, 10);
            });

        return faker.Generate(count);
    }

    private PrintTimePredictionModel TrainedModel()
    {
        var trainingData = GenerateTrainingData(1000);
        var model = new PrintTimePredictionModel(_mlContext);
        model.Train(trainingData);
        return model;
    }
}
```

**Manual Mapping Tests** (NO AutoMapper):

```csharp
namespace Maliev.PredictionService.UnitTests.Mappings;

/// <summary>
/// Tests for manual mapping extension methods.
/// MALIEV Standard: No AutoMapper - test manual mappings explicitly.
/// </summary>
public class PredictionMappingsTests
{
    [Fact]
    public void ToHealthResponse_WithValidModel_ShouldMapAllProperties()
    {
        // Arrange
        var model = new MLModel
        {
            Id = Guid.NewGuid(),
            ModelType = "print-time",
            Version = "v2.3.1",
            Status = ModelStatus.Active,
            TrainedAt = DateTime.UtcNow.AddDays(-5),
            PerformanceMetrics = new PerformanceMetrics
            {
                Accuracy = 0.94,
                MAE = 8.2,
                R2 = 0.89
            }
        };

        // Act
        var response = model.ToHealthResponse(
            predictionVolumeToday: 1234,
            averageLatencyMs: 420);

        // Assert - Standard xUnit assertions
        Assert.NotNull(response);
        Assert.Equal("print-time", response.ModelType);
        Assert.Equal("v2.3.1", response.Version);
        Assert.Equal("Active", response.Status);
        Assert.Equal(model.TrainedAt, response.LastTrainingDate);
        Assert.Equal(1234, response.PredictionVolumeToday);
        Assert.Equal(420, response.AverageLatencyMs);
        Assert.NotNull(response.PerformanceMetrics);
        Assert.Equal(0.94, response.PerformanceMetrics.Accuracy);
        Assert.Equal(8.2, response.PerformanceMetrics.MAE);
        Assert.Equal(0.89, response.PerformanceMetrics.R2);
    }

    [Fact]
    public void ToHealthResponse_ShouldCalculateNextTrainingDate()
    {
        // Arrange
        var model = new MLModel
        {
            ModelType = "print-time",
            Version = "v1.0",
            Status = ModelStatus.Active,
            TrainedAt = DateTime.UtcNow,
            PerformanceMetrics = new PerformanceMetrics()
        };

        // Act
        var response = model.ToHealthResponse(0, 0);

        // Assert
        Assert.True(response.NextScheduledTraining > DateTime.UtcNow);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(7), response.NextScheduledTraining.Date);
    }
}
```

### 9.2 Integration Tests (Testcontainers)

**Example**: `Tests/Integration/PredictionApiTests.cs`

```csharp
public class PredictionApiTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;
    private readonly RedisContainer _redisContainer;
    private WebApplicationFactory<Program> _factory = null!;

    public PredictionApiTests()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:18-alpine")
            .WithDatabase("maliev_predictions_test")
            .Build();

        _redisContainer = new RedisBuilder()
            .WithImage("redis:7-alpine")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();
        await _redisContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Replace real DB with test container
                    services.RemoveAll<DbContextOptions<PredictionDbContext>>();
                    services.AddDbContext<PredictionDbContext>(options =>
                        options.UseNpgsql(_postgresContainer.GetConnectionString()));

                    // Replace Redis
                    services.RemoveAll<IConnectionMultiplexer>();
                    services.AddSingleton<IConnectionMultiplexer>(
                        ConnectionMultiplexer.Connect(_redisContainer.GetConnectionString()));
                });
            });

        // Run migrations
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PredictionDbContext>();
        await dbContext.Database.MigrateAsync();
    }

    [Fact]
    public async Task POST_PrintTimePrediction_ReturnsValidResponse()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new PrintTimePredictionRequest
        {
            GeometryFile = GenerateTestSTLBase64(),
            Material = "PLA",
            PrinterType = "Prusa-MK4",
            InfillDensity = 20,
            LayerHeight = 0.2f
        };

        // Act
        var response = await client.PostAsJsonAsync("/predictions/v1/print-time", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PrintTimePredictionResponse>();
        Assert.NotNull(result);
        Assert.InRange(result.EstimatedMinutes, 10, 600);
        Assert.NotNull(result.Explanation);
        Assert.NotEmpty(result.Explanation.TopFactors);
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
```

### 9.3 Model Evaluation Tests

```csharp
public class ModelPerformanceTests
{
    [Fact]
    public async Task PrintTimeModel_MeetsAccuracyThreshold()
    {
        // Arrange
        var holdoutData = await LoadHoldoutDataAsync("print-time");
        var model = await LoadProductionModelAsync("print-time");

        // Act
        var metrics = model.Evaluate(holdoutData);

        // Assert
        Assert.InRange(metrics.R2, 0.85, 1.0);
        Assert.InRange(metrics.MAE, 0, 10);
        Assert.InRange(metrics.RMSE, 0, 15);
    }

    [Fact]
    public async Task ChurnModel_MeetsMinimumPrecision()
    {
        // Arrange
        var holdoutData = await LoadHoldoutDataAsync("churn");
        var model = await LoadProductionModelAsync("churn");

        // Act
        var metrics = model.Evaluate(holdoutData);

        // Assert
        Assert.InRange(metrics.Precision, 0.75, 1.0);
        Assert.InRange(metrics.Recall, 0.60, 1.0);
        Assert.InRange(metrics.F1Score, 0.65, 1.0);
        Assert.InRange(metrics.AUC, 0.80, 1.0);
    }
}
```

---

## 10. Deployment Strategy

### 10.1 Docker Configuration

**File**: `Maliev.PredictionService.Api/Dockerfile`

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy nuget.config for GitHub packages
COPY nuget.config ./

# Copy project files
COPY ["Maliev.PredictionService.Api/Maliev.PredictionService.Api.csproj", "Maliev.PredictionService.Api/"]
COPY ["Maliev.PredictionService.Application/Maliev.PredictionService.Application.csproj", "Maliev.PredictionService.Application/"]
COPY ["Maliev.PredictionService.Domain/Maliev.PredictionService.Domain.csproj", "Maliev.PredictionService.Domain/"]
COPY ["Maliev.PredictionService.Infrastructure/Maliev.PredictionService.Infrastructure.csproj", "Maliev.PredictionService.Infrastructure/"]

# Restore with secret mounting
RUN --mount=type=secret,id=nuget_username \
    --mount=type=secret,id=nuget_password \
    NUGET_USERNAME=$(cat /run/secrets/nuget_username) \
    NUGET_PASSWORD=$(cat /run/secrets/nuget_password) \
    dotnet restore "Maliev.PredictionService.Api/Maliev.PredictionService.Api.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/Maliev.PredictionService.Api"
RUN dotnet build "Maliev.PredictionService.Api.csproj" -c Release -o /app/build
RUN dotnet publish "Maliev.PredictionService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Install dependencies for ML.NET (if needed)
RUN apt-get update && apt-get install -y \
    libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Create directory for ML models
RUN mkdir -p /app/models && chmod 777 /app/models

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Non-root user
RUN useradd -m -u 1000 app && chown -R app:app /app
USER app

EXPOSE 8080
ENTRYPOINT ["dotnet", "Maliev.PredictionService.Api.dll"]
```

### 10.2 GitHub Actions Workflow

**File**: `.github/workflows/ci-develop.yml`

```yaml
name: CI - Develop Branch

on:
  push:
    branches: [develop]
  pull_request:
    branches: [develop]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    services:
      postgres:
        image: postgres:18-alpine
        env:
          POSTGRES_DB: maliev_predictions_test
          POSTGRES_USER: postgres
          POSTGRES_PASSWORD: postgres
        options: >-
          --health-cmd pg_isready
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 5432:5432

      redis:
        image: redis:7-alpine
        options: >-
          --health-cmd "redis-cli ping"
          --health-interval 10s
          --health-timeout 5s
          --health-retries 5
        ports:
          - 6379:6379

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET 10
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Restore dependencies
        run: dotnet restore
        env:
          NUGET_USERNAME: ${{ secrets.NUGET_USERNAME }}
          NUGET_PASSWORD: ${{ secrets.NUGET_PASSWORD }}

      - name: Build
        run: dotnet build --no-restore --configuration Release

      - name: Run Unit Tests
        run: dotnet test Tests/Maliev.PredictionService.UnitTests --no-build --configuration Release --verbosity normal

      - name: Run Integration Tests
        run: dotnet test Tests/Maliev.PredictionService.IntegrationTests --no-build --configuration Release --verbosity normal
        env:
          ConnectionStrings__DefaultConnection: "Host=localhost;Database=maliev_predictions_test;Username=postgres;Password=postgres"
          ConnectionStrings__Redis: "localhost:6379"

      - name: Build Docker Image
        run: |
          docker build -t maliev-prediction-service:${{ github.sha }} \
            --secret id=nuget_username,env=NUGET_USERNAME \
            --secret id=nuget_password,env=NUGET_PASSWORD \
            -f Maliev.PredictionService.Api/Dockerfile .
        env:
          NUGET_USERNAME: ${{ secrets.NUGET_USERNAME }}
          NUGET_PASSWORD: ${{ secrets.NUGET_PASSWORD }}

      - name: Push to Container Registry
        if: github.event_name == 'push'
        run: |
          echo ${{ secrets.GITHUB_TOKEN }} | docker login ghcr.io -u ${{ github.actor }} --password-stdin
          docker tag maliev-prediction-service:${{ github.sha }} ghcr.io/maliev-co-ltd/prediction-service:develop
          docker push ghcr.io/maliev-co-ltd/prediction-service:develop
```

### 10.3 Aspire Integration

**File**: `Aspire.AppHost/Program.cs` (additions)

```csharp
var predictionService = builder.AddProject<Projects.Maliev_PredictionService_Api>("predictionservice")
    .WithReference(postgres)
    .WithReference(redis)
    .WithReference(rabbitmq);

// Publish API endpoint
predictionService.WithHttpEndpoint(port: 5020, name: "http");
```

---

## 11. Monitoring & Observability

### 11.1 OpenTelemetry Configuration

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddRuntimeInstrumentation()
               .AddHttpClientInstrumentation()
               .AddAspNetCoreInstrumentation()
               .AddMeter("Maliev.PredictionService"); // Custom metrics
    })
    .WithTracing(tracing =>
    {
        tracing.AddAspNetCoreInstrumentation()
               .AddHttpClientInstrumentation()
               .AddNpgsql()
               .AddRedisInstrumentation();
    });
```

### 11.2 Custom Metrics

```csharp
public class PredictionMetrics
{
    private readonly Meter _meter;
    private readonly Counter<long> _predictionCounter;
    private readonly Histogram<double> _predictionDuration;
    private readonly Histogram<double> _modelAccuracy;
    private readonly Counter<long> _cacheHitCounter;

    public PredictionMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("Maliev.PredictionService");

        _predictionCounter = _meter.CreateCounter<long>(
            "predictions.total",
            description: "Total number of predictions made");

        _predictionDuration = _meter.CreateHistogram<double>(
            "predictions.duration",
            unit: "ms",
            description: "Prediction request duration");

        _modelAccuracy = _meter.CreateHistogram<double>(
            "model.accuracy",
            description: "Model accuracy when ground truth available");

        _cacheHitCounter = _meter.CreateCounter<long>(
            "cache.hits",
            description: "Number of cache hits");
    }

    public void RecordPrediction(string modelType, double durationMs, bool fromCache)
    {
        _predictionCounter.Add(1, new KeyValuePair<string, object?>("model.type", modelType));
        _predictionDuration.Record(durationMs, new KeyValuePair<string, object?>("model.type", modelType));

        if (fromCache)
        {
            _cacheHitCounter.Add(1, new KeyValuePair<string, object?>("model.type", modelType));
        }
    }

    public void RecordModelAccuracy(string modelType, double accuracy)
    {
        _modelAccuracy.Record(accuracy, new KeyValuePair<string, object?>("model.type", modelType));
    }
}
```

### 11.3 Grafana Dashboards

**Key Metrics to Monitor:**

1. **API Performance**
   - Request rate (requests/min)
   - P50, P95, P99 latency
   - Error rate (%)
   - Cache hit rate (%)

2. **Model Performance**
   - Prediction accuracy (MAE, RMSE, R²)
   - Model drift indicators
   - Prediction confidence distribution
   - Model age (days since last training)

3. **Infrastructure**
   - CPU/Memory utilization
   - Database connection pool
   - Redis cache size
   - RabbitMQ queue depth

4. **Business Metrics**
   - Predictions per model type
   - User adoption rate
   - Model usage distribution
   - Cost per prediction

---

## 12. Development Phases

### Phase 1: Foundation (Weeks 1-4)

#### Week 1: Project Setup
- **Tasks:**
  - Create project structure (Clean Architecture)
  - Set up PostgreSQL schemas and tables
  - Configure EF Core with migrations
  - Set up Redis caching
  - Configure MassTransit for RabbitMQ
  - Set up IHostedService for background tasks

- **Deliverables:**
  - Running API with health checks
  - Database migrations applied
  - Basic CRUD for model registry
  - Background service skeleton running

- **Success Criteria:**
  - Docker compose with all dependencies running
  - Migrations successfully applied
  - Health endpoint returns 200 OK
  - Background services starting successfully

#### Week 2: Data Pipeline
- **Tasks:**
  - Implement event consumers for OrderService, CustomerService
  - Build data transformation pipeline
  - Implement training data repository
  - Create data quality validation service
  - Set up initial data ingestion from historical sources

- **Deliverables:**
  - Event consumers for 3+ upstream services
  - Training data tables populated
  - Data quality reports

- **Success Criteria:**
  - 10,000+ training records ingested
  - Data quality validation passing
  - Event consumers processing messages

#### Week 3: Model Registry & Infrastructure
- **Tasks:**
  - Implement model registry service
  - Build model versioning system
  - Create model loading/unloading mechanism
  - Implement model A/B testing framework
  - Set up model file storage

- **Deliverables:**
  - Model registry API endpoints
  - Model versioning working
  - A/B testing infrastructure

- **Success Criteria:**
  - Can save/load models from registry
  - Can deploy multiple model versions
  - Canary rollout functioning

#### Week 4: Caching & Performance
- **Tasks:**
  - Implement Redis caching layer
  - Build cache invalidation strategy
  - Optimize feature engineering pipeline
  - Implement batch prediction support
  - Performance testing and optimization

- **Deliverables:**
  - Caching service with TTL management
  - Batch prediction endpoints
  - Performance test results

- **Success Criteria:**
  - Cache hit rate > 50% in tests
  - P95 latency < 2s for uncached
  - Batch processing 100 items < 5s

### Phase 2: Core ML Models (Weeks 5-8)

#### Week 5: Print Time Prediction
- **Tasks:**
  - Implement geometry feature extraction (STL parser)
  - Train initial print time model on historical data
  - Build prediction API endpoint
  - Implement explainability service
  - Create model evaluation tests

- **Deliverables:**
  - Print time prediction endpoint
  - Trained model with MAE < 15 min
  - Feature extraction library

- **Success Criteria:**
  - Model R² > 0.80
  - API response < 2s
  - 5 feature importance factors returned

#### Week 6: Demand Forecasting
- **Tasks:**
  - Implement time-series data aggregation
  - Train SSA forecasting model
  - Build forecast API endpoint
  - Implement anomaly detection for demand spikes
  - Create forecast visualization data

- **Deliverables:**
  - Demand forecast endpoint (7/30/90 day)
  - Trained model with MAPE < 20%
  - Anomaly detection alerts

- **Success Criteria:**
  - MAPE < 20% on 30-day forecast
  - Detects demand spikes with 80% accuracy
  - API returns confidence bands

#### Week 7: Testing & Refinement
- **Tasks:**
  - Write comprehensive unit tests
  - Build integration tests with Testcontainers
  - Implement model evaluation pipeline
  - Create automated retraining background services
  - Performance optimization

- **Deliverables:**
  - Test suite with >80% coverage
  - Automated model retraining via background services
  - Performance benchmarks

- **Success Criteria:**
  - All tests passing
  - CI/CD pipeline green
  - Background services executing on schedule

#### Week 8: Documentation & Monitoring
- **Tasks:**
  - Write API documentation (OpenAPI)
  - Set up OpenTelemetry metrics
  - Create Grafana dashboards
  - Write deployment guide
  - Conduct code review

- **Deliverables:**
  - Complete API documentation
  - Monitoring dashboards
  - Deployment runbook

- **Success Criteria:**
  - Swagger UI functional
  - Metrics flowing to Grafana
  - All endpoints documented

### Phase 3: Advanced Models (Weeks 9-12)

#### Week 9: Price Optimization
- **Tasks:**
  - Build customer LTV calculation
  - Train price recommendation model
  - Implement elasticity estimation
  - Create win probability prediction
  - Build competitive benchmarking

- **Deliverables:**
  - Price recommendation endpoint
  - Model with ±15% accuracy
  - Elasticity curves

- **Success Criteria:**
  - Price predictions within ±15% of optimal
  - Win probability AUC > 0.75
  - API < 500ms response time

#### Week 10: Churn Prediction
- **Tasks:**
  - Implement RFM feature extraction
  - Train churn classification model
  - Build risk scoring algorithm
  - Create intervention recommendations
  - Implement daily batch scoring

- **Deliverables:**
  - Churn risk endpoint
  - Model with precision > 0.70
  - Recommendation engine

- **Success Criteria:**
  - Precision > 0.70 on high-risk segment
  - Recall > 0.60
  - Daily batch completes in < 10 min

#### Week 11: Material Demand & Bottleneck Detection
- **Tasks:**
  - Train material demand forecasting model
  - Implement reorder point calculation
  - Build bottleneck detection model
  - Create resource reallocation recommendations
  - Implement alerting system

- **Deliverables:**
  - Material forecast endpoint
  - Bottleneck detection endpoint
  - Alert notifications

- **Success Criteria:**
  - Material forecast ±15% accuracy
  - Bottleneck detection 75% precision
  - Alerts sent within 1 minute

#### Week 12: Integration Testing
- **Tasks:**
  - End-to-end integration tests
  - Load testing (1000 req/min)
  - Chaos engineering tests
  - Security testing
  - UAT with stakeholders

- **Deliverables:**
  - Complete integration test suite
  - Load test report
  - Security scan results

- **Success Criteria:**
  - All integration tests pass
  - Load test: 1000 req/min sustained
  - No critical security vulnerabilities

### Phase 4: Production Readiness (Weeks 13-16)

#### Week 13: Employee Performance & Quality Anomaly
- **Tasks:**
  - Train employee performance model
  - Implement privacy-preserving aggregation
  - Build quality anomaly detection
  - Create root cause analysis
  - Implement feedback loop

- **Deliverables:**
  - Employee performance endpoint
  - Quality anomaly endpoint
  - Privacy compliance documentation

- **Success Criteria:**
  - Performance prediction ±20 points
  - Anomaly precision > 0.65
  - Privacy requirements met

#### Week 14: Optimization & Scaling
- **Tasks:**
  - ONNX model export for performance
  - Database query optimization
  - Cache warming strategies
  - Horizontal scaling tests
  - Cost optimization

- **Deliverables:**
  - ONNX models for critical paths
  - Optimized database indices
  - Scaling documentation

- **Success Criteria:**
  - ONNX models 2x faster
  - Database queries < 100ms P95
  - Scales to 3 instances

#### Week 15: Production Deployment
- **Tasks:**
  - Production environment setup
  - Blue-green deployment preparation
  - Monitoring alert configuration
  - Incident response runbook
  - Gradual rollout to production

- **Deliverables:**
  - Production deployment
  - Monitoring and alerting
  - Runbooks

- **Success Criteria:**
  - Deployed to production
  - All health checks green
  - Monitoring dashboards active

#### Week 16: Handoff & Documentation
- **Tasks:**
  - Final documentation review
  - Team training sessions
  - Post-launch monitoring
  - Performance baseline establishment
  - Retrospective and lessons learned

- **Deliverables:**
  - Complete documentation suite
  - Team training materials
  - Performance baseline report
  - v1.0 release

- **Success Criteria:**
  - All success metrics met
  - Team trained and confident
  - Production stable for 7 days

---

## 13. Risk Management

| Risk | Probability | Impact | Mitigation | Owner |
|------|-------------|--------|------------|-------|
| Insufficient training data | Medium | High | Start with synthetic data + transfer learning; incentivize data collection | Data Eng. |
| Model accuracy below targets | Medium | High | Baseline with simple models; iterative improvement; adjust targets if needed | ML Eng. |
| Integration failures with upstream services | Low | Medium | Mock services for testing; comprehensive integration tests | Backend |
| Performance degradation at scale | Medium | Medium | Load testing from Week 4; ONNX optimization; caching strategy | DevOps |
| Model drift post-deployment | High | Medium | Automated monitoring; scheduled retraining; A/B testing framework | ML Eng. |
| Security vulnerabilities | Low | High | Security scanning in CI/CD; code reviews; penetration testing | Security |
| Regulatory compliance (GDPR, AI Act) | Medium | High | Privacy-by-design; audit logging; explainable AI; legal review | Legal/Arch |

---

## 14. Success Metrics & KPIs

### 14.1 Development Metrics (During Implementation)
- **Sprint Velocity**: Complete 80%+ of planned tasks per week
- **Code Quality**: >80% test coverage, 0 critical SonarQube issues
- **Build Success Rate**: >95% CI/CD pipeline success rate
- **Technical Debt**: <5% of sprint capacity on debt reduction

### 14.2 Model Performance Metrics (Launch)
| Model Type | Metric | Target | Measurement |
|------------|--------|--------|-------------|
| Print Time | R² | > 0.85 | Holdout test set |
| Print Time | MAE | < 10 min | Production feedback |
| Demand Forecast | MAPE | < 15% | 30-day rolling window |
| Price Optimization | Margin Improvement | +15% | A/B test vs. baseline |
| Churn Prediction | Precision (High Risk) | > 0.75 | Production validation |
| Churn Prediction | Recall | > 0.60 | Production validation |

### 14.3 Operational Metrics (Post-Launch)
- **API Availability**: > 99.5% uptime
- **P95 Latency**: < 500ms (cached), < 2s (uncached)
- **Cache Hit Rate**: > 60%
- **Model Freshness**: Average age < 14 days
- **Prediction Volume**: 10,000+ predictions/day within 3 months
- **Error Rate**: < 1% of requests

### 14.4 Business Impact Metrics (3-6 Months)
- **Quote Turnaround Time**: Reduce from 45 min to < 5 min (89% improvement)
- **Inventory Holding Costs**: Reduce by 20%
- **Customer Retention**: Improve by 10%
- **Pricing Accuracy**: 15% reduction in lost deals due to pricing

---

## 15. Appendices

### Appendix A: Technology Decision Matrix

| Decision | Options Considered | Selected | Rationale |
|----------|-------------------|----------|-----------|
| ML Framework | TensorFlow.NET, ML.NET, Accord.NET | **ML.NET** | Native .NET, Microsoft support, production-ready, ONNX export |
| Time Series | Prophet, ARIMA (Python), ML.NET SSA | **ML.NET SSA** | No Python interop, good accuracy, built-in confidence intervals |
| Database | SQL Server, PostgreSQL | **PostgreSQL** | JSONB support, cost-effective, platform standard |
| Cache | Redis, Memcached | **Redis** | Distributed, TTL, data structures, industry standard |
| Background Jobs | Hangfire, Quartz.NET | **Hangfire** | Dashboard UI, PostgreSQL storage, simple API |
| CQRS | MediatR, Wolverine | **MediatR** | Mature, widely adopted, simple |

### Appendix B: Data Schema Examples

**Print Time Training Record:**
```json
{
  "orderId": "uuid",
  "geometryHash": "sha256-hash",
  "volume": 125.5,
  "surfaceArea": 450.2,
  "layerCount": 280,
  "supportPercentage": 5.2,
  "infillDensity": 20,
  "material": "PLA",
  "printerType": "Prusa-MK4",
  "layerHeight": 0.2,
  "printSpeed": 50,
  "actualPrintTimeMinutes": 182,
  "recordedAt": "2026-02-14T10:30:00Z"
}
```

### Appendix C: API Response Examples

See Section 4.3 (FR-13) in the specification document for complete API contract examples.

### Appendix D: Deployment Checklist

**Pre-Production:**
- [ ] All unit tests passing (>80% coverage)
- [ ] All integration tests passing
- [ ] Load testing completed (1000 req/min)
- [ ] Security scan completed (0 critical issues)
- [ ] Database migrations tested
- [ ] Monitoring dashboards created
- [ ] Alerting rules configured
- [ ] Runbooks written
- [ ] Rollback plan documented
- [ ] Team training completed

**MALIEV Platform Standards Verification:**
- [ ] ✅ Using `Maliev.ServiceDefaults` (required)
- [ ] ✅ Using `Maliev.MessagingContracts` for events
- [ ] ✅ Scalar API documentation (NOT Swagger/Swashbuckle)
- [ ] ✅ XML documentation on all public APIs (100%)
- [ ] ✅ MassTransit for all messaging (NO direct RabbitMQ client)
- [ ] ✅ Redis caching via `StackExchange.Redis`
- [ ] ✅ PostgreSQL with EF Core (NO Dapper)
- [ ] ✅ Manual mapping (NO AutoMapper)
- [ ] ✅ Manual validation (NO FluentValidation)
- [ ] ✅ Standard xUnit assertions (NO FluentAssertions)
- [ ] ✅ `System.Text.Json` (NO Newtonsoft.Json)
- [ ] ✅ Nullable reference types enabled, zero warnings
- [ ] ✅ Async/await for all I/O operations
- [ ] ✅ Background services using IHostedService
- [ ] ✅ OpenTelemetry metrics and tracing configured
- [ ] ✅ Health checks for all dependencies
- [ ] ✅ Conventional commits with Co-Authored-By tag
- [ ] ✅ Zero banned packages in solution

**Production Deployment:**
- [ ] Blue-green deployment setup
- [ ] Canary release (10% traffic)
- [ ] Monitor error rates and latency
- [ ] Gradual rollout to 50%
- [ ] Monitor for 24 hours
- [ ] Full rollout to 100%
- [ ] Post-deployment smoke tests
- [ ] Baseline performance metrics captured

**Post-Deployment:**
- [ ] Monitor for 7 days
- [ ] Performance baseline established
- [ ] User feedback collected
- [ ] Retrospective conducted
- [ ] Documentation updated
- [ ] v1.0 release tagged

---

## 16. MALIEV Platform Standards Quick Reference

### 16.1 Required Patterns

#### Startup (Program.cs)
```csharp
// 1. ALWAYS use ServiceDefaults first
builder.AddServiceDefaults();

// 2. Configure infrastructure
builder.Services.AddDbContext<YourDbContext>(...);
builder.Services.AddMassTransit(...);
builder.Services.AddOpenApi(); // NOT AddSwaggerGen

// 3. Register services
builder.Services.AddScoped<IYourService, YourService>();

// 4. Build app
var app = builder.Build();

// 5. ALWAYS map default endpoints
app.MapDefaultEndpoints();

// 6. Map OpenAPI and Scalar
app.MapOpenApi();
app.MapScalarApiReference();
```

#### Controller Actions
```csharp
/// <summary>XML documentation REQUIRED</summary>
/// <param name="request">Parameter description</param>
/// <returns>Return value description</returns>
[HttpPost("endpoint")]
[ProducesResponseType(typeof(Response), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Action(Request request, CancellationToken ct)
{
    // 1. Manual validation (NO FluentValidation)
    var errors = ValidateRequest(request);
    if (errors.Any())
        return BadRequest(new ValidationProblemDetails(errors));

    // 2. Manual mapping (NO AutoMapper)
    var command = new Command
    {
        Property = request.Property
    };

    // 3. Execute via MediatR
    var result = await _mediator.Send(command, ct);

    return Ok(result);
}
```

#### Manual Mapping
```csharp
// Extension method pattern
public static ResponseDto ToDto(this Entity entity)
{
    return new ResponseDto
    {
        Id = entity.Id,
        Name = entity.Name,
        // Explicit property mapping
    };
}
```

#### Manual Validation
```csharp
private static Dictionary<string, string[]> ValidateRequest(Request request)
{
    var errors = new Dictionary<string, string[]>();

    if (string.IsNullOrWhiteSpace(request.Name))
        errors[nameof(request.Name)] = new[] { "Name is required." };

    if (request.Value < 0)
        errors[nameof(request.Value)] = new[] { "Value must be positive." };

    return errors;
}
```

#### Unit Tests (xUnit)
```csharp
[Fact]
public void Method_WithCondition_ShouldExpectedBehavior()
{
    // Arrange
    var input = CreateInput();

    // Act
    var result = Method(input);

    // Assert - Standard xUnit (NO FluentAssertions)
    Assert.NotNull(result);
    Assert.Equal(expected, result.Value);
    Assert.InRange(result.Number, 1, 10);
    Assert.True(condition, "Reason for assertion");
}
```

#### Background Services
```csharp
public class MyBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Create scope for scoped services
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IService>();

            await service.DoWorkAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}
```

### 16.2 Banned Patterns

❌ **DO NOT DO THIS:**

```csharp
// ❌ AutoMapper
var dto = _mapper.Map<EntityDto>(entity);

// ❌ FluentValidation
public class RequestValidator : AbstractValidator<Request> { }

// ❌ FluentAssertions
result.Should().NotBeNull();
result.Value.Should().Be(expected);

// ❌ Newtonsoft.Json
var json = JsonConvert.SerializeObject(obj);

// ❌ Direct RabbitMQ usage
var factory = new ConnectionFactory();

// ❌ Swashbuckle
builder.Services.AddSwaggerGen();

// ❌ Service locator pattern
var service = serviceProvider.GetService<IService>();
```

✅ **DO THIS INSTEAD:**

```csharp
// ✅ Manual mapping
var dto = entity.ToDto();

// ✅ Manual validation
var errors = ValidateRequest(request);

// ✅ Standard xUnit
Assert.NotNull(result);
Assert.Equal(expected, result.Value);

// ✅ System.Text.Json
var json = JsonSerializer.Serialize(obj);

// ✅ MassTransit
builder.Services.AddMassTransit(x => { ... });

// ✅ Scalar
builder.Services.AddOpenApi();
app.MapScalarApiReference();

// ✅ Constructor injection
public class Service
{
    private readonly IDependency _dependency;
    public Service(IDependency dependency) => _dependency = dependency;
}
```

### 16.3 Code Quality Checklist

Before committing code, verify:

- [ ] All public APIs have XML documentation (`<summary>`, `<param>`, `<returns>`)
- [ ] No nullable reference warnings (`#nullable enable`)
- [ ] All I/O operations are async
- [ ] No magic strings (use constants or configuration)
- [ ] Manual mapping for all DTOs
- [ ] Manual validation for all inputs
- [ ] Standard xUnit assertions in tests
- [ ] Using `record` types for immutable DTOs
- [ ] Using pattern matching where appropriate
- [ ] No banned packages referenced
- [ ] Conventional commit message format
- [ ] Co-Authored-By: Claude Sonnet 4.5 tag in commits

---

## 17. Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2026-02-14 | Platform Team | Initial implementation plan |

---

**Document Status:** Ready for Implementation
**Next Steps:** Begin Phase 1 - Week 1 (Project Setup)
**Questions/Feedback:** Contact MALIEV Platform Team

---

**End of Implementation Plan**
