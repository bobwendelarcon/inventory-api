namespace inventory_api.DTOs.Purchasing.QaQcReceiving
{
    public class SaveQaQcReceivingInspectionLineDto
    {
        public int IncomingReceivingLineId { get; set; }

        public int? IncomingReceivingLineLotId { get; set; }


        // ============================================================
        // 1. APPEARANCE OF PACKAGING
        // ============================================================

        public bool PackagingClean { get; set; }

        public bool ReadableLabel { get; set; }

        public bool ProperlySealed { get; set; }

        public bool NoDeterioration { get; set; }


        // ============================================================
        // 2. APPEARANCE OF RAW MATERIAL
        // ============================================================

        public bool NoForeignMatter { get; set; }

        public bool NoInfestation { get; set; }

        public bool AppearanceNotApplicable { get; set; }

        public bool? PowderNoLump { get; set; }

        public bool? PowderGoodFlowability { get; set; }

        public bool? LiquidNoSolidification { get; set; }

        public bool? LiquidNoPrecipitation { get; set; }


        // ============================================================
        // 3. COLOR
        // COMPLIANT / NON_COMPLIANT / N/A
        // ============================================================

        public string? ColorResult { get; set; }

        public string? ActualColor { get; set; }


        // ============================================================
        // 4. ODOR
        // ============================================================

        public string? OdorResult { get; set; }

        public string? ActualOdor { get; set; }


        // ============================================================
        // 5. ASSAY
        // ============================================================

        public string? AssayResultStatus { get; set; }

        public string? CoaAssayResult { get; set; }

        public string? BnpiAssaySpecification { get; set; }


        // ============================================================
        // 6. SOLUBILITY
        // ============================================================

        public string? SolubilityResult { get; set; }

        public string? SolubilityTest { get; set; }


        // ============================================================
        // OTHER
        // ============================================================

        public string? Remarks { get; set; }
    }
}