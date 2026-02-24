# Research & Technology Decisions

**Feature**: Enterprise ML Prediction Service
**Date**: 2026-02-14
**Status**: Completed

## Executive Summary

This document captures all technology research and decisions made for implementing the ML prediction service. All unknowns from the Technical Context have been resolved through best practices research and architectural analysis.

## Decision Log

### 1. ML Framework Selection: ML.NET vs Python ML Stack

**Decision**: Use ML.NET 4.0+ for all model training and inference

**Rationale**:
- **Platform Integration**: Native .NET integration eliminates language barriers and serialization overhead
- **Production Performance**: ML.NET models run in-process with ASP.NET Core, reducing latency (no HTTP/gRPC calls to Python services)
- **Deployment Simplicity**: Single runtime (.NET 10), no Python environment management, no dependency hell
- **Type Safety**: Strongly-typed prediction inputs/outputs with compile-time checking
- **Observability**: Seamless OpenTelemetry integration via ServiceDefaults
- **Team Expertise**: .NET-first organization, reduced context switching

**Alternatives Considered**:
- **Python (scikit-learn, TensorFlow)**: Rejected - requires separate service, additional infrastructure, IPC overhead
- **ONNX Runtime**: Considered for production serving (still option for optimization), but ML.NET native preferred for simplicity
- **Azure ML / AWS SageMaker**: Rejected - vendor lock-in, cost, unnecessary complexity for current scale

**Trade-offs Accepted**:
- ML.NET ecosystem smaller than Python's (acceptable - covers all required algorithms)
- Fewer pre-trained models available (not needed - custom models required anyway)

---

### 2. ML Algorithms by Prediction Type

