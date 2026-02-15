# Feature Specification: Enterprise ML Prediction Service

**Feature Branch**: `001-enterprise-ml-prediction-service`
**Created**: 2026-02-14
**Status**: Draft
**Input**: User description: "Build enterprise ML prediction service providing 3D print time prediction, demand forecasting, dynamic pricing, customer churn prediction, material demand forecasting, bottleneck detection, and automated model management for the MALIEV manufacturing ecosystem"

## Clarifications

### Session 2026-02-14

- Q: Out-of-scope features - what's NOT included in this service? → A: Focus on core ML predictions only (no computer vision, no batch analytics, no BI dashboards)
- Q: Technical platform constraints - what technology stack must be used? → A: Standard MALIEV platform stack (.NET 10, ML.NET, PostgreSQL, RabbitMQ, Redis)
- Q: Observability metrics strategy - which metrics are critical for monitoring? → A: Both business and technical metrics (API performance + model health + prediction volume + cache metrics)
- Q: Model lifecycle state transitions - how do models progress through states? → A: Automated with quality gates (auto-promote if metrics pass thresholds, manual override available)
- Q: Prediction request deduplication - how to identify and handle duplicate requests? → A: Content-based hash with time window (hash input features, dedupe within cache TTL)

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Instant 3D Print Time Predictions (Priority: P0)

A sales engineer uploads a 3D geometry file and receives accurate manufacturing time estimates within seconds, enabling them to provide fast, reliable quotes to customers without manual calculation.

**Why this priority**: Critical for competitive advantage. Manual time estimation takes 30-45 minutes and has ±30% variance. This capability directly impacts sales velocity and quote accuracy, which are foundational to business operations.

**Independent Test**: Upload a sample STL file through the API, receive a prediction response with estimated minutes, confidence interval, and breakdown (print/post-processing/QC time). Can be validated against historical data for similar geometries.

**Acceptance Scenarios**:

1. **Given** a valid 3D geometry file (STL, OBJ, or 3MF format up to 50MB), **When** a sales engineer submits it for prediction, **Then** the system returns manufacturing time estimate within 2 seconds with ±5% accuracy and confidence interval
2. **Given** multiple 3D files (up to 100), **When** submitted as a batch prediction request, **Then** the system processes all files and returns individual predictions for each
3. **Given** a 3D geometry with specific printer type and material selected, **When** prediction is requested, **Then** the system returns time breakdown showing print time, post-processing time, and quality control time separately
4. **Given** a previously predicted geometry, **When** the same file is submitted again within cache window, **Then** the system returns the cached prediction in under 500ms

---

### User Story 2 - Sales Demand Forecasting (Priority: P0)

An operations manager views predicted demand for the next 30-90 days across product categories, enabling proactive inventory planning and production scheduling to prevent stockouts and reduce carrying costs.

**Why this priority**: Reactive planning currently causes 12% revenue loss from stockouts and 18% excess inventory costs. Demand forecasting is essential for operational efficiency and directly impacts profitability.

**Independent Test**: Request a 30-day demand forecast for a product category, verify it returns daily predictions with confidence bands. Compare predictions against actual demand after 30 days to validate accuracy (target: MAPE < 15%).

**Acceptance Scenarios**:

1. **Given** at least 24 months of historical sales data, **When** operations manager requests a 30-day demand forecast, **Then** the system returns daily demand predictions with 80% and 95% confidence bands
2. **Given** a forecast request for multiple time horizons (7-day, 30-day, 90-day), **When** submitted, **Then** the system returns forecasts for each horizon accounting for seasonality and trends
3. **Given** current sales patterns, **When** an anomalous demand pattern is detected (e.g., 40% spike predicted), **Then** the system alerts the operations team 5-7 days before the predicted spike
4. **Given** promotional periods or special events, **When** included in forecast parameters, **Then** the system adjusts predictions to account for non-standard demand patterns

---

### User Story 3 - Dynamic Price Optimization (Priority: P0)

A pricing strategist receives ML-driven price recommendations for each quote that consider material costs, customer history, manufacturing complexity, and competitive landscape, maximizing profit while maintaining win rates.

