using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.SupplierEvaluation
{
    [Table("supplier_evaluation_cost_metrics")]
    public class SupplierEvaluationCostMetric
    {
        [Key]
        [Column("cost_metric_id")]
        public int CostMetricId { get; set; }

        [Required]
        [Column("evaluation_id")]
        public int EvaluationId { get; set; }

        [Column("total_po_count")]
        public int TotalPoCount { get; set; }

        [Column("total_po_line_count")]
        public int TotalPoLineCount { get; set; }

        [Column("total_purchase_amount", TypeName = "decimal(18,4)")]
        public decimal TotalPurchaseAmount { get; set; }

        [Column("supplier_average_unit_price", TypeName = "decimal(18,4)")]
        public decimal SupplierAverageUnitPrice { get; set; }

        /// <summary>
        /// Average price from canvassing, competing suppliers,
        /// or historical purchase prices.
        /// </summary>
        [Column("comparison_average_unit_price", TypeName = "decimal(18,4)")]
        public decimal ComparisonAverageUnitPrice { get; set; }

        [Column("price_variance_amount", TypeName = "decimal(18,4)")]
        public decimal PriceVarianceAmount { get; set; }

        [Column("price_variance_percent", TypeName = "decimal(8,4)")]
        public decimal PriceVariancePercent { get; set; }

        [Column("lowest_price_line_count")]
        public int LowestPriceLineCount { get; set; }

        [Column("compared_line_count")]
        public int ComparedLineCount { get; set; }

        [Column("cost_score", TypeName = "decimal(6,2)")]
        public decimal CostScore { get; set; }

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