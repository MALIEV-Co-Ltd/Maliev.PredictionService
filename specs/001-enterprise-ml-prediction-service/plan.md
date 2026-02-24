# Implementation Plan: Enterprise ML Prediction Service

**Branch**: `001-enterprise-ml-prediction-service` | **Date**: 2026-02-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/001-enterprise-ml-prediction-service/spec.md` and comprehensive plan from `predictionservice-plan.md`

## Summary

Build an enterprise-grade ML prediction service providing 8 core prediction capabilities for the MALIEV manufacturing ecosystem:
- **P0 (Critical)**: 3D print time prediction, demand forecasting, dynamic pricing, automated model training
- **P1 (High)**: Customer churn prediction, material demand forecasting, production bottleneck detection, explainable AI

**Technical Approach**: Clean Architecture with CQRS pattern, ML.NET for model training/inference, automated model lifecycle management with quality gates, comprehensive observability, and event-driven data ingestion from upstream microservices.

## Technical Context

**Language/Version**: C# 13 / .NET 10.0 (latest LTS)
**Primary Dependencies**:
- ML.NET 4.0+ (model training, inference, feature engineering)
- ML.NET.TimeSeries 4.0+ (time-series forecasting for demand)
- MassTransit 8.x+ with RabbitMQ (event-driven integration)
- PostgreSQL 18 with EF Core 10.x (model registry, training data, audit logs)
- Redis 7 with StackExchange.Redis 2.x+ (prediction caching, distributed locking)
- MediatR 12.x+ (CQRS command/query pattern)
- Polly 8.x+ (resilience policies)
- Maliev.ServiceDefaults (NuGet - Aspire integration, OpenTelemetry, standardized configuration)

**Storage**:
- PostgreSQL (schemas: `ml_models`, `training`, `predictions`, `audit`)
- Redis (DB 0: prediction cache, DB 1: model metadata)
- File system / Azure Blob Storage (model binaries - ONNX, ML.NET ZIP archives)

**Testing**:
- xUnit 2.x+ (unit tests)
- Testcontainers 4.x+ (integration tests with real PostgreSQL, Redis, RabbitMQ)
- Moq 4.x+ (mocking for unit tests)
- Bogus 35.x+ (test data generation)

**Target Platform**: Linux containers (Docker), Kubernetes orchestration
**Project Type**: Web API microservice with background workers (IHostedService)

**Performance Goals**:
- P95 latency < 500ms for cached predictions
- P95 latency < 2s for uncached 3D geometry predictions
- P95 latency < 1s for all other prediction types
- Sustained throughput: 1,000 predictions/minute
- Burst capacity: 2,500 predictions/minute (5-minute window)
- Model training: < 4 hours for largest models

**Constraints**:
- No banned libraries: AutoMapper, FluentValidation, FluentAssertions, Newtonsoft.Json, Dapper, Swashbuckle
- Real infrastructure testing only (no in-memory databases/caches)
- Zero build warnings policy
- 80%+ code coverage for business logic
- OAuth 2.0 / OpenID Connect via IAMService
- All API responses must include explainability (feature importance)

**Scale/Scope**:
- 8 distinct ML model types (regression, time-series, classification)
- 100K+ historical records per model for training
- 10+ concurrent active models in production
- Batch processing: up to 1,000 predictions per request
- Cache hit rate target: >60%
- Model retraining: automated on schedule (daily/weekly/monthly)
- Multi-tenant support via tenant ID in requests

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Service Autonomy** | ✅ PASS | Own PostgreSQL schemas, independent domain logic, event-driven integration only |
| **II. Explicit Contracts** | ✅ PASS | OpenAPI via Scalar, versioned request/response DTOs, backward-compatible |
| **III. Test-First Development** | ✅ PASS | Tests specified in spec acceptance criteria, TDD workflow mandatory |
| **IV. Real Infrastructure Testing** | ✅ PASS | Testcontainers for PostgreSQL, Redis, RabbitMQ - no in-memory substitutes |
| **V. Auditability & Observability** | ✅ PASS | Structured logging, immutable audit logs (FR-063), health checks (FR-071), OpenTelemetry via ServiceDefaults |
| **VI. Security & Compliance** | ✅ PASS | OAuth 2.0 (FR-052), RBAC (FR-053), encryption (FR-059, FR-060), GDPR compliance (FR-062) |
| **VII. Secrets Management** | ✅ PASS | Google Secret Manager, no secrets in code, BuildKit secrets in Dockerfile |
| **VIII. Zero Warnings Policy** | ✅ PASS | Nullable reference types enabled, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` |
| **IX. Clean Project Artifacts** | ✅ PASS | No additional markdown docs in repo root, CODEOWNERS mandatory, proper .gitignore/.dockerignore |
| **X. Docker Best Practices** | ✅ PASS | Dockerfile in API project, built-in `app` user, multi-stage builds, BuildKit secrets for NuGet |
| **XI. Simplicity & Maintainability** | ✅ PASS | YAGNI applied, Clean Architecture, domain-driven design |
| **XII. Business Metrics** | ✅ PASS | FR-065 to FR-074 define comprehensive metrics (API + model health + business KPIs) |
| **XIII. Aspire Integration** | ✅ PASS | Maliev.ServiceDefaults as NuGet package, nuget.config required, AddServiceDefaults() in Program.cs |

