# Maliev Prediction Service - Product Specification

**Version:** 1.0.0
**Status:** Draft
**Last Updated:** 2026-02-14
**Owner:** MALIEV Platform Team

---

## 1. Executive Summary

### 1.1 Vision
The Maliev Prediction Service is the **intelligent brain** of the MALIEV manufacturing ecosystem. It provides enterprise-grade machine learning capabilities to predict, optimize, and automate critical business decisions across all microservices. By leveraging ML.NET and advanced time-series analysis, it transforms historical operational data into actionable insights that drive profitability, efficiency, and customer satisfaction.

### 1.2 Business Value
- **Revenue Optimization**: 15-25% improvement in pricing accuracy through ML-driven dynamic pricing
- **Cost Reduction**: 20-30% reduction in inventory holding costs via demand forecasting
- **Customer Retention**: 40% improvement in churn prevention through early risk detection
- **Operational Efficiency**: 50%+ reduction in manual estimation time for manufacturing processes
- **Competitive Advantage**: Real-time, data-driven decision making at scale

### 1.3 Strategic Position
**Role in MALIEV Architecture**: The centralized predictive intelligence layer. It consumes historical data from Order, Customer, Material, Invoice, and Manufacturing services to provide real-time predictions, forecasts, and recommendations that proactively optimize business operations across the entire platform.

---

## 2. Problem Statement

### 2.1 Current Challenges
1. **Manufacturing Time Estimation**: Manual estimation of 3D printing and manufacturing times is:
   - Time-consuming (30-45 minutes per complex geometry)
   - Inconsistent (±30% variance between estimators)
   - Not scalable for high-volume quote requests

2. **Demand Planning**: Reactive inventory management leads to:
   - Stockouts during peak demand (12% revenue loss)
   - Excess inventory carrying costs (18% of total costs)
   - Missed production planning opportunities

3. **Pricing Strategy**: Static pricing models result in:
   - Lost margin opportunities (customers willing to pay more)
   - Uncompetitive quotes (losing deals to competitors)
   - No dynamic adjustment for market conditions

4. **Customer Churn**: Reactive customer relationship management causes:
   - Late intervention (after customer has decided to leave)
   - No visibility into early warning signals
   - 22% annual customer churn rate

5. **Resource Optimization**: Manual resource allocation leads to:
   - Suboptimal employee scheduling
   - Equipment underutilization
   - Bottlenecks in production workflow

### 2.2 User Pain Points
- **Sales Team**: "We need instant, accurate quotes to close deals faster"
- **Operations Manager**: "We're always scrambling to meet unexpected demand"
- **Finance Team**: "Our pricing doesn't reflect true market value"
- **Customer Success**: "We only discover unhappy customers when they leave"
- **Production Manager**: "I can't predict which jobs will take longer than estimated"

---

## 3. Target Users & Personas

### 3.1 Primary Users

#### Persona 1: Sales Engineer (Quote Creator)
- **Goal**: Generate accurate quotes in < 5 minutes
- **Needs**:
  - Instant manufacturing time predictions from 3D geometry
  - Price recommendations based on complexity and market rates
  - Confidence intervals for delivery dates
- **Pain**: Manual calculation takes too long, loses deals

#### Persona 2: Operations Manager (Demand Planner)
- **Goal**: Optimize inventory and production capacity
- **Needs**:
  - Weekly/monthly demand forecasts by material type
  - Early warning of demand spikes
  - Production capacity utilization predictions
- **Pain**: Reactive planning causes stock issues and overtime costs

#### Persona 3: Pricing Strategist
- **Goal**: Maximize profit while remaining competitive
- **Needs**:
  - Dynamic pricing recommendations
  - Elasticity analysis
  - Competitor pricing insights
- **Pain**: Static pricing leaves money on the table

#### Persona 4: Customer Success Manager
- **Goal**: Proactively retain high-value customers
- **Needs**:
  - Churn risk scores for all active customers
  - Root cause analysis of churn factors
  - Recommended intervention strategies
- **Pain**: Discovering churn after it's too late

### 3.2 Secondary Users
- **API Consumers**: Other microservices (OrderService, QuotationService, etc.)
- **Data Scientists**: Model training, evaluation, and improvement
- **Platform Administrators**: Model lifecycle management and monitoring

---

## 4. Functional Requirements

### 4.1 Core Prediction Capabilities

