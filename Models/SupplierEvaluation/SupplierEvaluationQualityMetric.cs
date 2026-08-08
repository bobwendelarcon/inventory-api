using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_evaluation_quality_metrics")]
    public class SupplierEvaluationQualityMetric
    {
        [Key]
        [Column("quality_metric_id")]
        public int QualityMetricId { get; set; }

        [Required]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Column("total_receiving_report_count")]
        public int TotalReceivingReportCount { get; set; }

        [Column("total_qc_count")]
        public int TotalQcCount { get; set; }

        [Column("total_received_qty", TypeName = "decimal(18,4)")]
        public decimal TotalReceivedQty { get; set; }

        [Column("total_accepted_qty", TypeName = "decimal(18,4)")]
        public decimal TotalAcceptedQty { get; set; }

        [Column("total_rejected_qty", TypeName = "decimal(18,4)")]
        public decimal TotalRejectedQty { get; set; }

        [Column("acceptance_rate", TypeName = "decimal(10,2)")]
        public decimal AcceptanceRate { get; set; }

        [Column("rejection_rate", TypeName = "decimal(10,2)")]
        public decimal RejectionRate { get; set; }

        [Column("quality_score", TypeName = "decimal(10,2)")]
        public decimal QualityScore { get; set; }

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