**Result**: ✅ **ALL GATES PASSED** - Ready to proceed to Phase 0

## Project Structure

### Documentation (this feature)

```text
specs/001-enterprise-ml-prediction-service/
├── spec.md              # Feature specification (completed)
├── plan.md              # This file (implementation plan)
├── research.md          # Phase 0 output (technology decisions, best practices)
├── data-model.md        # Phase 1 output (entity definitions, relationships)
├── quickstart.md        # Phase 1 output (local development setup)
├── contracts/           # Phase 1 output (OpenAPI specs, event schemas)
│   ├── openapi.yaml
│   └── events/
│       ├── PredictionRequested.json
│       └── ModelTrained.json
├── checklists/          # Quality validation
│   └── requirements.md  # Specification quality checklist (completed)
└── tasks.md             # Phase 2 output (/speckit.tasks - NOT created yet)
```

### Source Code (repository root)

```text
Maliev.PredictionService/
├── Maliev.PredictionService.Api/                    # Web API layer
│   ├── Controllers/
│   │   ├── PredictionsController.cs                 # POST /predictions/v1/*
│   │   ├── ModelsController.cs                       # GET /predictions/v1/models/*
│   │   └── HealthController.cs                       # Health checks
│   ├── Middleware/
│   │   ├── ErrorHandlingMiddleware.cs
│   │   └── RequestLoggingMiddleware.cs
│   ├── Extensions/
│   │   └── ServiceCollectionExtensions.cs
│   ├── Program.cs                                    # Startup + ServiceDefaults
│   ├── appsettings.json
│   ├── appsettings.Development.json
│   ├── Dockerfile                                    # Multi-stage build
│   └── Maliev.PredictionService.Api.csproj
│
├── Maliev.PredictionService.Application/            # Use cases (CQRS)
│   ├── Commands/
│   │   ├── Predictions/
│   │   │   ├── PredictPrintTimeCommand.cs
│   │   │   ├── PredictDemandCommand.cs
│   │   │   ├── PredictPriceCommand.cs
│   │   │   └── PredictChurnCommand.cs
│   │   └── Models/
│   │       ├── TrainModelCommand.cs
│   │       └── DeployModelCommand.cs
│   ├── Queries/
│   │   ├── GetModelHealthQuery.cs
│   │   ├── GetPredictionHistoryQuery.cs
│   │   └── GetChurnRiskQuery.cs
│   ├── DTOs/
│   │   ├── Requests/
│   │   │   ├── PrintTimePredictionRequest.cs
│   │   │   ├── DemandForecastRequest.cs
│   │   │   └── PriceRecommendationRequest.cs
│   │   └── Responses/
│   │       ├── PredictionResponse.cs
│   │       ├── ExplanationData.cs
│   │       └── ModelHealthResponse.cs
│   ├── Services/
│   │   ├── PredictionOrchestrator.cs                # Caching, model selection
│   │   ├── ExplainabilityService.cs                 # Feature importance
│   │   └── CacheService.cs                          # Redis cache abstraction
│   ├── Validators/
│   │   ├── PrintTimeRequestValidator.cs             # Manual validation
│   │   └── DemandForecastRequestValidator.cs
│   └── Maliev.PredictionService.Application.csproj
│
├── Maliev.PredictionService.Domain/                 # Business logic
│   ├── Entities/
│   │   ├── MLModel.cs                                # Model registry entity
│   │   ├── TrainingDataset.cs
│   │   ├── PredictionAuditLog.cs
│   │   └── TrainingJob.cs
│   ├── ValueObjects/
│   │   ├── PerformanceMetrics.cs
│   │   ├── ConfidenceInterval.cs
│   │   ├── FeatureContribution.cs
│   │   └── ModelVersion.cs
│   ├── Enums/
│   │   ├── ModelType.cs                              # PrintTime, DemandForecast, etc.
│   │   ├── ModelStatus.cs                            # Draft, Testing, Active, Deprecated
│   │   └── PredictionStatus.cs
│   ├── Interfaces/
│   │   ├── IModelRepository.cs
│   │   ├── IPredictor.cs                             # Generic prediction interface
│   │   ├── IModelTrainer.cs                          # Model training contract
│   │   └── IExplainer.cs                             # Explainability contract
│   ├── Services/
│   │   └── ModelLifecycleManager.cs                  # State transitions, quality gates
│   └── Maliev.PredictionService.Domain.csproj
│
├── Maliev.PredictionService.Infrastructure/         # External dependencies
│   ├── Persistence/
│   │   ├── PredictionDbContext.cs                    # EF Core DbContext
│   │   ├── Configurations/
│   │   │   ├── MLModelConfiguration.cs              # Entity configurations
│   │   │   ├── TrainingDatasetConfiguration.cs
│   │   │   └── PredictionAuditLogConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── ModelRepository.cs
│   │   │   ├── TrainingDatasetRepository.cs
│   │   │   └── PredictionAuditRepository.cs
│   │   └── Migrations/
│   ├── ML/
│   │   ├── Predictors/
│   │   │   ├── PrintTimePredictor.cs                 # ML.NET FastTree regression
│   │   │   ├── DemandForecaster.cs                   # ML.NET SSA time-series
│   │   │   ├── PriceOptimizer.cs                     # ML.NET regression
│   │   │   └── ChurnPredictor.cs                     # ML.NET binary classification
│   │   ├── Trainers/
│   │   │   ├── PrintTimeTrainer.cs
│   │   │   ├── DemandForecastTrainer.cs
│   │   │   └── ChurnModelTrainer.cs
│   │   ├── FeatureEngineering/
│   │   │   ├── GeometryFeatureExtractor.cs          # 3D file → features
│   │   │   ├── TimeSeriesTransformer.cs
│   │   │   └── CustomerFeatureAggregator.cs
│   │   ├── Explainers/
│   │   │   └── SHAPExplainer.cs                      # Feature importance
│   │   └── ModelRegistry/
│   │       ├── ModelLoader.cs                        # Load from disk/blob
│   │       └── ModelVersionManager.cs
│   ├── Events/
│   │   ├── Consumers/
│   │   │   ├── OrderCreatedConsumer.cs              # MassTransit consumers
│   │   │   ├── CustomerUpdatedConsumer.cs
│   │   │   └── MaterialTransactionConsumer.cs
│   │   └── Publishers/
│   │       └── PredictionPublisher.cs                # Publish prediction events
│   ├── BackgroundServices/
│   │   ├── ModelTrainingService.cs                   # IHostedService for scheduled training
│   │   ├── ModelDriftDetectionService.cs             # Monitor accuracy degradation
│   │   └── DataCleanupService.cs                     # Cleanup old audit logs
│   ├── Caching/
│   │   ├── RedisCacheService.cs                      # StackExchange.Redis wrapper
│   │   └── CacheKeyGenerator.cs                      # SHA-256 hash generation
│   └── Maliev.PredictionService.Infrastructure.csproj
│
├── Tests/
│   ├── Maliev.PredictionService.UnitTests/
│   │   ├── Domain/
│   │   │   ├── ModelLifecycleManagerTests.cs
│   │   │   └── PerformanceMetricsTests.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   └── PredictPrintTimeCommandTests.cs
│   │   │   └── Services/
│   │   │       └── ExplainabilityServiceTests.cs
│   │   └── Maliev.PredictionService.UnitTests.csproj
│   └── Maliev.PredictionService.IntegrationTests/
│       ├── Api/
│       │   ├── PredictionsControllerTests.cs         # End-to-end API tests
│       │   └── ModelsControllerTests.cs
│       ├── Infrastructure/
│       │   ├── ModelRepositoryTests.cs               # Testcontainers PostgreSQL
│       │   ├── RedisCacheServiceTests.cs             # Testcontainers Redis
│       │   └── EventConsumerTests.cs                 # Testcontainers RabbitMQ
│       ├── ML/
│       │   ├── PrintTimePredictorTests.cs            # ML.NET integration tests
│       │   └── ModelTrainingTests.cs
│       ├── TestFixtures/
│       │   ├── PostgreSqlFixture.cs                  # Testcontainer setup
│       │   ├── RedisFixture.cs
│       │   └── RabbitMqFixture.cs
│       └── Maliev.PredictionService.IntegrationTests.csproj
│
├── nuget.config                                      # GitHub Packages source
├── .gitignore
├── .dockerignore
├── .github/
│   ├── CODEOWNERS                                    # * @MALIEV-Co-Ltd/core-developers
│   └── workflows/
│       ├── ci.yml                                    # Build, test, Docker
│       └── cd.yml                                    # Deploy to K8s
├── README.md
└── Maliev.PredictionService.sln
```

