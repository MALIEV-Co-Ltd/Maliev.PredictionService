# Maliev Prediction Service

Machine learning and predictive analytics service for the MALIEV platform, providing demand forecasting, price predictions, churn analysis, and intelligent recommendations with full IAM integration.

## Service Description

The Prediction Service leverages machine learning models to provide predictive insights across the MALIEV platform. It offers demand forecasting for inventory optimization, price predictions for strategic pricing, customer churn analysis, and personalized product recommendations.

## Architecture Overview

### Project Structure
```
Maliev.PredictionService/
├── Maliev.PredictionService.Api/          # Presentation layer
│   ├── Controllers/                       # REST API endpoints
│   ├── Services/                          # ML model services
│   ├── Models/                            # DTOs and ML models
│   └── ML/                                # ML.NET model definitions
├── Maliev.PredictionService.Data/         # Data access layer
│   ├── Entities/                          # Training data and predictions
│   └── Migrations/                        # Database migrations
└── Maliev.PredictionService.Tests/        # Integration tests
```

## Technologies Used

- **.NET 10.0** - Runtime and framework
- **ASP.NET Core** - Web API framework
- **ML.NET** - Machine learning framework
- **Entity Framework Core** - ORM with PostgreSQL provider
- **PostgreSQL 18** - Training data and prediction history
- **Redis** - Prediction result caching
- **RabbitMQ** - Message queue via MassTransit
- **OpenTelemetry** - Observability
- **Python/ONNX** - Advanced model integration (optional)

## Dependencies

### Databases
- **PostgreSQL**: Training data, model metadata, prediction history
- **Redis**: Prediction result caching, model warm-up

### Messaging
- **RabbitMQ**: Events for model updates, batch predictions

### External Services
- **IAM Service**: Authentication and authorization
- **Order Service**: Historical order data for forecasting
- **Customer Service**: Customer behavior data
- **Material Service**: Product data for recommendations
- **Inventory Service**: Stock level data

## IAM Integration

### Required Permissions
- `predictions.read` - View prediction results
- `predictions.create` - Request predictions
- `predictions.models.read` - View model information
- `predictions.models.train` - Trigger model training
- `predictions.models.deploy` - Deploy new models
- `predictions.analytics.read` - View prediction analytics

### Predefined Roles
- **Business Analyst**: Read predictions and analytics
- **Data Scientist**: Train and deploy models
- **Operations Manager**: Request forecasts for planning

## API Endpoints

### Demand Forecasting
- `POST /v1/predictions/demand-forecast` - Forecast demand for product/period
- `POST /v1/predictions/demand-forecast/batch` - Batch forecast for multiple products
- `GET /v1/predictions/demand-forecast/{productId}` - Get latest forecast

### Price Prediction
- `POST /v1/predictions/price-prediction` - Predict optimal price
- `POST /v1/predictions/price-elasticity` - Calculate price elasticity
- `GET /v1/predictions/pricing-recommendations` - Get pricing recommendations

### Customer Analytics
- `POST /v1/predictions/churn-analysis` - Predict customer churn probability
- `POST /v1/predictions/lifetime-value` - Predict customer lifetime value
- `GET /v1/predictions/churn-risk/customers` - Get high-risk customers

### Recommendations
- `POST /v1/predictions/product-recommendations` - Get personalized recommendations
- `POST /v1/predictions/cross-sell` - Cross-sell recommendations
- `POST /v1/predictions/upsell` - Upsell recommendations