**Why this priority**: Static pricing leaves 15-25% margin improvement opportunity on the table and causes uncompetitive quotes. Dynamic pricing directly increases revenue and competitive positioning.

**Independent Test**: Submit a quote request with customer details and manufacturing parameters, receive price recommendation with win probability and elasticity analysis. Validate by comparing recommended prices against historical win/loss data.

**Acceptance Scenarios**:

1. **Given** a quote request with material costs and geometry complexity, **When** pricing strategist requests recommendation, **Then** the system returns optimal price, price range (min/optimal/max), and expected win probability at each price point
2. **Given** customer's historical purchase patterns and lifetime value, **When** price recommendation is calculated, **Then** the system adjusts pricing strategy based on customer segment and relationship value
3. **Given** current capacity utilization and market conditions, **When** pricing recommendation is requested, **Then** the system factors in operational constraints and competitive landscape
4. **Given** a price recommendation with uncertainty, **When** delivered to user, **Then** the system provides explanation showing top 3-5 factors contributing to the recommendation with relative impact weights

---

### User Story 4 - Customer Churn Risk Prediction (Priority: P1)

A customer success manager identifies customers at high risk of churning 30-60 days in advance, receives root cause analysis and recommended intervention strategies, enabling proactive retention efforts before customers decide to leave.

**Why this priority**: High business impact (22% annual churn rate) but can be implemented after core quote/demand features. Proactive intervention is significantly more cost-effective than reactive customer acquisition.

**Independent Test**: Generate churn predictions for all active customers, verify high-risk customers (score >70) receive intervention recommendations. Track actual churn over next 90 days to validate precision (target: >75%).

**Acceptance Scenarios**:

1. **Given** customer accounts with 12+ months of history, **When** daily churn prediction runs, **Then** every active customer receives a churn risk score (0-100) with 30/60/90-day probability estimates
2. **Given** a customer with high churn risk (score >70), **When** prediction is generated, **Then** the system identifies top 5 risk factors (e.g., decreasing order frequency, support tickets, payment delays) with trend analysis
3. **Given** identified risk factors for a customer, **When** churn prediction is delivered, **Then** the system recommends 2-3 specific intervention strategies prioritized by expected impact
4. **Given** churn predictions from previous months, **When** actual churn occurs or doesn't occur, **Then** the system uses this feedback to improve future predictions through automated retraining

---

### User Story 5 - Material Demand Forecasting (Priority: P1)

A procurement manager receives predictions of material consumption by SKU for the next quarter, with alerts when predicted demand exceeds inventory levels and recommendations for reorder timing and quantities, optimizing bulk purchasing and preventing stockouts.

**Why this priority**: Important for cost optimization (20-30% potential reduction in inventory holding costs) but dependent on sales demand forecasting. Can be implemented once core forecasting capability exists.

**Independent Test**: Request material forecast for a specific SKU, verify it returns consumption predictions accounting for current inventory, open orders, and lead times. Validate accuracy against actual consumption (target: ±10% for top 20% materials).

**Acceptance Scenarios**:

1. **Given** 18 months of material transaction history, **When** procurement manager requests quarterly forecast for a material SKU, **Then** the system returns 30/60/90-day consumption predictions
2. **Given** predicted material demand and current inventory levels, **When** forecast shows demand exceeding stock within lead time window, **Then** the system generates an alert with recommended reorder quantity and timing
3. **Given** supplier lead times and minimum order quantities, **When** reorder recommendations are calculated, **Then** the system accounts for these constraints in timing and quantity suggestions
4. **Given** production schedule and open orders, **When** material forecast is generated, **Then** the system incorporates scheduled manufacturing activity into consumption predictions

---

### User Story 6 - Production Bottleneck Detection (Priority: P1)

A production manager receives predictions of equipment and workstation capacity constraints 5-7 days in advance, with resource reallocation recommendations, enabling proactive scheduling adjustments to prevent production delays.

**Why this priority**: Valuable for operational efficiency (50%+ reduction in manual estimation time) but requires integration with manufacturing execution systems. Can be implemented after core prediction capabilities are stable.

