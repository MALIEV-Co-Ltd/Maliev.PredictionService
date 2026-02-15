using Maliev.PredictionService.Domain.Entities;
using Maliev.PredictionService.Domain.Enums;
using Maliev.PredictionService.Domain.Repositories;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Maliev.PredictionService.Infrastructure.Events.Consumers;

/// <summary>
/// Event DTO for OrderCreated events.
/// TODO: Replace with actual MessagingContracts event when available.
/// </summary>
public record OrderCreated
{
    public required string OrderId { get; init; }
    public required string ProductId { get; init; }
    public required string CustomerId { get; init; }
    public required int Quantity { get; init; }
    public required decimal UnitPrice { get; init; }
    public required decimal TotalPrice { get; init; }
    public required DateTime OrderDate { get; init; }
}

/// <summary>
/// Consumes OrderCreated events to ingest order data into training dataset.
/// Implements T132-T136: event handling, validation, deduplication, retraining trigger.
/// </summary>
public class OrderCreatedConsumer : IConsumer<OrderCreated>
{
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private readonly IModelRepository _modelRepository;
    private readonly HashSet<string> _processedMessageIds = new(); // Simple in-memory deduplication

    private const int MinimumDatasetSizeForRetraining = 1000; // Trigger retraining when dataset reaches this size

