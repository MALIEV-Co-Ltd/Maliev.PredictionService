# Tasks: Enterprise ML Prediction Service

**Input**: Design documents from `/specs/001-enterprise-ml-prediction-service/`
**Prerequisites**: plan.md, spec.md, research.md

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create .NET solution file Maliev.PredictionService.sln at repository root (verify flat structure: no /src or /tests folders per Constitution XV)
- [X] T002 [P] Create Maliev.PredictionService.Api project (ASP.NET Core Web API)
- [X] T003 [P] Create Maliev.PredictionService.Application project (class library)
- [X] T004 [P] Create Maliev.PredictionService.Domain project (class library)
- [X] T005 [P] Create Maliev.PredictionService.Infrastructure project (class library)
- [X] T006 [P] Create Maliev.PredictionService.UnitTests project (xUnit test library)
- [X] T007 [P] Create Maliev.PredictionService.IntegrationTests project (xUnit test library)
- [X] T008 Add NuGet package references to Api project: Maliev.ServiceDefaults, MediatR, OpenTelemetry
- [X] T009 [P] Add NuGet package references to Application project: MediatR, MediatR.Contracts
- [X] T010 [P] Add NuGet package references to Domain project (no external dependencies except primitives)
- [X] T011 Add NuGet package references to Infrastructure project: EF Core 10.x, Npgsql, StackExchange.Redis, MassTransit.RabbitMQ, ML.NET 4.0.1 (pin exact version)
- [X] T012 [P] Add NuGet package references to UnitTests project: xUnit, Moq, Bogus
- [X] T013 [P] Add NuGet package references to IntegrationTests project: xUnit, Testcontainers.PostgreSql, Testcontainers.Redis, Testcontainers.RabbitMq
- [X] T014 Create nuget.config at repository root with GitHub Packages source
- [X] T015 [P] Create Dockerfile in Maliev.PredictionService.Api/ (NOT at repository root per Constitution X) with multi-stage build
- [X] T016 [P] Create .dockerignore at repository root
- [X] T017 [P] Create .github/CODEOWNERS with @MALIEV-Co-Ltd/core-developers
- [X] T018 [P] Create .github/workflows/ci-develop.yml for build, test, Docker image creation (develop branch)
- [X] T018A [P] Create .github/workflows/ci-staging.yml for build, test, Docker image creation (staging branch)
- [X] T018B [P] Create .github/workflows/ci-main.yml for build, test, Docker image creation (main branch)
- [X] T020 [P] Create README.md at repository root with architecture overview (Clean Architecture diagram), prerequisites (.NET 10, Docker), local development setup (clone, restore, run), testing instructions (unit + integration with Testcontainers)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**NOTE**: No docker-compose.yml per Constitution XVI - use Testcontainers for all integration testing

### Database & Persistence Foundation

- [X] T021 Create PredictionDbContext.cs in Maliev.PredictionService.Infrastructure/Persistence/ with ml_models, training, predictions, audit schemas
- [X] T022 [P] Configure EF Core conventions and JSONB value converters in PredictionDbContext.cs
- [X] T023 Create initial EF Core migration for multi-schema database in Maliev.PredictionService.Infrastructure/Persistence/Migrations/
- [X] T024 [P] Create PostgreSqlFixture.cs in Maliev.PredictionService.IntegrationTests/TestFixtures/ using Testcontainers
- [X] T025 [P] Create RedisFixture.cs in Maliev.PredictionService.IntegrationTests/TestFixtures/ using Testcontainers
- [X] T026 [P] Create RabbitMqFixture.cs in Maliev.PredictionService.IntegrationTests/TestFixtures/ using Testcontainers

### Domain Foundation (Entities, Enums, Interfaces)

- [X] T027 [P] Create ModelType.cs enum in Maliev.PredictionService.Domain/Enums/ (PrintTime, DemandForecast, PriceOptimization, ChurnPrediction, MaterialDemand, BottleneckDetection)
- [X] T028 [P] Create ModelStatus.cs enum in Maliev.PredictionService.Domain/Enums/ (Draft, Testing, Active, Deprecated, Archived)
- [X] T029 [P] Create PredictionStatus.cs enum in Maliev.PredictionService.Domain/Enums/ (Success, Failure, CachedHit)
- [X] T030 [P] Create PerformanceMetrics.cs value object in Maliev.PredictionService.Domain/ValueObjects/ with R², MAE, RMSE, Precision, Recall
- [X] T031 [P] Create ConfidenceInterval.cs value object in Maliev.PredictionService.Domain/ValueObjects/ with Lower, Upper, ConfidenceLevel
- [X] T032 [P] Create FeatureContribution.cs value object in Maliev.PredictionService.Domain/ValueObjects/ with FeatureName, ImpactWeight, TrendDirection
- [X] T033 [P] Create ModelVersion.cs value object in Maliev.PredictionService.Domain/ValueObjects/ with Major, Minor, Patch versioning
- [X] T034 Create MLModel.cs entity in Maliev.PredictionService.Domain/Entities/ with ModelType, ModelVersion, Status, PerformanceMetrics, TrainingDate, DeploymentDate, FilePath
- [X] T035 [P] Create TrainingDataset.cs entity in Maliev.PredictionService.Domain/Entities/ with RecordCount, DateRange, FeatureColumns, TargetColumn, DataQualityMetrics
- [ ] T036 [P] Create PredictionAuditLog.cs entity in Maliev.PredictionService.Domain/Entities/ with RequestId, InputFeatures, OutputPrediction, ModelVersion, CacheStatus, ResponseTime
- [ ] T037 [P] Create TrainingJob.cs entity in Maliev.PredictionService.Domain/Entities/ with Status, StartTime, EndTime, PerformanceMetrics, ValidationResults
- [ ] T038 [P] Create IModelRepository.cs interface in Maliev.PredictionService.Domain/Interfaces/ with GetActiveModel, SaveModel, GetModelHistory methods
- [ ] T039 [P] Create IPredictor.cs generic interface in Maliev.PredictionService.Domain/Interfaces/ with PredictAsync<TInput, TOutput> method
- [ ] T040 [P] Create IModelTrainer.cs interface in Maliev.PredictionService.Domain/Interfaces/ with TrainModelAsync, ValidateModelAsync methods
- [ ] T041 [P] Create IExplainer.cs interface in Maliev.PredictionService.Domain/Interfaces/ with ExplainPrediction method returning feature contributions

### Domain Services

- [ ] T042 Create ModelLifecycleManager.cs domain service in Maliev.PredictionService.Domain/Services/ implementing state transitions (Draft→Testing→Active→Deprecated→Archived)
- [ ] T043 Add quality gate validation logic to ModelLifecycleManager.cs (minimum dataset size, accuracy improvement >2%, holdout validation)
- [ ] T044 Add rollback capability to ModelLifecycleManager.cs with last 5 version preservation

### EF Core Entity Configurations