**Independent Test**: Submit current production schedule and queue depths, receive bottleneck predictions showing constraint points and recommended resource allocations. Validate by tracking actual bottlenecks against predictions (target: 80%+ accuracy).

**Acceptance Scenarios**:

1. **Given** current queue depths and equipment utilization, **When** bottleneck analysis runs daily, **Then** the system identifies workstations/equipment predicted to hit capacity constraints within 1-2 weeks
2. **Given** predicted bottlenecks, **When** analysis is complete, **Then** the system calculates expected queue wait times for each production stage and visualizes production flow
3. **Given** available resources and predicted constraints, **When** bottleneck detected, **Then** the system recommends specific resource reallocation strategies (e.g., shift employee assignments, adjust equipment scheduling)
4. **Given** employee availability and equipment maintenance schedules, **When** bottleneck predictions are calculated, **Then** the system accounts for these constraints in capacity planning

---

### User Story 7 - Automated Model Training and Deployment (Priority: P0)

A data scientist configures automated retraining schedules for prediction models, which automatically retrain on fresh data, evaluate performance, and deploy improved versions without manual intervention, ensuring predictions stay accurate as business conditions evolve.

**Why this priority**: Critical for long-term system health and accuracy. Without automated retraining, model performance will degrade over time (model drift), undermining all other features.

**Independent Test**: Configure a model to retrain weekly, verify it automatically pulls latest data, trains new version, evaluates against holdout dataset, and deploys only if performance exceeds threshold. Confirm version history and rollback capability.

**Acceptance Scenarios**:

1. **Given** a prediction model with configured retraining schedule (daily/weekly/monthly), **When** schedule triggers, **Then** the system automatically pulls latest data, validates quality, trains new model version, and logs training metrics
2. **Given** a newly trained model version, **When** evaluation completes, **Then** the system compares performance against current production model and deploys automatically only if metrics improve beyond threshold
3. **Given** a model experiencing performance degradation (drift detection), **When** accuracy drops below threshold, **Then** the system triggers event-based retraining outside normal schedule and notifies data science team
4. **Given** multiple model versions in registry, **When** production issue detected with current version, **Then** data scientist can rollback to previous version with single command while maintaining prediction history

---

### User Story 8 - Explainable Predictions (Priority: P1)

A business user receives human-readable explanations for each prediction showing the top contributing factors and their relative impact, building trust in ML recommendations and enabling informed decision-making.

**Why this priority**: High value for user adoption and trust, but not blocking for basic functionality. Can be implemented incrementally as models are deployed.

**Independent Test**: Request any prediction type, verify response includes feature importance breakdown and human-readable reasoning (e.g., "Price is high because customer LTV is in top 10% and complexity score is above average").

**Acceptance Scenarios**:

1. **Given** any prediction request, **When** result is returned, **Then** the response includes top 3-5 factors contributing to the prediction with impact weights (0.0-1.0)
2. **Given** a prediction with complex factors, **When** explanation is generated, **Then** the system provides a human-readable summary (e.g., "Churn risk is high because order frequency decreased 40% and support tickets increased 3x")
3. **Given** a prediction explanation, **When** user needs deeper analysis, **Then** the system provides visualizations showing how each factor influenced the final prediction
4. **Given** prediction history for auditing, **When** accessed, **Then** the system maintains immutable record of prediction logic and feature values used at time of prediction

---

### Edge Cases