#### FR-1: 3D Print Time Prediction
**Priority:** P0 (Critical)
**User Story:** As a sales engineer, I want to upload a 3D geometry file and instantly get accurate print time predictions, so I can provide fast, reliable quotes to customers.

**Acceptance Criteria:**
- Accept 3D geometry inputs (STL, OBJ, 3MF formats)
- Extract geometric features (volume, surface area, complexity metrics, support requirements)
- Predict total manufacturing time with ±5% accuracy
- Return prediction in < 2 seconds for files up to 50MB
- Provide confidence interval (e.g., 120 min ± 8 min)
- Handle batch predictions (up to 100 files simultaneously)
- Include breakdown: print time, post-processing time, quality control time
- Support multiple printer types and materials

**Technical Requirements:**
- ML.NET regression model trained on historical print jobs
- Feature extraction: volume, surface area, layer count, support percentage, infill density, print speed
- Model retraining: weekly with new completed jobs
- Minimum dataset: 10,000 historical print jobs for initial training

#### FR-2: Sales Demand Forecasting
**Priority:** P0 (Critical)
**User Story:** As an operations manager, I want to see predicted demand for the next 30-90 days, so I can optimize inventory and production scheduling.

**Acceptance Criteria:**
- Forecast daily/weekly demand by product category
- Support multiple forecast horizons: 7-day, 30-day, 90-day
- Include confidence bands (80%, 95%)
- Account for seasonality, trends, and promotional periods
- Detect and alert on anomalous demand patterns
- Accuracy target: MAPE < 15% for 30-day forecasts
- Update forecasts daily with latest data
- Support what-if scenario analysis

**Technical Requirements:**
- Time-series forecasting using ML.NET or TimeGPT
- Features: historical sales, calendar effects, promotions, external indicators
- Model: ARIMA, Prophet, or LSTM-based approach
- Minimum dataset: 24 months of historical sales data

#### FR-3: Dynamic Price Optimization
**Priority:** P0 (Critical)
**User Story:** As a pricing strategist, I want ML-driven price recommendations for each quote, so I can maximize profit while winning competitive deals.

**Acceptance Criteria:**
- Recommend optimal price per quote based on:
  - Material costs
  - Manufacturing complexity
  - Customer segment and history
  - Competitive landscape
  - Current capacity utilization
- Provide price elasticity estimates
- Calculate expected win probability at different price points
- Support A/B testing of pricing strategies
- Return recommendations in < 500ms
- Include reasoning/explanation for price recommendation

**Technical Requirements:**
- ML.NET regression model for price prediction
- Features: material cost, geometry complexity, customer LTV, historical win rates, market indicators
- Model retraining: bi-weekly
- Minimum dataset: 5,000 historical quotes with outcomes (won/lost)

#### FR-4: Customer Churn Prediction
**Priority:** P1 (High)
**User Story:** As a customer success manager, I want to identify customers at risk of churning 30-60 days in advance, so I can intervene proactively.

**Acceptance Criteria:**
- Calculate churn risk score (0-100) for every active customer
- Predict churn probability over next 30, 60, 90 days
- Identify top 5 churn risk factors per customer
- Segment customers by risk level: Low, Medium, High, Critical
- Recommend intervention strategies based on risk factors
- Refresh predictions daily
- Achieve 75%+ precision on high-risk predictions
- Provide trend analysis (risk increasing/decreasing)

**Technical Requirements:**
- ML.NET binary classification model
- Features: order frequency, recency, monetary value, support tickets, payment delays, engagement metrics
- Model: Gradient Boosted Trees or Random Forest
- Minimum dataset: 2,000 customers with 12+ months of history

#### FR-5: Material Demand Forecasting
**Priority:** P1 (High)
**User Story:** As a procurement manager, I want predictions of material consumption for the next quarter, so I can negotiate bulk discounts and prevent stockouts.

**Acceptance Criteria:**
- Forecast consumption by material SKU for 30, 60, 90 days
- Alert when predicted demand exceeds current inventory levels
- Recommend reorder quantities and timing
- Account for lead times and minimum order quantities
- Support multi-location inventory planning
- Accuracy target: ±10% for top 20% materials by volume

**Technical Requirements:**
- Time-series forecasting per material SKU
- Features: historical consumption, open orders, production schedule, seasonality
- Minimum dataset: 18 months of material transaction history