| Prediction Type | Algorithm | Rationale |
|-----------------|-----------|-----------|
| **3D Print Time** | FastTreeRegression | Non-linear relationships, handles categorical features (material/printer), excellent accuracy/speed trade-off |
| **Demand Forecast** | SSA (Singular Spectrum Analysis) | Time-series specific, trend/seasonality decomposition, handles irregular patterns better than ARIMA |
| **Price Optimization** | FastTreeRegression + Custom Business Logic | Regression for base price, custom elasticity/win-probability calculations based on domain knowledge |
| **Churn Prediction** | FastTreeBinaryClassification | Handles class imbalance (most customers don't churn), provides probability scores, feature importance built-in |
| **Material Demand** | SSA Time-Series | Same as sales demand, material-level granularity |
| **Bottleneck Detection** | FastTreeRegression | Predict queue wait times as regression problem |

**FastTree Family Chosen Because**:
- Gradient boosted trees: state-of-the-art for tabular data
- Built-in feature importance (supports explainability requirement FR-033)
- Handles missing values, categorical features without extensive preprocessing
- Fast training and inference
- ML.NET's most mature and battle-tested algorithm family

**SSA for Time-Series Because**:
- Better than ARIMA for non-stationary data (typical in demand forecasting)
- Automatically decomposes trend, seasonality, noise
- Handles irregular patterns (promotions, holidays)
- Native ML.NET support with minimal configuration

---

### 3. Feature Engineering Strategy

**Geometry Feature Extraction** (3D Print Time):
- **Library**: Custom implementation using `System.Numerics` for 3D math
- **Features Extracted**:
  - Volume (mm³)
  - Surface area (mm²)
  - Layer count (derived from bounding box height / layer height)
  - Support structure percentage (estimated via overhang detection)
  - Complexity score (triangle count, curvature metrics)
  - Bounding box dimensions
- **File Parsing**: STL binary format parsing (most common, simple to parse)
- **Performance**: <100ms for files up to 50MB (in-memory processing)

**Customer RFM Features** (Churn Prediction):
- **Recency**: Days since last order
- **Frequency**: Order count last 12 months
- **Monetary Value**: Total spend last 12 months
- **Augmented Features**:
  - Support ticket count (indicates dissatisfaction)
  - Payment delay frequency (indicates financial stress)
  - Engagement score (email opens, portal logins)
  - Customer lifetime (days since first order)

**Time-Series Transformations** (Demand Forecasting):
- **Calendar Features**: Day of week, month, quarter, holidays
- **Lag Features**: Previous 7, 14, 30 days demand
- **Rolling Statistics**: 7-day, 30-day moving averages
- **Promotion Flags**: Binary indicators for promotional periods

---

### 4. Model Explainability Approach

**Decision**: Implement SHAP-like feature importance calculation

**Implementation**:
- **For Tree Models**: Extract feature importance from trained tree ensemble (ML.NET provides `PermutationFeatureImportance` API)
- **For Linear Models**: Direct coefficient inspection
- **Output Format**: Top 3-5 features with impact weights (0.0-1.0), sorted by importance

**Human-Readable Explanations**:
- Template-based generation: "Price is high because {feature1} is in top 10% and {feature2} is above average"
- Contextual thresholds: Compare feature values to population percentiles

**Rationale**:
- SHAP is gold standard for model explanations
- ML.NET's PermutationFeatureImportance approximates SHAP values efficiently
- Meets FR-033 requirement without third-party SHAP libraries

---

### 5. Caching Strategy

**Decision**: Content-based hashing with Redis, TTL varies by model type

**Cache Key Design**:
```
Format: {modelType}:{sha256(sortedInputParams)}:{modelVersion}
Example: print-time:a3f2e1b9...:v2.3.1
```

**SHA-256 Hashing Approach**:
1. Sort input parameters alphabetically by key
2. JSON serialize with consistent formatting (no whitespace)
3. SHA-256 hash the JSON string
4. Hex encode for readability

**TTL by Model Type**:
| Model Type | TTL | Rationale |
|------------|-----|-----------|
| Print Time | 24 hours | Geometry doesn't change, printer/material specs stable |
| Demand Forecast | 6 hours | Daily updates, but intraday stability acceptable |
| Price Recommendation | 1 hour | Market conditions change frequently |
| Churn Risk | 24 hours | Customer behavior changes slowly |
| Material Demand | 12 hours | Balance between freshness and cache efficiency |

**Cache Invalidation**:
- **On Model Update**: Invalidate all keys matching `{modelType}:*:{oldVersion}`
- **Manual Purge**: Admin endpoint to clear specific model type cache
- **Automatic Expiry**: TTL enforcement by Redis

**Redis DB Separation**:
- DB 0: Prediction cache (high churn, shorter TTL)
- DB 1: Model metadata cache (low churn, longer TTL)

---

### 6. Model Lifecycle Automation

**State Transition Flow**:
```
Draft → Testing → Active → Deprecated → Archived
  │        │        │          │
  └────────┴────────┴──────────┘ (rollback possible)
```

**Quality Gates** (Testing → Active):
1. **Minimum Dataset Size**: Model-specific thresholds (10K for print-time, 5K for price, 2K for churn)
2. **Accuracy Improvement**: New model must exceed current production by >2% on primary metric (R², MAPE, Precision)
3. **Holdout Validation**: Evaluate on 20% held-out test set (never seen during training)
4. **Data Quality**: No critical warnings (>5% missing values, outliers beyond 3σ)

**Automated Promotion**:
- If all gates pass: Auto-deploy to "Active" status
- If gates fail: Remain in "Testing", alert data science team with failure details
- Manual override available (admin approval can bypass gates in emergency)

**A/B Testing**:
- **Canary Deployment**: New model serves 10% of traffic initially
- **Duration**: 24 hours at 10%, then 100% if no degradation detected
- **Rollback Trigger**: If production accuracy degrades >5% compared to baseline

**Rollback Mechanism**:
- **Automatic**: If drift detection identifies >5% accuracy drop in 24-hour rolling window
- **Manual**: Admin API endpoint to revert to specific previous version
- **Preservation**: Keep last 5 versions per model type, archive older versions

---

### 7. Data Ingestion Patterns

**Event-Driven Architecture**:
- **MassTransit Consumers**: Subscribe to domain events from upstream services
- **Events Consumed**:
  - `OrderCreated`, `OrderCompleted` (OrderService)
  - `CustomerUpdated`, `CustomerCreated` (CustomerService)
  - `MaterialTransaction`, `InventoryUpdated` (MaterialService)
  - `InvoiceCreated`, `PaymentReceived` (InvoiceService)
  - `ManufacturingJobCompleted` (ManufacturingService)

**Data Transformation Pipeline**:
1. **Event Reception**: MassTransit consumer receives event
2. **Validation**: Schema validation, null checks
3. **Transformation**: Map domain event to feature store format
4. **Deduplication**: Check if entity already processed (idempotency)
5. **Storage**: Insert into PostgreSQL feature store or training dataset
6. **Trigger**: If minimum dataset size reached, schedule model retraining

**Full Historical Sync**:
- **On Initialization**: One-time backfill from upstream services' historical data
- **Mechanism**: Call upstream services' batch export APIs (not real-time events)
- **Volume**: Expect 100K+ orders, 10K+ customers, 1M+ material transactions
- **Duration**: 1-2 hours for full sync (parallelized ingestion)

**Data Quality Checks**:
- **Null Detection**: Reject records with >10% null feature values
- **Outlier Detection**: Flag values beyond 3σ from mean (reviewed before training)
- **Schema Evolution**: Gracefully handle new/missing fields in events
- **Audit Trail**: Log all rejected records for debugging

---

### 8. Database Schema Design

**Multi-Schema Approach**:
- **ml_models**: Model registry, versions, performance metrics
- **training**: Datasets, training jobs, feature store
- **predictions**: Prediction results, cache metadata
- **audit**: Immutable audit logs (partitioned by month for performance)

**Partitioning Strategy**:
- **Audit Logs**: Monthly partitions (`audit.prediction_logs_2026_02`, etc.)
- **Rationale**: High write volume (100K+ predictions/day), query patterns favor recent data
- **Retention**: 13-month rolling window (current + 12 previous months)

**Indexing Strategy**:
- **Primary Queries**:
  - Get active model by type: Index on (model_type, status)
  - Prediction history by user: Index on (user_id, created_at DESC)
  - Training job status: Index on (status, created_at DESC)
  - Feedback lookup: Partial index on (actual_value) WHERE actual_value IS NOT NULL

**JSONB Usage**:
- **Performance Metrics**: Flexible schema for different model types (regression metrics vs classification)
- **Prediction Input/Output**: Varies by model type, JSONB allows schema flexibility
- **Feature Store**: Dynamic feature sets per entity type

---

### 9. Testing Strategy

**Unit Tests**:
- **Framework**: xUnit 2.x+
- **Mocking**: Moq 4.x+ for interface mocks
- **Coverage Target**: 80%+ for business logic (domain + application layers)
- **Test Data**: Bogus library for realistic fake data generation

**Integration Tests**:
- **Infrastructure**: Testcontainers 4.x+ for real PostgreSQL, Redis, RabbitMQ
- **Rationale**: In-memory databases have different behavior (violates Constitution IV)
- **Fixtures**:
  - `PostgreSqlFixture`: Spins up PostgreSQL container, applies migrations
  - `RedisFixture`: Spins up Redis container, flushes DB before each test
  - `RabbitMqFixture`: Spins up RabbitMQ container, purges queues

**ML Model Tests**:
- **Training Tests**: Verify model trains without errors, metrics within expected range
- **Inference Tests**: Verify predictions return expected data types, confidence scores
- **Data Quality Tests**: Verify feature extraction handles edge cases (empty files, malformed data)

**API Tests**:
- **WebApplicationFactory**: In-memory test server with real dependencies (Testcontainers)
- **End-to-End**: HTTP request → controller → CQRS → domain → infrastructure → response
- **Authentication**: Mock JWT tokens for testing RBAC

---

### 10. Observability Implementation

**Metrics** (FR-065 to FR-074):
- **API Performance**:
  - `prediction_request_count` (counter, labeled by model_type, status_code)
  - `prediction_latency_milliseconds` (histogram, P50/P95/P99)
  - `prediction_cache_hit_rate` (gauge)
- **Model Health**:
  - `model_accuracy` (gauge, by model_type, updated daily)
  - `model_drift_score` (gauge, by model_type)
  - `model_version_active` (info metric, by model_type)
- **Business Metrics**:
  - `daily_prediction_volume` (counter, by model_type)
  - `unique_users_daily` (gauge)
  - `avg_confidence_score` (gauge, by model_type)

**Traces**:
- **Distributed Tracing**: OpenTelemetry via ServiceDefaults
- **Correlation IDs**: Propagated across service boundaries via MassTransit headers
- **Span Hierarchy**: HTTP Request → CQRS Handler → Model Inference → Cache/DB Operations

**Logs**:
- **Structured Logging**: JSON format via OpenTelemetry
- **Log Levels**: Following Constitution V configuration
- **PII Handling**: Redact customer IDs, user IDs in logs (mask middle characters)

**Dashboards**:
- **Grafana Visualizations**:
  - Prediction latency over time (by model type)
  - Cache hit rate trends
  - Model accuracy degradation alerts
  - Daily prediction volume
  - Error rate spikes

---

### 11. Security & Compliance

**Authentication**:
- **OAuth 2.0 / OpenID Connect**: Via IAMService integration
- **JWT Token Validation**: Middleware in API layer
- **No Custom Auth**: Reuse platform standard (reduces attack surface)

**Authorization**:
- **RBAC**: Role-based access control
  - `PredictionUser`: Can request predictions
  - `PredictionAdmin`: Can trigger model training, view model health
  - `DataScientist`: Full access including model registry

**Data Encryption**:
- **In Transit**: TLS 1.3 for all HTTP/gRPC communication
- **At Rest**: PostgreSQL encryption at rest (platform standard)
- **Secrets**: Google Secret Manager (Constitution VII)

**GDPR Compliance**:
- **Data Deletion**: Support `DELETE /predictions/user/{userId}` endpoint
- **Anonymization**: PII anonymization in training datasets (hash customer IDs)
- **Audit Trail**: Immutable logs of all prediction requests (retention: 13 months)
- **Consent**: Only process data for customers with active consent flag (checked upstream)

---

### 12. Deployment Strategy

**Containerization**:
- **Dockerfile Location**: `Maliev.PredictionService.Api/Dockerfile` (Constitution X)
- **Multi-Stage Build**: SDK for build, ASP.NET runtime for final image
- **User**: Built-in `app` user (no custom user creation)
- **BuildKit Secrets**: NuGet credentials via `--mount=type=secret`

**CI/CD**:
- **GitHub Actions**:
  - Workflow 1: `ci.yml` - Build, test (unit + integration with Testcontainers), Docker build
  - Workflow 2: `cd.yml` - Deploy to Kubernetes (triggered on main branch merge)
- **Test Execution**: Run Testcontainers tests in GitHub Actions (Docker-in-Docker)

**Kubernetes Deployment**:
- **Replicas**: 3 for high availability
- **Resources**:
  - Requests: 1 CPU, 2Gi memory
  - Limits: 2 CPU, 4Gi memory (ML inference can be memory-intensive)
- **Health Checks**:
  - Liveness: `/predictionservice/liveness`
  - Readiness: `/predictionservice/readiness`
  - Startup Probe: `/predictionservice/health` (allows slow model loading)

**Model Storage**:
- **Local Development**: File system (`/app/models/`)
- **Production**: Azure Blob Storage (future enhancement, file system sufficient for v1.0)

---

## Open Questions (All Resolved)

All technical unknowns from the original Technical Context have been resolved. No blocking questions remain.

## Next Steps

1. Proceed to data-model.md generation
2. Generate OpenAPI contracts
3. Create quickstart.md for local development
4. Update agent context files

---

## References

- ML.NET Documentation: https://docs.microsoft.com/dotnet/machine-learning/
- Testcontainers .NET: https://dotnet.testcontainers.org/
- MALIEV Constitution: `.specify/memory/constitution.md`
- Feature Specification: `specs/001-enterprise-ml-prediction-service/spec.md`