**Structure Decision**: Clean Architecture with 4 layers (Api, Application, Domain, Infrastructure) following MALIEV platform standards. Application layer uses CQRS pattern with MediatR. Domain layer contains pure business logic with no external dependencies. Infrastructure layer implements all external integrations (PostgreSQL, Redis, RabbitMQ, ML.NET). Tests are organized by type (unit vs integration) with Testcontainers for real infrastructure testing.

## Complexity Tracking

> **Constitution Check violations**: NONE - all gates passed.

## Phase 0: Research & Technology Decisions

**Status**: ✅ Completed (extracted from comprehensive plan)

Key research findings documented in [research.md](./research.md):

### ML.NET Model Selection

| Prediction Type | Algorithm Choice | Rationale |
|-----------------|------------------|-----------|
| Print Time | FastTreeRegression | Handles non-linear relationships between geometry features and time |
| Demand Forecast | SSA (Singular Spectrum Analysis) | Time-series forecasting with trend/seasonality decomposition |
| Price Optimization | FastTreeRegression + custom elasticity calculation | Regression for price, business logic for probability |
| Churn Prediction | FastTreeBinaryClassification | Handles imbalanced datasets, provides probability scores |
| Material Demand | SSA time-series | Similar to sales demand, material-level granularity |
| Bottleneck Detection | FastTreeRegression | Predict queue wait times based on historical patterns |

