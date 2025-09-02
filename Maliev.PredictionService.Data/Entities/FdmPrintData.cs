using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Maliev.PredictionService.Data.Entities
{
    public class FdmPrintData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
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
