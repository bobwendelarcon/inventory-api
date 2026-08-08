using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_evaluation_delivery_metrics")]
    public class SupplierEvaluationDeliveryMetric
    {
        [Key]
        [Column("delivery_metric_id")]
        public int DeliveryMetricId { get; set; }

        [Required]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Column("total_scheduled_deliveries")]
        public int TotalScheduledDeliveries { get; set; }

        [Column("completed_deliveries")]
        public int CompletedDeliveries { get; set; }

        [Column("on_time_deliveries")]
        public int OnTimeDeliveries { get; set; }

        [Column("late_deliveries")]
        public int LateDeliveries { get; set; }

        [Column("early_deliveries")]
        public int EarlyDeliveries { get; set; }

        [Column("incomplete_deliveries")]
        public int IncompleteDeliveries { get; set; }

        [Column("undelivered_schedules")]
        public int UndeliveredSchedules { get; set; }

        [Column("average_delay_days", TypeName = "decimal(8,2)")]
        public decimal AverageDelayDays { get; set; }

        [Column("on_time_rate", TypeName = "decimal(8,4)")]
        public decimal OnTimeRate { get; set; }

        [Column("delivery_score", TypeName = "decimal(6,2)")]
        public decimal DeliveryScore { get; set; }

        [MaxLength(1000)]
        [Column("calculation_notes")]
        public string? CalculationNotes { get; set; }

        [Column("calculated_at")]
        public DateTime CalculatedAt { get; set; }

        [MaxLength(50)]
        [Column("calculated_by")]
        public string? CalculatedBy { get; set; }

        public SupplierPerformanceEvaluation Evaluation { get; set; } = null!;
    }
}