#### FR-6: Production Bottleneck Detection
**Priority:** P1 (High)
**User Story:** As a production manager, I want to predict where bottlenecks will occur in the next 1-2 weeks, so I can proactively allocate resources.

**Acceptance Criteria:**
- Identify equipment/workstation capacity constraints
- Predict queue wait times for each production stage
- Recommend resource reallocation strategies
- Alert 5-7 days before predicted bottleneck
- Visualize production flow and constraint points
- Accuracy: 80%+ in identifying actual bottlenecks

**Technical Requirements:**
- Queuing theory + ML.NET regression
- Features: current queue depths, equipment utilization, scheduled jobs, employee availability
- Real-time data integration from manufacturing execution systems

#### FR-7: Employee Performance Prediction
**Priority:** P2 (Medium)
**User Story:** As an HR manager, I want to predict employee performance and turnover risk, so I can provide targeted training and retention programs.

**Acceptance Criteria:**
- Predict individual employee productivity trends
- Identify employees at risk of leaving (90-day window)
- Recommend personalized training programs
- Calculate ROI of retention interventions
- Privacy-preserving: only aggregated insights accessible to managers

**Technical Requirements:**
- ML.NET classification + regression models
- Features: tenure, performance metrics, certifications, engagement scores, compensation benchmarks
- Privacy: differential privacy or federated learning for sensitive data
- Minimum dataset: 500 employees with 12+ months of history

#### FR-8: Quality Control Anomaly Detection
**Priority:** P2 (Medium)
**User Story:** As a quality manager, I want to predict which orders are likely to have quality issues, so I can allocate inspection resources efficiently.

**Acceptance Criteria:**
- Score each production job for quality risk (0-100)
- Identify root cause factors (material batch, printer, operator, design complexity)
- Recommend inspection priority queue
- Alert on unusual failure pattern clusters
- Accuracy: 70%+ precision on high-risk predictions

**Technical Requirements:**
- ML.NET anomaly detection + classification
- Features: material properties, printer history, operator experience, design complexity, environmental factors
- Model: Isolation Forest or Autoencoders for anomaly detection
- Minimum dataset: 3,000 jobs with quality inspection results

### 4.2 Model Management & Operations

#### FR-9: Automated Model Training & Retraining
**Priority:** P0 (Critical)
**User Story:** As a data scientist, I want models to automatically retrain on fresh data, so predictions stay accurate without manual intervention.

**Acceptance Criteria:**
- Scheduled retraining: daily, weekly, or monthly per model
- Event-triggered retraining: when model performance degrades
- Automated data validation before training
- Model versioning and rollback capability
- A/B testing framework for new model versions
- Notification of training completion and performance metrics
- Automatic deployment of improved models (if performance threshold met)

**Technical Requirements:**
- Background job scheduler (Hangfire or Quartz.NET)
- Model registry for version control
- Automated evaluation pipeline with holdout datasets
- Performance monitoring and drift detection
- CI/CD integration for model deployment

#### FR-10: Explainable AI / Model Interpretability
**Priority:** P1 (High)
**User Story:** As a business user, I want to understand why the model made a specific prediction, so I can trust and act on the recommendations.

**Acceptance Criteria:**
- Provide SHAP or LIME-style feature importance for each prediction
- Return top 3-5 factors contributing to the prediction
- Generate human-readable explanations (e.g., "Price is high because complexity score is in top 10%")
- Visualize feature impacts for interactive exploration
- Audit trail of prediction logic

**Technical Requirements:**
- ML.NET explainability features or custom SHAP implementation
- Feature contribution calculation with minimal performance overhead
- Explainability API endpoints for all prediction types

#### FR-11: Prediction Caching & Performance
**Priority:** P0 (Critical)
**User Story:** As an API consumer, I want predictions to return in < 500ms for real-time quote generation, so the user experience remains seamless.

**Acceptance Criteria:**
- Cache predictions for identical inputs (TTL: 1-24 hours depending on model)
- Sub-500ms response time for cached predictions
- Sub-2s response time for uncached 3D geometry predictions
- Sub-1s for all other prediction types
- Support batch prediction requests (10-100 items)
- Rate limiting per API consumer
- Cache invalidation on model updates

**Technical Requirements:**
- Redis distributed cache with intelligent TTL management
- Efficient feature engineering pipeline (pre-computed where possible)
- Model serving optimized for latency (ONNX runtime for critical models)
- Load testing: support 1,000 predictions/minute sustained