- [ ] T045 [P] Create MLModelConfiguration.cs in Maliev.PredictionService.Infrastructure/Persistence/Configurations/ with ml_models schema mapping, JSONB for PerformanceMetrics
- [ ] T046 [P] Create TrainingDatasetConfiguration.cs in Maliev.PredictionService.Infrastructure/Persistence/Configurations/ with training schema mapping
- [ ] T047 [P] Create PredictionAuditLogConfiguration.cs in Maliev.PredictionService.Infrastructure/Persistence/Configurations/ with audit schema mapping, monthly partitioning, JSONB for InputFeatures/OutputPrediction
- [ ] T048 [P] Create TrainingJobConfiguration.cs in Maliev.PredictionService.Infrastructure/Persistence/Configurations/ with training schema mapping

### Repositories

- [ ] T049 [P] Create ModelRepository.cs in Maliev.PredictionService.Infrastructure/Persistence/Repositories/ implementing IModelRepository with GetActiveModel, SaveModel, GetModelHistory
- [ ] T050 [P] Create TrainingDatasetRepository.cs in Maliev.PredictionService.Infrastructure/Persistence/Repositories/ with CRUD operations
- [ ] T051 [P] Create PredictionAuditRepository.cs in Maliev.PredictionService.Infrastructure/Persistence/Repositories/ with append-only logging

### Caching Infrastructure

- [ ] T052 Create CacheKeyGenerator.cs in Maliev.PredictionService.Infrastructure/Caching/ with SHA-256 hashing of sorted input parameters
- [ ] T053 Create RedisCacheService.cs in Maliev.PredictionService.Infrastructure/Caching/ implementing IDistributedCache with Get, Set, Remove, and TTL support per model type
- [ ] T054 Add cache key format implementation to CacheKeyGenerator.cs: {modelType}:{sha256Hash}:{modelVersion}

### CQRS & Application Infrastructure

- [ ] T055 Create ServiceCollectionExtensions.cs in Maliev.PredictionService.Api/Extensions/ with AddPredictionService() method
- [ ] T056 Add MediatR registration to ServiceCollectionExtensions.cs scanning Application assembly
- [ ] T057 [P] Add EF Core DbContext registration to ServiceCollectionExtensions.cs with PostgreSQL connection string
- [ ] T058 [P] Add Redis cache registration to ServiceCollectionExtensions.cs
- [ ] T059 [P] Add MassTransit with RabbitMQ registration to ServiceCollectionExtensions.cs
- [ ] T060 [P] Add repository registrations to ServiceCollectionExtensions.cs (ModelRepository, TrainingDatasetRepository, PredictionAuditRepository)
- [X] T061 Create CacheService.cs application service in Maliev.PredictionService.Application/Services/ wrapping RedisCacheService with domain-specific cache logic

### API Foundation

- [X] T062 Create Program.cs in Maliev.PredictionService.Api/ with WebApplicationBuilder, ServiceDefaults integration, Maliev.PredictionService registration
- [X] T063 [P] Create ErrorHandlingMiddleware.cs in Maliev.PredictionService.Api/Middleware/ with structured exception handling
- [X] T064 [P] Create RequestLoggingMiddleware.cs in Maliev.PredictionService.Api/Middleware/ with OpenTelemetry tracing and correlation IDs
- [X] T065 Create HealthController.cs in Maliev.PredictionService.Api/Controllers/ with /predictionservice/liveness, /readiness, /health endpoints
- [X] T066 [P] Create appsettings.json in Maliev.PredictionService.Api/ with PostgreSQL, Redis, RabbitMQ connection strings AND mandatory LogLevel configuration per Constitution V (Default: Information, Microsoft.AspNetCore: Warning, Microsoft.EntityFrameworkCore: Warning, Microsoft.AspNetCore.Watch.BrowserRefresh: None, Microsoft.Hosting.Lifetime: Information, Microsoft.AspNetCore.Watch: Warning, System: Warning)
- [X] T067 [P] Create appsettings.Development.json in Maliev.PredictionService.Api/ with local development overrides

### Authentication & Authorization

- [X] T068 Add OAuth 2.0 / OpenID Connect authentication middleware to Program.cs via ServiceDefaults
- [X] T069 [P] Add RBAC authorization policies to Program.cs (PredictionUser: can request predictions, PredictionAdmin: can trigger training and view model health, DataScientist: full access including model registry)
- [X] T069A [P] Implement API versioning strategy (URL-based /v1/ or header-based) with backward compatibility validation per FR-051

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - 3D Print Time Predictions (Priority: P0) 🎯 MVP

**Goal**: Enable sales engineers to upload 3D geometry files and receive accurate manufacturing time estimates within seconds

**Independent Test**: Upload STL file via POST /predictions/v1/print-time, verify response includes predicted time, confidence interval, and explanation within 2 seconds

### Implementation for User Story 1

#### Feature Engineering & ML Infrastructure

- [X] T070 [P] [US1] Create GeometryFeatureExtractor.cs in Maliev.PredictionService.Infrastructure/ML/FeatureEngineering/ with STL binary format parsing
- [X] T071 [US1] Implement volume calculation in GeometryFeatureExtractor.cs using System.Numerics
- [X] T072 [US1] Implement surface area calculation in GeometryFeatureExtractor.cs
- [X] T073 [US1] Implement layer count, support structure percentage, complexity score, and bounding box extraction in GeometryFeatureExtractor.cs
- [X] T074 [P] [US1] Create PrintTimeTrainer.cs in Maliev.PredictionService.Infrastructure/ML/Trainers/ with ML.NET FastTreeRegression configuration
- [X] T075 [US1] Implement data loading from TrainingDataset to IDataView in PrintTimeTrainer.cs
- [X] T076 [US1] Implement model training pipeline in PrintTimeTrainer.cs (feature selection, normalization, FastTree regression)
- [X] T077 [US1] Implement model evaluation (R², MAE, RMSE) in PrintTimeTrainer.cs with holdout test set
- [X] T078 [US1] Implement model persistence to file system in PrintTimeTrainer.cs with version naming
- [X] T079 [P] [US1] Create PrintTimePredictor.cs in Maliev.PredictionService.Infrastructure/ML/Predictors/ implementing IPredictor interface
- [X] T080 [US1] Implement model loading from file system in PrintTimePredictor.cs with ModelLoader service
- [X] T081 [US1] Implement prediction logic in PrintTimePredictor.cs with confidence interval calculation
- [X] T082 [US1] Add input validation in PrintTimePredictor.cs (file size <50MB, supported formats)

#### Application Layer (CQRS)

- [X] T083 [P] [US1] Create PrintTimePredictionRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with GeometryFile, MaterialType, PrinterType, LayerHeight
- [X] T084 [P] [US1] Create PredictionResponse.cs DTO in Maliev.PredictionService.Application/DTOs/Responses/ with PredictedValue, ConfidenceInterval, Explanation, ModelVersion, CacheStatus
- [X] T085 [P] [US1] Create PrintTimeRequestValidator.cs in Maliev.PredictionService.Application/Validators/ with manual validation (file size, format, required fields)
- [X] T086 Create PredictPrintTimeCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with PrintTimePredictionRequest payload
- [X] T087 Create PredictPrintTimeCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler<PredictPrintTimeCommand, PredictionResponse>
- [X] T088 Implement cache check in PredictPrintTimeCommandHandler.cs using CacheService with SHA-256 key generation
- [X] T089 Implement feature extraction call in PredictPrintTimeCommandHandler.cs using GeometryFeatureExtractor
- [X] T090 Implement prediction call in PredictPrintTimeCommandHandler.cs using PrintTimePredictor
- [X] T091 Implement cache storage in PredictPrintTimeCommandHandler.cs with 24-hour TTL for print-time predictions
- [X] T092 Implement audit logging in PredictPrintTimeCommandHandler.cs using PredictionAuditRepository
- [X] T093 [P] [US1] Create PredictionOrchestrator.cs in Maliev.PredictionService.Application/Services/ with model selection, caching coordination, error handling

