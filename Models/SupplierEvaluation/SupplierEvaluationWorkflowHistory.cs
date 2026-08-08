using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_evaluation_workflow_history")]
    public class SupplierEvaluationWorkflowHistory
    {
        [Key]
        [Column("history_id")]
        public long HistoryId { get; set; }

        [Required]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [MaxLength(30)]
        [Column("from_status")]
        public string? FromStatus { get; set; }

        [Required]
        [MaxLength(30)]
        [Column("to_status")]
        public string ToStatus { get; set; } = string.Empty;

        /// <summary>
        /// Examples:
        /// GENERATED, UPDATED, SUBMITTED_FOR_REVIEW,
        /// REVIEWED, APPROVED, FINALIZED.
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [MaxLength(2000)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("action_by")]
        public string ActionBy { get; set; } = string.Empty;

        [Column("action_at")]
        public DateTime ActionAt { get; set; }

        public SupplierPerformanceEvaluation Evaluation { get; set; } = null!;
    }
}