#### FR-12: Data Integration & ETL
**Priority:** P0 (Critical)
**User Story:** As a platform architect, I want the prediction service to automatically ingest data from all relevant microservices, so models always train on the latest information.

**Acceptance Criteria:**
- Subscribe to domain events from: Order, Customer, Material, Invoice, Manufacturing, Employee services
- Incremental data ingestion (event-driven)
- Full historical data sync on initialization
- Data quality validation and anomaly detection
- Handle data schema evolution gracefully
- Support point-in-time snapshots for reproducible training

**Technical Requirements:**
- MassTransit event consumers for real-time data ingestion
- PostgreSQL event store for prediction-relevant historical data
- Data transformation pipeline (map domain events to ML features)
- Data versioning and lineage tracking
- Automated data quality checks (null checks, outlier detection, schema validation)

### 4.3 API & Integration

#### FR-13: REST API
**Priority:** P0 (Critical)
**Endpoints:**

```http
# 3D Print Time Prediction
POST /predictions/v1/print-time
Content-Type: application/json
{
  "geometryFile": "<base64-encoded-stl>",
  "material": "PLA",
  "printerType": "Prusa-MK4",
  "infillDensity": 20,
  "layerHeight": 0.2
}
Response 200 OK:
{
  "predictionId": "uuid",
  "estimatedMinutes": 182,
  "confidenceInterval": { "lower": 170, "upper": 195 },
  "breakdown": {
    "printTime": 152,
    "postProcessing": 20,
    "qualityControl": 10
  },
  "confidence": 0.92,
  "explanation": {
    "topFactors": [
      { "factor": "volume", "impact": 0.45 },
      { "factor": "supportStructures", "impact": 0.30 },
      { "factor": "layerCount", "impact": 0.25 }
    ]
  },
  "modelVersion": "v2.3.1",
  "timestamp": "2026-02-14T10:30:00Z"
}

# Sales Demand Forecast
POST /predictions/v1/demand-forecast
{
  "forecastHorizonDays": 30,
  "productCategories": ["3D-Printing", "CNC-Machining"],
  "granularity": "daily"
}
Response 200 OK:
{
  "predictionId": "uuid",
  "forecastStart": "2026-02-15",
  "forecastEnd": "2026-03-16",
  "forecasts": [
    {
      "date": "2026-02-15",
      "category": "3D-Printing",
      "predictedDemand": 45,
      "confidenceBands": { "lower80": 38, "upper80": 52, "lower95": 33, "upper95": 58 }
    }
  ],
  "alerts": [
    { "date": "2026-02-28", "message": "Predicted demand spike (+40%) detected", "severity": "high" }
  ],
  "modelVersion": "v1.5.2",
  "timestamp": "2026-02-14T10:30:00Z"
}

# Dynamic Price Recommendation
POST /predictions/v1/price-recommendation
{
  "quoteId": "uuid",
  "customerId": "uuid",
  "totalCost": 450.00,
  "materialComplexity": "high",
  "urgency": "standard"
}
Response 200 OK:
{
  "predictionId": "uuid",
  "recommendedPrice": 675.00,
  "priceRange": { "minimum": 600.00, "optimal": 675.00, "maximum": 750.00 },
  "expectedWinProbability": 0.78,
  "elasticity": -1.2,
  "explanation": {
    "reasoning": "Customer has high LTV ($12K) and low price sensitivity. Market rate 50% above cost.",
    "topFactors": [
      { "factor": "customerLTV", "impact": 0.35 },
      { "factor": "marketBenchmark", "impact": 0.30 },
      { "factor": "complexity", "impact": 0.20 }
    ]
  },
  "competitorBenchmark": { "p25": 620, "median": 680, "p75": 740 },
  "modelVersion": "v3.1.0",
  "timestamp": "2026-02-14T10:30:00Z"
}

# Customer Churn Prediction
GET /predictions/v1/churn-risk/{customerId}
Response 200 OK:
{
  "customerId": "uuid",
  "churnRiskScore": 72,
  "riskLevel": "high",
  "probabilityNext30Days": 0.42,
  "probabilityNext60Days": 0.65,
  "probabilityNext90Days": 0.78,
  "topRiskFactors": [
    { "factor": "decreasingOrderFrequency", "impact": 0.40, "trend": "worsening" },
    { "factor": "increasedSupportTickets", "impact": 0.25, "trend": "stable" },
    { "factor": "paymentDelays", "impact": 0.20, "trend": "improving" }
  ],
  "recommendations": [
    { "action": "schedule-account-review", "priority": 1, "expectedImpact": "high" },
    { "action": "offer-loyalty-discount", "priority": 2, "expectedImpact": "medium" }
  ],
  "modelVersion": "v2.0.5",
  "lastUpdated": "2026-02-14T06:00:00Z"
}

# Batch Prediction (any model)
POST /predictions/v1/batch
{
  "modelType": "print-time",
  "predictions": [ /* array of prediction requests */ ]
}
Response 202 Accepted:
{
  "batchId": "uuid",
  "status": "processing",
  "totalRequests": 50,
  "statusUrl": "/predictions/v1/batch/{batchId}/status",
  "resultsUrl": "/predictions/v1/batch/{batchId}/results"
}

# Model Health & Metrics
GET /predictions/v1/models/{modelType}/health
Response 200 OK:
{
  "modelType": "print-time",
  "version": "v2.3.1",
  "status": "healthy",
  "lastTrainingDate": "2026-02-10T03:00:00Z",
  "nextScheduledTraining": "2026-02-17T03:00:00Z",
  "performanceMetrics": {
    "accuracy": 0.94,
    "mae": 8.2,
    "r2": 0.89
  },
  "predictionVolumeToday": 1234,
  "averageLatencyMs": 420
}
```

