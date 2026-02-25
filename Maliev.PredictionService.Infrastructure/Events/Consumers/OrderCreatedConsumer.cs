using Maliev.PredictionService.Infrastructure.Persistence.Repositories;
using Maliev.MessagingContracts.Contracts.Orders;
using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.PredictionService.Infrastructure.Events.Consumers;

/// <summary>
/// Consumes <see cref="OrderCreatedEvent"/> to ingest order line-item data into the demand-forecasting
/// training dataset. Implements T132-T136: event handling, validation, deduplication, retraining trigger.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly HashSet<string> _processedMessageIds = new(); // Simple in-memory deduplication

    private const int MinimumDatasetSizeForRetraining = 1000; // Trigger retraining when dataset reaches this size

    /// <summary>
    /// Initialises a new instance of <see cref="OrderCreatedConsumer"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="modelRepository">Repository for ML model access.</param>
    private readonly Maliev.PredictionService.Infrastructure.BackgroundServices.ModelRetrainingBackgroundService _retrainingService;
    private readonly TrainingDatasetRepository _datasetRepository;

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IModelRepository modelRepository,
        Maliev.PredictionService.Infrastructure.BackgroundServices.ModelRetrainingBackgroundService retrainingService,
        TrainingDatasetRepository datasetRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
        _retrainingService = retrainingService ?? throw new ArgumentNullException(nameof(retrainingService));
        _datasetRepository = datasetRepository ?? throw new ArgumentNullException(nameof(datasetRepository));
    }

    /// <summary>
    /// Handles the incoming <see cref="OrderCreatedEvent"/>.
    /// </summary>
    /// <param name="context">MassTransit consume context.</param>
    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var message = context.Message;
        var messageId = message.MessageId.ToString();

        _logger.LogInformation(
            "Received OrderCreatedEvent. MessageId: {MessageId}, OrderId: {OrderId}, Items: {ItemCount}",
            messageId, message.Payload.OrderId, message.Payload.Items.Count);

        try
        {
            // T135: Deduplication check (idempotency)
            if (_processedMessageIds.Contains(messageId))
            {
                _logger.LogWarning("Duplicate message detected. MessageId: {MessageId} already processed. Skipping.", messageId);
                return;
            }

            // T134: Data validation
            var validationErrors = ValidateEvent(message);
            if (validationErrors.Count > 0)
            {
                _logger.LogWarning(
                    "Validation failed for OrderCreatedEvent. MessageId: {MessageId}, Errors: {Errors}",
                    messageId, string.Join("; ", validationErrors));

                // Don't throw - log and skip invalid events
                return;
            }

            // T133: Ingest each order line item into training dataset
            foreach (var item in message.Payload.Items)
            {
                await IngestLineItemAsync(item, message.Payload, context.CancellationToken);
            }

            // Mark message as processed (deduplication)
            _processedMessageIds.Add(messageId);

            // T136: Check if we should trigger model retraining (per distinct product)
            var distinctProductIds = message.Payload.Items
                .Select(i => i.ProductId)
                .Distinct();

            foreach (var productId in distinctProductIds)
            {
                await CheckAndTriggerRetrainingAsync(productId.ToString(), context.CancellationToken);
            }

            _logger.LogInformation(
                "Successfully processed OrderCreatedEvent. MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, message.Payload.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing OrderCreatedEvent. MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, message.Payload.OrderId);

            // Rethrow to trigger MassTransit retry/error handling
            throw;
        }
    }

    /// <summary>
    /// T134: Validate the <see cref="OrderCreatedEvent"/> envelope and payload.
    /// </summary>
    /// <param name="message">The event to validate.</param>
    /// <returns>List of validation error strings; empty if valid.</returns>
    private static List<string> ValidateEvent(OrderCreatedEvent message)
    {
        var errors = new List<string>();
        var payload = message.Payload;

        if (payload.OrderId == Guid.Empty)
            errors.Add("OrderId is required");

        if (payload.CustomerId == Guid.Empty)
            errors.Add("CustomerId is required");

        if (payload.Items.Count == 0)
            errors.Add("Order must contain at least one line item");

        foreach (var item in payload.Items)
        {
            if (item.ProductId == Guid.Empty)
                errors.Add($"Line item ProductId is required");

            if (item.Quantity <= 0)
                errors.Add($"Line item Quantity must be positive. Got: {item.Quantity}");

            if (item.UnitPrice < 0)
                errors.Add($"Line item UnitPrice cannot be negative. Got: {item.UnitPrice}");

            var expectedTotal = item.Quantity * item.UnitPrice;
            if (Math.Abs(item.LineTotal - expectedTotal) > 0.01)
                errors.Add($"Line item LineTotal mismatch for product {item.ProductId}. Expected: {expectedTotal}, Got: {item.LineTotal}");
        }

        return errors;
    }

    /// <summary>
    /// T133: Ingest a single order line item into the demand-forecasting training dataset.
    /// </summary>
    /// <param name="item">The line item to ingest.</param>
    /// <param name="payload">The parent order payload for context fields.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task IngestLineItemAsync(
        OrderCreatedEventPayloadItemsItem item,
        OrderCreatedEventPayload payload,
        CancellationToken cancellationToken)
    {
        var datasetEntry = new
        {
            ProductId = item.ProductId,
            Date = payload.CreatedAt,
            Demand = (float)item.Quantity,
            UnitPrice = (float)item.UnitPrice,
            CustomerId = payload.CustomerId,
            IsPromotion = false, // Check payload.DiscountAmount > 0 if order-level discount indicates promotion
            IsHoliday = IsHoliday(payload.CreatedAt.UtcDateTime),
            Timestamp = DateTimeOffset.UtcNow
        };

        _logger.LogDebug(
            "Ingested order line item for training. ProductId: {ProductId}, Date: {Date:yyyy-MM-dd}, Demand: {Demand}",
            datasetEntry.ProductId, datasetEntry.Date, datasetEntry.Demand);

        // Convert to TrainingDataset entity and save
        var dataset = new TrainingDataset
        {
            Id = Guid.NewGuid(),
            ModelType = ModelType.DemandForecast,
            RecordCount = 1,
            DateRangeStart = datasetEntry.Date.UtcDateTime,
            DateRangeEnd = datasetEntry.Date.UtcDateTime,
            FeatureColumns = new List<string> { "Date", "Demand", "UnitPrice", "IsPromotion", "IsHoliday" },
            TargetColumn = "Demand",
            FilePath = "orders/" + payload.OrderId + "/" + item.ProductId + ".json", // simulated path for single record
            DataQualityMetrics = new Dictionary<string, object>
            {
                { "ProductId", datasetEntry.ProductId.ToString() },
                { "Demand", datasetEntry.Demand }
            }
        };

        await _datasetRepository.CreateAsync(dataset, cancellationToken);
    }

    /// <summary>
    /// T136: Check if minimum dataset size is reached for a product and trigger model retraining.
    /// </summary>
    /// <param name="productId">The product identifier to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task CheckAndTriggerRetrainingAsync(string productId, CancellationToken cancellationToken)
    {
        var datasetSize = await GetDatasetSizeAsync(productId, cancellationToken);

        if (datasetSize >= MinimumDatasetSizeForRetraining)
        {
            _logger.LogInformation(
                "Minimum dataset size reached for product {ProductId}. Dataset size: {Size}. Triggering model retraining.",
                productId, datasetSize);

            if (Guid.TryParse(productId, out var modelId))
            {
                await _retrainingService.EnqueueRetrainingJobAsync(modelId, ModelType.DemandForecast, cancellationToken);
            }
        }

        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Get the current training dataset record count for a given product.
    /// </summary>
    /// <param name="productId">The product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current record count.</returns>
    private async Task<int> GetDatasetSizeAsync(string productId, CancellationToken cancellationToken)
    {
        return await _datasetRepository.GetTotalRecordCountAsync(ModelType.DemandForecast, cancellationToken);
    }

    /// <summary>
    /// Simple check for common Thai public holidays.
    /// </summary>
    /// <param name="date">The date to check.</param>
    /// <returns>True if the date is a recognised holiday.</returns>
    private static bool IsHoliday(DateTime date)
    {
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),   // New Year's Day
            new DateTime(date.Year, 4, 6),   // Chakri Day
            new DateTime(date.Year, 4, 13),  // Songkran
            new DateTime(date.Year, 5, 1),   // Labour Day
            new DateTime(date.Year, 12, 5),  // King's Birthday
            new DateTime(date.Year, 12, 10), // Constitution Day
            new DateTime(date.Year, 12, 31), // New Year's Eve
        };

        return holidays.Any(h => h.Date == date.Date);
    }
}