- **What happens when a model has insufficient training data?** System should detect minimum dataset threshold violations (10K records for print-time, 5K for price, 2K for churn per research.md) and either use rule-based fallback or return prediction with warning about low confidence
- **How does the system handle 3D geometry files with unsupported formats or corrupted data?** Return clear validation error indicating specific issue (format not supported, file corrupted, size exceeded) with HTTP 400 status
- **What happens when prediction cache is stale due to model update?** System automatically invalidates all cached predictions when new model version is deployed, forcing recalculation with latest model
- **How does the system handle cold start scenarios (new customer with no history)?** Use hybrid approach: population-level models for new entities, gradually shift to personalized predictions as data accumulates (minimum 3-6 months of history)
- **What happens during model retraining if data quality validation fails?** Abort training, preserve current production model, alert data science team with specific quality issues detected (nulls, outliers, schema changes)
- **How does the system handle concurrent prediction requests exceeding capacity?** Implement request queuing with priority levels (real-time quotes = high priority, batch forecasts = low priority) and return 503 Service Unavailable if queue depth exceeds threshold
- **What happens when upstream data sources are temporarily unavailable?** Serve predictions from cached models using last known good data, return predictions with warning flag about data freshness
- **How does the system handle seasonal products with no historical data for current season?** Use time-series decomposition from previous years' seasonal patterns, clearly flag predictions as based on historical seasonality

## Requirements *(mandatory)*

### Functional Requirements

#### Core Prediction Capabilities

- **FR-001**: System MUST accept 3D geometry files in STL, OBJ, and 3MF formats up to 50MB for print time prediction
- **FR-002**: System MUST extract geometric features (volume, surface area, complexity metrics, support requirements) from 3D files automatically
- **FR-003**: System MUST predict total manufacturing time within ±5% of actual manufacturing time and return results within 2 seconds for individual requests
- **FR-004**: System MUST provide confidence intervals for all predictions (e.g., 120 min ± 8 min)
- **FR-005**: System MUST support batch predictions for up to 100 files simultaneously
- **FR-006**: System MUST provide time breakdown (print time, post-processing time, QC time) for manufacturing predictions
- **FR-007**: System MUST generate sales demand forecasts for 7-day, 30-day, and 90-day horizons with daily or weekly granularity
- **FR-008**: System MUST include 80% and 95% confidence bands for all demand forecasts
- **FR-009**: System MUST detect and alert on anomalous demand patterns (>40% deviation from expected)
- **FR-010**: System MUST achieve MAPE < 15% for 30-day demand forecasts
- **FR-011**: System MUST recommend optimal prices for quotes based on material costs, complexity, customer history, and market conditions
- **FR-012**: System MUST calculate expected win probability at different price points
- **FR-013**: System MUST return price recommendations within 500ms
- **FR-014**: System MUST calculate churn risk scores (0-100) for every active customer daily
- **FR-015**: System MUST predict churn probability over 30, 60, and 90-day windows
- **FR-016**: System MUST identify top 5 churn risk factors with trend analysis (improving/stable/worsening)
- **FR-017**: System MUST achieve >75% precision on high-risk (score >70) churn predictions
- **FR-018**: System MUST recommend intervention strategies based on identified risk factors
- **FR-019**: System MUST forecast material consumption by SKU for 30, 60, and 90-day periods
- **FR-020**: System MUST alert when predicted demand exceeds current inventory levels within lead time window
- **FR-021**: System MUST recommend reorder quantities and timing accounting for supplier lead times and minimum order quantities
- **FR-022**: System MUST predict production bottlenecks 5-7 days in advance with 80%+ accuracy
- **FR-023**: System MUST identify specific equipment/workstation capacity constraints and queue wait times
- **FR-024**: System MUST recommend resource reallocation strategies for predicted bottlenecks

#### Model Management & Operations

- **FR-025**: System MUST support scheduled model retraining (daily, weekly, or monthly per model)
- **FR-026**: System MUST trigger event-based retraining when model performance degrades below threshold
- **FR-027**: System MUST validate data quality before training (null checks, outlier detection, schema validation)
- **FR-028**: System MUST maintain version history for all models with rollback capability
- **FR-029**: System MUST automatically deploy new model versions only if performance exceeds current production model
- **FR-030**: System MUST notify data science team of training completion with performance metrics comparison
- **FR-031**: System MUST detect model drift by monitoring prediction accuracy against actual outcomes
- **FR-032**: System MUST support A/B testing framework for evaluating new model versions in production
- **FR-033**: System MUST provide feature importance/contribution for each prediction (SHAP/LIME-style explanations)
- **FR-034**: System MUST generate human-readable explanations for predictions (e.g., "Price high because complexity in top 10%")
- **FR-035**: System MUST maintain audit trail of all predictions including input features and model version used

