using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_performance_evaluation_lines")]
    public class SupplierPerformanceEvaluationLine
    {
        [Key]
        [Column("evaluation_line_id")]
        public int EvaluationLineId { get; set; }

        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Column("qc_line_id")]
        public int QcLineId { get; set; }

        [Column("rr_line_id")]
        public int RrLineId { get; set; }

        [Column("po_line_id")]
        public int PoLineId { get; set; }

        [Column("schedule_line_id")]
        public int? ScheduleLineId { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }

        [Column("approved_qty", TypeName = "decimal(18,4)")]
        public decimal ApprovedQty { get; set; }

        [Column("rejected_qty", TypeName = "decimal(18,4)")]
        public decimal RejectedQty { get; set; }

        [Column("total_inspected_qty", TypeName = "decimal(18,4)")]
        public decimal TotalInspectedQty { get; set; }

        [Column("quality_score", TypeName = "decimal(6,2)")]
        public decimal QualityScore { get; set; }

        [Column("quality_grade", TypeName = "decimal(6,2)")]
        public decimal QualityGrade { get; set; }

        [Column("scheduled_date")]
        public DateTime? ScheduledDate { get; set; }

        [Column("actual_delivery_date")]
        public DateTime ActualDeliveryDate { get; set; }

        [Column("is_on_time")]
        public bool IsOnTime { get; set; }

        [Column("on_time_score", TypeName = "decimal(6,2)")]
        public decimal OnTimeScore { get; set; }

        [Column("scheduled_qty", TypeName = "decimal(18,4)")]
        public decimal ScheduledQty { get; set; }

        [Column("delivered_qty", TypeName = "decimal(18,4)")]
        public decimal DeliveredQty { get; set; }

        [Column("in_full_score", TypeName = "decimal(6,2)")]
        public decimal InFullScore { get; set; }

        [Column("delivery_score", TypeName = "decimal(6,2)")]
        public decimal DeliveryScore { get; set; }

        [Column("delivery_grade", TypeName = "decimal(6,2)")]
        public decimal DeliveryGrade { get; set; }

        [Column("new_unit_price", TypeName = "decimal(18,4)")]
        public decimal NewUnitPrice { get; set; }

        [Column("previous_unit_price", TypeName = "decimal(18,4)")]
        public decimal? PreviousUnitPrice { get; set; }

        [Column("price_change_percent", TypeName = "decimal(10,4)")]
        public decimal? PriceChangePercent { get; set; }

        [Required]
        [MaxLength(40)]
        [Column("cost_status")]
        public string CostStatus { get; set; } = "NO_PREVIOUS_PRICE";

        [Column("cost_score", TypeName = "decimal(6,2)")]
        public decimal CostScore { get; set; }

        [Column("cost_grade", TypeName = "decimal(6,2)")]
        public decimal CostGrade { get; set; }

        [Column("coa_points", TypeName = "decimal(6,2)")]
        public decimal CoaPoints { get; set; }

        [Column("terms_points", TypeName = "decimal(6,2)")]
        public decimal TermsPoints { get; set; }

        [Column("other_points", TypeName = "decimal(6,2)")]
        public decimal OtherPoints { get; set; }

        [Column("reliability_score", TypeName = "decimal(6,2)")]
        public decimal ReliabilityScore { get; set; }

        [Column("reliability_grade", TypeName = "decimal(6,2)")]
        public decimal ReliabilityGrade { get; set; }

        [Column("total_grade", TypeName = "decimal(6,2)")]
        public decimal TotalGrade { get; set; }

        [MaxLength(2000)]
        [Column("remarks")]
        public string? Remarks { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("created_by")]
        public string CreatedBy { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [MaxLength(50)]
        [Column("updated_by")]
        public string? UpdatedBy { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        public SupplierPerformanceEvaluation Evaluation { get; set; } = null!;
    }
}