#### API Layer

- [X] T094 Create PredictionsController.cs in Maliev.PredictionService.Api/Controllers/ with [ApiController] and [Authorize] attributes
- [X] T095 [US1] Add POST /predictions/v1/print-time endpoint to PredictionsController.cs accepting multipart/form-data with geometry file
- [X] T096 [US1] Implement endpoint logic to dispatch PredictPrintTimeCommand via MediatR
- [X] T097 [US1] Add OpenAPI annotations to print-time endpoint with request/response examples
- [X] T098 [US1] Add rate limiting to print-time endpoint (1000 requests/min sustained, 2500 burst)

#### Integration Testing

- [ ] T099 [US1] Create PrintTimePredictorTests.cs in Maliev.PredictionService.IntegrationTests/ML/ with sample STL files
- [X] T100 [US1] Add test for GeometryFeatureExtractor.cs with known geometry file (verify volume, surface area calculations)
- [X] T101 [US1] Add test for PrintTimeTrainer.cs training workflow with mock dataset (verify model trains without errors)
- [X] T102 [US1] Add test for PrintTimePredictor.cs inference with trained model (verify prediction data types, confidence scores)
- [X] T103 [US1] Create PredictionsControllerTests.cs in Maliev.PredictionService.IntegrationTests/Api/ using WebApplicationFactory
- [X] T104 [US1] Add end-to-end test for POST /predictions/v1/print-time with real geometry file using Testcontainers PostgreSQL and Redis
- [X] T105 [US1] Add test for cache hit scenario (make identical request twice, verify second is faster and returns CachedHit status)
- [X] T106 [US1] Add test for cache invalidation on model version update (verify old cache keys no longer work)

**Checkpoint**: At this point, User Story 1 should be fully functional - sales engineers can upload 3D files and get print time predictions

---

## Phase 4: User Story 2 - Sales Demand Forecasting (Priority: P0)

**Goal**: Enable operations managers to forecast future sales demand with confidence bands to optimize inventory planning

**Independent Test**: POST /predictions/v1/demand-forecast with ProductId and Horizon (7/30/90 days), verify response includes daily/weekly forecast with 80%/95% confidence bands

### Implementation for User Story 2

#### Feature Engineering & ML Infrastructure

- [X] T107 [P] [US2] Create TimeSeriesTransformer.cs in Maliev.PredictionService.Infrastructure/ML/FeatureEngineering/ with calendar features (day of week, month, quarter, holidays)
- [X] T108 [US2] Implement lag features in TimeSeriesTransformer.cs (previous 7, 14, 30 days demand)
- [X] T109 [US2] Implement rolling statistics in TimeSeriesTransformer.cs (7-day, 30-day moving averages)
- [X] T110 [US2] Add promotion flags to TimeSeriesTransformer.cs (binary indicators for promotional periods)
- [X] T111 [P] [US2] Create DemandForecastTrainer.cs in Maliev.PredictionService.Infrastructure/ML/Trainers/ with ML.NET SSA (Singular Spectrum Analysis) configuration
- [X] T112 [US2] Implement time-series data loading in DemandForecastTrainer.cs from historical sales events
- [X] T113 [US2] Implement SSA model training pipeline in DemandForecastTrainer.cs with trend/seasonality decomposition
- [X] T114 [US2] Implement forecast evaluation (MAPE, RMSE) in DemandForecastTrainer.cs with 20% holdout
- [X] T115 [US2] Implement confidence band calculation in DemandForecastTrainer.cs (80%, 95% confidence levels)
- [X] T116 [P] [US2] Create DemandForecaster.cs in Maliev.PredictionService.Infrastructure/ML/Predictors/ implementing IPredictor interface
- [X] T117 [US2] Implement horizon-specific forecasting in DemandForecaster.cs (7-day, 30-day, 90-day with daily or weekly granularity)
- [X] T118 [US2] Implement anomaly detection in DemandForecaster.cs (>40% deviation from expected)

#### Application Layer (CQRS)

- [X] T119 [P] [US2] Create DemandForecastRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with ProductId, Horizon, Granularity
- [X] T120 [P] [US2] Create DemandForecastRequestValidator.cs in Maliev.PredictionService.Application/Validators/ with horizon validation (7, 30, or 90 days)
- [X] T121 Create PredictDemandCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with DemandForecastRequest payload
- [X] T122 Create PredictDemandCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler
- [X] T123 Implement cache check in PredictDemandCommandHandler.cs with 6-hour TTL for demand forecasts
- [X] T124 Implement time-series transformation in PredictDemandCommandHandler.cs using TimeSeriesTransformer
- [X] T125 Implement forecast prediction in PredictDemandCommandHandler.cs using DemandForecaster
- [X] T126 Implement anomaly detection alerts in PredictDemandCommandHandler.cs (>40% deviation)
- [X] T127 Implement cache storage and audit logging in PredictDemandCommandHandler.cs

#### API Layer

- [X] T128 [US2] Add POST /predictions/v1/demand-forecast endpoint to PredictionsController.cs
- [X] T129 [US2] Implement endpoint logic to dispatch PredictDemandCommand via MediatR
- [X] T130 [US2] Add OpenAPI annotations to demand-forecast endpoint with forecast horizon examples
- [X] T131 [US2] Add rate limiting to demand-forecast endpoint (1000 requests/min sustained)

#### Event-Driven Integration

- [X] T132 [P] [US2] Create OrderCreatedConsumer.cs in Maliev.PredictionService.Infrastructure/Events/Consumers/ implementing IConsumer<OrderCreated>
- [X] T133 [US2] Implement event handling logic in OrderCreatedConsumer.cs to ingest order data into training dataset
- [X] T134 [US2] Add data validation in OrderCreatedConsumer.cs (schema validation, null checks)
- [X] T135 [US2] Add deduplication logic in OrderCreatedConsumer.cs (idempotency check)
- [X] T136 [US2] Implement trigger for model retraining when minimum dataset size reached in OrderCreatedConsumer.cs

#### Integration Testing

- [ ] T137 [US2] Create DemandForecasterTests.cs in Maliev.PredictionService.IntegrationTests/ML/ with historical sales data
- [ ] T138 [US2] Add test for TimeSeriesTransformer.cs feature extraction (verify calendar features, lag features, rolling stats)
- [ ] T139 [US2] Add test for DemandForecastTrainer.cs with synthetic time-series data (verify model trains, MAPE <15%)
- [ ] T140 [US2] Add test for DemandForecaster.cs predictions (verify forecast returns correct horizon, confidence bands)
- [ ] T141 [US2] Add end-to-end test for POST /predictions/v1/demand-forecast using Testcontainers
- [ ] T142 [US2] Add test for OrderCreatedConsumer.cs event ingestion using Testcontainers RabbitMQ
- [ ] T143 [US2] Add test for anomaly detection (inject 50% spike in demand, verify alert triggered)

