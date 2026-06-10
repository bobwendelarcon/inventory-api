using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.Canvassing
{
    [Table("purchasing_canvass_quote")]
    public class PurchasingCanvassQuote
    {
        [Key]
        [Column("quote_id")]
        public int QuoteId { get; set; }

        [Column("canvass_line_id")]
        public int CanvassLineId { get; set; }

        [Column("supplier_id")]
        public int SupplierId { get; set; }

        [Column("manufacturer_id")]
        public int? ManufacturerId { get; set; }

        [Column("unit_price")]
        public decimal UnitPrice { get; set; }

        [Column("payment_terms")]
        public string? PaymentTerms { get; set; }

        [Column("delivery_days")]
        public int? DeliveryDays { get; set; }

        [Column("coa_available")]
        public bool CoaAvailable { get; set; }

        [Column("documents_remarks")]
        public string? DocumentsRemarks { get; set; }

        [Column("quotation_ref")]
        public string? QuotationRef { get; set; }

        [Column("quote_date")]
        public DateTime? QuoteDate { get; set; }

        [Column("remarks")]
        public string? Remarks { get; set; }

        [Column("is_recommended")]
        public bool IsRecommended { get; set; }

        [Column("recommendation_reason")]
        public string? RecommendationReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(CanvassLineId))]
        public PurchasingCanvassLine? Line { get; set; }
    }
}