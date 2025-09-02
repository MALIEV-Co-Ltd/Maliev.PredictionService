namespace Maliev.PredictionService.Api.DTOs
{
    public class FdmPrintDataDto
    {
        public int Id { get; set; }
        public required string Material { get; set; }
        public float LayerHeight { get; set; }
        public float InfillPercent { get; set; }
        public float DimensionX { get; set; }
        public float DimensionY { get; set; }
        public float DimensionZ { get; set; }
        public float OutboxVolume { get; set; }
        public float Volume { get; set; }
        public float EstimatedWeight { get; set; }
        public float NumberOfLayers { get; set; }
        public float PrintTimeMinutes { get; set; }
    }
}