#### FR-14: Event-Driven Predictions
**Priority:** P1 (High)
**User Story:** As a microservice, I want predictions to be automatically triggered by domain events, so I don't need to poll the prediction service.

**Acceptance Criteria:**
- Publish prediction results as domain events via RabbitMQ
- Subscribe to trigger events (e.g., "OrderCreated" → generate demand forecast update)
- Async prediction processing for non-time-critical requests
- Event schema versioning and backward compatibility

**Technical Requirements:**
- MassTransit message publishers/consumers
- Event contracts in Maliev.MessagingContracts
- Async task processing with retry logic

#### FR-15: SDK / Client Libraries
**Priority:** P2 (Medium)
**User Story:** As a .NET developer, I want a strongly-typed SDK to call the prediction service, so I don't need to manually construct HTTP requests.

**Acceptance Criteria:**
- NuGet package: `Maliev.PredictionService.Client`
- Strongly-typed request/response models
- Built-in retry logic and circuit breakers (Polly)
- Async/await support
- Automatic authentication token management

---

## 5. Non-Functional Requirements

### 5.1 Performance
- **Latency**: P95 < 500ms for cached predictions, P95 < 2s for uncached
- **Throughput**: Support 1,000 predictions/minute sustained, 2,500 burst
- **Availability**: 99.5% uptime (excluding planned maintenance)
- **Model Training**: Complete within 4 hours for largest models

### 5.2 Scalability
- Horizontal scaling for API tier (stateless)
- Handle 100K+ historical data records per model
- Support 10+ concurrent ML models
- Batch processing: up to 1,000 predictions in single request

### 5.3 Security
- Authentication: OAuth 2.0 / OpenID Connect integration with IAMService
- Authorization: Role-based access control (RBAC) for prediction types
- Data privacy: PII anonymization in training datasets
- Encryption: TLS 1.3 for data in transit, AES-256 for data at rest
- API rate limiting: prevent abuse and ensure fair usage

### 5.4 Reliability
- Model serving: graceful degradation if model unavailable (fallback to rule-based)
- Data pipeline: automatic retry with exponential backoff on failures
- Model rollback: revert to previous version if new model degrades performance
- Monitoring: alerts on model drift, data quality issues, API errors

### 5.5 Maintainability
- Code coverage: >80% for business logic
- XML documentation on all public APIs
- Automated integration tests with Testcontainers
- Feature flags for gradual model rollouts

### 5.6 Observability
- OpenTelemetry metrics: prediction latency, cache hit rate, model accuracy
- Distributed tracing: end-to-end request tracking
- Structured logging: all predictions logged for audit and debugging
- Dashboards: Grafana visualizations for model performance and API health

### 5.7 Compliance
- GDPR: Support data deletion requests (right to be forgotten)
- Audit logs: immutable record of all predictions and model changes
- Data retention: configurable retention policies per data type
- Model governance: versioning, approval workflows, and explainability

---

## 6. Data Models