#### Performance & Caching

- **FR-036**: System MUST cache predictions for identical inputs using SHA-256 hash of normalized input parameters with cache key format {modelType}:{sha256Hash}:{modelVersion}, configurable TTL per model type (1-24 hours), and automatic cache invalidation when model version updates
- **FR-037**: System MUST return cached predictions in <500ms
- **FR-039**: System MUST support request rate limiting per API consumer
- **FR-040**: System MUST handle 1,000 predictions/minute sustained load
- **FR-041**: System MUST support 2,500 predictions/minute burst capacity

#### Data Integration

- **FR-042**: System MUST subscribe to domain events from Order, Customer, Material, Invoice, Manufacturing, and Employee services
- **FR-043**: System MUST incrementally ingest data via event-driven updates
- **FR-044**: System MUST support full historical data sync on initialization
- **FR-045**: System MUST handle data schema evolution gracefully without breaking existing functionality
- **FR-046**: System MUST support point-in-time snapshots for reproducible model training
- **FR-047**: System MUST validate data quality on ingestion with configurable quality rules

#### API & Integration

- **FR-048**: System MUST expose REST API endpoints for all prediction types
- **FR-049**: System MUST publish prediction results as domain events via RabbitMQ
- **FR-050**: System MUST support asynchronous batch prediction with status polling endpoints
- **FR-051**: System MUST version API contracts (URL-based or header-based versioning) and maintain backward compatibility with automated compatibility checks
- **FR-052**: System MUST authenticate requests via OAuth 2.0 / OpenID Connect integration
- **FR-053**: System MUST enforce role-based access control (RBAC) for different prediction types with three roles: PredictionUser (can request predictions), PredictionAdmin (can trigger training and view model health), DataScientist (full access including model registry)
- **FR-054**: System MUST provide model health endpoints showing version, status, performance metrics, and prediction volume

#### Reliability & Fallbacks

- **FR-055**: System MUST implement graceful degradation with rule-based fallback when model is unavailable
- **FR-056**: System MUST retry failed data pipeline operations with exponential backoff
- **FR-057**: System MUST automatically rollback to previous model version if new version shows performance degradation in production
- **FR-058**: System MUST alert on model drift, data quality issues, and API errors
- **FR-059**: System MUST encrypt data in transit using TLS 1.3
- **FR-060**: System MUST encrypt sensitive data at rest using AES-256
- **FR-061**: System MUST anonymize PII in training datasets
- **FR-062**: System MUST support data deletion requests for GDPR compliance
- **FR-063**: System MUST maintain immutable audit logs of all predictions and model changes
- **FR-064**: System MUST support configurable data retention policies per data type

#### Observability & Monitoring

- **FR-065**: System MUST emit OpenTelemetry metrics for API performance (request count, latency percentiles, error rate, throughput)
- **FR-066**: System MUST emit metrics for model health (accuracy over time, prediction confidence distribution, drift detection scores)
- **FR-067**: System MUST emit business metrics (prediction volume per model type, cache hit rate, unique users, predictions per customer)
- **FR-068**: System MUST emit resource metrics (CPU usage, memory consumption, model loading time, training duration)
- **FR-069**: System MUST provide distributed tracing with correlation IDs across prediction request lifecycle
- **FR-070**: System MUST log prediction errors with feature values and model version for debugging
- **FR-071**: System MUST expose health check endpoints (liveness, readiness) for container orchestration
- **FR-072**: System MUST alert when model accuracy degrades beyond threshold (configurable per model type)
- **FR-073**: System MUST alert when API error rate exceeds 5% over 5-minute window
- **FR-074**: System MUST alert when cache hit rate drops below 40% (indicates cache configuration issue)

### Key Entities

