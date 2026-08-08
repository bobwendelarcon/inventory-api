using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_evaluation_reliability_scores")]
    public class SupplierEvaluationReliabilityScore
    {
        [Key]
        [Column("reliability_score_id")]
        public int ReliabilityScoreId { get; set; }

        [Required]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Column("responsiveness_score", TypeName = "decimal(6,2)")]
        public decimal ResponsivenessScore { get; set; }

        [Column("issue_resolution_score", TypeName = "decimal(6,2)")]
        public decimal IssueResolutionScore { get; set; }

        [Column("replacement_support_score", TypeName = "decimal(6,2)")]
        public decimal ReplacementSupportScore { get; set; }

        [Column("communication_score", TypeName = "decimal(6,2)")]
        public decimal CommunicationScore { get; set; }

        [Column("reliability_score", TypeName = "decimal(6,2)")]
        public decimal ReliabilityScore { get; set; }

        [MaxLength(2000)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [MaxLength(50)]
        [Column("scored_by")]
        public string? ScoredBy { get; set; }

        [Column("scored_at")]
        public DateTime? ScoredAt { get; set; }

        [MaxLength(50)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public SupplierPerformanceEvaluation Evaluation { get; set; } = null!;
    }
}