### Caching Strategy

- **Content-based hashing**: SHA-256 of normalized input parameters
- **Cache key format**: `{modelType}:{sha256(sortedInputParams)}:{modelVersion}`
- **TTL by model type**: Print-time (24h), Demand (6h), Price (1h), Churn (24h)
- **Cache invalidation**: Automatic on model version update
- **Redis DB separation**: DB 0 (predictions), DB 1 (model metadata)

### Model Lifecycle Automation

- **State transitions**: Draft → Testing (auto after training) → Active (auto if quality gates pass) → Deprecated (auto when new Active) → Archived (manual after 90 days)
- **Quality gates**:
  - Minimum dataset size (model-specific: 10K for print time, 5K for price, 2K for churn)
  - Accuracy improvement >2% over current production model
  - No critical data quality warnings
  - Validation on holdout dataset (20% of total data)
- **A/B testing**: Canary deployment with 10% traffic before full rollout
- **Rollback**: Automatic revert if production accuracy degrades >5% in 24 hours

### Data Ingestion Patterns

- **Event-driven**: MassTransit consumers for domain events (OrderCreated, CustomerUpdated, etc.)
- **Incremental updates**: Real-time feature store updates
- **Full sync**: One-time historical backfill on service initialization
- **Data quality**: Validation on ingestion, outlier detection, schema evolution handling