- **Prediction Request**: Represents a request for any type of prediction, including user context (user ID, tenant ID), request timestamp, and prediction-specific parameters (geometry file, forecast horizon, customer ID, etc.). **Identity**: Each request is assigned a unique request ID (GUID). **Deduplication**: Requests with identical input features (excluding metadata like request timestamp) are identified via SHA-256 hash of normalized input parameters and served from cache within TTL window (1-24 hours depending on model type). Hash key format: `{modelType}:{sha256(sortedInputParams)}:{modelVersion}`

- **Prediction Response**: Represents the result of a prediction, including predicted value(s), confidence score, confidence intervals/bands, model version used, feature importance breakdown, and human-readable explanation

- **ML Model**: Represents a trained machine learning model, including model type (print-time, demand-forecast, etc.), version identifier, training date, deployment date, performance metrics, status (draft/testing/active/deprecated/archived), and file storage location. **State Transitions**: Draft → Testing (automatic after training completes), Testing → Active (automatic if performance metrics exceed current production model by configured threshold, manual override available), Active → Deprecated (automatic when newer model promoted to Active), Deprecated → Archived (manual after 90-day retention period), any state → Draft (manual rollback). **Quality Gates**: Testing→Active requires minimum dataset size (model-specific), accuracy improvement >2% over current production, no critical data quality warnings, and validation on holdout dataset

- **Training Dataset**: Represents a versioned collection of data used to train a model, including record count, date range, feature columns, target column, data quality metrics, and storage location

- **Prediction Audit Log**: Represents an immutable record of a prediction made, including all input features, output prediction, model version, confidence, response time, cache status, and any errors encountered

- **Model Performance Metrics**: Represents evaluation metrics for a model version, including accuracy, precision, recall, F1 score, MAE, RMSE, R² (for regression), and custom domain-specific metrics

- **Feature Contribution**: Represents the impact of an individual feature on a specific prediction, including feature name, impact weight (0.0-1.0), and trend direction (improving/stable/worsening)

- **Churn Risk Factors**: Represents identified causes of customer churn risk, including factor type (order frequency, support tickets, payment delays), impact weight, current value, historical trend, and recommended intervention

- **Demand Forecast**: Represents predicted demand over time, including date range, granularity (daily/weekly), predicted values, confidence bands (80%/95%), detected anomalies, and alerts

- **Price Recommendation**: Represents dynamic pricing guidance, including optimal price, price range (min/optimal/max), win probability, elasticity estimate, competitor benchmark data, and reasoning/explanation

- **Bottleneck Prediction**: Represents predicted production constraints, including equipment/workstation identifier, predicted constraint date, queue wait times by production stage, severity level, and recommended resource reallocations

### Out of Scope

The following capabilities are explicitly **NOT** included in this service:

- **Computer Vision**: Real-time video monitoring, image classification, defect detection via camera feeds
- **Batch Analytics**: Data warehouse ETL, OLAP cubes, historical reporting dashboards
- **Business Intelligence**: User-facing BI tools, report builders, ad-hoc query interfaces
- **Natural Language Processing**: Chatbot intelligence, document understanding, sentiment analysis (NLP features belong in dedicated ChatbotService)
- **Admin UI**: Web-based administration dashboards (use existing platform admin tools)
- **Prescriptive Actions**: Automated execution of recommendations (e.g., auto-placing orders, auto-adjusting prices) - predictions only, execution remains manual
- **Real-time Streaming**: Sub-100ms latency event stream processing (predictions are request/response or async batch)

### Technical Constraints

The following technical stack is **REQUIRED** for platform consistency:

- **Runtime**: .NET 10 (latest LTS)
- **ML Framework**: ML.NET for model training, inference, and feature engineering
- **Database**: PostgreSQL for event store, model registry, and training datasets
- **Message Broker**: RabbitMQ via MassTransit for event-driven integration
- **Cache**: Redis for prediction caching and distributed locking
- **Authentication**: OAuth 2.0 / OpenID Connect via IAMService integration
- **API Framework**: ASP.NET Core Web API with OpenAPI/Scalar documentation
- **Observability**: OpenTelemetry for metrics, tracing, and logging
- **Prohibited Libraries**: No AutoMapper, no FluentValidation (per platform standards)
- **Model Format**: ONNX runtime for production model serving (optional optimization)
- **Containerization**: Docker with multi-stage builds for deployment