### 6.1 Prediction Request (Base)
```csharp
public record PredictionRequest
{
    public string RequestId { get; init; } = Guid.NewGuid().ToString();
    public DateTime RequestedAt { get; init; } = DateTime.UtcNow;
    public string UserId { get; init; }
    public string TenantId { get; init; }
    public Dictionary<string, object> Metadata { get; init; } = new();
}
```

### 6.2 Prediction Response (Base)
```csharp
public record PredictionResponse
{
    public string PredictionId { get; init; } = Guid.NewGuid().ToString();
    public DateTime PredictedAt { get; init; } = DateTime.UtcNow;
    public string ModelType { get; init; }
    public string ModelVersion { get; init; }
    public double Confidence { get; init; }
    public ExplanationData Explanation { get; init; }
    public PredictionMetadata Metadata { get; init; }
}

public record ExplanationData
{
    public List<FeatureContribution> TopFactors { get; init; }
    public string HumanReadableReasoning { get; init; }
}

public record FeatureContribution
{
    public string Factor { get; init; }
    public double Impact { get; init; } // 0.0 to 1.0
    public string Trend { get; init; } // "improving", "stable", "worsening"
}
```

### 6.3 Model Registry
```csharp
public class MLModel
{
    public Guid Id { get; set; }
    public string ModelType { get; set; } // "print-time", "demand-forecast", etc.
    public string Version { get; set; } // "v2.3.1"
    public ModelStatus Status { get; set; } // Draft, Testing, Active, Deprecated
    public string ModelPath { get; set; } // File system or blob storage path
    public DateTime TrainedAt { get; set; }
    public DateTime DeployedAt { get; set; }
    public PerformanceMetrics Metrics { get; set; }
    public TrainingConfiguration TrainingConfig { get; set; }
    public string CreatedBy { get; set; }
}

public enum ModelStatus
{
    Draft,
    Testing,
    Active,
    Deprecated,
    Archived
}

public class PerformanceMetrics
{
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double MAE { get; set; } // Mean Absolute Error
    public double RMSE { get; set; } // Root Mean Squared Error
    public double R2 { get; set; } // R-squared
    public Dictionary<string, double> CustomMetrics { get; set; }
}
```

### 6.4 Training Dataset
```csharp
public class TrainingDataset
{
    public Guid Id { get; set; }
    public string ModelType { get; set; }
    public string Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public int RecordCount { get; set; }
    public DateRange DataRange { get; set; }
    public DataQualityReport QualityReport { get; set; }
    public string StoragePath { get; set; }
    public List<string> FeatureColumns { get; set; }
    public string TargetColumn { get; set; }
}

public class DataQualityReport
{
    public int TotalRecords { get; set; }
    public int InvalidRecords { get; set; }
    public Dictionary<string, int> MissingValuesByColumn { get; set; }
    public Dictionary<string, OutlierInfo> OutliersByColumn { get; set; }
    public List<string> DataQualityWarnings { get; set; }
}
```

### 6.5 Prediction Audit Log
```csharp
public class PredictionAuditLog
{
    public Guid Id { get; set; }
    public string PredictionId { get; set; }
    public string ModelType { get; set; }
    public string ModelVersion { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public string UserId { get; set; }
    public string TenantId { get; set; }
    public object InputData { get; set; } // JSON
    public object OutputData { get; set; } // JSON
    public double Confidence { get; set; }
    public int ResponseTimeMs { get; set; }
    public bool FromCache { get; set; }
    public string ErrorMessage { get; set; } // null if successful
}
```

---

## 7. Integration Points

### 7.1 Upstream Dependencies (Data Sources)
- **OrderService**: Historical order data, manufacturing times, costs
- **CustomerService**: Customer profiles, purchase history, engagement metrics
- **MaterialService**: Material properties, inventory levels, supplier data
- **InvoiceService**: Payment history, revenue data
- **EmployeeService**: Performance metrics, scheduling data
- **SupplierService**: Supplier reliability, lead times
- **QuotationService**: Quote-to-order conversion rates, pricing history

### 7.2 Downstream Consumers (Prediction Users)
- **QuotationService**: Print time + price predictions for automated quotes
- **OrderService**: Demand forecasts for capacity planning
- **CustomerService**: Churn predictions for retention campaigns
- **MaterialService**: Material demand forecasts for procurement
- **EmployeeService**: Performance predictions for HR interventions
- **ChatbotService**: Predictions for customer self-service