## Phase 1: Data Model & Contracts

**Status**: ⏳ In Progress

Deliverables:
1. **data-model.md** - Entity definitions, relationships, validation rules
2. **contracts/** - OpenAPI spec + event schemas
3. **quickstart.md** - Local development setup guide
4. **Agent context update** - Technology stack documented for AI agents

### Entity Relationships

```
MLModel (1) ←→ (N) TrainingDataset
MLModel (1) ←→ (N) TrainingJob
MLModel (1) ←→ (N) PredictionAuditLog

PredictionAuditLog (N) ←→ (1) MLModel
TrainingDataset (N) ←→ (1) TrainingJob

Customer (external) ←→ (N) PredictionAuditLog [via customer_id]
```

### API Endpoints Summary

```http
# Predictions
POST   /predictions/v1/print-time           # 3D geometry → time estimate
POST   /predictions/v1/demand-forecast      # Category/horizon → demand forecast
POST   /predictions/v1/price-recommendation # Quote params → optimal price
GET    /predictions/v1/churn-risk/{customerId}  # Customer → churn risk score
POST   /predictions/v1/material-forecast    # SKU → consumption forecast
POST   /predictions/v1/bottleneck-detection # Schedule → bottleneck predictions
POST   /predictions/v1/batch                # Batch prediction (async)
GET    /predictions/v1/batch/{batchId}/status
GET    /predictions/v1/batch/{batchId}/results

# Models
GET    /predictions/v1/models                # List all models
GET    /predictions/v1/models/{modelType}/health  # Model health check
GET    /predictions/v1/models/{modelType}/versions  # Version history
POST   /predictions/v1/models/{modelType}/train  # Trigger training (admin)
POST   /predictions/v1/models/{modelType}/rollback/{version}  # Rollback
GET    /predictions/v1/models/{modelType}/metrics  # Performance metrics

# Health & Observability
GET    /predictionservice/health              # ServiceDefaults health check
GET    /predictionservice/liveness            # Kubernetes liveness probe
GET    /predictionservice/readiness           # Kubernetes readiness probe
GET    /predictionservice/metrics             # Prometheus metrics
```

## Next Steps

1. **Complete Phase 1 artifacts**:
   - Generate `research.md` with detailed technology decisions
   - Generate `data-model.md` with full entity definitions
   - Generate `contracts/openapi.yaml` with complete API spec
   - Generate `contracts/events/*` with MassTransit message schemas
   - Generate `quickstart.md` with Docker Compose setup

2. **Update agent context**:
   - Run `.specify/scripts/powershell/update-agent-context.ps1 -AgentType claude`
   - Document ML.NET, CQRS, Clean Architecture patterns

3. **Proceed to Phase 2**:
   - Run `/speckit.tasks` to generate actionable task breakdown
   - Prioritize P0 features (print time, demand, pricing, model training)
   - Implement TDD workflow with Testcontainers

## Dependencies & Risks

| Dependency | Risk Level | Mitigation |
|------------|------------|------------|
| Upstream service events | Medium | Implement event replay, cache last known state |
| Model training data quality | High | Automated validation, data quality gates before training |
| ML.NET model accuracy | Medium | Regular retraining, A/B testing, rollback capability |
| Cache performance (Redis) | Low | Redis is proven, connection pooling via ServiceDefaults |
| 3D geometry file parsing | Medium | Validate formats early, fallback to rule-based estimation |
| Cross-service authentication | Low | OAuth 2.0 via IAMService (platform standard) |

## Success Metrics (Re-stated from Spec)

- **API Performance**: P95 < 500ms cached, P95 < 2s uncached geometry, P95 < 1s other
- **Model Accuracy**: R² > 0.85 (print time), MAPE < 15% (demand), Precision > 0.75 (churn)
- **Business Impact**: Quote time 30-45min → <5min, Churn reduction 15%, Margin improvement 15-25%
- **Operational**: 99.5% uptime, 80%+ code coverage, zero build warnings, automated deployment
- **Observability**: 100% prediction audit logs, model drift detection within 24h, comprehensive metrics (FR-065 to FR-074)