    public OrderCreatedConsumer(
        ILogger<OrderCreatedConsumer> logger,
        IModelRepository modelRepository)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _modelRepository = modelRepository ?? throw new ArgumentNullException(nameof(modelRepository));
    }

    public async Task Consume(ConsumeContext<OrderCreated> context)
    {
        var message = context.Message;
        var messageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();

        _logger.LogInformation(
            "Received OrderCreated event. MessageId: {MessageId}, OrderId: {OrderId}, ProductId: {ProductId}",
            messageId, message.OrderId, message.ProductId);

        try
        {
            // T135: Deduplication check (idempotency)
            if (_processedMessageIds.Contains(messageId))
            {
                _logger.LogWarning("Duplicate message detected. MessageId: {MessageId} already processed. Skipping.", messageId);
                return;
            }

            // T134: Data validation
            var validationErrors = ValidateOrderCreatedEvent(message);
            if (validationErrors.Any())
            {
                _logger.LogWarning(
                    "Validation failed for OrderCreated event. MessageId: {MessageId}, Errors: {Errors}",
                    messageId, string.Join("; ", validationErrors));

                // Don't throw - log and skip invalid events
                return;
            }

            // T133: Ingest order data into training dataset
            await IngestOrderDataAsync(message, context.CancellationToken);

            // Mark message as processed (deduplication)
            _processedMessageIds.Add(messageId);

            // T136: Check if we should trigger model retraining
            await CheckAndTriggerRetrainingAsync(message.ProductId, context.CancellationToken);

            _logger.LogInformation(
                "Successfully processed OrderCreated event. MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, message.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing OrderCreated event. MessageId: {MessageId}, OrderId: {OrderId}",
                messageId, message.OrderId);

            // Rethrow to trigger MassTransit retry/error handling
            throw;
        }
    }

    /// <summary>
    /// T134: Validate OrderCreated event schema and data quality.
    /// </summary>
    private List<string> ValidateOrderCreatedEvent(OrderCreated orderEvent)
    {
        var errors = new List<string>();

        // Null checks
        if (string.IsNullOrWhiteSpace(orderEvent.OrderId))
            errors.Add("OrderId is required");

        if (string.IsNullOrWhiteSpace(orderEvent.ProductId))
            errors.Add("ProductId is required");

        if (string.IsNullOrWhiteSpace(orderEvent.CustomerId))
            errors.Add("CustomerId is required");

        // Range validation
        if (orderEvent.Quantity <= 0)
            errors.Add($"Quantity must be positive. Got: {orderEvent.Quantity}");

        if (orderEvent.UnitPrice < 0)
            errors.Add($"UnitPrice cannot be negative. Got: {orderEvent.UnitPrice}");

        if (orderEvent.TotalPrice < 0)
            errors.Add($"TotalPrice cannot be negative. Got: {orderEvent.TotalPrice}");

        // Logical validation
        var expectedTotal = orderEvent.Quantity * orderEvent.UnitPrice;
        if (Math.Abs(orderEvent.TotalPrice - expectedTotal) > 0.01m) // Allow small floating point differences
        {
            errors.Add($"TotalPrice mismatch. Expected: {expectedTotal}, Got: {orderEvent.TotalPrice}");
        }

        // Date validation
        if (orderEvent.OrderDate > DateTime.UtcNow.AddDays(1))
            errors.Add("OrderDate cannot be in the future");

        if (orderEvent.OrderDate < DateTime.UtcNow.AddYears(-10))
            errors.Add("OrderDate is too far in the past (>10 years)");

        return errors;
    }

    /// <summary>
    /// T133: Ingest order data into training dataset for demand forecasting.
    /// </summary>
    private async Task IngestOrderDataAsync(OrderCreated orderEvent, CancellationToken cancellationToken)
    {
        // Create or update training dataset record
        // In production, this would append to a CSV/Parquet file or database table
        // For now, we'll create a simplified dataset record

        var datasetEntry = new
        {
            ProductId = orderEvent.ProductId,
            Date = orderEvent.OrderDate,
            Demand = (float)orderEvent.Quantity,
            UnitPrice = (float)orderEvent.UnitPrice,
            CustomerId = orderEvent.CustomerId,
            IsPromotion = false, // TODO: Detect promotions from order metadata
            IsHoliday = IsHoliday(orderEvent.OrderDate),
            Timestamp = DateTime.UtcNow
        };

        _logger.LogDebug(
            "Ingested order data for training. ProductId: {ProductId}, Date: {Date:yyyy-MM-dd}, Demand: {Demand}",
            datasetEntry.ProductId, datasetEntry.Date, datasetEntry.Demand);

        // TODO: Persist to actual training dataset storage
        // await _datasetRepository.AppendTrainingDataAsync(datasetEntry, cancellationToken);

        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// T136: Check if minimum dataset size reached and trigger model retraining.
    /// </summary>
    private async Task CheckAndTriggerRetrainingAsync(string productId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual dataset size check
        // For now, this is a placeholder for the retraining trigger logic

        // Check current dataset size for this product
        var datasetSize = await GetDatasetSizeAsync(productId, cancellationToken);

        if (datasetSize >= MinimumDatasetSizeForRetraining)
        {
            _logger.LogInformation(
                "Minimum dataset size reached for product {ProductId}. Dataset size: {Size}. Triggering model retraining.",
                productId, datasetSize);

            // TODO: Publish TrainingJobRequested event or create TrainingJob entity
            // await _mediator.Publish(new TrainingJobRequested
            // {
            //     ModelType = ModelType.DemandForecast,
            //     ProductId = productId,
            //     DatasetSize = datasetSize
            // }, cancellationToken);
        }

        await Task.CompletedTask; // Placeholder
    }

    /// <summary>
    /// Get current dataset size for a product.
    /// </summary>
    private async Task<int> GetDatasetSizeAsync(string productId, CancellationToken cancellationToken)
    {
        // TODO: Implement actual dataset size query
        // return await _datasetRepository.GetRecordCountAsync(ModelType.DemandForecast, productId, cancellationToken);

        await Task.CompletedTask; // Placeholder
        return 0; // Placeholder
    }

    /// <summary>
    /// Simple holiday detection (US holidays).
    /// </summary>
    private bool IsHoliday(DateTime date)
    {
        // Simplified - check common US holidays
        var holidays = new[]
        {
            new DateTime(date.Year, 1, 1),   // New Year's Day
            new DateTime(date.Year, 7, 4),   // Independence Day
            new DateTime(date.Year, 12, 25), // Christmas
            // Add more holidays as needed
        };

        return holidays.Any(h => h.Date == date.Date);
    }
}