### Model Management
- `GET /v1/models` - List available models
- `GET /v1/models/{modelId}` - Get model details
- `POST /v1/models/{modelId}/train` - Trigger model retraining
- `POST /v1/models/{modelId}/deploy` - Deploy model version
- `GET /v1/models/{modelId}/metrics` - Get model performance metrics

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "PredictionDatabase": "Host=postgres;Port=5432;Database=maliev_predictions;Username=app;Password=secret",
    "Redis": "redis:6379"
  },
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Username": "guest",
    "Password": "guest"
  },
  "Jwt": {
    "Key": "base64-encoded-key",
    "Issuer": "maliev-prediction-service",
    "Audience": "maliev-services"
  },
  "ExternalServices": {
    "IAM": {
      "BaseUrl": "http://iam-service:8080"
    }
  },
  "MachineLearning": {
    "ModelStoragePath": "/models",
    "AutoRetrainEnabled": true,
    "RetrainIntervalDays": 7,
    "MinTrainingDataPoints": 100,
    "CachePredictionMinutes": 60
  }
}
```

## Database

**PostgreSQL 18** with Entity Framework Core migrations.

**Main Tables:**
- `Models` - ML model metadata and versions
- `TrainingData` - Historical data for model training
- `Predictions` - Prediction results and history
- `ModelMetrics` - Model performance tracking
- `FeatureImportance` - Feature importance scores

## Running the Service

### Development
```bash
cd Maliev.PredictionService.Api
dotnet run
```

**Access:**
- API: http://localhost:5000
- Health: http://localhost:5000/predictions/liveness
- Model Metrics: http://localhost:5000/predictions/metrics

### Docker
```bash
docker build -t maliev/prediction-service:latest .
docker run -p 8080:8080 \
  -v /models:/models \
  maliev/prediction-service:latest
```

### Tests
```bash
dotnet test
```

## Test Status

**From Test Summary (2025-12-24):**
- **Status**: PASSED (No tests found)
- **Note**: Service infrastructure exists but no tests currently defined

**Recommended:** Add tests for:
- Model prediction accuracy
- Feature engineering
- Batch processing
- Model versioning

## Key Features

### Demand Forecasting
- **Time Series Analysis**: Seasonal, trend, and cyclical patterns
- **Multi-Product**: Forecast for individual or grouped products
- **Confidence Intervals**: Prediction with uncertainty bounds
- **Scenario Analysis**: What-if scenarios for planning

### Price Optimization
- **Dynamic Pricing**: ML-based price recommendations
- **Elasticity Modeling**: Price sensitivity analysis
- **Competitive Pricing**: Market-aware pricing suggestions
- **Margin Optimization**: Balance volume and profit margin

### Customer Intelligence
- **Churn Prediction**: Identify at-risk customers
- **Lifetime Value**: Predict customer long-term value
- **Segmentation**: ML-based customer clustering
- **Next Best Action**: Personalized recommendations

### Recommendation Engine
- **Collaborative Filtering**: User-based recommendations
- **Content-Based**: Product similarity recommendations
- **Hybrid Approach**: Combined recommendation strategies
- **Context-Aware**: Time, location, and behavior-based

### Model Lifecycle Management
- **Automated Retraining**: Scheduled model updates
- **A/B Testing**: Compare model versions
- **Performance Monitoring**: Track prediction accuracy
- **Feature Engineering**: Automated feature selection

## Machine Learning Models

### Implemented Models
1. **Demand Forecast**: Time series forecasting (ARIMA, LSTM)
2. **Price Prediction**: Regression models (Random Forest, Gradient Boosting)
3. **Churn Analysis**: Binary classification (Logistic Regression, XGBoost)
4. **Recommendations**: Matrix factorization and neural networks

### Model Metrics
- **Demand**: MAPE (Mean Absolute Percentage Error)
- **Price**: RMSE (Root Mean Square Error)
- **Churn**: AUC-ROC, Precision, Recall
- **Recommendations**: Precision@K, NDCG

## Events Published

- `ModelTrainedEvent` - New model version trained
- `ModelDeployedEvent` - Model deployed to production
- `PredictionRequestedEvent` - Batch prediction started
- `HighChurnRiskEvent` - Customer identified as high churn risk

## Performance Considerations

- **Caching**: Predictions cached in Redis (configurable TTL)
- **Batch Processing**: Support for bulk predictions
- **Async Training**: Model training runs asynchronously
- **Model Warm-up**: Pre-load models on service startup

## Support

- Test Summary: `B:\maliev\all-services-test-summary.txt`
- ServiceDefaults: `B:\maliev\Maliev.Aspire\Maliev.Aspire.ServiceDefaults\README.md`

## License

Proprietary - Copyright 2025 MALIEV Co., Ltd. All rights reserved.