## Success Criteria *(mandatory)*

### Measurable Outcomes

#### Business Impact

- **SC-001**: Sales engineers can generate accurate quotes in under 5 minutes (down from 30-45 minutes), measured by average time from geometry upload to quote delivery
- **SC-002**: Quote accuracy improves with manufacturing time predictions within ±5% of actual, measured by comparing predictions to completed job times
- **SC-003**: Pricing recommendations improve margin by 15-25% while maintaining or improving win rates, measured by revenue per quote and conversion rate
- **SC-004**: Inventory holding costs reduce by 20-30% within 6 months through improved demand forecasting, measured by average inventory value and carrying costs
- **SC-005**: Stockout incidents decrease by 12% through proactive demand alerts, measured by count of unfulfilled orders due to material unavailability
- **SC-006**: Customer churn rate reduces by 15% within 12 months through proactive intervention, measured by quarterly retention rates for high-risk customers
- **SC-007**: Customer retention interventions achieve 40% success rate (customers flagged as high-risk who don't churn after intervention), measured by tracking high-risk customers over 90 days

#### User Experience

- **SC-008**: Users receive 3D print time predictions in under 2 seconds for files up to 50MB, measured by P95 API response latency
- **SC-009**: Users receive all other prediction types in under 1 second, measured by P95 API response latency
- **SC-010**: Cached predictions return in under 500ms, measured by P95 response time for cache hits
- **SC-011**: 90% of predictions are successfully delivered on first attempt without errors, measured by success rate excluding client errors
- **SC-012**: Users understand prediction reasoning, with 100% of predictions including human-readable explanations and top contributing factors

#### Model Performance

- **SC-013**: Print time prediction model achieves R² > 0.85 and MAE < 10 minutes on holdout test data
- **SC-014**: Demand forecasting model achieves MAPE < 15% for 30-day forecasts across all product categories
- **SC-015**: Price optimization model improves win probability by 20% compared to manual pricing, measured through A/B testing
- **SC-016**: Churn prediction model achieves >75% precision for high-risk customers (score >70), measured against actual churn over 90 days
- **SC-017**: Material demand forecasting achieves ±10% accuracy for top 20% of materials by volume, measured by prediction error against actual consumption
- **SC-018**: Bottleneck detection achieves 80%+ accuracy in identifying actual bottlenecks 5-7 days in advance, measured by true positive rate

#### System Performance

- **SC-019**: System maintains 99.5% uptime excluding planned maintenance, measured by availability monitoring
- **SC-020**: System handles 1,000 predictions per minute sustained load without degradation, measured by load testing
- **SC-021**: System handles 2,500 predictions per minute burst capacity for up to 5 minutes, measured by burst load testing
- **SC-022**: Cache hit rate exceeds 60% for repeated prediction requests, measured by cache hits / total requests
- **SC-023**: Models are automatically retrained on schedule with average model age under 14 days, measured by time since last training
- **SC-024**: Model training completes within 4 hours for largest models, measured by training job duration

#### Data Quality & Operations

- **SC-025**: Data quality validation catches 95%+ of invalid records before training, measured by validation error detection rate
- **SC-026**: Model deployment is fully automated with zero-downtime version updates, measured by deployment process time and service interruptions
- **SC-027**: Prediction audit logs are complete and queryable for all predictions, with 100% coverage measured by log entries vs. prediction count
- **SC-028**: System detects model drift within 24 hours of degradation, measured by time between accuracy drop and alert generation
- **SC-029**: Failed predictions are retried successfully 90% of the time through automatic retry mechanisms, measured by retry success rate

#### Integration & Adoption

- **SC-030**: Other microservices successfully integrate with prediction service APIs with <5% error rate, measured by consumer application error logs
- **SC-031**: Event-driven predictions are delivered to subscribers within 10 seconds of trigger event, measured by event publish-to-consume latency
- **SC-032**: Prediction service processes 80%+ of production quotes through automated print time predictions within 3 months of launch, measured by quote volume using predictions vs. manual estimates