**Checkpoint**: At this point, User Story 2 should be fully functional - operations managers can forecast demand with confidence bands

---

## Phase 5: User Story 3 - Dynamic Price Optimization (Priority: P0)

**Goal**: Enable sales engineers to receive data-driven pricing recommendations that optimize margins while maintaining competitive win rates

**Independent Test**: POST /predictions/v1/price-recommendation with QuoteId, verify response includes optimal price, win probability, price range, and reasoning

### Implementation for User Story 3

#### ML Infrastructure

- [ ] T144 [P] [US3] Create PriceOptimizationTrainer.cs in Maliev.PredictionService.Infrastructure/ML/Trainers/ with ML.NET FastTreeRegression for base price prediction
- [ ] T145 [US3] Implement feature engineering in PriceOptimizationTrainer.cs (material costs, complexity, customer history, market conditions)
- [ ] T146 [US3] Implement training pipeline in PriceOptimizationTrainer.cs with regression for base price
- [ ] T147 [US3] Add elasticity calculation logic to PriceOptimizationTrainer.cs (price sensitivity by customer segment)
- [ ] T148 [P] [US3] Create PriceOptimizer.cs in Maliev.PredictionService.Infrastructure/ML/Predictors/ implementing IPredictor interface
- [ ] T149 [US3] Implement base price prediction in PriceOptimizer.cs using trained regression model
- [ ] T150 [US3] Implement win probability calculation in PriceOptimizer.cs based on historical quote outcomes and elasticity
- [ ] T151 [US3] Implement price range recommendation in PriceOptimizer.cs (min/optimal/max with win probabilities)

#### Application Layer (CQRS)

- [ ] T152 [P] [US3] Create PriceRecommendationRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with QuoteId, MaterialCost, Complexity, CustomerId, CompetitorPrices
- [ ] T153 [P] [US3] Create PriceRecommendationRequestValidator.cs in Maliev.PredictionService.Application/Validators/
- [ ] T154 Create PredictPriceCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with PriceRecommendationRequest payload
- [ ] T155 Create PredictPriceCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler
- [ ] T156 Implement cache check in PredictPriceCommandHandler.cs with 1-hour TTL for price recommendations
- [ ] T157 Implement price prediction in PredictPriceCommandHandler.cs using PriceOptimizer
- [ ] T158 Implement win probability calculation in PredictPriceCommandHandler.cs at different price points
- [ ] T159 Implement cache storage and audit logging in PredictPriceCommandHandler.cs

#### API Layer

- [ ] T160 [US3] Add POST /predictions/v1/price-recommendation endpoint to PredictionsController.cs
- [ ] T161 [US3] Implement endpoint logic to dispatch PredictPriceCommand via MediatR
- [ ] T162 [US3] Add OpenAPI annotations to price-recommendation endpoint with win probability examples
- [ ] T163 [US3] Add rate limiting to price-recommendation endpoint (1000 requests/min sustained)

#### Integration Testing

- [ ] T164 [US3] Create PriceOptimizerTests.cs in Maliev.PredictionService.IntegrationTests/ML/
- [ ] T165 [US3] Add test for PriceOptimizationTrainer.cs with historical quote data (verify model trains, calculates elasticity)
- [ ] T166 [US3] Add test for PriceOptimizer.cs predictions (verify price range, win probabilities)
- [ ] T167 [US3] Add end-to-end test for POST /predictions/v1/price-recommendation using Testcontainers
- [ ] T168 [US3] Add test for cache behavior with 1-hour TTL (verify cache hit, verify expiration)

**Checkpoint**: At this point, User Story 3 should be fully functional - sales engineers get pricing recommendations with win probabilities

---

## Phase 6: User Story 7 - Automated Model Training (Priority: P0)

**Goal**: Enable automatic model retraining on schedules or performance degradation triggers without manual intervention

**Independent Test**: Trigger model training via POST /predictions/v1/models/{modelType}/train, verify training job completes, evaluates quality gates, and auto-deploys if metrics improve >2%

### Implementation for User Story 7

#### Application Layer (CQRS)

- [ ] T169 Create TrainModelCommand.cs in Maliev.PredictionService.Application/Commands/Models/ with ModelType, Force flag
- [ ] T170 Create TrainModelCommandHandler.cs in Maliev.PredictionService.Application/Commands/Models/ implementing IRequestHandler
- [ ] T171 Implement data quality validation in TrainModelCommandHandler.cs (null checks, outlier detection, schema validation)
- [ ] T172 Implement minimum dataset size check in TrainModelCommandHandler.cs (model-specific thresholds: 10K for print-time, 5K for price, 2K for churn)
- [ ] T173 Implement trainer dispatch in TrainModelCommandHandler.cs (route to correct trainer based on ModelType)
- [ ] T174 Implement holdout validation in TrainModelCommandHandler.cs (20% test set evaluation)
- [ ] T175 Implement quality gate evaluation in TrainModelCommandHandler.cs (accuracy improvement >2%, no critical warnings)
- [ ] T176 Implement ModelLifecycleManager integration in TrainModelCommandHandler.cs (Draft→Testing→Active promotion)
- [ ] T177 Implement cache invalidation in TrainModelCommandHandler.cs (clear all keys matching {modelType}:*:{oldVersion})
- [ ] T178 Implement notification to data science team in TrainModelCommandHandler.cs (success/failure alerts)
- [ ] T179 Create DeployModelCommand.cs in Maliev.PredictionService.Application/Commands/Models/ with ModelId, CanaryPercentage
- [ ] T180 Create DeployModelCommandHandler.cs in Maliev.PredictionService.Application/Commands/Models/ implementing A/B testing framework (10% canary, 24 hours, then 100% if no degradation)

#### Background Services

- [ ] T181 Create ModelTrainingService.cs in Maliev.PredictionService.Infrastructure/BackgroundServices/ implementing IHostedService
- [ ] T182 Implement scheduled training logic in ModelTrainingService.cs (daily, weekly, or monthly per model type)
- [ ] T183 [P] Create ModelDriftDetectionService.cs in Maliev.PredictionService.Infrastructure/BackgroundServices/ implementing IHostedService
- [ ] T184 Implement drift detection in ModelDriftDetectionService.cs (monitor accuracy against actual outcomes in 24-hour rolling window)
- [ ] T185 Implement automatic rollback trigger in ModelDriftDetectionService.cs (if accuracy degrades >5%)
- [ ] T186 Implement automatic retraining trigger in ModelDriftDetectionService.cs (if drift detected, dispatch TrainModelCommand)

#### API Layer