### 7.3 External Integrations (Future)
- **3D Geometry Analysis APIs**: Cloud-based geometry feature extraction
- **Market Data Providers**: External pricing and demand signals
- **Weather APIs**: Environmental factors for production planning
- **Economic Indicators**: Macro trends for demand forecasting

---

## 8. Success Metrics

### 8.1 Model Performance KPIs
- **Print Time Prediction**: MAE < 10 minutes, R² > 0.85
- **Demand Forecast**: MAPE < 15% for 30-day horizon
- **Price Optimization**: 20% improvement in margin vs. manual pricing
- **Churn Prediction**: Precision > 0.75 for high-risk customers
- **Material Forecast**: 90% accuracy in preventing stockouts

### 8.2 Business Impact KPIs
- **Quote Turnaround Time**: Reduce from 45 min to < 5 min
- **Inventory Holding Costs**: Reduce by 25% within 6 months
- **Customer Retention**: Improve by 15% within 12 months
- **Pricing Accuracy**: 20% reduction in lost deals due to pricing

### 8.3 Operational KPIs
- **API Availability**: > 99.5%
- **P95 Latency**: < 500ms
- **Cache Hit Rate**: > 60%
- **Model Freshness**: Average model age < 14 days

---

## 9. Out of Scope (v1.0)

The following features are explicitly **NOT** included in the initial release:

1. **Real-time Computer Vision**: Live monitoring of 3D printers with CV-based defect detection
2. **Reinforcement Learning**: Autonomous optimization of production schedules
3. **Natural Language Queries**: "What will demand be like next month?" conversational interface
4. **Multi-tenancy**: Isolated models per customer (all customers share same models initially)
5. **AutoML**: Automated model architecture search and hyperparameter tuning
6. **Federated Learning**: Privacy-preserving distributed model training
7. **Mobile SDKs**: iOS/Android client libraries
8. **Real-time Streaming Predictions**: Sub-100ms latency for streaming data

These may be considered for future releases based on customer feedback and business priorities.

---

## 10. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| Insufficient training data | High | Medium | Start with transfer learning from public datasets; incentivize data collection |
| Model drift over time | High | High | Automated monitoring, scheduled retraining, A/B testing framework |
| Cold start problem (new customers/products) | Medium | High | Hybrid approach: ML for existing + rule-based for new |
| Regulatory compliance (GDPR, AI Act) | High | Medium | Privacy-by-design, explainable AI, audit logs |
| Scalability bottlenecks | Medium | Medium | Load testing, horizontal scaling, caching strategy |
| Dependency on external services | Medium | Low | Circuit breakers, fallback strategies, SLA monitoring |

---

## 11. Assumptions & Constraints

### 11.1 Assumptions
- Historical data quality is sufficient for model training (>80% complete records)
- Data labeling is accurate (e.g., manufacturing times logged correctly)
- Upstream services provide timely event notifications
- Business processes remain relatively stable (no major workflow changes mid-project)

### 11.2 Constraints
- Must use .NET 10 and ML.NET (per platform standards)
- No banned libraries (AutoMapper, FluentValidation, etc.)
- Must integrate with existing IAMService for auth
- Budget: within standard infrastructure costs (no expensive 3rd-party ML platforms)
- Timeline: v1.0 target delivery in 12-16 weeks

---

## 12. Glossary

- **MAPE**: Mean Absolute Percentage Error - forecast accuracy metric
- **MAE**: Mean Absolute Error - prediction error metric
- **R²**: Coefficient of determination - regression model fit metric
- **SHAP**: SHapley Additive exPlanations - model interpretability method
- **LIME**: Local Interpretable Model-agnostic Explanations
- **LTV**: Lifetime Value - total revenue from a customer over their lifetime
- **Churn**: Customer attrition or departure
- **Model Drift**: Degradation of model performance over time as data patterns change
- **Feature Engineering**: Creating predictive variables from raw data
- **ONNX**: Open Neural Network Exchange - model interoperability format

---

## 13. Appendices

### Appendix A: Related Documents
- MALIEV Platform Architecture Overview
- IAMService Integration Guide
- MessagingContracts Event Catalog
- ML.NET Best Practices Guide

### Appendix B: Revision History
| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0.0 | 2026-02-14 | Platform Team | Initial specification |

---

**Document Status:** Ready for Review
**Next Steps:** Create implementation plan (predictionservice-plan.md)
