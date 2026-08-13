namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationLineDto
{
    public int EvaluationLineId { get; set; }

    public int EvaluationId { get; set; }

    public int QcLineId { get; set; }

    public int RrLineId { get; set; }

    public int PoLineId { get; set; }

    public int? ScheduleLineId { get; set; }


    // Material

    public int MaterialId { get; set; }

    public string MaterialCode { get; set; } = string.Empty;

    public string MaterialName { get; set; } = string.Empty;


    // QUALITY - 40%

    public decimal ApprovedQty { get; set; }

    public decimal RejectedQty { get; set; }

    public decimal TotalInspectedQty { get; set; }

    /// <summary>
    /// Accepted / Inspected × 100
    /// </summary>
    public decimal QualityScore { get; set; }

    /// <summary>
    /// QualityScore × 40%
    /// </summary>
    public decimal QualityGrade { get; set; }


    // DELIVERY - 30%

    public DateTime? ScheduledDate { get; set; }

    public DateTime ActualDeliveryDate { get; set; }

    public bool IsOnTime { get; set; }

    public decimal OnTimeScore { get; set; }

    public decimal ScheduledQty { get; set; }

    public decimal DeliveredQty { get; set; }

    public decimal InFullScore { get; set; }

    /// <summary>
    /// 50% On Time + 50% In Full
    /// </summary>
    public decimal DeliveryScore { get; set; }

    /// <summary>
    /// DeliveryScore × 30%
    /// </summary>
    public decimal DeliveryGrade { get; set; }


    // COST COMPETITIVENESS - 20%

    public decimal NewUnitPrice { get; set; }

    public decimal? PreviousUnitPrice { get; set; }

    public decimal? PriceChangePercent { get; set; }

    public string CostStatus { get; set; } = string.Empty;

    public decimal CostScore { get; set; }

    public decimal CostGrade { get; set; }


    // RELIABILITY / AFTER SALES - 10%

    public decimal CoaPoints { get; set; }

    public decimal TermsPoints { get; set; }

    public decimal OtherPoints { get; set; }

    public decimal ReliabilityScore { get; set; }

    public decimal ReliabilityGrade { get; set; }


    // FINAL

    public decimal TotalGrade { get; set; }

    public string? Remarks { get; set; }
}