- [ ] T187 Create ModelsController.cs in Maliev.PredictionService.Api/Controllers/ with [ApiController] and [Authorize(Roles = "PredictionAdmin,DataScientist")]
- [ ] T188 [US7] Add POST /predictions/v1/models/{modelType}/train endpoint to ModelsController.cs
- [ ] T189 [US7] Add POST /predictions/v1/models/{modelId}/deploy endpoint to ModelsController.cs
- [ ] T190 [US7] Add POST /predictions/v1/models/{modelId}/rollback endpoint to ModelsController.cs
- [ ] T191 [US7] Add GET /predictions/v1/models/{modelType}/health endpoint to ModelsController.cs returning ModelHealthResponse
- [ ] T192 [US7] Add GET /predictions/v1/models/{modelType}/versions endpoint to ModelsController.cs (list version history)

#### Queries

- [ ] T193 [P] [US7] Create ModelHealthResponse.cs DTO in Maliev.PredictionService.Application/DTOs/Responses/ with Version, Status, PerformanceMetrics, PredictionVolume
- [ ] T194 Create GetModelHealthQuery.cs in Maliev.PredictionService.Application/Queries/ with ModelType parameter
- [ ] T195 Create GetModelHealthQueryHandler.cs in Maliev.PredictionService.Application/Queries/ implementing IRequestHandler
- [ ] T196 Implement query logic in GetModelHealthQueryHandler.cs (fetch active model, calculate daily prediction volume, return metrics)

#### Integration Testing

- [ ] T197 [US7] Create ModelTrainingTests.cs in Maliev.PredictionService.IntegrationTests/ML/
- [ ] T198 [US7] Add test for TrainModelCommandHandler.cs end-to-end workflow (verify data validation, training, quality gates, promotion)
- [ ] T199 [US7] Add test for quality gate failure scenario (verify model stays in Testing status, alert sent)
- [ ] T200 [US7] Add test for quality gate pass scenario (verify auto-promotion to Active, cache invalidation)
- [ ] T201 [US7] Add test for ModelDriftDetectionService.cs (inject degraded accuracy, verify rollback triggered)
- [ ] T202 [US7] Add end-to-end test for POST /predictions/v1/models/{modelType}/train using Testcontainers

**Checkpoint**: At this point, User Story 7 should be fully functional - models retrain automatically and promote based on quality gates

---

## Phase 7: User Story 4 - Customer Churn Risk Prediction (Priority: P1)

**Goal**: Enable customer success teams to identify high-risk customers proactively with actionable intervention recommendations

**Independent Test**: POST /predictions/v1/churn-risk with CustomerId, verify response includes churn score (0-100), probability by window (30/60/90 days), top 5 risk factors, and intervention strategies

### Implementation for User Story 4

#### Feature Engineering & ML Infrastructure

