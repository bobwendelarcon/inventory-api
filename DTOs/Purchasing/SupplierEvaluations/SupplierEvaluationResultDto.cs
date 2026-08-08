namespace inventory_api.DTOs.Purchasing.SupplierEvaluations;

public class SupplierEvaluationResultDto
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public int? EvaluationId { get; set; }

    public string? EvaluationNo { get; set; }

    public string? Status { get; set; }
}