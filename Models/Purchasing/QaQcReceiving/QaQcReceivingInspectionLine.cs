using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace inventory_api.Models.Purchasing.QaQcReceiving
{
    [Table("purchasing_qa_qc_receiving_inspection_line")]
    public class QaQcReceivingInspectionLine
    {
        [Key]
        [Column("qa_receiving_line_id")]
        public int QaReceivingLineId { get; set; }

        [Column("qa_receiving_id")]
        public int QaReceivingId { get; set; }

        [Column("incoming_receiving_line_id")]
        public int IncomingReceivingLineId { get; set; }

        [Column("incoming_receiving_line_lot_id")]
        public int? IncomingReceivingLineLotId { get; set; }

        [Column("material_id")]
        public int MaterialId { get; set; }


        // ============================================================
        // 1. APPEARANCE OF PACKAGING
        // ============================================================

        [Column("packaging_clean")]
        public bool PackagingClean { get; set; }

        [Column("readable_label")]
        public bool ReadableLabel { get; set; }

        [Column("properly_sealed")]
        public bool ProperlySealed { get; set; }

        [Column("no_deterioration")]
        public bool NoDeterioration { get; set; }


        // ============================================================
        // 2. APPEARANCE OF RAW MATERIAL
        // ============================================================

        [Column("no_foreign_matter")]
        public bool NoForeignMatter { get; set; }

        [Column("no_infestation")]
        public bool NoInfestation { get; set; }

        [Column("appearance_not_applicable")]
        public bool AppearanceNotApplicable { get; set; }

        [Column("powder_no_lump")]
        public bool? PowderNoLump { get; set; }

        [Column("powder_good_flowability")]
        public bool? PowderGoodFlowability { get; set; }

        [Column("liquid_no_solidification")]
        public bool? LiquidNoSolidification { get; set; }

        [Column("liquid_no_precipitation")]
        public bool? LiquidNoPrecipitation { get; set; }


        // ============================================================
        // 3. COLOR
        // ============================================================

        [Column("color_result")]
        [MaxLength(20)]
        public string? ColorResult { get; set; }

        [Column("actual_color")]
        [MaxLength(200)]
        public string? ActualColor { get; set; }


        // ============================================================
        // 4. ODOR
        // ============================================================

        [Column("odor_result")]
        [MaxLength(20)]
        public string? OdorResult { get; set; }

        [Column("actual_odor")]
        [MaxLength(200)]
        public string? ActualOdor { get; set; }


        // ============================================================
        // 5. ASSAY
        // ============================================================

        [Column("assay_result_status")]
        [MaxLength(20)]
        public string? AssayResultStatus { get; set; }

        [Column("coa_assay_result")]
        [MaxLength(200)]
        public string? CoaAssayResult { get; set; }

        [Column("bnpi_assay_specification")]
        [MaxLength(200)]
        public string? BnpiAssaySpecification { get; set; }


        // ============================================================
        // 6. SOLUBILITY
        // ============================================================

        [Column("solubility_result")]
        [MaxLength(20)]
        public string? SolubilityResult { get; set; }

        [Column("solubility_test")]
        [MaxLength(500)]
        public string? SolubilityTest { get; set; }


        // ============================================================
        // RESULT
        // ============================================================

        [Column("status")]
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING";

        [Column("remarks")]
        [MaxLength(1000)]
        public string? Remarks { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }


        // Navigation

        [ForeignKey(nameof(QaReceivingId))]
        public QaQcReceivingInspection? Header { get; set; }
    }
}