- [ ] T203 [P] [US4] Create CustomerFeatureAggregator.cs in Maliev.PredictionService.Infrastructure/ML/FeatureEngineering/ with RFM features (Recency, Frequency, Monetary Value)
- [ ] T204 [US4] Implement augmented features in CustomerFeatureAggregator.cs (support ticket count, payment delay frequency, engagement score, customer lifetime)
- [ ] T205 [US4] Add 12-month rolling window calculation to CustomerFeatureAggregator.cs
- [ ] T206 [P] [US4] Create ChurnModelTrainer.cs in Maliev.PredictionService.Infrastructure/ML/Trainers/ with ML.NET FastTreeBinaryClassification configuration
- [ ] T207 [US4] Implement class imbalance handling in ChurnModelTrainer.cs (most customers don't churn)
- [ ] T208 [US4] Implement training pipeline in ChurnModelTrainer.cs with precision optimization for high-risk segment (score >70)
- [ ] T209 [US4] Implement feature importance extraction in ChurnModelTrainer.cs using PermutationFeatureImportance API
- [ ] T210 [P] [US4] Create ChurnPredictor.cs in Maliev.PredictionService.Infrastructure/ML/Predictors/ implementing IPredictor interface
- [ ] T211 [US4] Implement churn score calculation (0-100) in ChurnPredictor.cs from probability output
- [ ] T212 [US4] Implement multi-window prediction in ChurnPredictor.cs (30, 60, 90-day windows)
- [ ] T213 [US4] Implement risk factor identification in ChurnPredictor.cs (top 5 features with trend analysis)
- [ ] T214 [US4] Implement intervention strategy recommendations in ChurnPredictor.cs based on risk factors (order frequency → re-engagement campaign, support tickets → proactive outreach)

#### Application Layer (CQRS)

- [ ] T215 [P] [US4] Create ChurnRiskRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with CustomerId
- [ ] T216 Create PredictChurnCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with ChurnRiskRequest payload
- [ ] T217 Create PredictChurnCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler
- [ ] T218 Implement cache check in PredictChurnCommandHandler.cs with 24-hour TTL for churn predictions
- [ ] T219 Implement customer feature aggregation in PredictChurnCommandHandler.cs using CustomerFeatureAggregator
- [ ] T220 Implement churn prediction in PredictChurnCommandHandler.cs using ChurnPredictor
- [ ] T221 Implement cache storage and audit logging in PredictChurnCommandHandler.cs
- [ ] T222 Create GetChurnRiskQuery.cs in Maliev.PredictionService.Application/Queries/ with CustomerId parameter for historical churn risk tracking
- [ ] T223 Create GetChurnRiskQueryHandler.cs in Maliev.PredictionService.Application/Queries/ retrieving prediction history for customer

#### API Layer

- [ ] T224 [US4] Add POST /predictions/v1/churn-risk endpoint to PredictionsController.cs
- [ ] T225 [US4] Add GET /predictions/v1/churn-risk/{customerId}/history endpoint to PredictionsController.cs
- [ ] T226 [US4] Implement endpoint logic to dispatch PredictChurnCommand via MediatR
- [ ] T227 [US4] Add OpenAPI annotations to churn-risk endpoint with intervention strategy examples

#### Event-Driven Integration

- [ ] T228 [P] [US4] Create CustomerUpdatedConsumer.cs in Maliev.PredictionService.Infrastructure/Events/Consumers/ implementing IConsumer<CustomerUpdated>
- [ ] T229 [US4] Implement event handling to update customer features in CustomerUpdatedConsumer.cs
- [ ] T230 [US4] Add trigger for daily churn score recalculation for active customers in CustomerUpdatedConsumer.cs

#### Integration Testing

- [ ] T231 [US4] Create ChurnPredictorTests.cs in Maliev.PredictionService.IntegrationTests/ML/
- [ ] T232 [US4] Add test for CustomerFeatureAggregator.cs (verify RFM calculations, augmented features)
- [ ] T233 [US4] Add test for ChurnModelTrainer.cs with imbalanced dataset (verify >75% precision for high-risk segment)
- [ ] T234 [US4] Add test for ChurnPredictor.cs (verify churn score, multi-window predictions, risk factors)
- [ ] T235 [US4] Add end-to-end test for POST /predictions/v1/churn-risk using Testcontainers
- [ ] T236 [US4] Add test for CustomerUpdatedConsumer.cs event ingestion

**Checkpoint**: At this point, User Story 4 should be fully functional - customer success teams can identify high-risk customers with intervention strategies

---

## Phase 8: User Story 5 - Material Demand Forecasting (Priority: P1)

**Goal**: Enable procurement teams to forecast material consumption and receive reorder alerts based on lead times and minimum order quantities

**Independent Test**: POST /predictions/v1/material-demand with MaterialSKU and Horizon, verify response includes consumption forecast, alerts when predicted demand exceeds current inventory within lead time, and reorder recommendations

### Implementation for User Story 5

#### Application Layer (CQRS)

- [ ] T237 [P] [US5] Create MaterialDemandRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with MaterialSKU, Horizon
- [ ] T238 Create PredictMaterialDemandCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with MaterialDemandRequest payload
- [ ] T239 Create PredictMaterialDemandCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler
- [ ] T240 Implement cache check in PredictMaterialDemandCommandHandler.cs with 12-hour TTL for material demand forecasts
- [ ] T241 Implement material-level demand forecasting in PredictMaterialDemandCommandHandler.cs (reuse DemandForecaster with material granularity)
- [ ] T242 Implement inventory level check in PredictMaterialDemandCommandHandler.cs (compare forecast to current stock)
- [ ] T243 Implement lead time window calculation in PredictMaterialDemandCommandHandler.cs (alert if demand exceeds stock within lead time)
- [ ] T244 Implement reorder quantity recommendation in PredictMaterialDemandCommandHandler.cs (account for supplier lead times, minimum order quantities)
- [ ] T245 Implement cache storage and audit logging in PredictMaterialDemandCommandHandler.cs

#### API Layer

- [ ] T246 [US5] Add POST /predictions/v1/material-demand endpoint to PredictionsController.cs
- [ ] T247 [US5] Implement endpoint logic to dispatch PredictMaterialDemandCommand via MediatR
- [ ] T248 [US5] Add OpenAPI annotations to material-demand endpoint with reorder recommendation examples

#### Event-Driven Integration

- [ ] T249 [P] [US5] Create MaterialTransactionConsumer.cs in Maliev.PredictionService.Infrastructure/Events/Consumers/ implementing IConsumer<MaterialTransaction>
- [ ] T250 [US5] Implement event handling to ingest material consumption data into training dataset in MaterialTransactionConsumer.cs
- [ ] T251 [US5] Add trigger for material demand model retraining when minimum dataset size reached in MaterialTransactionConsumer.cs

#### Integration Testing

- [ ] T252 [US5] Add test for material-level demand forecasting (verify DemandForecaster works with material SKU granularity)
- [ ] T253 [US5] Add end-to-end test for POST /predictions/v1/material-demand using Testcontainers
- [ ] T254 [US5] Add test for reorder alert scenario (inject forecast exceeding inventory, verify alert triggered)
- [ ] T255 [US5] Add test for MaterialTransactionConsumer.cs event ingestion

**Checkpoint**: At this point, User Story 5 should be fully functional - procurement teams can forecast material demand with reorder alerts

---

## Phase 9: User Story 6 - Production Bottleneck Detection (Priority: P1)

**Goal**: Enable production managers to predict bottlenecks 5-7 days in advance with resource reallocation recommendations

**Independent Test**: POST /predictions/v1/bottleneck-prediction with FacilityId and DateRange, verify response includes predicted bottlenecks, equipment/workstation constraints, queue wait times, and reallocation strategies

### Implementation for User Story 6

#### ML Infrastructure

- [ ] T256 [P] [US6] Create BottleneckTrainer.cs in Maliev.PredictionService.Infrastructure/ML/Trainers/ with ML.NET FastTreeRegression for queue wait time prediction
- [ ] T257 [US6] Implement feature engineering in BottleneckTrainer.cs (current queue depth, utilization rate, order complexity, historical patterns)
- [ ] T258 [US6] Implement training pipeline in BottleneckTrainer.cs with regression for wait time by equipment/workstation
- [ ] T259 [P] [US6] Create BottleneckPredictor.cs in Maliev.PredictionService.Infrastructure/ML/Predictors/ implementing IPredictor interface
- [ ] T260 [US6] Implement 5-7 day ahead prediction in BottleneckPredictor.cs (daily predictions for next week)
- [ ] T261 [US6] Implement capacity constraint identification in BottleneckPredictor.cs (equipment where predicted wait time exceeds threshold)
- [ ] T262 [US6] Implement severity level calculation in BottleneckPredictor.cs (low/medium/high based on wait time and queue depth)
- [ ] T263 [US6] Implement resource reallocation recommendations in BottleneckPredictor.cs (shift staffing, reschedule jobs, add overtime)

#### Application Layer (CQRS)

- [ ] T264 [P] [US6] Create BottleneckPredictionRequest.cs DTO in Maliev.PredictionService.Application/DTOs/Requests/ with FacilityId, DateRange
- [ ] T265 Create PredictBottleneckCommand.cs in Maliev.PredictionService.Application/Commands/Predictions/ with BottleneckPredictionRequest payload
- [ ] T266 Create PredictBottleneckCommandHandler.cs in Maliev.PredictionService.Application/Commands/Predictions/ implementing IRequestHandler
- [ ] T267 Implement cache check in PredictBottleneckCommandHandler.cs with 6-hour TTL
- [ ] T268 Implement bottleneck prediction in PredictBottleneckCommandHandler.cs using BottleneckPredictor
- [ ] T269 Implement cache storage and audit logging in PredictBottleneckCommandHandler.cs

#### API Layer

- [ ] T270 [US6] Add POST /predictions/v1/bottleneck-prediction endpoint to PredictionsController.cs
- [ ] T271 [US6] Implement endpoint logic to dispatch PredictBottleneckCommand via MediatR
- [ ] T272 [US6] Add OpenAPI annotations to bottleneck-prediction endpoint with reallocation strategy examples

#### Event-Driven Integration

- [ ] T273 [P] [US6] Create ManufacturingJobCompletedConsumer.cs in Maliev.PredictionService.Infrastructure/Events/Consumers/ implementing IConsumer<ManufacturingJobCompleted>
- [ ] T274 [US6] Implement event handling to ingest job completion times and queue metrics in ManufacturingJobCompletedConsumer.cs
- [ ] T275 [US6] Add trigger for bottleneck model retraining when minimum dataset size reached in ManufacturingJobCompletedConsumer.cs

#### Integration Testing

- [ ] T276 [US6] Create BottleneckPredictorTests.cs in Maliev.PredictionService.IntegrationTests/ML/
- [ ] T277 [US6] Add test for BottleneckTrainer.cs with historical queue data
- [ ] T278 [US6] Add test for BottleneckPredictor.cs 5-7 day ahead predictions (verify accuracy >80%)
- [ ] T279 [US6] Add end-to-end test for POST /predictions/v1/bottleneck-prediction using Testcontainers
- [ ] T280 [US6] Add test for ManufacturingJobCompletedConsumer.cs event ingestion

**Checkpoint**: At this point, User Story 6 should be fully functional - production managers can predict bottlenecks with reallocation strategies

---

## Phase 10: User Story 8 - Explainable Predictions (Priority: P1)

**Goal**: Enable all prediction users to understand why a prediction was made through feature importance and human-readable explanations

**Independent Test**: For any prediction response, verify it includes top 3-5 contributing features with impact weights (0.0-1.0), sorted by importance, plus human-readable explanation (e.g., "Price is high because complexity is in top 10% and material cost is above average")

### Implementation for User Story 8

#### Explainability Infrastructure

- [ ] T281 [P] [US8] Create SHAPExplainer.cs in Maliev.PredictionService.Infrastructure/ML/Explainers/ implementing IExplainer interface
- [ ] T282 [US8] Implement feature importance extraction in SHAPExplainer.cs using ML.NET PermutationFeatureImportance API
- [ ] T283 [US8] Implement top N feature selection in SHAPExplainer.cs (top 3-5 by impact weight)
- [ ] T284 [US8] Implement impact weight normalization in SHAPExplainer.cs (0.0-1.0 scale)
- [ ] T285 Create ExplainabilityService.cs in Maliev.PredictionService.Application/Services/ with template-based explanation generation
- [ ] T286 Implement contextual threshold calculation in ExplainabilityService.cs (compare feature values to population percentiles)
- [ ] T287 Implement human-readable explanation templates in ExplainabilityService.cs (e.g., "Price is high because {feature1} is in top 10% and {feature2} is above average")
- [ ] T288 [P] [US8] Create ExplanationData.cs DTO in Maliev.PredictionService.Application/DTOs/Responses/ with FeatureContributions, HumanReadableExplanation

#### Integration into Existing Prediction Handlers

- [ ] T289 [US8] Integrate SHAPExplainer into PredictPrintTimeCommandHandler.cs (add feature importance to prediction response)
- [ ] T290 [US8] Integrate ExplainabilityService into PredictPrintTimeCommandHandler.cs (add human-readable explanation)
- [ ] T291 [US8] Integrate SHAPExplainer into PredictDemandCommandHandler.cs
- [ ] T292 [US8] Integrate ExplainabilityService into PredictDemandCommandHandler.cs
- [ ] T293 [US8] Integrate SHAPExplainer into PredictPriceCommandHandler.cs
- [ ] T294 [US8] Integrate ExplainabilityService into PredictPriceCommandHandler.cs
- [ ] T295 [US8] Integrate SHAPExplainer into PredictChurnCommandHandler.cs
- [ ] T296 [US8] Integrate ExplainabilityService into PredictChurnCommandHandler.cs
- [ ] T297 [US8] Integrate SHAPExplainer into PredictMaterialDemandCommandHandler.cs
- [ ] T298 [US8] Integrate ExplainabilityService into PredictMaterialDemandCommandHandler.cs
- [ ] T299 [US8] Integrate SHAPExplainer into PredictBottleneckCommandHandler.cs
- [ ] T300 [US8] Integrate ExplainabilityService into PredictBottleneckCommandHandler.cs

#### Unit Testing

- [ ] T301 [P] [US8] Create ExplainabilityServiceTests.cs in Maliev.PredictionService.UnitTests/Application/Services/
- [ ] T302 [US8] Add test for feature importance extraction (verify top 3-5 features, sorted by impact weight)
- [ ] T303 [US8] Add test for human-readable explanation generation (verify template substitution, percentile comparisons)

#### Integration Testing

- [ ] T304 [US8] Update all prediction endpoint tests to verify ExplanationData is included in responses
- [ ] T305 [US8] Add test for print-time prediction explanation (verify geometry features appear in top contributors)
- [ ] T306 [US8] Add test for churn prediction explanation (verify RFM features and risk factors appear)

**Checkpoint**: At this point, User Story 8 should be fully functional - all predictions include feature importance and human-readable explanations

---

## Phase 11: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final production readiness

### Observability & Monitoring

- [ ] T307 [P] Implement OpenTelemetry metrics in PredictionsController.cs (prediction_request_count, prediction_latency_milliseconds, prediction_cache_hit_rate)
- [ ] T308 [P] Implement model health metrics in ModelDriftDetectionService.cs (model_accuracy, model_drift_score, model_version_active)
- [ ] T309 [P] Implement business metrics in PredictionOrchestrator.cs (daily_prediction_volume, unique_users_daily, avg_confidence_score)
- [ ] T310 [P] Implement resource metrics in ModelTrainingService.cs (model_loading_time, training_duration)
- [ ] T311 [P] Add distributed tracing correlation IDs to all MediatR handlers (propagate via OpenTelemetry context)
- [ ] T312 [P] Add prediction error logging with feature values and model version to ErrorHandlingMiddleware.cs
- [ ] T313 [P] Configure alert rules in observability stack (model accuracy degradation, API error rate >5%, cache hit rate <40%)

### Security & Compliance

- [ ] T314 [P] Implement PII anonymization in training datasets (hash customer IDs before model training)
- [ ] T315 [P] Add data deletion endpoint DELETE /predictions/user/{userId} for GDPR compliance in PredictionsController.cs
- [ ] T316 [P] Implement data deletion handler to purge user predictions and training data
- [ ] T317 [P] Configure TLS 1.3 for all HTTP communication in Program.cs
- [ ] T318 [P] Configure Google Secret Manager integration for database credentials, API keys
- [ ] T319 [P] Add PII masking to structured logs (redact customer IDs, user IDs in log output)

### Data Cleanup & Retention

- [ ] T320 Create DataCleanupService.cs in Maliev.PredictionService.Infrastructure/BackgroundServices/ implementing IHostedService
- [ ] T321 Implement 13-month rolling window cleanup for audit logs in DataCleanupService.cs (monthly partitions)
- [ ] T322 Implement 90-day retention for Deprecated models before archiving in DataCleanupService.cs
- [ ] T323 Implement configurable data retention policies per data type in DataCleanupService.cs

### Documentation & Deployment

- [ ] T324 [P] Update README.md with architecture overview, local development setup, testing instructions
- [ ] T325 [P] Create Kubernetes deployment manifests (deployment.yaml, service.yaml, configmap.yaml, secret.yaml) with 3 replicas, health checks
- [ ] T326 [P] Configure resource requests (1 CPU, 2Gi memory) and limits (2 CPU, 4Gi memory) in Kubernetes deployment
- [ ] T327 [P] Create OpenAPI documentation via Scalar from controller annotations (Scalar UI at /api-docs) with optional authentication based on environment
- [ ] T328 [P] Generate API client libraries for common languages (C#, TypeScript, Python) from OpenAPI spec

### Performance Optimization

- [ ] T329 [P] Add response compression middleware to Program.cs for API responses >1KB
- [ ] T330 [P] Configure connection pooling for PostgreSQL (min 10, max 100 connections)
- [ ] T331 [P] Configure Redis connection multiplexing in RedisCacheService.cs
- [ ] T332 [P] Add database query optimization (review generated SQL for N+1 queries, add missing indexes)
- [ ] T333 [P] Implement batch prediction endpoint POST /predictions/v1/batch with async processing and status polling

### Final Validation

- [ ] T334 Run full integration test suite with all Testcontainers (PostgreSQL, Redis, RabbitMQ) and verify 100% pass rate
- [ ] T335 Run load testing with 1,000 predictions/min sustained for 10 minutes, verify P95 latency <2s, no errors
- [ ] T336 Verify Docker build completes successfully with multi-stage Dockerfile
- [ ] T337 Verify CI/CD pipeline runs successfully (build, test, Docker push)
- [ ] T338 Perform security scan on Docker image (Trivy or similar) and address critical/high vulnerabilities
- [ ] T339 Verify all OpenTelemetry metrics are emitting correctly to observability stack
- [ ] T340 Perform manual smoke testing of all prediction endpoints with real-world-like data
- [ ] T341 Verify model training workflow end-to-end (train, quality gates, auto-deploy, cache invalidation)
- [ ] T342 Verify model rollback workflow (trigger degradation, verify automatic rollback)
- [ ] T343 [P] Run code coverage analysis and verify ≥80% coverage for business logic (Domain + Application layers) per Constitution III
- [ ] T344 [P] Implement graceful degradation with rule-based fallback for FR-055 (when model unavailable, use simple heuristic calculations)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-10)**: All depend on Foundational phase completion
  - P0 stories (US1, US2, US3, US7): Highest priority, implement first
  - P1 stories (US4, US5, US6, US8): Lower priority, implement after P0 stories complete
  - User stories CAN proceed in parallel if team capacity allows
- **Polish (Phase 11)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (Print Time, P0)**: No dependencies on other stories - can start after Foundational
- **User Story 2 (Demand Forecast, P0)**: No dependencies on other stories - can start after Foundational
- **User Story 3 (Price Optimization, P0)**: No dependencies on other stories - can start after Foundational
- **User Story 7 (Model Training, P0)**: Depends on at least one predictor being implemented (US1, US2, or US3) for training workflow testing
- **User Story 4 (Churn, P1)**: No dependencies on other stories - can start after Foundational
- **User Story 5 (Material Demand, P1)**: Reuses DemandForecaster from US2, but can implement independently
- **User Story 6 (Bottleneck, P1)**: No dependencies on other stories - can start after Foundational
- **User Story 8 (Explainability, P1)**: Integrates into all prediction handlers, best implemented after US1-US6 complete

### Within Each User Story

- Feature engineering before ML trainers/predictors
- ML trainers before predictors (training must happen first)
- Domain entities and interfaces before implementations
- CQRS commands/queries before handlers
- Handlers before API controllers
- API controllers before integration tests
- Core implementation before explainability integration (for US8)

### Parallel Opportunities

- **Setup (Phase 1)**: All project creation tasks (T002-T007), all NuGet package tasks (T008-T013), all infrastructure tasks (T015-T020) can run in parallel
- **Foundational (Phase 2)**: Database fixtures (T024-T026), domain foundation (T027-T041), repositories (T049-T051), all config tasks (T057-T060, T066-T067) can run in parallel within their groups
- **User Stories**: After Foundational phase completes, all P0 stories (US1, US2, US3, US7) can start in parallel. All P1 stories (US4, US5, US6, US8) can start after P0 stories complete (or in parallel if team capacity allows)
- **Within User Stories**: Feature engineering tasks, ML trainer/predictor creation, DTO creation, validators, and test creation can run in parallel within each story
- **Polish (Phase 11)**: All observability tasks (T307-T313), all security tasks (T314-T319), all documentation tasks (T324-T328), all performance tasks (T329-T333) can run in parallel

---

## Parallel Example: User Story 1

```bash
# Phase 1: Feature Engineering (parallel)
Task T070: "Create GeometryFeatureExtractor.cs"
Task T074: "Create PrintTimeTrainer.cs" (can start in parallel)

# Phase 2: ML Components (sequential after phase 1)
Task T075-T078: "Implement PrintTimeTrainer.cs logic" (sequential within trainer)
Task T079: "Create PrintTimePredictor.cs" (can start after trainer exists)

# Phase 3: Application Layer (parallel)
Task T083: "Create PrintTimePredictionRequest.cs DTO"
Task T084: "Create PredictionResponse.cs DTO"
Task T085: "Create PrintTimeRequestValidator.cs"

# Phase 4: Integration Testing (parallel)
Task T099: "Create PrintTimePredictorTests.cs"
Task T103: "Create PredictionsControllerTests.cs"
```

---

## Implementation Strategy

### MVP First (P0 Stories Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 (Print Time)
4. Complete Phase 4: User Story 2 (Demand Forecast)
5. Complete Phase 5: User Story 3 (Price Optimization)
6. Complete Phase 6: User Story 7 (Model Training)
7. **STOP and VALIDATE**: Test all P0 stories independently
8. Deploy/demo if ready (this is a production-ready MVP)

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Add User Story 7 → Test independently → Deploy/Demo (MVP complete - all P0 stories done)
6. Add User Story 4 → Test independently → Deploy/Demo
7. Add User Story 5 → Test independently → Deploy/Demo
8. Add User Story 6 → Test independently → Deploy/Demo
9. Add User Story 8 → Test independently → Deploy/Demo (all stories complete)
10. Polish phase → Final production hardening

### Parallel Team Strategy

With 4 developers after Foundational phase:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (Print Time)
   - Developer B: User Story 2 (Demand Forecast)
   - Developer C: User Story 3 (Price Optimization)
   - Developer D: User Story 7 (Model Training) - starts after US1 complete
3. After P0 stories complete:
   - Developer A: User Story 4 (Churn)
   - Developer B: User Story 5 (Material Demand)
   - Developer C: User Story 6 (Bottleneck)
   - Developer D: User Story 8 (Explainability) - starts after US1-6 complete
4. All developers: Polish phase together

---

## Notes

- **[P] tasks** = different files, no dependencies, can run in parallel
- **[Story] label** maps task to specific user story for traceability (US1, US2, US3, US4, US5, US6, US7, US8)
- **Each user story** should be independently completable and testable
- **P0 priority** (US1, US2, US3, US7) = critical for MVP, implement first
- **P1 priority** (US4, US5, US6, US8) = high value but not blocking, implement after MVP
- **Foundational phase** is CRITICAL - no user story work can begin until it's complete
- **Model Training (US7)** requires at least one predictor implemented first (suggest US1 or US2)
- **Explainability (US8)** best implemented last as it integrates into all prediction handlers
- **Commit frequently** after each task or logical group
- **Stop at checkpoints** to validate story independently before proceeding
- **Avoid**: vague tasks, same file conflicts, cross-story dependencies that break independence

---

## Task Summary

- **Total Tasks**: 347 (updated after issue resolution)
- **Setup Phase**: 22 tasks (includes T018A, T018B for Constitution XVI compliance)
- **Foundational Phase**: 49 tasks (includes T069A for API versioning)
- **User Story 1 (Print Time, P0)**: 37 tasks
- **User Story 2 (Demand Forecast, P0)**: 37 tasks
- **User Story 3 (Price Optimization, P0)**: 25 tasks
- **User Story 7 (Model Training, P0)**: 34 tasks
- **User Story 4 (Churn, P1)**: 34 tasks
- **User Story 5 (Material Demand, P1)**: 19 tasks
- **User Story 6 (Bottleneck, P1)**: 25 tasks
- **User Story 8 (Explainability, P1)**: 26 tasks
- **Polish Phase**: 39 tasks (includes T343 for code coverage, T344 for graceful degradation)

**Parallel Opportunities Identified**: 150+ tasks marked [P] can run in parallel within their phases

**MVP Scope** (P0 stories only): Setup + Foundational + US1 + US2 + US3 + US7 = 199 tasks
