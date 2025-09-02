using System.ComponentModel.DataAnnotations;

namespace Maliev.PredictionService.Api.DTOs
{
    public class UpdateFdmPrintDataRequest
    {
        [Required]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public required string Material { get; set; }

        [Required]
        public float LayerHeight { get; set; }

        [Required]
        public float InfillPercent { get; set; }

        [Required]
        public float DimensionX { get; set; }

        [Required]
        public float DimensionY { get; set; }

        [Required]
        public float DimensionZ { get; set; }

        [Required]
        public float OutboxVolume { get; set; }

        [Required]
        public float Volume { get; set; }

        [Required]
        public float EstimatedWeight { get; set; }

        [Required]
        public float NumberOfLayers { get; set; }

        [Required]
        public float PrintTimeMinutes { get; set; }
    }
}
