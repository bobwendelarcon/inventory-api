using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_performance_evaluations")]
    public class SupplierPerformanceEvaluation
    {
        [Key]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("evaluation_no")]
        public string EvaluationNo { get; set; } = string.Empty;

        [Required]
        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("po_id")]
        public int? PoId { get; set; }

        [Column("schedule_id")]
        public int? ScheduleId { get; set; }

        [Column("rr_id")]
        public int? RrId { get; set; }

        [Column("qc_id")]
        public int? QcId { get; set; }

        [Column("evaluation_date")]
        public DateTime? EvaluationDate { get; set; }

        [Column("delivery_date")]
        public DateTime? DeliveryDate { get; set; }

        // Legacy monthly fields.
        // Kept nullable for compatibility with old records.
        [Column("evaluation_year")]
        public int? EvaluationYear { get; set; }

        [Column("evaluation_month")]
        public int? EvaluationMonth { get; set; }

        [Column("period_start")]
        public DateTime? PeriodStart { get; set; }

        [Column("period_end")]
        public DateTime? PeriodEnd { get; set; }

        // Header summary scores.

        [Column("quality_score", TypeName = "decimal(10,2)")]
        public decimal QualityScore { get; set; }

        [Column("quality_weighted_score", TypeName = "decimal(10,2)")]
        public decimal QualityWeightedScore { get; set; }

        [Column("on_time_delivery_score", TypeName = "decimal(10,2)")]
        public decimal OnTimeDeliveryScore { get; set; }

        [Column("delivery_weighted_score", TypeName = "decimal(10,2)")]
        public decimal DeliveryWeightedScore { get; set; }

        [Column("cost_competitiveness_score", TypeName = "decimal(10,2)")]
        public decimal CostCompetitivenessScore { get; set; }

        [Column("cost_weighted_score", TypeName = "decimal(10,2)")]
        public decimal CostWeightedScore { get; set; }

        [Column("reliability_score", TypeName = "decimal(10,2)")]
        public decimal ReliabilityScore { get; set; }

        [Column("reliability_weighted_score", TypeName = "decimal(10,2)")]
        public decimal ReliabilityWeightedScore { get; set; }

        [Column("total_score", TypeName = "decimal(10,2)")]
        public decimal TotalScore { get; set; }

        [MaxLength(40)]
        [Column("performance_rating")]
        public string? PerformanceRating { get; set; }

        [Required]
        [MaxLength(40)]
        [Column("status")]
        public string Status { get; set; } = "PENDING_PURCHASING";

        [MaxLength(1000)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [MaxLength(50)]
        [Column("generated_by")]
        public string? GeneratedBy { get; set; }

        [Column("generated_at")]
        public DateTime? GeneratedAt { get; set; }

        [MaxLength(50)]
        [Column("submitted_by")]
        public string? SubmittedBy { get; set; }

        [Column("submitted_at")]
        public DateTime? SubmittedAt { get; set; }

        [MaxLength(50)]
        [Column("reviewed_by")]
        public string? ReviewedBy { get; set; }

        [Column("reviewed_at")]
        public DateTime? ReviewedAt { get; set; }

        [MaxLength(50)]
        [Column("approved_by")]
        public string? ApprovedBy { get; set; }

        [Column("approved_at")]
        public DateTime? ApprovedAt { get; set; }

        [MaxLength(50)]
        [Column("finalized_by")]
        public string? FinalizedBy { get; set; }

        [Column("finalized_at")]
        public DateTime? FinalizedAt { get; set; }

        [MaxLength(50)]
        [Column("created_by")]
        public string? CreatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [MaxLength(50)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public ICollection<SupplierPerformanceEvaluationLine> Lines { get; set; }
            = new List<SupplierPerformanceEvaluationLine>();

        public ICollection<SupplierEvaluationWorkflowHistory> WorkflowHistory { get; set; }
            = new List<SupplierEvaluationWorkflowHistory>();
    }
}